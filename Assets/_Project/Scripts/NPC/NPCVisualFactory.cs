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
        [Tooltip("Xoay model con quanh Y. Parent đã xoay theo hướng đi — để 0 nếu model Meshy AI hướng +Z.")]
        [SerializeField] private float modelYawOffset = 0f;

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
                    // Tắt ghi đè material để giữ nguyên material gốc của prefab
                    // if (selectedModelData.material != null) mr.sharedMaterial = selectedModelData.material;
                }
                var skinnedRenderers = modelObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in skinnedRenderers)
                {
                    smr.gameObject.SetActive(true);
                    smr.enabled = true;
                    // Tắt ghi đè material để giữ nguyên material gốc của prefab
                    // if (selectedModelData.material != null) smr.sharedMaterial = selectedModelData.material;
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
                modelObj.transform.localRotation = Quaternion.Euler(0f, modelYawOffset, 0f);

                // Kiểm tra và thiết lập Animator cho animation đi bộ
                var animator = modelObj.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    if (selectedModelData.animatorController != null)
                    {
                        animator.runtimeAnimatorController = selectedModelData.animatorController;
                    }
                    animator.enabled = true;
                    animator.applyRootMotion = false;
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
            
            // Inject controller trực tiếp thay vì dùng GetComponentInParent trong Start()
            // để tránh race condition do Unity timing
            var npcController = parent.GetComponent<NPCController>();
            var npcAnimator = visualRoot.GetComponentInChildren<Animator>();
            if (proceduralAnim != null)
            {
                proceduralAnim.Initialize(npcController, npcAnimator);
            }
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
        
        // Sitting support
        private Transform hipsBone;          // Xương Hips để theo dõi vị trí ngồi
        private Transform npcModelTransform; // NPCModel child transform
        private float npcModelOriginalLocalY = 0f; // LocalY gốc của NPCModel (khi đứng)
        private float sittingYOffset = 0f;  // Độ bù Y khi ngồi, được tính 1 lần
        private bool sittingOffsetCalculated = false;
        private int sittingFrameCount = 0;  // Đếm frame để chờ animation blend

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

        /// <summary>
        /// Khởi tạo trực tiếp với reference đến controller và animator.
        /// Được gọi từ NPCVisualFactory.CreateNPCVisual() để tránh race condition.
        /// </summary>
        public void Initialize(NPCController npcController, Animator npcAnimator)
        {
            controller = npcController;
            animator = npcAnimator;
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
            hasAnimator = (animator != null && animator.runtimeAnimatorController != null);
            if (animator != null && !hasAnimator)
                hasAnimator = true;
            
            // Tìm Hips bone và NPCModel transform
            if (animator != null)
            {
                npcModelTransform = animator.transform; // NPCModel
                npcModelOriginalLocalY = npcModelTransform.localPosition.y; // Lưu Y gốc
                var allTransforms = animator.GetComponentsInChildren<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name == "Hips") { hipsBone = t; break; }
                }
            }
            sittingOffsetCalculated = false;
            sittingFrameCount = 0;

        }

        private void Start()
        {
            // Nếu Initialize() chưa được gọi (fallback), tự tìm
            if (controller == null)
            {
                controller = GetComponentInParent<NPCController>(true);
                if (controller == null)
                {
                    var parents = GetComponentsInParent<NPCController>(true);
                    if (parents != null && parents.Length > 0)
                        controller = parents[0];
                }
            }
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
            
            if (animator != null && !hasAnimator)
            {
                hasAnimator = (animator.runtimeAnimatorController != null);
                if (!hasAnimator) hasAnimator = true; // biped luôn có animation
            }
            
            // Fallback tìm Hips
            if (hipsBone == null && animator != null)
            {
                npcModelTransform = animator.transform;
                npcModelOriginalLocalY = npcModelTransform.localPosition.y; // Lưu Y gốc
                var allTransforms = animator.GetComponentsInChildren<Transform>();
                foreach (var t in allTransforms)
                    if (t.name == "Hips") { hipsBone = t; break; }
            }
        }

        private void Update()
        {
            if (controller == null) return;
            animTimer += Time.deltaTime;

            bool isWalking = (controller.CurrentState == NPCState.WalkingIn || controller.CurrentState == NPCState.WalkingOut);
            bool isSitting = (controller.CurrentState == NPCState.SittingDown ||
                              controller.CurrentState == NPCState.Ordering ||
                              controller.CurrentState == NPCState.Waiting ||
                              controller.CurrentState == NPCState.Drinking ||
                              controller.CurrentState == NPCState.Paying);

            if (hasAnimator && animator != null)
            {
                // === BIPED MODEL: Dùng Animator parameter State (0=Walk, 1=Sit) ===
                if (isWalking)
                {
                    animator.enabled = true;
                    animator.speed = 0.35f;
                    transform.localPosition = originalPos;
                    // Restore NPCModel Y gốc khi đi bộ
                    if (npcModelTransform != null)
                    {
                        var mp = npcModelTransform.localPosition;
                        npcModelTransform.localPosition = new Vector3(mp.x, npcModelOriginalLocalY, mp.z);
                    }
                    if (HasParameter(animator, "State"))
                        animator.SetInteger("State", 0);
                    sittingOffsetCalculated = false;
                    sittingFrameCount = 0;
                }
                else if (isSitting)
                {
                    animator.enabled = true;
                    animator.speed = 1f;
                    if (HasParameter(animator, "State"))
                        animator.SetInteger("State", 1);
                    
                    // Tính VisualRoot Y offset để NPC ngồi đúng trên ghế.
                    // Hips ở animation Sitting Idle có localY ≈ 57.49cm (trong Armature space).
                    // Armature scale = 0.01 (GLB cm→m), NPCModel scale = npcModelTransform.lossyScale.y
                    // => HipsWorldY = NPCModel.worldY + 57.49 * 0.01 * npcModelScale
                    // Muốn HipsWorldY = seatSurfaceY
                    // => offset = seatSurfaceY - HipsWorldY_khi_VR=0
                    if (!sittingOffsetCalculated && controller.TargetSeat != null && npcModelTransform != null)
                    {
                        var seatRenderer = controller.TargetSeat.GetComponentInChildren<MeshRenderer>();
                        float seatSurfaceY = seatRenderer != null
                            ? seatRenderer.bounds.max.y
                            : controller.TargetSeat.transform.position.y + 0.18f;
                        
                        // Sitting animation Hips.localY trong Armature space (đo thực tế)
                        const float HIPS_SITTING_LOCAL_Y = 57.49f; // cm trong Armature
                        const float ARMATURE_SCALE = 0.01f; // GLB cm→m
                        float npcModelScale = npcModelTransform.lossyScale.y;
                        
                        // Thiết lập VR về gốc, NPCModel về original
                        transform.localPosition = originalPos;
                        if (npcModelTransform != null)
                        {
                            var mp2 = npcModelTransform.localPosition;
                            npcModelTransform.localPosition = new Vector3(mp2.x, npcModelOriginalLocalY, mp2.z);
                        }
                        
                        // Tính Hips worldY khi VR=(0,0,0) v\u00e0 NPCModel.localY = originalLocalY, d\u1ef1a tr\u00ean v\u1ecb trí gh\u1ebf thay v\u00ec v\u1ecb trí NPC (NPC c\u00f3 th\u1ec3 ch\u01b0a snap xu\u1ed1ng gh\u1ebf)
                        float targetNpcWorldY = controller.TargetSeat.transform.position.y;
                        float npcModelWorldY = targetNpcWorldY + npcModelOriginalLocalY;
                        float hipsWorldYWhenSitting = npcModelWorldY + HIPS_SITTING_LOCAL_Y * ARMATURE_SCALE * npcModelScale;
                        
                        // Offset cần thêm vào VisualRoot
                        sittingYOffset = seatSurfaceY - hipsWorldYWhenSitting;
                        sittingOffsetCalculated = true;
                    }
                    
                    // Restore NPCModel.localY gốc (không thêm offset vào model)
                    if (npcModelTransform != null)
                    {
                        var mp = npcModelTransform.localPosition;
                        npcModelTransform.localPosition = new Vector3(mp.x, npcModelOriginalLocalY, mp.z);
                    }
                    // Apply offset vào VisualRoot
                    float targetY = originalPos.y + (sittingOffsetCalculated ? sittingYOffset : 0f);
                    transform.localPosition = new Vector3(originalPos.x, targetY, originalPos.z);
                }
                else
                {
                    animator.speed = 0f;
                    transform.localPosition = originalPos;
                    // Restore NPCModel Y gốc
                    if (npcModelTransform != null)
                    {
                        var mp = npcModelTransform.localPosition;
                        npcModelTransform.localPosition = new Vector3(mp.x, npcModelOriginalLocalY, mp.z);
                    }
                    sittingOffsetCalculated = false;
                    if (HasParameter(animator, "State"))
                        animator.SetInteger("State", 0);
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

        private bool HasParameter(Animator anim, string paramName)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return false;
            foreach (var p in anim.parameters)
                if (p.name == paramName) return true;
            return false;
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
