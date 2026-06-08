using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.NPC
{
    /// <summary>
    /// Tạo visual cho NPC (dùng hình khối primitive nếu chưa có model) và animation thủ tục.
    /// </summary>
    public class NPCVisualFactory : MonoBehaviour
    {
        [System.Serializable]
        public class NPCModelData
        {
            public GameObject prefab;
            public Material material;
            public RuntimeAnimatorController animatorController;
        }

        [Header("Materials")]
        [SerializeField] private Material baseMaterial;

        [Header("NPC Model Settings")]
        [SerializeField] private System.Collections.Generic.List<NPCModelData> npcModels = new System.Collections.Generic.List<NPCModelData>();

        // Bảng màu cho từng loại NPC
        private Color colorFisherman = new Color(0.2f, 0.4f, 0.8f);    // Xanh dương
        private Color colorWorker = new Color(0.6f, 0.4f, 0.2f);       // Nâu
        private Color colorBusDriver = new Color(0.5f, 0.5f, 0.5f);    // Xám
        private Color colorIslandTraveler = new Color(0.3f, 0.8f, 0.4f); // Xanh lá
        private Color colorResident = new Color(0.9f, 0.9f, 0.8f);     // Trắng kem

        public GameObject CreateNPCVisual(NPCType type, Transform parent)
        {
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(parent);
            visualRoot.transform.localPosition = Vector3.zero;

            NPCModelData selectedModelData = null;
            if (npcModels != null && npcModels.Count > 0)
            {
                selectedModelData = npcModels[UnityEngine.Random.Range(0, npcModels.Count)];
            }

            if (selectedModelData != null && selectedModelData.prefab != null)
            {
                // Instantiate mô hình NPC (biped có animation đi bộ)
                GameObject modelObj = Instantiate(selectedModelData.prefab, visualRoot.transform);
                modelObj.name = "NPCModel";
                
                // Kích hoạt tất cả renderers (MeshRenderer + SkinnedMeshRenderer)
                var meshRenderers = modelObj.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in meshRenderers)
                {
                    mr.gameObject.SetActive(true);
                    mr.enabled = true;
                    if (selectedModelData.material != null) mr.sharedMaterial = selectedModelData.material;
                }
                var skinnedRenderers = modelObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in skinnedRenderers)
                {
                    smr.gameObject.SetActive(true);
                    smr.enabled = true;
                    if (selectedModelData.material != null) smr.sharedMaterial = selectedModelData.material;
                }

                // Tính toán bounds của model để tự động scale
                Bounds combinedBounds = new Bounds();
                bool hasBounds = false;
                // Ưu tiên SkinnedMeshRenderer bounds
                foreach (var smr in skinnedRenderers)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = smr.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(smr.bounds);
                    }
                }
                // Fallback dùng MeshFilter bounds
                if (!hasBounds)
                {
                    foreach (var mr in meshRenderers)
                    {
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            if (!hasBounds)
                            {
                                combinedBounds = mf.sharedMesh.bounds;
                                hasBounds = true;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(mf.sharedMesh.bounds);
                            }
                        }
                    }
                }

                // Chiều cao NPC mong muốn (khoảng 1.8m world units)
                float targetHeight = 1.8f;
                float scaleFactor = 1f;
                float meshHeight = Mathf.Max(combinedBounds.size.y, Mathf.Max(combinedBounds.size.x, combinedBounds.size.z));
                if (hasBounds && meshHeight > 0.001f)
                {
                    scaleFactor = targetHeight / meshHeight;
                }
                else
                {
                    scaleFactor = 1.8f; // Giá trị dự phòng nếu không đo được
                }

                modelObj.transform.localScale = Vector3.one * scaleFactor;

                // Căn chỉnh vị trí chân đứng trên mặt đất (Y=0)
                float localY = 0f;
                if (hasBounds)
                {
                    localY = -(combinedBounds.min.y * scaleFactor);
                }
                modelObj.transform.localPosition = new Vector3(0f, localY, 0f);
                modelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Xoay 180 độ quanh Y để đúng hướng di chuyển (tránh bị đi lùi)

                // Kiểm tra và thiết lập Animator cho animation đi bộ
                var animator = modelObj.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    if (selectedModelData.animatorController != null)
                    {
                        animator.runtimeAnimatorController = selectedModelData.animatorController;
                    }
                    animator.enabled = true;
                    // Đặt tốc độ animation chậm lại để giống đi bộ (model gốc là Running)
                    animator.speed = 0.35f;
                }
            }
            else
            {
                // Fallback nếu không có model (Cơ chế capsule cũ)
                if (baseMaterial == null)
                {
                    baseMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                }

                // Thân (Capsule)
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(body.GetComponent<CapsuleCollider>());
                body.transform.SetParent(visualRoot.transform);
                body.transform.localPosition = new Vector3(0, 0.5f, 0); // Kéo lên khỏi mặt đất
                body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                // Đầu (Sphere)
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(head.GetComponent<SphereCollider>());
                head.transform.SetParent(visualRoot.transform);
                head.transform.localPosition = new Vector3(0, 1.2f, 0);
                head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

                // Mắt (Cube) - để biết NPC đang quay mặt hướng nào
                GameObject eyes = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(eyes.GetComponent<BoxCollider>());
                eyes.transform.SetParent(head.transform);
                eyes.transform.localPosition = new Vector3(0.1f, 0.1f, 0.15f);
                eyes.transform.localScale = new Vector3(0.3f, 0.1f, 0.1f);
                eyes.GetComponent<MeshRenderer>().material.color = Color.black;

                // Đổi màu theo loại NPC
                Color bodyColor = Color.white;
                switch (type)
                {
                    case NPCType.Fisherman: bodyColor = colorFisherman; break;
                    case NPCType.Worker: bodyColor = colorWorker; break;
                    case NPCType.BusDriver: bodyColor = colorBusDriver; break;
                    case NPCType.IslandTraveler: bodyColor = colorIslandTraveler; break;
                    case NPCType.LocalResident: bodyColor = colorResident; break;
                }

                Material bodyMat = new Material(baseMaterial);
                bodyMat.color = bodyColor;
                body.GetComponent<MeshRenderer>().material = bodyMat;

                Material headMat = new Material(baseMaterial);
                headMat.color = new Color(1f, 0.8f, 0.6f); // Màu da
                head.GetComponent<MeshRenderer>().material = headMat;
            }     // Thêm Procedural Animator
            var proceduralAnim = visualRoot.AddComponent<NPCProceduralAnimator>();
            return visualRoot;
        }
    }

    /// <summary>
    /// Component gắn vào VisualRoot của NPC.
    /// Nếu model có Animator (biped): điều khiển play/pause animation đi bộ.
    /// Nếu model static: dùng procedural animation (nhấp nhô, lắc vai).
    /// </summary>
    public class NPCProceduralAnimator : MonoBehaviour
    {
        private NPCController controller;
        private float animTimer;
        private Vector3 originalPos;
        private Quaternion originalRot;
        private Animator animator;
        private bool hasAnimator = false;

        // Walking animation parameters (chỉ dùng cho model static)
        private const float WALK_CYCLE_SPEED = 8f;
        private const float BOB_AMOUNT = 0.06f;
        private const float SWAY_AMOUNT = 3f;
        private const float LEAN_FORWARD = 5f;
        private const float ARM_SWING_AMOUNT = 8f;

        // Idle animation parameters  
        private const float IDLE_BREATHE_SPEED = 2f;
        private const float IDLE_BREATHE_AMOUNT = 0.01f;
        private const float IDLE_SWAY_SPEED = 1.2f;
        private const float IDLE_SWAY_AMOUNT = 1f;

        private void Start()
        {
            controller = GetComponentInParent<NPCController>();
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
            
            // Kiểm tra model có Animator (biped skeleton) không
            animator = GetComponentInChildren<Animator>();
            hasAnimator = (animator != null && animator.runtimeAnimatorController != null);
            
            // Nếu có Animator nhưng không có controller, vẫn dùng để play clip trực tiếp
            if (animator != null && !hasAnimator)
            {
                hasAnimator = true; // Model biped luôn có animation baked in
            }
        }

        private void Update()
        {
            if (controller == null) return;
            animTimer += Time.deltaTime;

            bool isWalking = (controller.CurrentState == NPCState.WalkingIn || controller.CurrentState == NPCState.WalkingOut);
            bool isSitting = (controller.CurrentState == NPCState.SittingDown || controller.CurrentState == NPCState.Waiting || controller.CurrentState == NPCState.Drinking);

            if (hasAnimator && animator != null)
            {
                // === BIPED MODEL: Dùng animation gốc ===
                if (isWalking)
                {
                    animator.enabled = true;
                    animator.speed = 0.35f; // Chậm lại cho giống đi bộ (gốc là Running)
                    transform.localPosition = originalPos;
                }
                else if (isSitting)
                {
                    // Dừng animation khi idle/ngồi
                    animator.speed = 0f;
                    // Hạ thấp xuống giống như đang ngồi ghế nhựa thấp
                    float breathe = Mathf.Sin(animTimer * 2f) * 0.015f;
                    transform.localPosition = originalPos + new Vector3(0, -0.35f + breathe, 0);
                }
                else
                {
                    // Dừng animation khi idle/ngồi
                    animator.speed = 0f;
                    transform.localPosition = originalPos;
                }
            }
            else
            {
                // === STATIC MODEL: Procedural animation ===
                if (isWalking)
                {
                    AnimateWalking();
                }
                else if (isSitting)
                {
                    AnimateSitting();
                }
                else
                {
                    AnimateIdle();
                }
            }
        }

        private void AnimateWalking()
        {
            float t = animTimer * WALK_CYCLE_SPEED;
            float bobY = Mathf.Abs(Mathf.Sin(t)) * BOB_AMOUNT;
            float swayZ = Mathf.Sin(t * 0.5f) * SWAY_AMOUNT;
            float strideLean = Mathf.Sin(t) * ARM_SWING_AMOUNT;
            float leanX = LEAN_FORWARD;

            transform.localPosition = originalPos + new Vector3(0, bobY, 0);
            transform.localRotation = originalRot * Quaternion.Euler(leanX + strideLean, 0, swayZ);
        }

        private void AnimateSitting()
        {
            float breathe = Mathf.Sin(animTimer * 2f) * 0.015f;
            transform.localPosition = originalPos + new Vector3(0, -0.35f + breathe, 0);
            transform.localRotation = originalRot;
        }

        private void AnimateIdle()
        {
            float breathe = Mathf.Sin(animTimer * IDLE_BREATHE_SPEED) * IDLE_BREATHE_AMOUNT;
            float sway = Mathf.Sin(animTimer * IDLE_SWAY_SPEED) * IDLE_SWAY_AMOUNT;
            transform.localPosition = originalPos + new Vector3(0, breathe, 0);
            transform.localRotation = originalRot * Quaternion.Euler(0, 0, sway);
        }
    }
}
