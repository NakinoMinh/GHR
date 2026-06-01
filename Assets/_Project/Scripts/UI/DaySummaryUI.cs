using UnityEngine;
using TMPro;
using UnityEngine.UI;
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
        [SerializeField] private Button continueButton;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (titleText != null)
                titleText.text = $"Tổng Kết Ngày {GameManager.Instance.CurrentDay}";

            var playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                if (customersServedText != null)
                    customersServedText.text = $"Khách đã phục vụ: {playerStats.CustomersServedToday} người";
                
                if (moneyEarnedText != null)
                    moneyEarnedText.text = $"Doanh thu: {playerStats.MoneyEarnedToday:N0} VNĐ";
                
                if (stressLevelText != null)
                {
                    float stressPerc = (playerStats.Stress / Constants.PLAYER_STRESS_MAX) * 100f;
                    stressLevelText.text = $"Mức căng thẳng: {stressPerc:F1}%";
                    if (stressPerc > 80f) stressLevelText.color = Color.red;
                    else if (stressPerc > 50f) stressLevelText.color = new Color(1f, 0.5f, 0f);
                    else stressLevelText.color = Color.white;
                }
            }
        }

        private void OnContinueClicked()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            var loop = FindAnyObjectByType<Systems.GameplayLoop>();
            if (loop != null)
            {
                loop.EndDaySummary();
            }
        }
    }
}
