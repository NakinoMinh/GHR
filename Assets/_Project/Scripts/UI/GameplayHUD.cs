using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GanhHangRong.Core;
using GanhHangRong.Player;

namespace GanhHangRong.UI
{
    /// <summary>
    /// Quản lý giao diện HUD (Avatar, Thanh năng lượng/stress, Inventory, Thời gian, Tiền).
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Avatar & Vitals")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Slider energySlider;  // Năng lượng (Fatigue đảo ngược)
        [SerializeField] private Slider stressSlider;  // Căng thẳng

        [Header("Inventory (6 Slots)")]
        [SerializeField] private TextMeshProUGUI teaCountText;
        [SerializeField] private TextMeshProUGUI sugarCountText;
        [SerializeField] private TextMeshProUGUI cupCountText;
        [SerializeField] private TextMeshProUGUI iceLevelText;

        [Header("Top Right Info")]
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private Image timeIconImage; // Icon hiển thị buổi (sáng/trưa/tối)
        
        [Header("Canvas Group")]
        [SerializeField] private CanvasGroup canvasGroup;

        private PlayerStats playerStats;

        private void OnEnable()
        {
            EventManager.OnMoneyChanged += UpdateMoney;
            EventManager.OnFatigueChanged += UpdateFatigue;
            EventManager.OnStressChanged += UpdateStress;
            EventManager.OnIceLevelChanged += UpdateIceLevel;
            EventManager.OnHourChanged += UpdateClock;
            EventManager.OnTimeOfDayChanged += UpdateTimeIcon;
            EventManager.OnNewDay += UpdateDay;
            EventManager.OnGamePhaseChanged += HandleGamePhaseChanged;
        }

        private void OnDisable()
        {
            EventManager.OnMoneyChanged -= UpdateMoney;
            EventManager.OnFatigueChanged -= UpdateFatigue;
            EventManager.OnStressChanged -= UpdateStress;
            EventManager.OnIceLevelChanged -= UpdateIceLevel;
            EventManager.OnHourChanged -= UpdateClock;
            EventManager.OnTimeOfDayChanged -= UpdateTimeIcon;
            EventManager.OnNewDay -= UpdateDay;
            EventManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
        }

        private void Start()
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                UpdateMoney(playerStats.Money);
                UpdateFatigue(playerStats.Fatigue);
                UpdateStress(playerStats.Stress);
                UpdateIceLevel(playerStats.IceLevel);
                UpdateInventory(); // Update các items còn lại
            }

            if (GameManager.HasInstance)
            {
                UpdateDay();
            }

            if (playerNameText != null) playerNameText.text = "Hoàng Hôn";
        }

        private void Update()
        {
            // Trong game thực tế có thể dùng Event khi inventory đổi, 
            // tạm thời dùng Update để refetch số lượng nếu nó thay đổi (vì chưa làm event riêng cho item)
            if (Time.frameCount % 30 == 0 && playerStats != null) 
            {
                UpdateInventory();
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
            if (energySlider != null)
            {
                // Năng lượng = 1 - (Mệt mỏi / Max)
                energySlider.value = 1f - (fatigue / Constants.PLAYER_FATIGUE_MAX);
            }
        }

        private void UpdateStress(float stress)
        {
            if (stressSlider != null)
            {
                stressSlider.value = stress / Constants.PLAYER_STRESS_MAX;
            }
        }

        private void UpdateIceLevel(float ice)
        {
            if (iceLevelText != null)
            {
                iceLevelText.text = $"ĐÁ:\n{Mathf.RoundToInt((ice / Constants.ICE_MAX) * 100)}%";
            }
        }

        private void UpdateInventory()
        {
            if (teaCountText != null) teaCountText.text = $"TRÀ:\n{playerStats.TeaSupply}";
            if (sugarCountText != null) sugarCountText.text = $"ĐƯỜNG:\n{playerStats.SugarSupply}";
            if (cupCountText != null) cupCountText.text = $"LY:\n{playerStats.CupSupply}";
        }

        private void UpdateClock(float hour)
        {
            if (clockText != null)
            {
                int h = Mathf.FloorToInt(hour);
                int m = Mathf.FloorToInt((hour - h) * 60f);
                clockText.text = $"{h:00}:{m:00}";
            }
        }

        private void UpdateTimeIcon(TimeOfDay timeOfDay)
        {
            // Nếu có icon thật thì gán ở đây, tạm thời ta có thể dùng Color để nhận biết hoặc bỏ qua
            if (timeIconImage != null)
            {
                switch (timeOfDay)
                {
                    case TimeOfDay.EarlyMorning: timeIconImage.color = new Color(1f, 0.7f, 0.4f); break;
                    case TimeOfDay.Morning: timeIconImage.color = Color.yellow; break;
                    case TimeOfDay.Afternoon: timeIconImage.color = Color.white; break;
                    case TimeOfDay.Evening: timeIconImage.color = new Color(1f, 0.4f, 0.2f); break;
                    case TimeOfDay.Night: timeIconImage.color = new Color(0.2f, 0.2f, 0.5f); break;
                    case TimeOfDay.LateNight: timeIconImage.color = new Color(0.1f, 0.1f, 0.3f); break;
                }
            }
        }

        private void UpdateDay()
        {
            if (dayText != null && GameManager.HasInstance)
            {
                dayText.text = $"Ngày {GameManager.Instance.CurrentDay}";
            }
        }

        private void HandleGamePhaseChanged(GamePhase phase)
        {
            if (canvasGroup == null) return;

            if (phase == GamePhase.Playing)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
