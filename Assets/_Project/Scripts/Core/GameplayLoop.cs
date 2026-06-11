using UnityEngine;
using GanhHangRong.Core;
using GanhHangRong.Player;
using GanhHangRong.Economy;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Quản lý vòng lặp gameplay chính trong ngày.
    /// Từ chuẩn bị (Evening) -> Bán hàng (Night) -> Tổng kết (LateNight).
    /// </summary>
    public class GameplayLoop : MonoBehaviour
    {
        private bool hasShownSummary = false;
        private DayNightCycle dayNightCycle;

        private void Start()
        {
            dayNightCycle = FindAnyObjectByType<DayNightCycle>();
        }

        private void OnEnable()
        {
            EventManager.OnTimeOfDayChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            EventManager.OnTimeOfDayChanged -= HandleTimeChanged;
        }

        private void HandleTimeChanged(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Evening:
                    // Bắt đầu pha chuẩn bị (hiện tại mặc định là bắt đầu game lúc này)
                    hasShownSummary = false;
                    Debug.Log("[GameplayLoop] Pha Chuẩn Bị (17:00 - 20:00)");
                    break;
                    
                case TimeOfDay.Night:
                    // Bắt đầu pha bán hàng
                    Debug.Log("[GameplayLoop] Pha Bán Hàng (20:00 - 01:00)");
                    break;
                    
                case TimeOfDay.LateNight:
                    // Kết thúc ngày, hiện bảng tổng kết
                    if (!hasShownSummary)
                    {
                        Debug.Log("[GameplayLoop] Pha Tổng Kết (01:00 - 05:00)");
                        ShowDaySummary();
                        hasShownSummary = true;
                    }
                    break;
            }
        }

        private void ShowDaySummary()
        {
            GameManager.Instance.PauseGame(); // Tạm dừng game để xem tổng kết
            
            // Tìm và hiển thị UI Tổng kết
            var summaryUI = FindAnyObjectByType<UI.DaySummaryUI>(FindObjectsInactive.Include);
            if (summaryUI != null)
            {
                summaryUI.Show();
            }
            else
            {
                Debug.LogWarning("[GameplayLoop] Không tìm thấy DaySummaryUI trong scene!");
                // Nếu không có UI, tự động qua ngày mới luôn
                EndDaySummary();
            }
        }

        public void EndDaySummary()
        {
            hasShownSummary = false;
            GameManager.Instance.ResumeGame();
            if (dayNightCycle != null)
            {
                dayNightCycle.SkipToHour(17f); // Nhảy đến chiều tối ngày hôm sau
                int skippedCalendarDays = Mathf.Max(1, dayNightCycle.ConsumeCalendarDayOffset());
                for (int i = 0; i < skippedCalendarDays; i++)
                {
                    GameManager.Instance.AdvanceDay();
                }
                
                // Hồi phục hoàn toàn cho ngày mới
                var playerStats = FindAnyObjectByType<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.ModifyFatigue(-Constants.PLAYER_FATIGUE_MAX);
                    playerStats.ModifyStress(-Constants.PLAYER_STRESS_MAX);
                    playerStats.RefillIce(); // Tự động châm đá miễn phí tạm thời
                }
            }
        }
    }
}
