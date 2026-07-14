using UnityEngine;
using GanhHangRong.Core;
using GanhHangRong.Player;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Singleton quản lý ông Ba bán đá.
    /// Tự spawn ông Ba vào scene, expose CallIceVendor() cho UI gọi.
    /// </summary>
    public class IceVendorManager : MonoBehaviour
    {
        public static IceVendorManager Instance { get; private set; }

        [Header("Model ông Ba")]
        [Tooltip("FBX model ông Ba (Assets/ongbantrada/...)")]
        [SerializeField] private GameObject ongBaPrefab;

        [Header("Cài đặt")]
        [SerializeField] private int deliveryCost = 5000;

        private NPC.IceVendorNPC vendorNPC;
        private bool isDelivering = false;

        public bool IsDelivering => isDelivering;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<IceVendorManager>() != null) return;

            var go = new GameObject("[IceVendorManager]");
            go.AddComponent<IceVendorManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SpawnVendor();
        }

        private void SpawnVendor()
        {
            // Tải FBX và tạo material từ texture
            string fbxPath = "Assets/ongbantrada/Meshy_AI_Delivery_Rider_with_I_0712231937_texture.fbx";
            string texPath = "Assets/ongbantrada/Meshy_AI_Delivery_Rider_with_I_0712231937_texture.png";
            string normalPath = "Assets/ongbantrada/Meshy_AI_Delivery_Rider_with_I_0712231937_texture_normal.png";

#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogWarning("[IceVendorManager] Không tìm thấy FBX ông Ba tại: " + fbxPath);
                return;
            }

            // Tạo material với texture đầy đủ
            Texture2D albedo   = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Texture2D normalTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            // Kiểm tra URP hay Standard
            Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                            ? Shader.Find("Universal Render Pipeline/Lit")
                            : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            if (shader.name.Contains("Universal"))
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTexture("_BumpMap", normalTex);
                if (normalTex != null) mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                mat.mainTexture = albedo;
                mat.SetTexture("_BumpMap", normalTex);
                if (normalTex != null) mat.EnableKeyword("_NORMALMAP");
            }

            // Create parent GameObject
            GameObject vendorParent = new GameObject("OngBaBanDa");
            vendorParent.transform.position = new Vector3(60f, 0f, -3f);

            // Spawn FBX as child
            GameObject vendorVisual = Instantiate(prefab, vendorParent.transform);
            vendorVisual.name = "Visual";
            
            // Fix orientation (AI generated meshes often lie on their side)
            vendorVisual.transform.localRotation = Quaternion.Euler(-90f, -90f, 0f);

            // Tính scale từ bounds (giống logic nhà)
            Bounds b = new Bounds(vendorVisual.transform.position, Vector3.zero);
            foreach (var r in vendorVisual.GetComponentsInChildren<Renderer>())
            {
                r.material = mat;
                b.Encapsulate(r.bounds);
            }

            // Scale để nhân vật cao ~1.8m
            float targetHeight = 1.8f;
            if (b.size.y > 0.001f)
            {
                float scaleFactor = targetHeight / b.size.y;
                vendorVisual.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            }

            // Adjust local Y so bottom is at parent's origin (0)
            b = new Bounds(vendorVisual.transform.position, Vector3.zero);
            foreach (var r in vendorVisual.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
            float yOffset = vendorParent.transform.position.y - b.min.y;
            vendorVisual.transform.localPosition = new Vector3(0, yOffset, 0);

            // Gắn script điều khiển vào parent
            vendorNPC = vendorParent.AddComponent<NPC.IceVendorNPC>();
#else
            // Runtime: ongBaPrefab phải được gán trong Inspector hoặc dùng Resources
            if (ongBaPrefab != null)
            {
                GameObject vendorGO = Instantiate(ongBaPrefab, new Vector3(60f, 0f, 5f), Quaternion.identity);
                vendorGO.name = "OngBaBanDa";
                vendorNPC = vendorGO.GetComponent<NPC.IceVendorNPC>();
                if (vendorNPC == null) vendorNPC = vendorGO.AddComponent<NPC.IceVendorNPC>();
            }
#endif
        }

        /// <summary>
        /// Gọi từ UI khi nhấn "Gọi Ông Ba". 
        /// Trừ tiền và ra lệnh ông Ba chạy đến xe đẩy.
        /// </summary>
        public bool CallIceVendor()
        {
            if (isDelivering)
            {
                Debug.Log("[IceVendorManager] Ông Ba đang trên đường rồi!");
                return false;
            }

            // Trừ tiền
            var stats = FindAnyObjectByType<PlayerStats>();
            if (stats == null || !stats.SpendMoney(deliveryCost))
            {
                Debug.Log("[IceVendorManager] Không đủ tiền để gọi ông Ba!");
                return false;
            }

            EventManager.TriggerDeliveryFeePaid(deliveryCost);

            // Tìm xe đẩy
            var cart = FindAnyObjectByType<Interaction.TeaCart>();
            if (cart == null)
            {
                Debug.LogWarning("[IceVendorManager] Không tìm thấy xe đẩy!");
                stats.AddMoney(deliveryCost); // Hoàn tiền
                return false;
            }

            isDelivering = true;

            if (vendorNPC != null)
            {
                vendorNPC.StartDelivery(cart.transform);
                StartCoroutine(WatchDelivery());
            }
            else
            {
                // Không có NPC visual: giao đá ngay lập tức
                if (stats != null) stats.RefillIce();
                isDelivering = false;
            }

            return true;
        }

        private System.Collections.IEnumerator WatchDelivery()
        {
            // Đợi ông Ba hoàn thành (chuyển sang Leaving)
            yield return new WaitForSeconds(1f);
            while (vendorNPC != null &&
                   vendorNPC.CurrentState == NPC.IceVendorNPC.VendorState.DeliveringIce ||
                   vendorNPC != null &&
                   vendorNPC.CurrentState == NPC.IceVendorNPC.VendorState.Dumping)
            {
                yield return new WaitForSeconds(0.5f);
            }
            isDelivering = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
