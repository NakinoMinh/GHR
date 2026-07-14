using System;
using GanhHangRong.Core;
using GanhHangRong.Economy;
using UnityEngine;

namespace GanhHangRong.Environment
{
    public enum TimePeriod
    {
        EarlyMorning,
        Noon,
        Afternoon,
        Evening,
        Night
    }

    /// <summary>
    /// Adapter thời gian cho UI và môi trường. Khi có DayNightCycle, lớp này không tự chạy
    /// đồng hồ mà đồng bộ từ nguồn gameplay duy nhất đó.
    /// </summary>
    public class TimeOfDayManager : MonoBehaviour
    {
        [Header("Thoi gian bat dau")]
        [SerializeField, Range(0, 23)] private int startHour = 6;
        [SerializeField, Range(0, 59)] private int startMinute;
        [SerializeField, Min(1)] private int startDay = 1;

        [Header("Toc do thoi gian du phong")]
        [SerializeField, Min(0f)] private float timeScale = 2f;
        [SerializeField] private bool runTime = true;
        [SerializeField] private DayNightCycle gameTimeSource;

        [Header("Moc chia thoi diem")]
        [SerializeField, Range(0, 23)] private int earlyMorningStartHour = 5;
        [SerializeField, Range(0, 23)] private int noonStartHour = 9;
        [SerializeField, Range(0, 23)] private int afternoonStartHour = 15;
        [SerializeField, Range(0, 23)] private int eveningStartHour = 18;
        [SerializeField, Range(0, 23)] private int nightStartHour = 21;

        public event Action<int> DayChanged;
        public event Action<int, int, int> TimeChanged;
        public event Action<TimePeriod> PeriodChanged;

        private float currentMinuteOfDay;
        private int currentDay;
        private TimePeriod currentPeriod;

        public int CurrentHour => Mathf.FloorToInt(currentMinuteOfDay / 60f);
        public int CurrentMinute => Mathf.FloorToInt(currentMinuteOfDay % 60f);
        public int CurrentDay => currentDay;
        public float CurrentMinuteOfDay => currentMinuteOfDay;
        public float NormalizedTime => currentMinuteOfDay / 1440f;
        public TimePeriod CurrentPeriod => currentPeriod;
        public bool IsRunning => gameTimeSource != null ? gameTimeSource.IsClockRunning : runTime;

        private void Awake()
        {
            ResetToStartTime();
            ResolveGameTimeSource();
        }

        private void OnEnable()
        {
            EventManager.OnHourChanged += HandleGameHourChanged;
            EventManager.OnNewDay += HandleNewDay;
        }

        private void Start()
        {
            ResolveGameTimeSource();
            if (gameTimeSource != null)
            {
                SyncFromGameClock(gameTimeSource.CurrentHour, true);
            }
        }

        private void OnDisable()
        {
            EventManager.OnHourChanged -= HandleGameHourChanged;
            EventManager.OnNewDay -= HandleNewDay;
        }

        private void Update()
        {
            if (gameTimeSource == null)
            {
                ResolveGameTimeSource();
            }

            if (gameTimeSource == null && runTime && timeScale > 0f)
            {
                AdvanceFallbackTime(Time.deltaTime * timeScale);
            }
        }

        public void ResetToStartTime()
        {
            currentDay = Mathf.Max(1, startDay);
            currentMinuteOfDay = Mathf.Clamp(startHour, 0, 23) * 60f + Mathf.Clamp(startMinute, 0, 59);
            currentPeriod = GetCurrentPeriod();
            NotifyTimeChanged(true);
        }

        public void SetRunning(bool shouldRun)
        {
            runTime = shouldRun;
            if (gameTimeSource != null)
            {
                gameTimeSource.SetRunning(shouldRun);
            }
        }

        public void SetTime(int hour, int minute)
        {
            float targetHour = Mathf.Clamp(hour, 0, 23) + Mathf.Clamp(minute, 0, 59) / 60f;
            if (gameTimeSource != null)
            {
                gameTimeSource.SkipToHour(targetHour);
                return;
            }

            currentMinuteOfDay = targetHour * 60f;
            NotifyTimeChanged(true);
        }

        public void SetDay(int day)
        {
            int nextDay = Mathf.Max(1, day);
            if (nextDay != currentDay)
            {
                currentDay = nextDay;
                DayChanged?.Invoke(currentDay);
            }
            NotifyTimeChanged(false);
        }

        public void AdvanceTime(float gameMinutes)
        {
            if (gameMinutes <= 0f) return;
            if (gameTimeSource != null)
            {
                gameTimeSource.AdvanceMinutes(gameMinutes);
                return;
            }

            AdvanceFallbackTime(gameMinutes);
        }

        public TimePeriod GetCurrentPeriod()
        {
            return GetPeriodForHour(CurrentHour);
        }

        public TimePeriod GetPeriodForHour(int hour)
        {
            hour = Mathf.Clamp(hour, 0, 23);
            if (IsHourInRange(hour, earlyMorningStartHour, noonStartHour)) return TimePeriod.EarlyMorning;
            if (IsHourInRange(hour, noonStartHour, afternoonStartHour)) return TimePeriod.Noon;
            if (IsHourInRange(hour, afternoonStartHour, eveningStartHour)) return TimePeriod.Afternoon;
            if (IsHourInRange(hour, eveningStartHour, nightStartHour)) return TimePeriod.Evening;
            return TimePeriod.Night;
        }

        public string GetCurrentPeriodName()
        {
            return GetVietnamesePeriodName(currentPeriod);
        }

        private void ResolveGameTimeSource()
        {
            if (gameTimeSource == null)
            {
                gameTimeSource = FindAnyObjectByType<DayNightCycle>();
            }
        }

        private void HandleGameHourChanged(float hour)
        {
            SyncFromGameClock(hour, false);
        }

        private void HandleNewDay()
        {
            int day = GameManager.HasInstance ? GameManager.Instance.CurrentDay : currentDay + 1;
            if (day != currentDay)
            {
                currentDay = day;
                DayChanged?.Invoke(currentDay);
            }
            NotifyTimeChanged(false);
        }

        private void SyncFromGameClock(float hour, bool forcePeriodEvent)
        {
            currentMinuteOfDay = Mathf.Repeat(hour, 24f) * 60f;
            int day = GameManager.HasInstance ? GameManager.Instance.CurrentDay : currentDay;
            if (day != currentDay)
            {
                currentDay = day;
                DayChanged?.Invoke(currentDay);
            }
            NotifyTimeChanged(forcePeriodEvent);
        }

        private void AdvanceFallbackTime(float gameMinutes)
        {
            currentMinuteOfDay += gameMinutes;
            while (currentMinuteOfDay >= 1440f)
            {
                currentMinuteOfDay -= 1440f;
                currentDay++;
                DayChanged?.Invoke(currentDay);
            }
            NotifyTimeChanged(false);
        }

        private void NotifyTimeChanged(bool forcePeriodEvent)
        {
            TimePeriod newPeriod = GetCurrentPeriod();
            if (forcePeriodEvent || newPeriod != currentPeriod)
            {
                currentPeriod = newPeriod;
                PeriodChanged?.Invoke(currentPeriod);
            }
            TimeChanged?.Invoke(currentDay, CurrentHour, CurrentMinute);
        }

        private static bool IsHourInRange(int hour, int startHour, int endHour)
        {
            return startHour < endHour
                ? hour >= startHour && hour < endHour
                : hour >= startHour || hour < endHour;
        }

        public static string GetVietnamesePeriodName(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning: return "Sáng";
                case TimePeriod.Noon: return "Trưa";
                case TimePeriod.Afternoon: return "Chiều";
                case TimePeriod.Evening: return "Tối";
                default: return "Đêm";
            }
        }
    }
}
