using TMPro;
using UnityEngine;

namespace GanhHangRong.Environment
{
    public class TimeUIController : MonoBehaviour
    {
        [Header("Nguon thoi gian")]
        [SerializeField] private TimeOfDayManager timeManager;

        [Header("Text hien thi")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI periodText;

        private bool warnedMissingTimeManager;

        private void Reset()
        {
            timeManager = FindAnyObjectByType<TimeOfDayManager>();
        }

        private void OnEnable()
        {
            if (timeManager == null)
            {
                timeManager = FindAnyObjectByType<TimeOfDayManager>();
            }

            if (timeManager == null)
            {
                if (!warnedMissingTimeManager)
                {
                    Debug.LogWarning($"{nameof(TimeUIController)} chua co TimeOfDayManager de hien thi UI gio/ngay.", this);
                    warnedMissingTimeManager = true;
                }

                return;
            }

            timeManager.TimeChanged += HandleTimeChanged;
            timeManager.DayChanged += HandleDayChanged;
            timeManager.PeriodChanged += HandlePeriodChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (timeManager == null)
            {
                return;
            }

            timeManager.TimeChanged -= HandleTimeChanged;
            timeManager.DayChanged -= HandleDayChanged;
            timeManager.PeriodChanged -= HandlePeriodChanged;
        }

        public void Refresh()
        {
            if (timeManager == null)
            {
                return;
            }

            SetTime(timeManager.CurrentHour, timeManager.CurrentMinute);
            SetDay(timeManager.CurrentDay);
            SetPeriod(timeManager.CurrentPeriod);
        }

        private void HandleTimeChanged(int day, int hour, int minute)
        {
            SetTime(hour, minute);
            SetDay(day);
        }

        private void HandleDayChanged(int day)
        {
            SetDay(day);
        }

        private void HandlePeriodChanged(TimePeriod period)
        {
            SetPeriod(period);
        }

        private void SetTime(int hour, int minute)
        {
            if (timeText != null)
            {
                timeText.text = $"{hour:00}:{minute:00}";
            }
        }

        private void SetDay(int day)
        {
            if (dayText != null)
            {
                dayText.text = $"Ngay {day}";
            }
        }

        private void SetPeriod(TimePeriod period)
        {
            if (periodText != null)
            {
                periodText.text = TimeOfDayManager.GetVietnamesePeriodName(period);
            }
        }
    }
}
