using System;
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

    public class TimeOfDayManager : MonoBehaviour
    {
        [Header("Thoi gian bat dau")]
        [SerializeField, Range(0, 23)] private int startHour = 7;
        [SerializeField, Range(0, 59)] private int startMinute = 0;
        [SerializeField, Min(1)] private int startDay = 1;

        [Header("Toc do thoi gian")]
        [Tooltip("So phut trong game troi qua moi giay that. Vi du 2 = moi giay that tang 2 phut game.")]
        [SerializeField, Min(0f)] private float timeScale = 2f;
        [SerializeField] private bool runTime = true;

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
        public bool IsRunning => runTime;

        private void Awake()
        {
            ResetToStartTime();
        }

        private void Update()
        {
            if (!runTime || timeScale <= 0f)
            {
                return;
            }

            // Cong thoi gian bang phut game de de can chinh toc do trong Inspector.
            AdvanceTime(Time.deltaTime * timeScale);
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
        }

        public void SetTime(int hour, int minute)
        {
            currentMinuteOfDay = Mathf.Clamp(hour, 0, 23) * 60f + Mathf.Clamp(minute, 0, 59);
            NotifyTimeChanged(true);
        }

        public void SetDay(int day)
        {
            currentDay = Mathf.Max(1, day);
            DayChanged?.Invoke(currentDay);
            NotifyTimeChanged(false);
        }

        public void AdvanceTime(float gameMinutes)
        {
            if (gameMinutes <= 0f)
            {
                return;
            }

            currentMinuteOfDay += gameMinutes;
            while (currentMinuteOfDay >= 1440f)
            {
                currentMinuteOfDay -= 1440f;
                currentDay++;
                DayChanged?.Invoke(currentDay);
            }

            NotifyTimeChanged(false);
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
                case TimePeriod.EarlyMorning:
                    return "Sang";
                case TimePeriod.Noon:
                    return "Trua";
                case TimePeriod.Afternoon:
                    return "Chieu";
                case TimePeriod.Evening:
                    return "Toi";
                default:
                    return "Dem";
            }
        }
    }
}
