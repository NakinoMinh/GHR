using UnityEngine;
using System.Collections;
using GanhHangRong.Interaction;
using GanhHangRong.Player;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Cửa có thể tương tác: nhấn F (hoặc click chuột trái) để mở/đóng.
    /// Có animation xoay mượt mà.
    /// Khi cửa mở, collider vật lý của cánh cửa sẽ bị tắt để nhân vật đi xuyên qua được.
    /// </summary>
    public class DoorInteractable : Interactable
    {
        [Header("Cửa")]
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private bool isOpen = false;

        public void SetOpenAngle(float angle)
        {
            openAngle = angle;
        }

        [Header("Collider Settings")]
        [Tooltip("Collider vật lý NON-trigger của cánh cửa (sẽ tắt khi mở). Để trống = tự tìm.")]
        [SerializeField] private Collider doorPhysicsCollider;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine animationCoroutine;

        public bool IsOpen => isOpen;

        private void Start()
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);

            // Tự tìm collider nếu chưa gán thủ công
            if (doorPhysicsCollider == null)
            {
                foreach (var col in GetComponents<Collider>())
                {
                    if (!col.isTrigger)
                    {
                        doorPhysicsCollider = col;
                        break;
                    }
                }

                // Nếu không có non-trigger, lấy đại collider nào cũng được
                if (doorPhysicsCollider == null)
                {
                    doorPhysicsCollider = GetComponent<Collider>();
                }

                // Nếu không có trên this, tìm trong children
                if (doorPhysicsCollider == null)
                {
                    foreach (var col in GetComponentsInChildren<Collider>())
                    {
                        if (!col.isTrigger)
                        {
                            doorPhysicsCollider = col;
                            break;
                        }
                    }
                    if (doorPhysicsCollider == null)
                    {
                        doorPhysicsCollider = GetComponentInChildren<Collider>();
                    }
                }
            }

            ApplyDoorState(isOpen, false);
        }

        protected override void OnInteract(PlayerController player)
        {
            isOpen = !isOpen;
            ApplyDoorState(isOpen, true);
        }

        private void ApplyDoorState(bool open, bool animate)
        {
            Quaternion targetRotation = open ? openRotation : closedRotation;
            promptText = open ? "Nhấn F / Click để Đóng Cửa" : "Nhấn F / Click để Mở Cửa";

            // Tắt collider vật lý để đi xuyên qua, hoặc bật lại để cản đường
            if (doorPhysicsCollider != null)
            {
                doorPhysicsCollider.enabled = !open;
            }

            if (animate)
            {
                if (animationCoroutine != null)
                {
                    StopCoroutine(animationCoroutine);
                }
                animationCoroutine = StartCoroutine(AnimateDoor(targetRotation));
            }
            else
            {
                transform.localRotation = targetRotation;
            }
        }

        private IEnumerator AnimateDoor(Quaternion targetRot)
        {
            Quaternion startRot = transform.localRotation;
            float time = 0f;

            while (time < 1f)
            {
                time += Time.deltaTime / animationDuration;
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, time);
                yield return null;
            }

            transform.localRotation = targetRot;
        }
    }
}
