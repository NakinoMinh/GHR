using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Player
{
    /// <summary>
    /// Quản lý animation cho nhân vật.
    /// Khi có Animator Controller 3D: Dùng Animator với smooth parameter transitions.
    /// Khi không có: Procedural Animation (fallback).
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Procedural Animation Settings (Fallback)")]
        [SerializeField] private float walkBobSpeed = 8f;
        [SerializeField] private float walkBobAmount = 0.05f;
        [SerializeField] private float runBobSpeed = 12f;
        [SerializeField] private float runBobAmount = 0.07f;
        [SerializeField] private float idleSwaySpeed = 1.5f;
        [SerializeField] private float idleSwayAmount = 0.02f;
        [SerializeField] private float pushBobSpeed = 6f;
        [SerializeField] private float pushBobAmount = 0.03f;

        [Header("References")]
        [SerializeField] private Transform visualTransform;
        [Tooltip("Xoay model con quanh Y để khớp hướng animation Meshy AI (tránh đi lùi).")]
        [SerializeField] private float modelYawOffset = 0f;

        [Header("Sitting Settings")]
        [SerializeField] private float sittingYOffset = 0f;

        [Header("Cup Holding Arm Adjustments (Xoay xương cánh tay khi cầm ly)")]
        [Tooltip("Điều chỉnh góc xoay upper arm phải khi cầm ly (Euler angles)")]
        [SerializeField] private Vector3 rightArmOffset = new Vector3(-55f, 0f, -25f);
        [Tooltip("Điều chỉnh góc xoay forearm phải khi cầm ly")]
        [SerializeField] private Vector3 rightForeArmOffset = new Vector3(-65f, 0f, 0f);
        [Tooltip("Điều chỉnh góc xoay bàn tay phải khi cầm ly")]
        [SerializeField] private Vector3 rightHandOffset = new Vector3(0f, -15f, 10f);

        [SerializeField] private Vector3 leftArmOffset = new Vector3(-45f, 0f, 25f);
        [SerializeField] private Vector3 leftForeArmOffset = new Vector3(-55f, 0f, 0f);
        [SerializeField] private Vector3 leftHandOffset = new Vector3(0f, 15f, -10f);

        [Header("Chế Độ Cầm Ly")]
        [Tooltip("true = Dùng procedural bone rotation (mặc định, luôn hoạt động). false = Dùng Animator HoldCupLayer (cần chạy GHR > Thiết lập Animator trước).")]
        [SerializeField] private bool useProceduralHold = true;

        private PlayerState currentState = PlayerState.Idle;
        private Vector3 originalPosition;
        private float animTimer;
        private Animator animator;
        private bool has3DAnimator = false;

        // Xương cánh tay phải
        private Transform rightArmBone;
        private Transform rightForeArmBone;
        private Transform rightHandBone;

        // Xương cánh tay trái
        private Transform leftArmBone;
        private Transform leftForeArmBone;
        private Transform leftHandBone;

        // Smooth animation blending
        private int currentAnimState = 0;
        private float currentSpeed = 0f;
        private float speedVelocity = 0f; // for SmoothDamp
        private Quaternion modelBaseRotation = Quaternion.identity;

        private void Start()
        {
            if (visualTransform == null)
            {
                if (transform.childCount > 0)
                    visualTransform = transform.GetChild(0);
                else
                    visualTransform = transform;
            }

            modelBaseRotation = Quaternion.Euler(0f, modelYawOffset, 0f);
            visualTransform.localRotation = modelBaseRotation;
            originalPosition = visualTransform.localPosition;
            animator = GetComponentInChildren<Animator>();
            
            if (animator != null)
                animator.applyRootMotion = false;

            // Kiểm tra xem có 3D Animator thật không
            has3DAnimator = (animator != null && animator.runtimeAnimatorController != null);

            // Tìm các bone tay phải để xoay cầm ly nước
            FindArmBones();
        }

        private void Update()
        {
            animTimer += Time.deltaTime;

            if (has3DAnimator)
            {
                // === 3D ANIMATION MODE ===
                int targetState = (int)currentState;
                
                // Smooth speed parameter (cho blend mượt)
                float targetSpeed = 0f;
                switch (currentState)
                {
                    case PlayerState.Idle: targetSpeed = 0f; break;
                    case PlayerState.Walking: targetSpeed = 0.5f; break;
                    case PlayerState.Running: targetSpeed = 1f; break;
                    case PlayerState.PushingCart: targetSpeed = 0.3f; break;
                    case PlayerState.Serving: targetSpeed = 0f; break;
                    case PlayerState.Sitting: targetSpeed = 0f; break;
                }
                
                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, 0.1f);
                
                animator.SetInteger("State", targetState);
                if (HasParameter("Speed"))
                    animator.SetFloat("Speed", currentSpeed);

                // Cập nhật trạng thái bưng ly vào Animator
                bool isHolding = Interaction.CartItem.IsHoldingCup || Interaction.CartItem.HasPreparedTea;
                if (HasParameter("IsHoldingCup"))
                {
                    animator.SetBool("IsHoldingCup", isHolding);
                }

                // Cập nhật Weight của HoldCupLayer (chỉ khi KHÔNG dùng procedural)
                if (!useProceduralHold)
                {
                    int holdLayerIndex = animator.GetLayerIndex("HoldCupLayer");
                    if (holdLayerIndex != -1)
                    {
                        float currentWeight = animator.GetLayerWeight(holdLayerIndex);
                        float targetWeight = isHolding ? 1f : 0f;
                        float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * 5f);
                        animator.SetLayerWeight(holdLayerIndex, newWeight);
                    }
                }
                else
                {
                    // Đảm bảo HoldCupLayer weight = 0 khi dùng procedural
                    int holdLayerIndex = animator.GetLayerIndex("HoldCupLayer");
                    if (holdLayerIndex != -1)
                    {
                        animator.SetLayerWeight(holdLayerIndex, 0f);
                    }
                }

                // Căn chỉnh vị trí mô hình 3D khi ngồi
                if (currentState == PlayerState.Sitting)
                {
                    float breathe = Mathf.Sin(animTimer * 1f) * 0.01f;
                    visualTransform.localPosition = originalPosition + new Vector3(0, sittingYOffset + breathe, 0);
                }
                else
                {
                    visualTransform.localPosition = originalPosition;
                }
                
                return; // KHÔNG chạy procedural khi có 3D animator
            }

            // === PROCEDURAL ANIMATION (Fallback) ===
            switch (currentState)
            {
                case PlayerState.Idle:
                    AnimateIdle();
                    break;
                case PlayerState.Walking:
                    AnimateWalk();
                    break;
                case PlayerState.Running:
                    AnimateRun();
                    break;
                case PlayerState.PushingCart:
                    AnimatePushCart();
                    break;
                case PlayerState.Serving:
                    AnimateServing();
                    break;
                case PlayerState.Sitting:
                    AnimateSitting();
                    break;
            }
        }

        public void SetState(PlayerState state)
        {
            if (currentState == state) return;
            currentState = state;
            animTimer = 0f;
        }

        private bool HasParameter(string paramName)
        {
            if (animator == null) return false;
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }

        // === PROCEDURAL ANIMATIONS (Fallback) ===
        private void AnimateIdle()
        {
            float sway = Mathf.Sin(animTimer * idleSwaySpeed) * idleSwayAmount;
            visualTransform.localPosition = originalPosition + new Vector3(0, sway, 0);
        }

        private void AnimateWalk()
        {
            float bob = Mathf.Abs(Mathf.Sin(animTimer * walkBobSpeed)) * (walkBobAmount * 0.5f);
            visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, originalPosition + new Vector3(0, bob, 0), Time.deltaTime * 10f);
        }

        private void AnimateRun()
        {
            float bob = Mathf.Abs(Mathf.Sin(animTimer * runBobSpeed)) * (runBobAmount * 0.5f);
            visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, originalPosition + new Vector3(0, bob, 0), Time.deltaTime * 15f);
        }

        private void AnimatePushCart()
        {
            float bob = Mathf.Abs(Mathf.Sin(animTimer * pushBobSpeed)) * pushBobAmount;
            float lean = Mathf.Sin(animTimer * pushBobSpeed * 0.5f) * 2f;
            visualTransform.localPosition = originalPosition + new Vector3(0, bob, 0);
            visualTransform.localRotation = modelBaseRotation * Quaternion.Euler(0, 0, lean);
        }

        private void AnimateServing()
        {
            float lean = Mathf.Sin(animTimer * 3f) * 3f;
            visualTransform.localRotation = modelBaseRotation * Quaternion.Euler(0, 0, -5f + lean);
        }

        private void AnimateSitting()
        {
            float breathe = Mathf.Sin(animTimer * 1f) * 0.01f;
            visualTransform.localPosition = originalPosition + new Vector3(0, sittingYOffset + breathe, 0);
            visualTransform.localRotation = modelBaseRotation;
        }

        private void LateUpdate()
        {
            // Nếu đang cầm ly pha chế hoặc đã có ly trà đá thành phẩm
            if (Interaction.CartItem.IsHoldingCup || Interaction.CartItem.HasPreparedTea)
            {
                // Dùng procedural bone rotation khi toggle bật
                if (useProceduralHold)
                {
                    // Xoay tay phải
                    if (rightArmBone != null)
                    {
                        rightArmBone.localRotation = rightArmBone.localRotation * Quaternion.Euler(rightArmOffset);
                    }
                    if (rightForeArmBone != null)
                    {
                        rightForeArmBone.localRotation = rightForeArmBone.localRotation * Quaternion.Euler(rightForeArmOffset);
                    }
                    if (rightHandBone != null)
                    {
                        rightHandBone.localRotation = rightHandBone.localRotation * Quaternion.Euler(rightHandOffset);
                    }

                    // Xoay tay trái (đối xứng để bưng bằng hai tay)
                    if (leftArmBone != null)
                    {
                        leftArmBone.localRotation = leftArmBone.localRotation * Quaternion.Euler(leftArmOffset);
                    }
                    if (leftForeArmBone != null)
                    {
                        leftForeArmBone.localRotation = leftForeArmBone.localRotation * Quaternion.Euler(leftForeArmOffset);
                    }
                    if (leftHandBone != null)
                    {
                        leftHandBone.localRotation = leftHandBone.localRotation * Quaternion.Euler(leftHandOffset);
                    }
                }
            }
        }

        private void FindArmBones()
        {
            // Tay phải — ưu tiên tên của Meshy AI biped model trước, rồi Mixamo
            string[] rightArmCandidates = { "RightArm", "RightUpperArm", "mixamorig:RightArm", "mixamorig:RightUpperArm", "Bip001 R UpperArm", "RightArmUp", "Right_Arm", "upper_arm.R" };
            string[] rightForeArmCandidates = { "RightForeArm", "mixamorig:RightForeArm", "Bip001 R Forearm", "RightForeArmUp", "ForeArm_R", "Right_ForeArm", "forearm.R" };
            string[] rightHandCandidates = { "RightHand", "mixamorig:RightHand", "Hand_R", "R_Hand", "hand_r", "Bip001 R Hand", "RHand", "Right_Hand", "hand.R" };

            // Tay trái
            string[] leftArmCandidates = { "LeftArm", "LeftUpperArm", "mixamorig:LeftArm", "mixamorig:LeftUpperArm", "Bip001 L UpperArm", "LeftArmUp", "Left_Arm", "upper_arm.L" };
            string[] leftForeArmCandidates = { "LeftForeArm", "mixamorig:LeftForeArm", "Bip001 L Forearm", "LeftForeArmUp", "ForeArm_L", "Left_ForeArm", "forearm.L" };
            string[] leftHandCandidates = { "LeftHand", "mixamorig:LeftHand", "Hand_L", "L_Hand", "hand_l", "Bip001 L Hand", "LHand", "Left_Hand", "hand.L" };

            // Tìm tay phải
            foreach (var name in rightArmCandidates)
            {
                rightArmBone = FindBoneRecursive(visualTransform, name);
                if (rightArmBone != null) break;
            }
            foreach (var name in rightForeArmCandidates)
            {
                rightForeArmBone = FindBoneRecursive(visualTransform, name);
                if (rightForeArmBone != null) break;
            }
            foreach (var name in rightHandCandidates)
            {
                rightHandBone = FindBoneRecursive(visualTransform, name);
                if (rightHandBone != null) break;
            }

            // Tìm tay trái
            foreach (var name in leftArmCandidates)
            {
                leftArmBone = FindBoneRecursive(visualTransform, name);
                if (leftArmBone != null) break;
            }
            foreach (var name in leftForeArmCandidates)
            {
                leftForeArmBone = FindBoneRecursive(visualTransform, name);
                if (leftForeArmBone != null) break;
            }
            foreach (var name in leftHandCandidates)
            {
                leftHandBone = FindBoneRecursive(visualTransform, name);
                if (leftHandBone != null) break;
            }

            Debug.Log($"[PlayerAnimator] === KẾT QUẢ TÌM BONE ===");
            Debug.Log($"[PlayerAnimator] Tay phải - Arm: {(rightArmBone != null ? rightArmBone.name : "KHÔNG TÌM THẤY")}");
            Debug.Log($"[PlayerAnimator] Tay phải - ForeArm: {(rightForeArmBone != null ? rightForeArmBone.name : "KHÔNG TÌM THẤY")}");
            Debug.Log($"[PlayerAnimator] Tay phải - Hand: {(rightHandBone != null ? rightHandBone.name : "KHÔNG TÌM THẤY")}");
            Debug.Log($"[PlayerAnimator] Tay trái - Arm: {(leftArmBone != null ? leftArmBone.name : "KHÔNG TÌM THẤY")}");
            Debug.Log($"[PlayerAnimator] Tay trái - ForeArm: {(leftForeArmBone != null ? leftForeArmBone.name : "KHÔNG TÌM THẤY")}");
            Debug.Log($"[PlayerAnimator] Tay trái - Hand: {(leftHandBone != null ? leftHandBone.name : "KHÔNG TÌM THẤY")}");

            if (rightArmBone == null && rightForeArmBone == null && rightHandBone == null)
            {
                Debug.LogWarning("[PlayerAnimator] ⚠️ Không tìm thấy bone tay phải nào! Liệt kê toàn bộ bone hierarchy:");
                LogBoneHierarchy(visualTransform, 0);
            }
        }

        private void LogBoneHierarchy(Transform parent, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"[PlayerAnimator] {indent}{parent.name} (children: {parent.childCount})");
            if (depth < 5) // Giới hạn depth tránh log quá dài
            {
                foreach (Transform child in parent)
                {
                    LogBoneHierarchy(child, depth + 1);
                }
            }
        }

        private Transform FindBoneRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindBoneRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}

