using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Vật phẩm trên mặt bàn xe đẩy — có thể tương tác khi đang ở chế độ góc nhìn xe đẩy.
    /// Gắn script này lên các FBX models: ấm đun nước, bình trà, gánh nước mía.
    /// </summary>
    public class CartItem : MonoBehaviour
    {
        public enum CartItemType
        {
            WaterKettle,    // Ấm đun nước (amdunnuoc.fbx)
            TeaTin,         // Bình trà (Meshy_AI_Red_Vintage_Tea_Tin)
            SugarcaneJuice, // Gánh nước mía (Meshy_AI_Vietnam_Sugarcane_Jui)
            SugarJar,       // Hũ đường (Meshy_AI_Cracked_Vintage_Jar_w)
            GasStove,       // Bếp ga (Meshy_AI_Namilux_High_Power_Po)
            Coffee,         // Cà phê (Meshy_AI_Cà_Phê_Phổ_Cổ_V)
            IceCooler,      // Bình đựng đá (Meshy_AI_Open_Red_Cooler_with)
            WaterBottle,    // Bình nước (Meshy_AI_Sài_Gòn_Aquwa_Bottl)
            WaterCup        // Ly nước (Meshy_AI_Steaming_Glass_Beer_M)
        }

        [Header("Cấu Hình Vật Phẩm")]
        [SerializeField] private CartItemType itemType;
        [SerializeField] private string itemName = "Vật phẩm";
        [SerializeField] private string itemDescription = "Mô tả vật phẩm";

        [Header("Hiệu Ứng")]
        [SerializeField] private float hoverScaleMultiplier = 1.15f;
        [SerializeField] private float hoverBobSpeed = 2f;
        [SerializeField] private float hoverBobAmount = 0.05f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);

        [Header("Ly Trà Đá — Mô Hình Cầm Tay")]
        [Tooltip("Prefab mô hình ly trà đá (WaterCup FBX). Nếu để trống sẽ dùng primitive Cylinder thay thế.")]
        [SerializeField] private GameObject teaCupHeldPrefab;

        // State
        private bool isHighlighted = false;
        private bool isInteracting = false;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private Renderer[] renderers;
        private Color[] originalColors;
        private Material[] originalMaterials;
        private float hoverTimer = 0f;
        
        private static bool isBoilingWater = false;
        public static bool IsBoilingWater => isBoilingWater;
        private static bool isWaterBoiled = false;
        public static bool IsWaterBoiled => isWaterBoiled;

        private static float bottleWater = 30f;
        public static float BottleWater => bottleWater;

        private static float kettleWater = 1.2f;
        public static float KettleWater => kettleWater;

        private const float maxKettleWater = 1.2f;
        private const float minKettleWaterToRefill = 0.2f;

        private static bool isHoldingCup = false;
        public static bool IsHoldingCup => isHoldingCup;

        private static int teaInCup = 0;
        public static int TeaInCup => teaInCup;

        private static float waterInCup = 0f;
        public static float WaterInCup => waterInCup;

        private static float iceInCup = 0f;
        public static float IceInCup => iceInCup;

        private static bool hasPreparedTea = false;
        public static bool HasPreparedTea
        {
            get => hasPreparedTea;
            set => hasPreparedTea = value;
        }

        private static Coroutine activeCoolDownCoroutine = null;
        private static CartItem activeInstance = null;

        // Mô hình ly trà đá đang cầm trên tay nhân vật
        private static GameObject heldTeaCupObj = null;

        public static void ConsumeWater(float amount)
        {
            kettleWater = Mathf.Max(0f, kettleWater - amount);
            if (kettleWater <= 0.01f)
            {
                isWaterBoiled = false;
                if (activeInstance != null && activeCoolDownCoroutine != null)
                {
                    activeInstance.StopCoroutine(activeCoolDownCoroutine);
                    activeCoolDownCoroutine = null;
                }
            }
        }

        public CartItemType ItemType => itemType;
        public string ItemName => itemName;
        public string ItemDescription => itemDescription;
        public bool IsHighlighted => isHighlighted;

        private void Awake()
        {
            if (itemType == CartItemType.WaterKettle)
            {
                activeInstance = this;
            }
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
            renderers = GetComponentsInChildren<Renderer>();

            // Lưu màu gốc
            originalColors = new Color[renderers.Length];
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material != null)
                {
                    originalMaterials[i] = renderers[i].material;
                    originalColors[i] = renderers[i].material.color;
                }
            }

            // Đảm bảo có collider để raycast detect
            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                // Tự động fit collider vào mesh bounds
                var meshFilter = GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    col.center = meshFilter.sharedMesh.bounds.center;
                    col.size = meshFilter.sharedMesh.bounds.size;
                }
            }
        }

        private void Update()
        {
            // Đã tắt hiệu ứng nhấp nhô (bobbing) và phóng to (scaling) khi hover theo yêu cầu để vật phẩm đứng yên cố định
        }

        /// <summary>
        /// Bật highlight khi chuột trỏ vào.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted) return;
            isHighlighted = highlighted;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    if (highlighted)
                    {
                        // Thêm emission để vật phẩm sáng lên
                        renderers[i].material.EnableKeyword("_EMISSION");
                        renderers[i].material.SetColor("_EmissionColor", highlightColor * 0.3f);
                    }
                    else
                    {
                        renderers[i].material.DisableKeyword("_EMISSION");
                        renderers[i].material.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }

        /// <summary>
        /// Xử lý khi người chơi click vào vật phẩm.
        /// </summary>
        public void OnItemClicked(Player.PlayerController player)
        {
            if (isInteracting) return;

            switch (itemType)
            {
                case CartItemType.WaterKettle:
                    OnWaterKettleInteract(player);
                    break;
                case CartItemType.TeaTin:
                    OnTeaTinInteract(player);
                    break;
                case CartItemType.SugarcaneJuice:
                    OnSugarcaneJuiceInteract(player);
                    break;
                case CartItemType.SugarJar:
                    OnSugarJarInteract(player);
                    break;
                case CartItemType.GasStove:
                    OnGasStoveInteract(player);
                    break;
                case CartItemType.Coffee:
                    OnCoffeeInteract(player);
                    break;
                case CartItemType.IceCooler:
                    OnIceCoolerInteract(player);
                    break;
                case CartItemType.WaterBottle:
                    OnWaterBottleInteract(player);
                    break;
                case CartItemType.WaterCup:
                    OnWaterCupInteract(player);
                    break;
            }

            // Hiệu ứng click — rung nhẹ
            StartCoroutine(ClickFeedback());
        }

        private void OnWaterKettleInteract(Player.PlayerController player)
        {
            if (isHoldingCup)
            {
                if (waterInCup >= 0.2f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly nước đã có đủ nước sôi rồi!");
                    return;
                }

                // Nếu nước chưa sôi hoặc đã nguội → tự động đun ngay
                if (!isWaterBoiled)
                {
                    if (isBoilingWater)
                    {
                        EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đang được đun, hãy đợi một chút rồi nhấp lại vào ấm để rót nước sôi!");
                        return;
                    }

                    // Tìm các đối tượng cần thiết trong cảnh để đun
                    GameObject kettleObj = GameObject.Find("KettleProp");
                    GameObject stoveObj = GameObject.Find("GasStoveProp");
                    GameObject waterObj = GameObject.Find("WaterBottleProp");

                    if (kettleObj == null || stoveObj == null || waterObj == null)
                    {
                        EventManager.TriggerDialogueLine("Hoàng Hôn", "Thiếu dụng cụ đun nước! Cần có Ấm Nước, Bếp Ga và Bình Nước.");
                        return;
                    }

                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đã nguội! Đun lại ngay để pha trà...");
                    StartCoroutine(BoilWaterRoutine(kettleObj, stoveObj, waterObj));
                    return;
                }

                if (kettleWater < 0.2f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ấm hết nước rồi! Đang lấy thêm nước từ bình và đun sôi...");
                    GameObject kettleObj2 = GameObject.Find("KettleProp");
                    GameObject stoveObj2 = GameObject.Find("GasStoveProp");
                    GameObject waterObj2 = GameObject.Find("WaterBottleProp");
                    if (kettleObj2 != null && stoveObj2 != null && waterObj2 != null)
                        StartCoroutine(BoilWaterRoutine(kettleObj2, stoveObj2, waterObj2));
                    return;
                }

                // Rót 0.2L nước sôi vào ly
                ConsumeWater(0.2f);
                waterInCup += 0.2f;
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã rót 200ml nước sôi vào ly. (Nước trong ly: {Mathf.RoundToInt(waterInCup * 1000f)}ml / 200ml)");

                CheckBrewingCompletion(player);
                return;
            }

            if (isWaterBoiled)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Ấm nước nóng đã sôi 100 độ C, sẵn sàng pha trà ngon rồi! (Nước trong ấm: {kettleWater:F1}L)");
                return;
            }

            if (isBoilingWater)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đang được đun trên bếp ga, hãy đợi một chút!");
                return;
            }

            // Tìm các đối tượng cần thiết trong cảnh (không cầm ly)
            GameObject kettleProp = GameObject.Find("KettleProp");
            GameObject stoveProp = GameObject.Find("GasStoveProp");
            GameObject waterProp = GameObject.Find("WaterBottleProp");

            if (kettleProp == null || stoveProp == null || waterProp == null)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Thiếu dụng cụ đun nước! Cần có Ấm Nước, Bếp Ga và Bình Nước.");
                return;
            }

            StartCoroutine(BoilWaterRoutine(kettleProp, stoveProp, waterProp));
            Debug.Log("[CartItem] Tương tác ấm đun nước (bình thủy) -> Bắt đầu đun nước");
        }

        private void OnTeaTinInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (!isHoldingCup)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hãy lấy 1 chiếc ly sạch trước khi lấy trà!");
                return;
            }

            if (stats.TeaSupply < 50)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Không đủ trà khô trong hộp trà (cần ít nhất 50g)!");
                return;
            }

            // Lấy 50g trà bỏ vào ly
            stats.ConsumeTea(50);
            teaInCup += 50;
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã cho 50g trà vào ly. (Trà trong ly: {teaInCup}g / 50g)");

            CheckBrewingCompletion(player);
        }

        private void OnSugarcaneJuiceInteract(Player.PlayerController player)
        {
            // Gánh nước mía — lấy đường
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats != null)
            {
                stats.AddSupplies(0, 200, 0); // Thêm 200g đường
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Lấy đường từ gánh nước mía. (+200g đường, hiện có {stats.SugarSupply}g)");
            }
            Debug.Log("[CartItem] Tương tác gánh nước mía");
        }

        private void OnSugarJarInteract(Player.PlayerController player)
        {
            // Hũ đường — lấy đường
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats != null)
            {
                stats.AddSupplies(0, 200, 0); // Thêm 200g đường
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Lấy đường từ hũ đường. (+200g đường, hiện có {stats.SugarSupply}g)");
            }
            Debug.Log("[CartItem] Tương tác hũ đường");
        }

        private void OnGasStoveInteract(Player.PlayerController player)
        {
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Bếp ga Namilux đang hoạt động tốt để đun nước nóng.");
            Debug.Log("[CartItem] Tương tác bếp ga");
        }

        private void OnCoffeeInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats != null)
            {
                stats.AddCoffee(150); // Thêm 150g cà phê
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Lấy thêm bột cà phê. (+150g cà phê, hiện có {stats.CoffeeSupply}g)");
            }
            Debug.Log("[CartItem] Tương tác hũ cà phê");
        }

        private void OnIceCoolerInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (isHoldingCup)
            {
                if (stats.IceLevel < 5f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Thùng đựng đá đã hết đá sạch! Hãy tiếp thêm đá.");
                    return;
                }

                if (iceInCup >= 5f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly nước đã có đủ đá lạnh rồi!");
                    return;
                }

                // Lấy 5% đá cho vào ly
                stats.ModifyIceLevel(-5f);
                iceInCup += 5f;
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã thêm 5% đá vào ly. (Đá trong ly: {iceInCup}% / 5%)");

                CheckBrewingCompletion(player);
                return;
            }

            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Thùng đựng đá sạch. (Hiện còn: {Mathf.RoundToInt(stats.IceLevel)}% đá). Hãy lấy 1 chiếc ly sạch trước khi lấy đá!");
            Debug.Log("[CartItem] Tương tác bình đựng đá");
        }

        private void OnWaterBottleInteract(Player.PlayerController player)
        {
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đây là bình nước lọc Sài Gòn Aquwa. (Hiện còn: {bottleWater:F1}L). Hãy tương tác với Ấm Nước để lấy nước và đun sôi!");
            Debug.Log("[CartItem] Tương tác bình nước");
        }

        private void CheckBrewingCompletion(Player.PlayerController player)
        {
            if (isHoldingCup && teaInCup >= 50 && waterInCup >= 0.2f && iceInCup >= 5f)
            {
                isHoldingCup = false;
                hasPreparedTea = true;
                teaInCup = 0;
                waterInCup = 0f;
                iceInCup = 0f;

                EventManager.TriggerDialogueLine("Hoàng Hôn", "★ Hoàn thành 1 ly nước trà đá mát lạnh! Hoàng Hôn cầm ly trà trên tay. Nhấn Space để phục vụ hoặc đi đến bàn khách để đặt ly xuống.");

                // Gắn mô hình ly trà đá lên tay phải nhân vật
                AttachTeaCupToPlayer(player);
            }
        }

        private static void AttachEmptyCupToPlayer(Player.PlayerController player)
        {
            // Dọn mô hình cũ nếu còn tồn tại
            if (heldTeaCupObj != null)
            {
                Destroy(heldTeaCupObj);
                heldTeaCupObj = null;
            }

            Transform attachPoint = FindRightHandBone(player.transform);
            GameObject cupGO = CreateFallbackEmptyCupModel();

            if (attachPoint != null)
            {
                cupGO.transform.SetParent(attachPoint, false);
                
                // Khắc phục tỷ lệ scale của xương biped (tránh bị quá nhỏ/vô hình)
                Vector3 parentScale = attachPoint.lossyScale;
                float scaleX = 0.12f / (parentScale.x != 0 ? Mathf.Abs(parentScale.x) : 1f);
                float scaleY = 0.12f / (parentScale.y != 0 ? Mathf.Abs(parentScale.y) : 1f);
                float scaleZ = 0.12f / (parentScale.z != 0 ? Mathf.Abs(parentScale.z) : 1f);
                cupGO.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                // Khắc phục vị trí theo tỉ lệ xương
                float posX = 0f;
                float posY = 0.08f / (parentScale.y != 0 ? Mathf.Abs(parentScale.y) : 1f);
                float posZ = 0.03f / (parentScale.z != 0 ? Mathf.Abs(parentScale.z) : 1f);
                cupGO.transform.localPosition = new Vector3(posX, posY, posZ);
                cupGO.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                
                Debug.Log($"[CartItem] Gắn ly rỗng vào xương {attachPoint.name}. Bone scale: {parentScale}, Local scale set: {cupGO.transform.localScale}");
            }
            else
            {
                cupGO.transform.SetParent(player.transform, false);
                cupGO.transform.localPosition = new Vector3(0.35f, 0.85f, 0.15f);
                cupGO.transform.localRotation = Quaternion.identity;
                cupGO.transform.localScale = Vector3.one * 0.12f;
                Debug.LogWarning("[CartItem] Không tìm thấy xương tay phải, gắn ly rỗng vào thân nhân vật.");
            }

            cupGO.name = "HeldEmptyCup";
            heldTeaCupObj = cupGO;
        }

        private void AttachTeaCupToPlayer(Player.PlayerController player)
        {
            // Dọn mô hình cũ nếu còn tồn tại
            if (heldTeaCupObj != null)
            {
                Destroy(heldTeaCupObj);
                heldTeaCupObj = null;
            }

            Transform attachPoint = FindRightHandBone(player.transform);

            if (teaCupHeldPrefab == null)
            {
                teaCupHeldPrefab = Resources.Load<GameObject>("lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture");
                if (teaCupHeldPrefab != null)
                {
                    Debug.Log("[CartItem] Đã load thành công ly trà đá fbx từ Resources.");
                }
            }
#if UNITY_EDITOR
            if (teaCupHeldPrefab == null)
            {
                teaCupHeldPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture.fbx");
            }
#endif

            GameObject cupGO;
            if (teaCupHeldPrefab != null)
            {
                cupGO = Instantiate(teaCupHeldPrefab);
            }
            else
            {
                cupGO = CreateFallbackTeaCupModel();
            }

            if (attachPoint != null)
            {
                cupGO.transform.SetParent(attachPoint, false);
                
                // Khắc phục tỷ lệ scale của xương biped (tránh bị quá nhỏ/vô hình)
                Vector3 parentScale = attachPoint.lossyScale;
                float scaleX = 0.12f / (parentScale.x != 0 ? Mathf.Abs(parentScale.x) : 1f);
                float scaleY = 0.12f / (parentScale.y != 0 ? Mathf.Abs(parentScale.y) : 1f);
                float scaleZ = 0.12f / (parentScale.z != 0 ? Mathf.Abs(parentScale.z) : 1f);
                cupGO.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                // Khắc phục vị trí theo tỉ lệ xương
                float posX = 0f;
                float posY = 0.08f / (parentScale.y != 0 ? Mathf.Abs(parentScale.y) : 1f);
                float posZ = 0.03f / (parentScale.z != 0 ? Mathf.Abs(parentScale.z) : 1f);
                cupGO.transform.localPosition = new Vector3(posX, posY, posZ);
                cupGO.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

                Debug.Log($"[CartItem] Gắn ly trà đá vào xương {attachPoint.name}. Bone scale: {parentScale}, Local scale set: {cupGO.transform.localScale}");
            }
            else
            {
                cupGO.transform.SetParent(player.transform, false);
                cupGO.transform.localPosition = new Vector3(0.35f, 0.85f, 0.15f);
                cupGO.transform.localRotation = Quaternion.identity;
                cupGO.transform.localScale = Vector3.one * 0.12f;
                Debug.LogWarning("[CartItem] Không tìm thấy xương tay phải, gắn ly trà đá vào thân nhân vật.");
            }

            cupGO.name = "HeldTeaCup";
            heldTeaCupObj = cupGO;
        }

        /// <summary>Xóa mô hình ly trà khỏi tay khi phục vụ xong.</summary>
        public static void DetachTeaCup()
        {
            if (heldTeaCupObj != null)
            {
                Destroy(heldTeaCupObj);
                heldTeaCupObj = null;
                Debug.Log("[CartItem] Đã tháo mô hình ly trà khỏi tay sau khi phục vụ.");
            }
        }

        public static GameObject CreateStaticTeaCupModel(Vector3 worldPosition)
        {
            if (activeInstance != null && activeInstance.teaCupHeldPrefab == null)
            {
                activeInstance.teaCupHeldPrefab = Resources.Load<GameObject>("lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture");
#if UNITY_EDITOR
                if (activeInstance.teaCupHeldPrefab == null)
                {
                    activeInstance.teaCupHeldPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture.fbx");
                }
#endif
            }

            GameObject cupGO;
            if (activeInstance != null && activeInstance.teaCupHeldPrefab != null)
            {
                cupGO = Instantiate(activeInstance.teaCupHeldPrefab);
            }
            else
            {
                cupGO = CreateFallbackTeaCupModel();
            }

            cupGO.transform.position = worldPosition;
            cupGO.transform.rotation = Quaternion.identity;
            cupGO.transform.localScale = Vector3.one * 0.12f;
            cupGO.name = "PlacedTeaCup";

            return cupGO;
        }

        private static Transform FindRightHandBone(Transform root)
        {
            // Tìm bone tay phải theo các tên phổ biến của Unity Humanoid / Mixamo / Meshy
            string[] candidateNames = {
                "RightHand", "Hand_R", "R_Hand", "hand_r", "mixamorig:RightHand",
                "Bip001 R Hand", "RHand", "HandRight"
            };

            foreach (string name in candidateNames)
            {
                Transform found = root.Find(name);
                if (found != null) return found;

                // Tìm đệ quy trong toàn bộ cây
                found = FindChildRecursive(root, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static GameObject CreateFallbackEmptyCupModel()
        {
            GameObject cup = new GameObject("FallbackEmptyCup");

            // Thân ly (Cylinder trong suốt trắng)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(cup.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1f, 0.6f, 1f);
            Object.Destroy(body.GetComponent<Collider>());

            var rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    mat.color = new Color(0.9f, 0.95f, 1f, 0.25f); // Thủy tinh trắng trong suốt
                    mat.SetFloat("_Mode", 3f); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                    rend.material = mat;
                }
            }

            return cup;
        }

        private static GameObject CreateFallbackTeaCupModel()
        {
            GameObject cup = new GameObject("FallbackTeaCup");

            // 1. Thân ly (Cylinder trong suốt trắng)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(cup.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1f, 0.6f, 1f);
            Object.Destroy(body.GetComponent<Collider>());

            var rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    mat.color = new Color(0.9f, 0.95f, 1f, 0.25f); // Thủy tinh trắng trong suốt
                    mat.SetFloat("_Mode", 3f); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                    rend.material = mat;
                }
            }

            // 2. Nước trà bên trong (Cylinder màu hổ phách/cam trà)
            GameObject teaLiquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teaLiquid.transform.SetParent(cup.transform, false);
            teaLiquid.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            teaLiquid.transform.localScale = new Vector3(0.92f, 0.5f, 0.92f);
            Object.Destroy(teaLiquid.GetComponent<Collider>());

            var liquidRend = teaLiquid.GetComponent<Renderer>();
            if (liquidRend != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    mat.color = new Color(0.75f, 0.42f, 0.12f, 0.85f); // Màu trà đá hổ phách
                    mat.SetFloat("_Mode", 3f); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3001; // Render sau ly thủy tinh
                    liquidRend.material = mat;
                }
            }

            // 3. Đá viên (Tạo 3 Cube nhỏ màu trắng đục bay ở phần trên ly nước)
            Vector3[] icePositions = {
                new Vector3(0.18f, 0.15f, 0.12f),
                new Vector3(-0.15f, 0.18f, -0.1f),
                new Vector3(-0.05f, 0.2f, 0.2f)
            };
            Vector3[] iceRotations = {
                new Vector3(25f, 40f, 15f),
                new Vector3(-35f, 12f, 45f),
                new Vector3(10f, -60f, -20f)
            };

            for (int i = 0; i < 3; i++)
            {
                GameObject iceCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                iceCube.transform.SetParent(cup.transform, false);
                iceCube.transform.localPosition = icePositions[i];
                iceCube.transform.localRotation = Quaternion.Euler(iceRotations[i]);
                iceCube.transform.localScale = Vector3.one * 0.3f;
                Object.Destroy(iceCube.GetComponent<Collider>());

                var iceRend = iceCube.GetComponent<Renderer>();
                if (iceRend != null)
                {
                    Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
                    if (mat != null)
                    {
                        mat.color = new Color(0.92f, 0.96f, 1f, 0.65f); // Đá viên mờ đục trong suốt
                        mat.SetFloat("_Mode", 3f);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.renderQueue = 3002; // Render sau cùng
                        iceRend.material = mat;
                    }
                }
            }

            return cup;
        }

        private System.Collections.IEnumerator CoolDownRoutine()
        {
            yield return new WaitForSeconds(5f);
            isWaterBoiled = false;
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước trong ấm đã nguội rồi, cần đun sôi lại để pha trà.");
            activeCoolDownCoroutine = null;
        }

        private System.Collections.IEnumerator BoilWaterRoutine(GameObject kettle, GameObject stove, GameObject water)
        {
            isBoilingWater = true;
            isWaterBoiled = false;

            if (activeCoolDownCoroutine != null)
            {
                StopCoroutine(activeCoolDownCoroutine);
                activeCoolDownCoroutine = null;
            }

            Transform kettleT = kettle.transform;
            Vector3 kettleOrigPos = kettleT.position;
            Quaternion kettleOrigRot = kettleT.rotation;

            bool needsRefill = kettleWater <= minKettleWaterToRefill;

            if (needsRefill)
            {
                if (bottleWater <= 0.01f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Bình nước Sài Gòn Aquwa đã hết sạch nước rồi! Không thể đun.");
                    isBoilingWater = false;
                    yield break;
                }

                // 1. Cầm ấm qua bình nước lấy nước
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Cầm ấm nước qua bình nước Sài Gòn Aquwa để hứng nước sạch...");
                Vector3 pourPos = water.transform.position + Vector3.up * 0.38f;
                Quaternion pourRot = Quaternion.Euler(-45f, 180f, 0f); // Nghiêng ấm để hứng/rót nước
                yield return StartCoroutine(SmoothMove(kettleT, pourPos, pourRot, 1.5f));

                float refillAmount = maxKettleWater - kettleWater;
                if (bottleWater < refillAmount)
                {
                    refillAmount = bottleWater;
                }
                bottleWater -= refillAmount;
                kettleWater += refillAmount;

                // Hứng nước trong 2 giây
                float elapsedWater = 0f;
                while (elapsedWater < 2f)
                {
                    elapsedWater += Time.deltaTime;
                    EventManager.TriggerInteractionPromptShow($"Đang lấy nước vào ấm... {Mathf.CeilToInt(2f - elapsedWater)}s");
                    yield return null;
                }
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước trong ấm vẫn còn nhiều, bắt đầu đun sôi trực tiếp...");
            }

            // 2. Đặt ấm nước lên bếp ga đun sôi
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đặt ấm lên bếp ga Namilux và bật lửa đun sôi...");
            Vector3 stovePos = stove.transform.position + Vector3.up * 0.08f; // Đặt khớp nắp bếp ga
            Quaternion stoveRot = Quaternion.identity; // Phẳng ngang
            yield return StartCoroutine(SmoothMove(kettleT, stovePos, stoveRot, 1.5f));

            // Tạo hiệu ứng hơi nước
            GameObject steamFx = CreateSteamParticles(kettleT);

            // Đun nước trong 10 giây
            float elapsedBoil = 0f;
            while (elapsedBoil < 10f)
            {
                elapsedBoil += Time.deltaTime;
                EventManager.TriggerInteractionPromptShow($"Nước đang sôi: {Mathf.CeilToInt(10f - elapsedBoil)}s");
                yield return null;
            }

            // Hủy hiệu ứng hơi nước
            if (steamFx != null)
            {
                Destroy(steamFx);
            }

            isWaterBoiled = true;
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đã sôi sùng sục 100 độ C! Nhấc ấm nước nóng đặt lại chỗ cũ.");

            // 3. Di chuyển ấm về vị trí ban đầu
            yield return StartCoroutine(SmoothMove(kettleT, kettleOrigPos, kettleOrigRot, 1.5f));

            isBoilingWater = false;
            EventManager.TriggerInteractionPromptShow("Nhấn F để tương tác");

            // Bắt đầu đếm ngược 5 giây nguội
            activeCoolDownCoroutine = StartCoroutine(CoolDownRoutine());
        }

        private System.Collections.IEnumerator SmoothMove(Transform target, Vector3 destPos, Quaternion destRot, float duration)
        {
            Vector3 startPos = target.position;
            Quaternion startRot = target.rotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);
                target.position = Vector3.Lerp(startPos, destPos, t);
                target.rotation = Quaternion.Lerp(startRot, destRot, t);
                yield return null;
            }
            target.position = destPos;
            target.rotation = destRot;
        }

        private GameObject CreateSteamParticles(Transform parent)
        {
            GameObject steam = new GameObject("SteamParticles");
            steam.transform.SetParent(parent);
            steam.transform.localPosition = new Vector3(0f, 0.15f, 0.05f); // Đầu vòi ấm
            steam.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem ps = steam.AddComponent<ParticleSystem>();
            
            // Cấu hình ParticleSystem.MainModule
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.0f;
            main.startSpeed = 0.4f;
            main.startSize = 0.02f;
            main.maxParticles = 60;
            main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.25f); // Hơi nước mờ nhẹ màu xám trắng
            main.gravityModifier = -0.08f; // Hơi bay lên trên

            // Cấu hình Emission
            var emission = ps.emission;
            emission.rateOverTime = 20f;

            // Cấu hình Shape (Cone)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.015f;

            // Cấu hình ColorOverLifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.35f, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Thiết lập Material tương thích runtime
            var renderer = steam.GetComponent<ParticleSystemRenderer>();
            Shader defaultShader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (defaultShader != null)
            {
                renderer.sharedMaterial = new Material(defaultShader);
            }

            return steam;
        }

        private void OnWaterCupInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (hasPreparedTea)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Bạn đang có 1 ly trà đá pha sẵn rồi! Hãy phục vụ khách hàng trước (nhấn Space hoặc đi đến bàn khách đặt ly xuống).");
                return;
            }

            if (isHoldingCup)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Bạn đang cầm sẵn một chiếc ly. (Trà: {teaInCup}g, Nước: {Mathf.RoundToInt(waterInCup * 1000f)}ml, Đá: {iceInCup}%)");
                return;
            }

            if (stats.CupSupply < 1)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hết ly sạch rồi! Cần mua thêm ly.");
                return;
            }

            // Bắt đầu cầm ly pha chế
            stats.TakeOneCup();
            isHoldingCup = true;
            teaInCup = 0;
            waterInCup = 0f;
            iceInCup = 0f;

            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã lấy 1 ly sạch đặt lên tay. Hãy tích vào bình trà để lấy 50g trà!");
            Debug.Log("[CartItem] Tương tác ly nước -> Cầm ly pha chế");

            // Gắn mô hình ly trống lên tay Hoàng Hôn
            AttachEmptyCupToPlayer(player);
        }

        private System.Collections.IEnumerator ClickFeedback()
        {
            isInteracting = true;

            // Rung nhẹ — scale up rồi down
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 targetScale = originalScale * 1.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (t < 0.5f)
                {
                    // Scale up
                    transform.localScale = Vector3.Lerp(originalScale, targetScale, t * 2f);
                }
                else
                {
                    // Scale down
                    transform.localScale = Vector3.Lerp(targetScale, originalScale, (t - 0.5f) * 2f);
                }

                yield return null;
            }

            transform.localScale = originalScale;
            isInteracting = false;
        }

        private void OnDestroy()
        {
            // Cleanup materials
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
