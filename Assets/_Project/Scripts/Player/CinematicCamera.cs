using UnityEngine;
using GanhHangRong.Core;
using UnityEngine.InputSystem;

namespace GanhHangRong.Player
{
    /// <summary>
    /// Camera góc nhìn thứ ba (Third Person Orbit Follow Camera) xoay bằng chuột.
    /// </summary>
    public class CinematicCamera : MonoBehaviour
    {
        [Header("Mục Tiêu")]
        [SerializeField] private Transform target;

        [Header("Theo Dõi")]
        [SerializeField] private float smoothTime = Constants.CAMERA_SMOOTH_TIME;
        [SerializeField] private float yOffset = Constants.CAMERA_Y_OFFSET;
        [SerializeField] private float zOffset = Constants.CAMERA_Z_OFFSET;

        [Header("Over The Shoulder Settings")]
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.7f, 0.15f, 0f);

        [Header("Third Person Orbit Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private float distance = 3f;

        private float yaw = 0f;
        private float pitch = 12f;
        private Vector3 currentVelocity = Vector3.zero;

        private void Start()
        {
            if (target != null)
            {
                // Lấy góc quay ban đầu của camera
                yaw = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Xoay camera bằng chuột khi game đang chơi và chuột đang khóa
            if (GameManager.Instance.IsPlaying && Cursor.lockState == CursorLockMode.Locked)
            {
                if (Mouse.current != null)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    yaw += mouseDelta.x * mouseSensitivity * 0.1f;
                    pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
                    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                }
            }

            // Điểm nhìn của camera (phía trên vị trí nhân vật một chút)
            Vector3 targetLookAt = target.position + Vector3.up * yOffset;

            // Tính toán rotation và vị trí mục tiêu mới với shoulder offset
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 targetPosition = targetLookAt + (targetRotation * shoulderOffset) - (targetRotation * Vector3.forward * distance);

            // Nội suy mượt mà vị trí và góc quay của camera
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                yaw = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
                SnapToTarget();
            }
        }

        // Tương thích ngược với các lời gọi cũ
        public void SetBounds(float min, float max) { }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 targetLookAt = target.position + Vector3.up * yOffset;
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = targetLookAt + (targetRotation * shoulderOffset) - (targetRotation * Vector3.forward * distance);
            transform.rotation = targetRotation;
            currentVelocity = Vector3.zero;
        }
    }
}
