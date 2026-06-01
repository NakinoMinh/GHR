using UnityEngine;
using TMPro;
using GanhHangRong.Core;
using UnityEngine.UI;

namespace GanhHangRong.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Slider fatigueSlider;
        [SerializeField] private Slider iceLevelSlider;
        [SerializeField] private CanvasGroup hudCanvasGroup;

        private void OnEnable()
        {
            EventManager.OnMoneyChanged += UpdateMoney;
            EventManager.OnFatigueChanged += UpdateFatigue;
            EventManager.OnIceLevelChanged += UpdateIceLevel;
            EventManager.OnHourChanged += UpdateTime;
            EventManager.OnGamePhaseChanged += HandleGamePhaseChanged;
        }

        private void OnDisable()
        {
            EventManager.OnMoneyChanged -= UpdateMoney;
            EventManager.OnFatigueChanged -= UpdateFatigue;
            EventManager.OnIceLevelChanged -= UpdateIceLevel;
            EventManager.OnHourChanged -= UpdateTime;
            EventManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
        }

        private void Start()
        {
            var stats = FindAnyObjectByType<Player.PlayerStats>();
            if (stats != null)
            {
                UpdateMoney(stats.Money);
                UpdateFatigue(stats.Fatigue);
                UpdateIceLevel(stats.IceLevel);
            }
        }

        private void UpdateMoney(int money)
        {
            if (moneyText != null)
            {
                moneyText.text = $"{money:N0} VNĐ";
            }
        }

        private void UpdateFatigue(float fatigue)
        {
            if (fatigueSlider != null)
            {
                fatigueSlider.value = fatigue / Constants.PLAYER_FATIGUE_MAX;
            }
        }

        private void UpdateIceLevel(float ice)
        {
            if (iceLevelSlider != null)
            {
                iceLevelSlider.value = ice / Constants.ICE_MAX;
            }
        }

        private void UpdateTime(float hour)
        {
            if (timeText != null)
            {
                int h = Mathf.FloorToInt(hour);
                int m = Mathf.FloorToInt((hour - h) * 60f);
                timeText.text = $"{h:00}:{m:00}";
            }
        }

        private void HandleGamePhaseChanged(GamePhase phase)
        {
            if (hudCanvasGroup == null) return;

            if (phase == GamePhase.Playing)
            {
                hudCanvasGroup.alpha = 1f;
                hudCanvasGroup.interactable = true;
            }
            else
            {
                hudCanvasGroup.alpha = 0f;
                hudCanvasGroup.interactable = false;
            }
        }
    }
}
