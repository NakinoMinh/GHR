using System;
using UnityEngine;

namespace GanhHangRong.Core
{
    public static class EventManager
    {
        // GAME STATE
        public static event Action<GamePhase> OnGamePhaseChanged;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;

        public static void TriggerGamePhaseChanged(GamePhase phase) => OnGamePhaseChanged?.Invoke(phase);
        public static void TriggerGamePaused() => OnGamePaused?.Invoke();
        public static void TriggerGameResumed() => OnGameResumed?.Invoke();

        // THỜI GIAN
        public static event Action<TimeOfDay> OnTimeOfDayChanged;
        public static event Action<float> OnHourChanged;
        public static event Action OnNewDay;

        public static void TriggerTimeOfDayChanged(TimeOfDay time) => OnTimeOfDayChanged?.Invoke(time);
        public static void TriggerHourChanged(float hour) => OnHourChanged?.Invoke(hour);
        public static void TriggerNewDay() => OnNewDay?.Invoke();

        // THỜI TIẾT
        public static event Action<WeatherType> OnWeatherChanged;
        public static event Action<float> OnRainIntensityChanged;

        public static void TriggerWeatherChanged(WeatherType w) => OnWeatherChanged?.Invoke(w);
        public static void TriggerRainIntensityChanged(float i) => OnRainIntensityChanged?.Invoke(i);

        // KINH TẾ
        public static event Action<int> OnMoneyChanged;
        public static event Action<int> OnMoneyEarned;
        public static event Action<int> OnMoneySpent;

        public static void TriggerMoneyChanged(int m) => OnMoneyChanged?.Invoke(m);
        public static void TriggerMoneyEarned(int a) => OnMoneyEarned?.Invoke(a);
        public static void TriggerMoneySpent(int a) => OnMoneySpent?.Invoke(a);

        // KHÁCH HÀNG
        public static event Action<NPCType> OnCustomerArrived;
        public static event Action<NPCType> OnCustomerServed;
        public static event Action<NPCType> OnCustomerLeftHappy;
        public static event Action<NPCType> OnCustomerLeftSad;

        public static void TriggerCustomerArrived(NPCType t) => OnCustomerArrived?.Invoke(t);
        public static void TriggerCustomerServed(NPCType t) => OnCustomerServed?.Invoke(t);
        public static void TriggerCustomerLeftHappy(NPCType t) => OnCustomerLeftHappy?.Invoke(t);
        public static void TriggerCustomerLeftSad(NPCType t) => OnCustomerLeftSad?.Invoke(t);

        // PLAYER
        public static event Action<PlayerState> OnPlayerStateChanged;
        public static event Action<float> OnFatigueChanged;
        public static event Action<float> OnIceLevelChanged;

        public static void TriggerPlayerStateChanged(PlayerState s) => OnPlayerStateChanged?.Invoke(s);
        public static void TriggerFatigueChanged(float f) => OnFatigueChanged?.Invoke(f);
        public static void TriggerIceLevelChanged(float i) => OnIceLevelChanged?.Invoke(i);

        // STRESS
        public static event Action<float> OnStressChanged;
        public static void TriggerStressChanged(float s) => OnStressChanged?.Invoke(s);

        // ĐỐI THOẠI
        public static event Action OnDialogueStarted;
        public static event Action OnDialogueEnded;
        public static event Action<string, string, Sprite> OnDialogueLine;

        public static void TriggerDialogueStarted() => OnDialogueStarted?.Invoke();
        public static void TriggerDialogueEnded() => OnDialogueEnded?.Invoke();
        public static void TriggerDialogueLine(string speaker, string text, Sprite avatar = null) => OnDialogueLine?.Invoke(speaker, text, avatar);

        // CẢM XÚC
        public static event Action<EmotionalLevel> OnEmotionalLevelChanged;
        public static void TriggerEmotionalLevelChanged(EmotionalLevel l) => OnEmotionalLevelChanged?.Invoke(l);

        // TƯƠNG TÁC
        public static event Action<string> OnInteractionPromptShow;
        public static event Action OnInteractionPromptHide;

        public static void TriggerInteractionPromptShow(string t) => OnInteractionPromptShow?.Invoke(t);
        public static void TriggerInteractionPromptHide() => OnInteractionPromptHide?.Invoke();

        public static void ClearAll()
        {
            OnGamePhaseChanged = null; OnGamePaused = null; OnGameResumed = null;
            OnTimeOfDayChanged = null; OnHourChanged = null; OnNewDay = null;
            OnWeatherChanged = null; OnRainIntensityChanged = null;
            OnMoneyChanged = null; OnMoneyEarned = null; OnMoneySpent = null;
            OnCustomerArrived = null; OnCustomerServed = null;
            OnCustomerLeftHappy = null; OnCustomerLeftSad = null;
            OnPlayerStateChanged = null; OnFatigueChanged = null; OnIceLevelChanged = null;
            OnStressChanged = null;
            OnDialogueStarted = null; OnDialogueEnded = null; OnDialogueLine = null;
            OnEmotionalLevelChanged = null;
            OnInteractionPromptShow = null; OnInteractionPromptHide = null;
        }
    }
}
