using UnityEngine;
using System.Collections;
using GanhHangRong.Core;
using GanhHangRong.Environment;
using GanhHangRong.Systems;

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

        private TimeOfDayManager timeManager;
        private BusinessDayController businessDayController;

        private void Start()
        {
            timeManager = FindAnyObjectByType<TimeOfDayManager>();
            businessDayController = FindAnyObjectByType<BusinessDayController>();
            UpdatePrompt();
        }

        private void Update()
        {
            // Cập nhật prompt text theo thời gian
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            if (businessDayController == null)
            {
                businessDayController = FindAnyObjectByType<BusinessDayController>();
            }

            if (businessDayController != null && businessDayController.IsManagingGameLoop)
            {
                canInteract = businessDayController.CanSleep;
                switch (businessDayController.CurrentPhase)
                {
                    case BusinessDayPhase.Trading:
                        promptText = "Hãy đóng cửa quán trước khi ngủ";
                        break;
                    case BusinessDayPhase.Closing:
                        promptText = "Vẫn còn khách đang được phục vụ";
                        break;
                    case BusinessDayPhase.AfterHours:
                        promptText = "Nhấn F để ngủ và kết thúc ngày";
                        break;
                    default:
                        promptText = "Chưa đến lúc kết thúc ngày";
                        break;
                }
                return;
            }

            if (timeManager == null)
            {
                timeManager = FindAnyObjectByType<TimeOfDayManager>();
            }
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
            if (businessDayController != null && businessDayController.IsManagingGameLoop)
            {
                if (!businessDayController.CanSleep) return;
                StartCoroutine(SleepRoutine());
                return;
            }

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

            if (businessDayController != null && businessDayController.IsManagingGameLoop)
            {
                businessDayController.CompleteDayBySleeping();
                if (player != null) player.EnableMovement();
                yield break;
            }

            // Tương thích loop cũ ở các chapter chưa dùng BusinessDayController.
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
