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
        [SerializeField] private int currentChapter = 1;
        [SerializeField] private bool chapter1Completed = false;

        public GamePhase CurrentPhase => currentPhase;
        public EmotionalLevel CurrentEmotionalLevel => emotionalLevel;
        public int CurrentDay => currentDay;
        public int CurrentChapter => currentChapter;
        public bool Chapter1Completed => chapter1Completed;
        public bool IsPlaying => currentPhase == GamePhase.Playing;
        public bool IsPaused => currentPhase == GamePhase.Paused;

        private GamePhase previousPhase;

        protected override void OnSingletonAwake()
        {
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
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
            if (currentPhase == GamePhase.Paused) return;
            previousPhase = currentPhase;
            currentPhase = GamePhase.Paused;
            Time.timeScale = 0f;
            EventManager.TriggerGamePhaseChanged(currentPhase);
            EventManager.TriggerGamePaused();
        }

        public void ResumeGame()
        {
            currentPhase = previousPhase != GamePhase.Paused ? previousPhase : GamePhase.Playing;
            Time.timeScale = 1f;
            EventManager.TriggerGamePhaseChanged(currentPhase);
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

        public void MarkChapter1Completed()
        {
            chapter1Completed = true;
        }

        public void StartChapter2()
        {
            currentChapter = 2;
            chapter1Completed = true;
            SetGamePhase(GamePhase.Playing);
        }

        public void RestoreProgress(int savedDay, int savedChapter, bool savedChapter1Completed)
        {
            currentDay = Mathf.Max(1, savedDay);
            currentChapter = Mathf.Clamp(savedChapter, 1, 4);
            chapter1Completed = savedChapter1Completed;
            EventManager.TriggerNewDay();
        }

        public void StartChapter1()
        {
            currentChapter = 1;
            SetGamePhase(GamePhase.Playing);
        }
    }
}
