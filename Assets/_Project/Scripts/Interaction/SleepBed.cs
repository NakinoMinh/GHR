using UnityEngine;
using System.Collections;
using GanhHangRong.Core;
using GanhHangRong.Environment;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Gắn vào giường trong ngôi nhà.
    /// Nhân vật tương tác (F hoặc click) vào giường sau 12 giờ đêm → kết thúc ngày.
    /// </summary>
    public class SleepBed : Interactable
    {
        [Header("Giờ tối thiểu để có thể ngủ (24h)")]
        [SerializeField] private int sleepAvailableHour = 22; // Từ 22h trở đi

        [Header("Hiệu ứng mờ dần khi ngủ")]
        [SerializeField] private float fadeOutDuration = 1.5f;

        private void Start()
        {
            UpdatePrompt();
        }

        private void Update()
        {
            // Cập nhật prompt text theo thời gian
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            var timeManager = FindAnyObjectByType<TimeOfDayManager>();
            if (timeManager != null && timeManager.CurrentHour >= sleepAvailableHour)
            {
                promptText = "Nhấn F để Ngủ và bắt đầu ngày mới";
                canInteract = true;
            }
            else
            {
                int hour = timeManager != null ? timeManager.CurrentHour : 0;
                promptText = $"Chưa đến giờ ngủ ({hour:00}h / cần {sleepAvailableHour:00}h+)";
                canInteract = false;
            }
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            var timeManager = FindAnyObjectByType<TimeOfDayManager>();
            if (timeManager == null || timeManager.CurrentHour < sleepAvailableHour)
            {
                return;
            }

            StartCoroutine(SleepRoutine());
        }

        private IEnumerator SleepRoutine()
        {
            // Tắt di chuyển nhân vật
            var player = FindAnyObjectByType<Player.PlayerController>();
            if (player != null) player.DisableMovement();

            // Fade to black
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Kết thúc ngày → gọi GameplayLoop.EndDaySummary()
            var loop = FindAnyObjectByType<Systems.GameplayLoop>();
            if (loop != null)
            {
                loop.EndDaySummary();
            }
            else
            {
                // Fallback: tự advance day
                if (GameManager.HasInstance)
                    GameManager.Instance.AdvanceDay();
            }
            
            if (player != null) player.EnableMovement();
        }
    }
}
