using UnityEngine;
using UnityEngine.InputSystem;

namespace GanhHangRong.Core
{
    /// <summary>
    /// GameManager — singleton điều khiển trạng thái tổng thể của game.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Trạng Thái Game")]
        [SerializeField] private GamePhase currentPhase = GamePhase.Playing;
        [SerializeField] private EmotionalLevel emotionalLevel = EmotionalLevel.Normal;
        [SerializeField] private int currentDay = 1;

        public GamePhase CurrentPhase => currentPhase;
        public EmotionalLevel CurrentEmotionalLevel => emotionalLevel;
        public int CurrentDay => currentDay;
        public bool IsPlaying => currentPhase == GamePhase.Playing;
        public bool IsPaused => currentPhase == GamePhase.Paused;

        private GamePhase previousPhase;

        protected override void OnSingletonAwake()
        {
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (currentPhase == GamePhase.Playing)
                    PauseGame();
                else if (currentPhase == GamePhase.Paused)
                    ResumeGame();
            }
        }

        public void SetGamePhase(GamePhase newPhase)
        {
            if (currentPhase == newPhase) return;
            previousPhase = currentPhase;
            currentPhase = newPhase;
            EventManager.TriggerGamePhaseChanged(newPhase);
        }

        public void PauseGame()
        {
            previousPhase = currentPhase;
            currentPhase = GamePhase.Paused;
            Time.timeScale = 0f;
            EventManager.TriggerGamePaused();
        }

        public void ResumeGame()
        {
            currentPhase = previousPhase != GamePhase.Paused ? previousPhase : GamePhase.Playing;
            Time.timeScale = 1f;
            EventManager.TriggerGameResumed();
        }

        public void SetEmotionalLevel(EmotionalLevel level)
        {
            if (emotionalLevel == level) return;
            emotionalLevel = level;
            EventManager.TriggerEmotionalLevelChanged(level);
        }

        public void AdvanceDay()
        {
            currentDay++;
            EventManager.TriggerNewDay();
        }

        public void StartChapter1()
        {
            SetGamePhase(GamePhase.Playing);
        }
    }
}
