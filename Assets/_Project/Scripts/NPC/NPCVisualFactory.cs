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
        private const float ModelGroundSink = 0.08f;

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

            NPCModelData selectedModelData = SelectNPCModelData();

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
                modelObj.transform.localPosition = Vector3.zero;
                modelObj.transform.localRotation = Quaternion.Euler(0f, modelYawOffset, 0f);
                PinModelBottomToVisualRoot(modelObj.transform, visualRoot.transform);

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
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
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
            return visualRoot;
        }

        private NPCModelData SelectNPCModelData()
        {
            if (npcModels == null || npcModels.Count == 0) return null;

            int startIndex = UnityEngine.Random.Range(0, npcModels.Count);
            for (int i = 0; i < npcModels.Count; i++)
            {
                NPCModelData candidate = npcModels[(startIndex + i) % npcModels.Count];
                if (candidate != null && candidate.prefab != null && !LooksLikeTPoseModel(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void PinModelBottomToVisualRoot(Transform model, Transform visualRoot)
        {
            if (model == null || visualRoot == null) return;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float deltaWorldY = (visualRoot.position.y - ModelGroundSink) - bounds.min.y;
            Vector3 localDelta = visualRoot.InverseTransformVector(Vector3.up * deltaWorldY);
            model.localPosition += localDelta;
        }

        private static bool LooksLikeTPoseModel(NPCModelData modelData)
        {
            if (modelData == null) return false;

            return ContainsTPoseName(modelData.prefab != null ? modelData.prefab.name : null) ||
                   ContainsTPoseName(modelData.animatorController != null ? modelData.animatorController.name : null);
        }

        private static bool ContainsTPoseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            return name.IndexOf("T_Pose", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("T-Pose", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("TPose", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
        private Vector3 previousWorldPosition;
        private bool hasPreviousWorldPosition;
        private bool useProceduralAnimatorSitting;
        private bool proceduralBonesCached;
        private Transform procHips;
        private Transform procSpine;
        private Transform procLeftUpperLeg;
        private Transform procRightUpperLeg;
        private Transform procLeftLowerLeg;
        private Transform procRightLowerLeg;
        private Transform procLeftFoot;
        private Transform procRightFoot;
        private System.Collections.Generic.Dictionary<Transform, Quaternion> proceduralOriginalRotations;
        private System.Collections.Generic.Dictionary<Transform, Vector3> proceduralOriginalPositions;
        
        // Sitting support
        private Transform hipsBone;          // Xương Hips để theo dõi vị trí ngồi
        private Transform npcModelTransform; // NPCModel child transform
        private float npcModelOriginalLocalY = 0f; // LocalY gốc của NPCModel (khi đứng)
        private Vector3 npcModelOriginalLocalPosition;
        private Quaternion npcModelOriginalLocalRotation;
        private Vector3 npcModelOriginalLocalScale = Vector3.one;
        private float sittingYOffset = 0f;  // Độ bù Y khi ngồi, được tính 1 lần
        private bool sittingOffsetCalculated = false;
        private int sittingFrameCount = 0;  // Đếm frame để chờ animation blend
        private const float SeatedHipAboveSeat = 0.18f;
        private const float SeatedFeetFloorClearance = 0.02f;
        private const float SittingOffsetClamp = 1.25f;
        private const float ProceduralSeatForwardOffset = 0.10f;
        private const float ProceduralSeatVisualDrop = -0.30f;

        // Walking animation parameters (chỉ dùng cho model static)
        private const float WALK_CYCLE_SPEED = 8f;
        private const float BOB_AMOUNT = 0.06f;
        private const float SWAY_AMOUNT = 3f;
        private const float LEAN_FORWARD = 5f;
        private const float ARM_SWING_AMOUNT = 8f;
        private const float STATIC_SIT_DROP = 0.62f;
        private const float STATIC_SIT_LEAN = -10f;
        private const float STATIC_SIT_SCALE_Y = 0.82f;
        private const float INFERRED_WALK_SPEED_THRESHOLD = 0.03f;
        private MeshFilter[] staticMeshFilters;
        private Mesh[] staticAnimatedMeshes;
        private Vector3[][] staticBaseVertices;
        private Vector3[][] staticWorkingVertices;
        private Bounds[] staticBaseBounds;
        private int[] staticHeightAxes;
        private int[] staticSideAxes;
        private int[] staticForwardAxes;
        private bool staticMeshesPrepared;

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
            hasAnimator = IsUsableAnimator(animator);
            useProceduralAnimatorSitting = ShouldUseProceduralAnimatorSitting(animator);
            if (animator != null && !hasAnimator)
                animator.enabled = false;
            ConfigureAnimator();

            if (animator != null)
            {
                npcModelTransform = animator.transform;
                CacheNPCModelTransform();
                var allTransforms = animator.GetComponentsInChildren<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name == "Hips") { hipsBone = t; break; }
                }
            }
            else
            {
                CacheNPCModelTransform();
            }

            sittingOffsetCalculated = false;
            sittingFrameCount = 0;
            previousWorldPosition = transform.position;
            hasPreviousWorldPosition = true;
        }

private void Start()
        {
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
                hasAnimator = IsUsableAnimator(animator);
                useProceduralAnimatorSitting = ShouldUseProceduralAnimatorSitting(animator);
                if (!hasAnimator)
                    animator.enabled = false;
            }
            else
            {
                useProceduralAnimatorSitting = ShouldUseProceduralAnimatorSitting(animator);
            }

            ConfigureAnimator();
            previousWorldPosition = transform.position;
            hasPreviousWorldPosition = true;

            if (hipsBone == null && animator != null)
            {
                npcModelTransform = animator.transform;
                CacheNPCModelTransform();
                var allTransforms = animator.GetComponentsInChildren<Transform>();
                foreach (var t in allTransforms)
                    if (t.name == "Hips") { hipsBone = t; break; }
            }
            else
            {
                CacheNPCModelTransform();
            }
        }

        private void Update()
        {
            animTimer += Time.deltaTime;
            if (controller == null)
            {
                AnimateWithoutController();
                return;
            }

            bool isWalking = (controller.CurrentState == NPCState.WalkingIn ||
                              controller.CurrentState == NPCState.Approaching ||
                              controller.CurrentState == NPCState.LeavingSeat ||
                              controller.CurrentState == NPCState.WalkingOut);
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
                        KeepNPCModelPinned();
                    }
                    if (HasParameter(animator, "State"))
                        animator.SetInteger("State", 0);
                    sittingOffsetCalculated = false;
                    sittingFrameCount = 0;
                }
                else if (isSitting)
                {
                    if (useProceduralAnimatorSitting)
                    {
                        AnimateAnimatorProceduralSitting();
                        return;
                    }

                    animator.enabled = true;
                    animator.speed = 1f;
                    if (HasParameter(animator, "State"))
                        animator.SetInteger("State", 1);
                    
                    // Seat the hips slightly above the chair surface, and keep feet from dipping below the floor.
                    if (!sittingOffsetCalculated && controller.TargetSeat != null && npcModelTransform != null)
                    {
                        transform.localPosition = originalPos;
                        KeepNPCModelPinned();
                        sittingFrameCount++;
                        
                        // Thiết lập VR về gốc, NPCModel về original
                        transform.localPosition = originalPos;
                        if (npcModelTransform != null)
                        {
                            KeepNPCModelPinned();
                        }
                        
                        // Tính Hips worldY khi VR=(0,0,0) v\u00e0 NPCModel.localY = originalLocalY, d\u1ef1a tr\u00ean v\u1ecb trí gh\u1ebf thay v\u00ec v\u1ecb trí NPC (NPC c\u00f3 th\u1ec3 ch\u01b0a snap xu\u1ed1ng gh\u1ebf)
                        if (sittingFrameCount >= 2)
                        {
                            float seatSurfaceY = controller.TargetSeat.GetSeatSurfaceY();
                            float hipsWorldY = GetCurrentHipWorldY();
                            float hipOffset = (seatSurfaceY + SeatedHipAboveSeat) - hipsWorldY;

                            float bottomOffset = 0f;
                            if (TryGetModelBounds(out Bounds modelBounds))
                            {
                                float floorY = controller.TargetSeat.GetSeatBaseY() + SeatedFeetFloorClearance;
                                bottomOffset = floorY - modelBounds.min.y;
                            }

                            sittingYOffset = Mathf.Clamp(Mathf.Max(hipOffset, bottomOffset), -SittingOffsetClamp, SittingOffsetClamp);
                            sittingOffsetCalculated = true;
                        }
                    }
                    
                    // Restore NPCModel.localY gốc (không thêm offset vào model)
                    if (npcModelTransform != null)
                    {
                        KeepNPCModelPinned();
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

        private void AnimateWithoutController()
        {
            float speed = 0f;
            if (hasPreviousWorldPosition)
            {
                Vector3 delta = transform.position - previousWorldPosition;
                delta.y = 0f;
                speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            }

            previousWorldPosition = transform.position;
            hasPreviousWorldPosition = true;
            bool isWalking = speed > INFERRED_WALK_SPEED_THRESHOLD;

            if (hasAnimator && animator != null)
            {
                animator.enabled = true;
                animator.speed = isWalking ? 0.35f : 0f;
                transform.localPosition = originalPos;
                if (HasParameter(animator, "State"))
                    animator.SetInteger("State", 0);
                KeepNPCModelPinned();
                return;
            }

            if (isWalking)
                AnimateWalking();
            else
                AnimateIdle();
        }

        private void LateUpdate()
        {
            if (hasAnimator)
            {
                if (controller != null && useProceduralAnimatorSitting &&
                    (controller.CurrentState == NPCState.SittingDown ||
                     controller.CurrentState == NPCState.Ordering ||
                     controller.CurrentState == NPCState.Waiting ||
                     controller.CurrentState == NPCState.Drinking ||
                     controller.CurrentState == NPCState.Paying))
                {
                    AnimateAnimatorProceduralSitting();
                    ApplyProceduralSeatedBonePose();
                }
                else
                {
                    KeepNPCModelPinned();
                }
            }
        }

        private void ConfigureAnimator()
        {
            if (animator == null) return;

            if (!IsUsableAnimator(animator))
            {
                animator.enabled = false;
                return;
            }

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private static bool IsUsableAnimator(Animator candidate)
        {
            return candidate != null && candidate.runtimeAnimatorController != null;
        }

        private static bool ShouldUseProceduralAnimatorSitting(Animator candidate)
        {
            if (!IsUsableAnimator(candidate)) return false;

            string controllerName = candidate.runtimeAnimatorController.name;
            return controllerName.IndexOf("NewNPC", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   controllerName.IndexOf("NPCOverride", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void KeepNPCModelPinned()
        {
            if (npcModelTransform == null) return;

            npcModelTransform.localPosition = npcModelOriginalLocalPosition;
            npcModelTransform.localRotation = npcModelOriginalLocalRotation;
            npcModelTransform.localScale = npcModelOriginalLocalScale;
        }

        private void AnimateAnimatorProceduralSitting()
        {
            animator.enabled = true;
            animator.speed = 0f;
            if (HasParameter(animator, "State"))
                animator.SetInteger("State", 0);

            KeepNPCModelPinned();
            RestoreProceduralBonePose();

            if (controller != null && controller.TargetSeat != null)
            {
                Vector3 seatForwardLocal = transform.parent != null
                    ? transform.parent.InverseTransformDirection(controller.TargetSeat.transform.forward)
                    : controller.TargetSeat.transform.forward;

                transform.localPosition = originalPos + Vector3.up * ProceduralSeatVisualDrop + seatForwardLocal.normalized * ProceduralSeatForwardOffset;
            }
            else
            {
                transform.localPosition = originalPos + Vector3.up * ProceduralSeatVisualDrop;
            }

            transform.localRotation = originalRot;
            ApplyProceduralSeatedBonePose();
        }

        private void CacheProceduralBones()
        {
            if (proceduralBonesCached || animator == null) return;

            proceduralOriginalRotations = new System.Collections.Generic.Dictionary<Transform, Quaternion>();
            proceduralOriginalPositions = new System.Collections.Generic.Dictionary<Transform, Vector3>();
            Transform[] bones = animator.GetComponentsInChildren<Transform>(true);
            foreach (Transform bone in bones)
            {
                string name = bone.name.ToLowerInvariant();
                proceduralOriginalRotations[bone] = bone.localRotation;
                proceduralOriginalPositions[bone] = bone.localPosition;

                if (procHips == null && (name.Contains("hips") || name.Contains("pelvis"))) procHips = bone;
                else if (procSpine == null && name.Contains("spine")) procSpine = bone;

                bool left = name.StartsWith("l") || name.Contains("left") || name.Contains("_l") || name.Contains(".l");
                bool right = name.StartsWith("r") || name.Contains("right") || name.Contains("_r") || name.Contains(".r");
                bool upperLeg = name.Contains("upleg") || name.Contains("upperleg") || name.Contains("thigh");
                bool lowerLeg = name.Contains("leg") && (name.Contains("lower") || name.Contains("calf") || name.Contains("shin"));
                bool foot = name.Contains("foot");

                if (left && upperLeg && procLeftUpperLeg == null) procLeftUpperLeg = bone;
                else if (right && upperLeg && procRightUpperLeg == null) procRightUpperLeg = bone;
                else if (left && lowerLeg && procLeftLowerLeg == null) procLeftLowerLeg = bone;
                else if (right && lowerLeg && procRightLowerLeg == null) procRightLowerLeg = bone;
                else if (left && foot && procLeftFoot == null) procLeftFoot = bone;
                else if (right && foot && procRightFoot == null) procRightFoot = bone;
            }

            proceduralBonesCached = true;
        }

        private void RestoreProceduralBonePose()
        {
            CacheProceduralBones();
            if (proceduralOriginalRotations == null) return;

            foreach (var pair in proceduralOriginalRotations)
            {
                if (pair.Key != null)
                    pair.Key.localRotation = pair.Value;
            }

            if (proceduralOriginalPositions != null)
            {
                foreach (var pair in proceduralOriginalPositions)
                {
                    if (pair.Key != null)
                        pair.Key.localPosition = pair.Value;
                }
            }
        }

        private void ApplyProceduralSeatedBonePose()
        {
            CacheProceduralBones();
            if (proceduralOriginalRotations == null) return;

            SetBoneRotation(procHips, 0f, 0f, 0f);
            SetBoneRotation(procSpine, -8f, 0f, 0f);
            SetBoneRotation(procLeftUpperLeg, 72f, 0f, 0f);
            SetBoneRotation(procRightUpperLeg, 72f, 0f, 0f);
            SetBoneRotation(procLeftLowerLeg, -82f, 0f, 0f);
            SetBoneRotation(procRightLowerLeg, -82f, 0f, 0f);
            SetBoneRotation(procLeftFoot, 12f, 0f, 0f);
            SetBoneRotation(procRightFoot, 12f, 0f, 0f);
        }

        private void SetBoneRotation(Transform bone, float x, float y, float z)
        {
            if (bone == null || proceduralOriginalRotations == null || !proceduralOriginalRotations.ContainsKey(bone)) return;

            bone.localRotation = proceduralOriginalRotations[bone] * Quaternion.Euler(x, y, z);
        }

private void CacheNPCModelTransform()
        {
            if (npcModelTransform == null)
            {
                npcModelTransform = transform.Find("NPCModel");
                if (npcModelTransform == null && transform.childCount > 0)
                {
                    npcModelTransform = transform.GetChild(0);
                }
            }

            if (npcModelTransform == null) return;

            npcModelOriginalLocalY = npcModelTransform.localPosition.y;
            npcModelOriginalLocalPosition = npcModelTransform.localPosition;
            npcModelOriginalLocalRotation = npcModelTransform.localRotation;
            npcModelOriginalLocalScale = npcModelTransform.localScale;
        }

        private void RestoreStaticModelPose()
        {
            if (npcModelTransform == null) return;

            npcModelTransform.localPosition = npcModelOriginalLocalPosition;
            npcModelTransform.localRotation = npcModelOriginalLocalRotation;
            npcModelTransform.localScale = npcModelOriginalLocalScale;
            RestoreStaticMeshes();
        }

        private void PrepareStaticMeshes()
        {
            if (staticMeshesPrepared || npcModelTransform == null || hasAnimator) return;

            MeshFilter[] filters = npcModelTransform.GetComponentsInChildren<MeshFilter>(true);
            var filterList = new System.Collections.Generic.List<MeshFilter>();
            var meshList = new System.Collections.Generic.List<Mesh>();
            var vertexList = new System.Collections.Generic.List<Vector3[]>();
            var workingVertexList = new System.Collections.Generic.List<Vector3[]>();
            var boundsList = new System.Collections.Generic.List<Bounds>();
            var heightAxisList = new System.Collections.Generic.List<int>();
            var sideAxisList = new System.Collections.Generic.List<int>();
            var forwardAxisList = new System.Collections.Generic.List<int>();

            foreach (var filter in filters)
            {
                if (filter == null || filter.sharedMesh == null) continue;
                if (!filter.sharedMesh.isReadable) continue;

                try
                {
                    Mesh mesh = Instantiate(filter.sharedMesh);
                    if (!mesh.isReadable) continue;

                    Vector3[] vertices = mesh.vertices;
                    if (vertices == null || vertices.Length == 0) continue;

                    mesh.name = filter.sharedMesh.name + "_NPCSharedRuntimeAnimation";
                    mesh.MarkDynamic();
                    filter.sharedMesh = mesh;

                    Bounds bounds = mesh.bounds;
                    int heightAxis = GetLargestAxis(bounds.size);
                    int sideAxis = GetWidestRemainingAxis(bounds.size, heightAxis);
                    int forwardAxis = 3 - heightAxis - sideAxis;

                    filterList.Add(filter);
                    meshList.Add(mesh);
                    vertexList.Add(vertices);
                    workingVertexList.Add(new Vector3[vertices.Length]);
                    boundsList.Add(bounds);
                    heightAxisList.Add(heightAxis);
                    sideAxisList.Add(sideAxis);
                    forwardAxisList.Add(forwardAxis);
                }
                catch
                {
                    // Some imported meshes can be non-readable. They still get whole-body procedural motion.
                }
            }

            staticMeshFilters = filterList.ToArray();
            staticAnimatedMeshes = meshList.ToArray();
            staticBaseVertices = vertexList.ToArray();
            staticWorkingVertices = workingVertexList.ToArray();
            staticBaseBounds = boundsList.ToArray();
            staticHeightAxes = heightAxisList.ToArray();
            staticSideAxes = sideAxisList.ToArray();
            staticForwardAxes = forwardAxisList.ToArray();
            staticMeshesPrepared = true;
        }

        private void ApplyStaticMeshWalk(float t)
        {
            PrepareStaticMeshes();
            if (staticAnimatedMeshes == null) return;

            for (int i = 0; i < staticAnimatedMeshes.Length; i++)
            {
                Mesh mesh = staticAnimatedMeshes[i];
                Vector3[] source = staticBaseVertices[i];
                Vector3[] vertices = staticWorkingVertices[i];
                if (mesh == null || source == null || vertices == null) continue;

                Bounds bounds = staticBaseBounds[i];
                int heightAxis = staticHeightAxes[i];
                int sideAxis = staticSideAxes[i];
                int forwardAxis = staticForwardAxes[i];
                float heightMin = GetAxis(bounds.min, heightAxis);
                float heightSize = Mathf.Max(GetAxis(bounds.size, heightAxis), 0.0001f);
                float sideCenter = GetAxis(bounds.center, sideAxis);
                float sideExtent = Mathf.Max(GetAxis(bounds.extents, sideAxis), 0.0001f);

                for (int v = 0; v < source.Length; v++)
                {
                    Vector3 p = source[v];
                    float height01 = Mathf.Clamp01((GetAxis(p, heightAxis) - heightMin) / heightSize);
                    float sideNorm = Mathf.Clamp((GetAxis(p, sideAxis) - sideCenter) / sideExtent, -1f, 1f);
                    float side = sideNorm >= 0f ? 1f : -1f;
                    float stride = Mathf.Sin(t + (side > 0f ? 0f : Mathf.PI));
                    float counterStride = Mathf.Sin(t + (side > 0f ? Mathf.PI : 0f));
                    float lowerWeight = Mathf.Clamp01((0.58f - height01) / 0.36f) * Mathf.Clamp01(Mathf.Abs(sideNorm) * 2.2f);
                    float armWeight = Mathf.Clamp01((height01 - 0.42f) / 0.25f) * Mathf.Clamp01((0.92f - height01) / 0.22f) * Mathf.Clamp01(Mathf.Abs(sideNorm) * 1.9f);

                    SetAxis(ref p, forwardAxis, GetAxis(p, forwardAxis) + stride * 0.18f * lowerWeight + counterStride * 0.12f * armWeight);
                    SetAxis(ref p, heightAxis, GetAxis(p, heightAxis) + Mathf.Abs(stride) * 0.035f * lowerWeight);
                    SetAxis(ref p, sideAxis, GetAxis(p, sideAxis) + side * Mathf.Abs(stride) * 0.025f * lowerWeight);
                    vertices[v] = p;
                }

                mesh.vertices = vertices;
                mesh.RecalculateBounds();
            }
        }

        private void ApplyStaticMeshSit(float breathe)
        {
            PrepareStaticMeshes();
            if (staticAnimatedMeshes == null) return;

            for (int i = 0; i < staticAnimatedMeshes.Length; i++)
            {
                Mesh mesh = staticAnimatedMeshes[i];
                Vector3[] source = staticBaseVertices[i];
                Vector3[] vertices = staticWorkingVertices[i];
                if (mesh == null || source == null || vertices == null) continue;

                Bounds bounds = staticBaseBounds[i];
                int heightAxis = staticHeightAxes[i];
                int forwardAxis = staticForwardAxes[i];
                float heightMin = GetAxis(bounds.min, heightAxis);
                float heightSize = Mathf.Max(GetAxis(bounds.size, heightAxis), 0.0001f);

                for (int v = 0; v < source.Length; v++)
                {
                    Vector3 p = source[v];
                    float height01 = Mathf.Clamp01((GetAxis(p, heightAxis) - heightMin) / heightSize);
                    float foldedHeight = height01 < 0.58f ? height01 * 0.58f : 0.34f + (height01 - 0.58f) * 1.08f;
                    float forwardOffset = height01 < 0.58f ? 0.24f * (1f - height01) : -0.055f * (height01 - 0.58f);

                    SetAxis(ref p, heightAxis, heightMin + foldedHeight * heightSize + breathe);
                    SetAxis(ref p, forwardAxis, GetAxis(p, forwardAxis) + forwardOffset);
                    vertices[v] = p;
                }

                mesh.vertices = vertices;
                mesh.RecalculateBounds();
            }
        }

        private void RestoreStaticMeshes()
        {
            if (staticAnimatedMeshes == null || staticBaseVertices == null || staticWorkingVertices == null) return;

            for (int i = 0; i < staticAnimatedMeshes.Length; i++)
            {
                if (staticAnimatedMeshes[i] == null || staticBaseVertices[i] == null || staticWorkingVertices[i] == null) continue;
                System.Array.Copy(staticBaseVertices[i], staticWorkingVertices[i], staticBaseVertices[i].Length);
                staticAnimatedMeshes[i].vertices = staticWorkingVertices[i];
                staticAnimatedMeshes[i].RecalculateBounds();
            }
        }

        private static int GetLargestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z) return 0;
            return size.y >= size.z ? 1 : 2;
        }

        private static int GetWidestRemainingAxis(Vector3 size, int excludedAxis)
        {
            int a = excludedAxis == 0 ? 1 : 0;
            int b = excludedAxis == 2 ? 1 : 2;
            return GetAxis(size, a) >= GetAxis(size, b) ? a : b;
        }

        private static float GetAxis(Vector3 value, int axis)
        {
            if (axis == 0) return value.x;
            return axis == 1 ? value.y : value.z;
        }

        private static void SetAxis(ref Vector3 value, int axis, float axisValue)
        {
            if (axis == 0) value.x = axisValue;
            else if (axis == 1) value.y = axisValue;
            else value.z = axisValue;
        }


        private bool TryGetModelBounds(out Bounds bounds)
        {
            bounds = default;
            if (npcModelTransform == null) return false;

            Renderer[] renderers = npcModelTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private float GetCurrentHipWorldY()
        {
            if (hipsBone != null)
            {
                return hipsBone.position.y;
            }

            if (TryGetModelBounds(out Bounds bounds))
            {
                return bounds.min.y + bounds.size.y * 0.52f;
            }

            return transform.position.y;
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

            transform.localPosition = originalPos + new Vector3(Mathf.Sin(t) * 0.025f, bobY, 0);
            transform.localRotation = originalRot * Quaternion.Euler(leanX + strideLean, 0, swayZ);

            if (npcModelTransform != null)
            {
                npcModelTransform.localPosition = npcModelOriginalLocalPosition + new Vector3(0, Mathf.Sin(t * 2f) * 0.012f, 0);
                npcModelTransform.localRotation = npcModelOriginalLocalRotation *
                    Quaternion.Euler(Mathf.Sin(t) * 4f, Mathf.Sin(t * 0.5f) * 2f, -swayZ * 0.35f);
                npcModelTransform.localScale = npcModelOriginalLocalScale;
            }
            ApplyStaticMeshWalk(t);
        }

private void AnimateSitting()
        {
            float breathe = Mathf.Sin(animTimer * 2f) * 0.015f;
            transform.localPosition = originalPos + new Vector3(0, -STATIC_SIT_DROP + breathe, 0);
            transform.localRotation = originalRot * Quaternion.Euler(STATIC_SIT_LEAN, 0, 0);

            if (npcModelTransform != null)
            {
                npcModelTransform.localPosition = npcModelOriginalLocalPosition + new Vector3(0, 0.08f, -0.08f);
                npcModelTransform.localRotation = npcModelOriginalLocalRotation * Quaternion.Euler(-7f, 0, 0);
                npcModelTransform.localScale = new Vector3(
                    npcModelOriginalLocalScale.x,
                    npcModelOriginalLocalScale.y * STATIC_SIT_SCALE_Y,
                    npcModelOriginalLocalScale.z);
            }
            ApplyStaticMeshSit(breathe);
        }

private void AnimateIdle()
        {
            float breathe = Mathf.Sin(animTimer * IDLE_BREATHE_SPEED) * IDLE_BREATHE_AMOUNT;
            float sway = Mathf.Sin(animTimer * IDLE_SWAY_SPEED) * IDLE_SWAY_AMOUNT;
            transform.localPosition = originalPos + new Vector3(0, breathe, 0);
            transform.localRotation = originalRot * Quaternion.Euler(0, 0, sway);
            RestoreStaticModelPose();
        }
    }
}
