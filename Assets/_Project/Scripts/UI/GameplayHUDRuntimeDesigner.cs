using GanhHangRong.Core;
using GanhHangRong.Economy;
using GanhHangRong.NPC;
using GanhHangRong.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GanhHangRong.UI
{
    public class GameplayHUDRuntimeDesigner : MonoBehaviour
    {
        private CanvasGroup recipeCanvasGroup;
        private TextMeshProUGUI recipeDrinkText;
        private TextMeshProUGUI recipeBodyText;
        private TextMeshProUGUI runtimeClockText;
        private TextMeshProUGUI runtimeDayText;
        private TextMeshProUGUI runtimePeriodText;
        private TextMeshProUGUI runtimeMoneyText;

        private bool isCartInteractionActive;
        private bool hasActiveOrder;
        private int activeDrinkId = -1;
        private string activeDrinkName = string.Empty;

        private void OnEnable()
        {
            EventManager.OnHourChanged += UpdateClock;
            EventManager.OnTimeOfDayChanged += UpdatePeriod;
            EventManager.OnNewDay += UpdateDay;
            EventManager.OnMoneyChanged += UpdateMoney;
            EventManager.OnCartInteractionChanged += HandleCartInteractionChanged;
            EventManager.OnCustomerOrderPlaced += HandleCustomerOrderPlaced;
            EventManager.OnCustomerOrderCleared += HandleCustomerOrderCleared;
        }

        private void OnDisable()
        {
            EventManager.OnHourChanged -= UpdateClock;
            EventManager.OnTimeOfDayChanged -= UpdatePeriod;
            EventManager.OnNewDay -= UpdateDay;
            EventManager.OnMoneyChanged -= UpdateMoney;
            EventManager.OnCartInteractionChanged -= HandleCartInteractionChanged;
            EventManager.OnCustomerOrderPlaced -= HandleCustomerOrderPlaced;
            EventManager.OnCustomerOrderCleared -= HandleCustomerOrderCleared;
        }

        private void Start()
        {
            TuneCanvasForCrispText();
            HideLegacyTopRightWidgets();
            BuildTopRightHud();
            BuildRecipePanel();
            InitializeValues();
            UpdateRecipeVisibility();
        }

        private void TuneCanvasForCrispText()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.pixelPerfect = true;
            }

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void HideLegacyTopRightWidgets()
        {
            string[] names = { "ClockPanel", "ClockArt", "MoneyBoard", "InfoHUD" };
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == null || child == transform) continue;
                for (int i = 0; i < names.Length; i++)
                {
                    if (child.name == names[i])
                    {
                        child.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        private void BuildTopRightHud()
        {
            RectTransform dock = CreateRect("Runtime_TimeMoneyHUD", transform);
            dock.anchorMin = new Vector2(1f, 1f);
            dock.anchorMax = new Vector2(1f, 1f);
            dock.pivot = new Vector2(1f, 1f);
            dock.anchoredPosition = new Vector2(-24f, -24f);
            dock.sizeDelta = new Vector2(260f, 118f);

            Image dockBg = dock.gameObject.AddComponent<Image>();
            dockBg.color = new Color(0.09f, 0.08f, 0.065f, 0.88f);
            dockBg.raycastTarget = false;

            RectTransform accent = CreateRect("Accent", dock);
            accent.anchorMin = new Vector2(0f, 1f);
            accent.anchorMax = new Vector2(1f, 1f);
            accent.pivot = new Vector2(0.5f, 1f);
            accent.anchoredPosition = Vector2.zero;
            accent.sizeDelta = new Vector2(0f, 5f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.95f, 0.55f, 0.18f, 0.95f);
            accentImage.raycastTarget = false;

            runtimeClockText = CreateText("Clock", dock, 38, FontStyles.Bold, TextAlignmentOptions.TopRight);
            runtimeClockText.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            runtimeClockText.rectTransform.anchorMax = new Vector2(1f, 1f);
            runtimeClockText.rectTransform.offsetMin = new Vector2(18f, 0f);
            runtimeClockText.rectTransform.offsetMax = new Vector2(-18f, -14f);
            runtimeClockText.color = new Color(1f, 0.91f, 0.72f, 1f);

            runtimeDayText = CreateText("Day", dock, 19, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
            runtimeDayText.rectTransform.anchorMin = new Vector2(0f, 0.1f);
            runtimeDayText.rectTransform.anchorMax = new Vector2(0.55f, 0.45f);
            runtimeDayText.rectTransform.offsetMin = new Vector2(18f, 10f);
            runtimeDayText.rectTransform.offsetMax = new Vector2(0f, 0f);
            runtimeDayText.color = new Color(0.88f, 0.98f, 0.95f, 1f);

            runtimePeriodText = CreateText("Period", dock, 18, FontStyles.Bold, TextAlignmentOptions.BottomRight);
            runtimePeriodText.rectTransform.anchorMin = new Vector2(0.45f, 0.1f);
            runtimePeriodText.rectTransform.anchorMax = new Vector2(1f, 0.45f);
            runtimePeriodText.rectTransform.offsetMin = Vector2.zero;
            runtimePeriodText.rectTransform.offsetMax = new Vector2(-18f, 0f);
            runtimePeriodText.color = new Color(0.74f, 0.9f, 1f, 1f);

            RectTransform moneyPanel = CreateRect("MoneyPanel", dock);
            moneyPanel.anchorMin = new Vector2(0f, 0f);
            moneyPanel.anchorMax = new Vector2(1f, 0f);
            moneyPanel.pivot = new Vector2(0.5f, 0f);
            moneyPanel.anchoredPosition = new Vector2(0f, -48f);
            moneyPanel.sizeDelta = new Vector2(0f, 38f);
            Image moneyBg = moneyPanel.gameObject.AddComponent<Image>();
            moneyBg.color = new Color(0.45f, 0.2f, 0.08f, 0.92f);
            moneyBg.raycastTarget = false;

            runtimeMoneyText = CreateText("Money", moneyPanel, 21, FontStyles.Bold, TextAlignmentOptions.Center);
            runtimeMoneyText.rectTransform.anchorMin = Vector2.zero;
            runtimeMoneyText.rectTransform.anchorMax = Vector2.one;
            runtimeMoneyText.rectTransform.offsetMin = new Vector2(14f, 2f);
            runtimeMoneyText.rectTransform.offsetMax = new Vector2(-14f, -2f);
            runtimeMoneyText.color = new Color(1f, 0.86f, 0.48f, 1f);
        }

        private void BuildRecipePanel()
        {
            RectTransform panel = CreateRect("Runtime_CustomerRecipePanel", transform);
            panel.anchorMin = new Vector2(0f, 0.5f);
            panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = new Vector2(28f, -20f);
            panel.sizeDelta = new Vector2(390f, 322f);

            recipeCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            Image bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.075f, 0.065f, 0.055f, 0.91f);
            bg.raycastTarget = false;

            RectTransform stripe = CreateRect("Stripe", panel);
            stripe.anchorMin = new Vector2(0f, 0f);
            stripe.anchorMax = new Vector2(0f, 1f);
            stripe.pivot = new Vector2(0f, 0.5f);
            stripe.anchoredPosition = Vector2.zero;
            stripe.sizeDelta = new Vector2(6f, 0f);
            Image stripeImage = stripe.gameObject.AddComponent<Image>();
            stripeImage.color = new Color(0.1f, 0.7f, 0.62f, 1f);
            stripeImage.raycastTarget = false;

            TextMeshProUGUI caption = CreateText("Caption", panel, 19, FontStyles.Bold, TextAlignmentOptions.Left);
            caption.rectTransform.anchorMin = new Vector2(0f, 1f);
            caption.rectTransform.anchorMax = new Vector2(1f, 1f);
            caption.rectTransform.pivot = new Vector2(0.5f, 1f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            caption.rectTransform.sizeDelta = new Vector2(-40f, 24f);
            caption.text = "YÊU CẦU CỦA KHÁCH";
            caption.color = new Color(0.95f, 0.6f, 0.25f, 1f);

            recipeDrinkText = CreateText("Drink", panel, 34, FontStyles.Bold, TextAlignmentOptions.Left);
            recipeDrinkText.rectTransform.anchorMin = new Vector2(0f, 1f);
            recipeDrinkText.rectTransform.anchorMax = new Vector2(1f, 1f);
            recipeDrinkText.rectTransform.pivot = new Vector2(0.5f, 1f);
            recipeDrinkText.rectTransform.anchoredPosition = new Vector2(0f, -46f);
            recipeDrinkText.rectTransform.sizeDelta = new Vector2(-40f, 38f);
            recipeDrinkText.color = new Color(1f, 0.93f, 0.75f, 1f);

            recipeBodyText = CreateText("Recipe", panel, 24, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            recipeBodyText.rectTransform.anchorMin = new Vector2(0f, 0f);
            recipeBodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            recipeBodyText.rectTransform.offsetMin = new Vector2(22f, 18f);
            recipeBodyText.rectTransform.offsetMax = new Vector2(-18f, -92f);
            recipeBodyText.color = new Color(1f, 0.97f, 0.88f, 1f);
            recipeBodyText.textWrappingMode = TextWrappingModes.Normal;
            recipeBodyText.lineSpacing = 7f;
        }

        private void InitializeValues()
        {
            PlayerStats stats = FindAnyObjectByType<PlayerStats>();
            if (stats != null) UpdateMoney(stats.Money);

            DayNightCycle cycle = FindAnyObjectByType<DayNightCycle>();
            if (cycle != null)
            {
                UpdateClock(cycle.CurrentHour);
                UpdatePeriod(cycle.CurrentTimeOfDay);
            }

            UpdateDay();
            RefreshOrderFromScene();
        }

        private void HandleCartInteractionChanged(bool active)
        {
            isCartInteractionActive = active;
            if (active) RefreshOrderFromScene();
            UpdateRecipeVisibility();
        }

        private void HandleCustomerOrderPlaced(int drinkId, string drinkName)
        {
            activeDrinkId = drinkId;
            activeDrinkName = drinkName;
            hasActiveOrder = true;
            UpdateRecipeContent();
            UpdateRecipeVisibility();
        }

        private void HandleCustomerOrderCleared()
        {
            hasActiveOrder = false;
            activeDrinkId = -1;
            activeDrinkName = string.Empty;
            UpdateRecipeVisibility();
        }

        private void RefreshOrderFromScene()
        {
            if (hasActiveOrder) return;

            NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsInactive.Exclude);
            foreach (NPCController npc in npcs)
            {
                if (npc != null && npc.CurrentState == NPCState.Waiting)
                {
                    activeDrinkId = npc.OrderedDrinkId;
                    activeDrinkName = npc.OrderedDrinkName;
                    hasActiveOrder = true;
                    UpdateRecipeContent();
                    return;
                }
            }
        }

        private void UpdateRecipeContent()
        {
            if (recipeDrinkText == null || recipeBodyText == null || !hasActiveOrder) return;

            recipeDrinkText.text = activeDrinkName;
            recipeBodyText.text = ChapterOrderCatalog.GetOrderRecipe(activeDrinkId);
        }

        private void UpdateRecipeVisibility()
        {
            if (recipeCanvasGroup == null) return;

            bool visible = isCartInteractionActive && hasActiveOrder;
            recipeCanvasGroup.alpha = visible ? 1f : 0f;
            recipeCanvasGroup.interactable = false;
            recipeCanvasGroup.blocksRaycasts = false;
        }

        private void UpdateMoney(int money)
        {
            if (runtimeMoneyText != null)
            {
                runtimeMoneyText.text = string.Format("{0:N0} VNĐ", money);
            }
        }

        private void UpdateClock(float hour)
        {
            if (runtimeClockText == null) return;
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60f);
            runtimeClockText.text = string.Format("{0:00}:{1:00}", h, m);
        }

        private void UpdateDay()
        {
            if (runtimeDayText != null && GameManager.HasInstance)
            {
                runtimeDayText.text = string.Format("Ngày {0}", GameManager.Instance.CurrentDay);
            }
        }

        private void UpdatePeriod(TimeOfDay timeOfDay)
        {
            if (runtimePeriodText == null) return;
            switch (timeOfDay)
            {
                case TimeOfDay.EarlyMorning:
                    runtimePeriodText.text = "Bình minh";
                    break;
                case TimeOfDay.Morning:
                    runtimePeriodText.text = "Buổi sáng";
                    break;
                case TimeOfDay.Afternoon:
                    runtimePeriodText.text = "Buổi chiều";
                    break;
                case TimeOfDay.Evening:
                    runtimePeriodText.text = "Chiều tối";
                    break;
                case TimeOfDay.Night:
                    runtimePeriodText.text = "Đêm bán";
                    break;
                default:
                    runtimePeriodText.text = "Khuya";
                    break;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.extraPadding = true;
            text.enableWordWrapping = false;
            text.outlineWidth = 0.12f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
            text.raycastTarget = false;
            return text;
        }
    }
}
