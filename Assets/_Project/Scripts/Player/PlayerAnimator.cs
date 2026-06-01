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

        private PlayerState currentState = PlayerState.Idle;
        private Vector3 originalPosition;
        private float animTimer;
        private Animator animator;
        private bool has3DAnimator = false;

        // Smooth animation blending
        private int currentAnimState = 0;
        private float currentSpeed = 0f;
        private float speedVelocity = 0f; // for SmoothDamp

        private void Start()
        {
            if (visualTransform == null)
            {
                if (transform.childCount > 0)
                    visualTransform = transform.GetChild(0);
                else
                    visualTransform = transform;
            }

            originalPosition = visualTransform.localPosition;
            animator = GetComponentInChildren<Animator>();
            
            // Kiểm tra xem có 3D Animator thật không
            has3DAnimator = (animator != null && animator.runtimeAnimatorController != null);
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

                // Căn chỉnh vị trí mô hình 3D khi ngồi
                if (currentState == PlayerState.Sitting)
                {
                    float breathe = Mathf.Sin(animTimer * 1f) * 0.01f;
                    visualTransform.localPosition = originalPosition + new Vector3(0, -0.35f + breathe, 0);
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
            visualTransform.localRotation = Quaternion.Euler(0, 0, lean);
        }

        private void AnimateServing()
        {
            float lean = Mathf.Sin(animTimer * 3f) * 3f;
            visualTransform.localRotation = Quaternion.Euler(0, 0, -5f + lean);
        }

        private void AnimateSitting()
        {
            float breathe = Mathf.Sin(animTimer * 1f) * 0.01f;
            visualTransform.localPosition = originalPosition + new Vector3(0, -0.35f + breathe, 0);
            visualTransform.localRotation = Quaternion.identity;
        }
    }
}

