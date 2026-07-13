using UnityEngine;

namespace GanhHangRong.Interaction
{
    public class InteractiveDoor : Interactable
    {
        [SerializeField] private float openAngle = 90f; // Target local Y angle when open
        [SerializeField] private float closeAngle = 0f; // Target local Y angle when closed
        [SerializeField] private float speed = 4f;       // Speed of opening/closing
        [SerializeField] private bool isOpen = false;

        private float targetAngle;

        private void Start()
        {
            // If starting open, set rotation accordingly
            targetAngle = isOpen ? openAngle : closeAngle;
            transform.localRotation = Quaternion.Euler(0, targetAngle, 0);
            
            UpdatePrompt();
        }

        private void Update()
        {
            // Smoothly rotate towards target angle
            float currentAngle = transform.localEulerAngles.y;
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, speed * 100f * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, nextAngle, 0);
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            ToggleDoor();
        }

        public void ToggleDoor()
        {
            isOpen = !isOpen;
            targetAngle = isOpen ? openAngle : closeAngle;
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            promptText = isOpen ? "Nhấn F hoặc Click để Đóng" : "Nhấn F hoặc Click để Mở";
        }
    }
}
