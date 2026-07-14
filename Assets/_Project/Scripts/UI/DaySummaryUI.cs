using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("chapterProgressText")]
        [SerializeField] private TextMeshProUGUI dailyProgressText;
        [SerializeField] private Button continueButton;

        private TextMeshProUGUI continueButtonTmpText;
        private Text continueButtonLegacyText;

        private void Awake()
        {
            ConfigureLayout();

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
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ConfigureLayout();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (titleText != null && GameManager.HasInstance)
                titleText.text = $"TỔNG KẾT NGÀY {GameManager.Instance.CurrentDay}";

            var playerStats = FindAnyObjectByType<PlayerStats>();
            var ledger = Systems.DailyBusinessLedger.Instance != null
                ? Systems.DailyBusinessLedger.Instance
                : FindAnyObjectByType<Systems.DailyBusinessLedger>();
            if (playerStats != null)
            {
                if (customersServedText != null)
                {
                    customersServedText.text = ledger != null
                        ? $"Khách: {playerStats.CustomersServedToday} | Hài lòng: {ledger.HappyCustomers} | Bỏ đi: {ledger.LostCustomers}"
                        : $"Khách đã phục vụ: {playerStats.CustomersServedToday} người";
                }

                if (moneyEarnedText != null)
                {
                    moneyEarnedText.text = ledger != null
                        ? $"Doanh thu: {ledger.Revenue:N0} | Chi phí: {ledger.Expenses:N0} | Lợi nhuận: {ledger.Profit:N0} VNĐ"
                        : $"Doanh thu: {playerStats.MoneyEarnedToday:N0} VNĐ";
                }

                if (stressLevelText != null)
                {
                    float stressPerc = (playerStats.Stress / Constants.PLAYER_STRESS_MAX) * 100f;
                    stressLevelText.text = ledger != null
                        ? $"Bán chạy: {ledger.GetBestSellingDishName()} | Đánh giá: {ledger.Rating:F1}/5 | Căng thẳng: {stressPerc:F0}%"
                        : $"Mức căng thẳng: {stressPerc:F1}%";
                    if (stressPerc > 80f) stressLevelText.color = Color.red;
                    else if (stressPerc > 50f) stressLevelText.color = new Color(1f, 0.5f, 0f);
                    else stressLevelText.color = Color.white;
                }

                string progressText = $"Tổng tích lũy: {playerStats.TotalCustomersServed} khách | {playerStats.TotalMoneyEarned:N0} VNĐ";
                if (dailyProgressText != null)
                {
                    dailyProgressText.text = progressText;
                }
                else if (stressLevelText != null)
                {
                    stressLevelText.text += $"\n{progressText}";
                }
            }

            SetContinueButtonText("Ngày tiếp theo");
        }

        private void ConfigureLayout()
        {
            RectTransform root = transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }

            Image background = GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.025f, 0.03f, 0.035f, 0.94f);
                background.raycastTarget = true;
            }

            ConfigureText(titleText, new Vector2(0f, 188f), new Vector2(920f, 72f), 42f, 32f, FontStyles.Bold);
            ConfigureText(customersServedText, new Vector2(0f, 100f), new Vector2(940f, 52f), 26f, 20f);
            ConfigureText(moneyEarnedText, new Vector2(0f, 38f), new Vector2(940f, 58f), 26f, 20f);
            ConfigureText(stressLevelText, new Vector2(0f, -28f), new Vector2(940f, 58f), 25f, 19f);
            ConfigureText(dailyProgressText, new Vector2(0f, -100f), new Vector2(1000f, 66f), 23f, 18f, FontStyles.Bold);

            if (continueButton != null)
            {
                RectTransform buttonRect = continueButton.transform as RectTransform;
                if (buttonRect != null)
                {
                    buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                    buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                    buttonRect.pivot = new Vector2(0.5f, 0.5f);
                    buttonRect.anchoredPosition = new Vector2(0f, -195f);
                    buttonRect.sizeDelta = new Vector2(250f, 58f);
                }

                Image buttonImage = continueButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(0.24f, 0.64f, 0.39f, 1f);
                }
            }

            if (continueButtonTmpText != null)
            {
                continueButtonTmpText.enableAutoSizing = true;
                continueButtonTmpText.fontSizeMin = 18f;
                continueButtonTmpText.fontSizeMax = 24f;
                continueButtonTmpText.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void ConfigureText(
            TextMeshProUGUI text,
            Vector2 position,
            Vector2 size,
            float maxFontSize,
            float minFontSize,
            FontStyles fontStyle = FontStyles.Normal)
        {
            if (text == null) return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = minFontSize;
            text.fontSizeMax = maxFontSize;
            text.raycastTarget = false;
        }

        private void OnContinueClicked()
        {
            Hide();

            var loop = FindAnyObjectByType<Systems.GameplayLoop>();
            if (Systems.BusinessDayController.HasInstance &&
                Systems.BusinessDayController.Instance.IsManagingGameLoop)
            {
                Systems.BusinessDayController.Instance.StartNextDayFromSummary();
                return;
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

    }
}
