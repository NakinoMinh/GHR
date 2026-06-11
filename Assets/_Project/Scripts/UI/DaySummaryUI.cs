using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GanhHangRong.Player;
using GanhHangRong.Core;

namespace GanhHangRong.UI
{
    public class DaySummaryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI customersServedText;
        [SerializeField] private TextMeshProUGUI moneyEarnedText;
        [SerializeField] private TextMeshProUGUI stressLevelText;
        [SerializeField] private TextMeshProUGUI chapterProgressText;
        [SerializeField] private Button continueButton;

        private bool canAdvanceToChapter2 = false;
        private TextMeshProUGUI continueButtonTmpText;
        private Text continueButtonLegacyText;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButtonTmpText = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
                continueButtonLegacyText = continueButton.GetComponentInChildren<Text>(true);
            }

            Hide();
        }

        public void Show()
        {
            canAdvanceToChapter2 = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (titleText != null && GameManager.HasInstance)
                titleText.text = $"Tong Ket Ngay {GameManager.Instance.CurrentDay}";

            var playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                if (customersServedText != null)
                    customersServedText.text = $"Khach da phuc vu: {playerStats.CustomersServedToday} nguoi";

                if (moneyEarnedText != null)
                    moneyEarnedText.text = $"Doanh thu: {playerStats.MoneyEarnedToday:N0} VND";

                if (stressLevelText != null)
                {
                    float stressPerc = (playerStats.Stress / Constants.PLAYER_STRESS_MAX) * 100f;
                    stressLevelText.text = $"Muc cang thang: {stressPerc:F1}%";
                    if (stressPerc > 80f) stressLevelText.color = Color.red;
                    else if (stressPerc > 50f) stressLevelText.color = new Color(1f, 0.5f, 0f);
                    else stressLevelText.color = Color.white;
                }

                string progressText = ChapterProgression.GetChapter1ProgressText(playerStats);
                if (chapterProgressText != null)
                {
                    chapterProgressText.text = progressText;
                }
                else if (stressLevelText != null)
                {
                    stressLevelText.text += $"\n{progressText}";
                }

                canAdvanceToChapter2 = ChapterProgression.IsChapter1Complete(playerStats);
                if (canAdvanceToChapter2 && GameManager.HasInstance)
                    GameManager.Instance.MarkChapter1Completed();
            }

            SetContinueButtonText(canAdvanceToChapter2 ? "Bat dau Chuong 2" : "Ngay tiep theo");
        }

        private void OnContinueClicked()
        {
            Hide();

            var loop = FindAnyObjectByType<Systems.GameplayLoop>();
            if (canAdvanceToChapter2)
            {
                Time.timeScale = 1f;
                if (GameManager.HasInstance)
                    GameManager.Instance.StartChapter2();

                if (CanLoadChapter2(out string sceneToLoad))
                {
                    SceneManager.LoadScene(sceneToLoad);
                    return;
                }
            }

            if (loop != null)
                loop.EndDaySummary();
        }

        private void Hide()
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void SetContinueButtonText(string text)
        {
            if (continueButtonTmpText != null)
                continueButtonTmpText.text = text;
            else if (continueButtonLegacyText != null)
                continueButtonLegacyText.text = text;
        }

        private bool CanLoadChapter2(out string sceneToLoad)
        {
            sceneToLoad = Constants.CHAPTER2_SCENE_NAME;
            if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
                return true;

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(Constants.CHAPTER2_SCENE_PATH);
            if (buildIndex >= 0)
            {
                sceneToLoad = Constants.CHAPTER2_SCENE_PATH;
                return true;
            }

            return false;
        }
    }
}
