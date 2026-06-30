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
        private TextMeshProUGUI recipeTitleText;
        private RectTransform ingredientContainer;
        private System.Collections.Generic.List<GameObject> ingredientRows = new System.Collections.Generic.List<GameObject>();
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
            string[] names = { "ClockPanel", "ClockArt", "MoneyBoard", "InfoHUD", "GHR_TimeHUD" };
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform[] children = parentCanvas != null
                ? parentCanvas.GetComponentsInChildren<Transform>(true)
                : GetComponentsInChildren<Transform>(true);
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
            panel.sizeDelta = new Vector2(360f, 340f);

            recipeCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            Image bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.145f, 0.18f, 0.26f, 0.95f);
            bg.raycastTarget = false;

            recipeTitleText = CreateText("Title", panel, 24, FontStyles.Bold, TextAlignmentOptions.Left);
            recipeTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            recipeTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            recipeTitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            recipeTitleText.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            recipeTitleText.rectTransform.sizeDelta = new Vector2(-44f, 32f);
            recipeTitleText.color = new Color(1f, 0.93f, 0.75f, 1f);

            RectTransform separator = CreateRect("Separator", panel);
            separator.anchorMin = new Vector2(0f, 1f);
            separator.anchorMax = new Vector2(1f, 1f);
            separator.pivot = new Vector2(0.5f, 1f);
            separator.anchoredPosition = new Vector2(0f, -54f);
            separator.sizeDelta = new Vector2(-44f, 1.5f);
            Image sepImage = separator.gameObject.AddComponent<Image>();
            sepImage.color = new Color(0.3f, 0.4f, 0.5f, 0.5f);
            sepImage.raycastTarget = false;

            ingredientContainer = CreateRect("IngredientContainer", panel);
            ingredientContainer.anchorMin = new Vector2(0f, 0f);
            ingredientContainer.anchorMax = new Vector2(1f, 1f);
            ingredientContainer.offsetMin = new Vector2(22f, 18f);
            ingredientContainer.offsetMax = new Vector2(-22f, -65f);

            VerticalLayoutGroup vlg = ingredientContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 12f;
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
            if (recipeTitleText == null || !hasActiveOrder) return;

            recipeTitleText.text = "ÐON HÀNG: " + activeDrinkName;

            string recipeStr = ChapterOrderCatalog.GetOrderRecipe(activeDrinkId);
            string[] ingredients = ParseIngredients(recipeStr);

            for (int i = 0; i < ingredients.Length; i++)
            {
                if (i >= ingredientRows.Count)
                {
                    ingredientRows.Add(CreateIngredientRow());
                }
                ingredientRows[i].SetActive(true);
                PopulateIngredientRow(ingredientRows[i], ingredients[i]);
            }

            for (int i = ingredients.Length; i < ingredientRows.Count; i++)
            {
                ingredientRows[i].SetActive(false);
            }
        }

        private string[] ParseIngredients(string recipeString)
        {
            if (string.IsNullOrEmpty(recipeString)) return new string[0];
            string[] lines = recipeString.Split('\n');
            string firstLine = lines[0];
            if (firstLine.StartsWith("Công th?c: "))
            {
                firstLine = firstLine.Substring("Công th?c: ".Length);
            }
            if (firstLine.EndsWith("."))
            {
                firstLine = firstLine.Substring(0, firstLine.Length - 1);
            }
            return firstLine.Split(new string[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        private GameObject CreateIngredientRow()
        {
            RectTransform row = CreateRect("Row", ingredientContainer);
            row.sizeDelta = new Vector2(0f, 48f);

            RectTransform iconContainer = CreateRect("IconBg", row);
            iconContainer.anchorMin = new Vector2(0f, 0.5f);
            iconContainer.anchorMax = new Vector2(0f, 0.5f);
            iconContainer.pivot = new Vector2(0f, 0.5f);
            iconContainer.anchoredPosition = new Vector2(0f, 0f);
            iconContainer.sizeDelta = new Vector2(44f, 44f);
            Image iconBg = iconContainer.gameObject.AddComponent<Image>();
            iconBg.color = new Color(0.12f, 0.15f, 0.22f, 0.9f);

            RectTransform icon = CreateRect("Icon", iconContainer);
            icon.anchorMin = Vector2.zero;
            icon.anchorMax = Vector2.one;
            icon.offsetMin = new Vector2(6f, 6f);
            icon.offsetMax = new Vector2(-6f, -6f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI text = CreateText("Text", row, 24, FontStyles.Normal, TextAlignmentOptions.Left);
            text.rectTransform.anchorMin = new Vector2(0f, 0f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.offsetMin = new Vector2(60f, 0f);
            text.rectTransform.offsetMax = Vector2.zero;
            text.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            text.enableWordWrapping = false;

            return row.gameObject;
        }

        private void PopulateIngredientRow(GameObject row, string ingredientText)
        {
            Transform iconBg = row.transform.Find("IconBg");
            Image iconImage = iconBg.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI text = row.transform.Find("Text").GetComponent<TextMeshProUGUI>();

            string dispText = ingredientText;
            if (dispText.Length > 0)
            {
                dispText = char.ToUpper(dispText[0]) + dispText.Substring(1);
            }
            if (dispText.StartsWith("1 ly")) dispText = dispText.Replace("1 ly", "1 Ly");

            text.text = dispText;

            string lowerText = ingredientText.ToLower();
            string spriteName = null;
            if (lowerText.Contains("ly") || lowerText.Contains("c?c")) spriteName = "ly";
            else if (lowerText.Contains("cà phê") || lowerText.Contains("coffee")) spriteName = "caphe";
            else if (lowerText.Contains("trà")) spriteName = "tra";
            else if (lowerText.Contains("nu?c") || lowerText.Contains("sôi")) spriteName = "nuoc";
            else if (lowerText.Contains("dá") || lowerText.Contains("ice")) spriteName = "da";

            if (!string.IsNullOrEmpty(spriteName))
            {
                Sprite s = Resources.Load<Sprite>("RecipeIcons/" + spriteName);
                if (s != null)
                {
                    iconImage.sprite = s;
                    iconImage.color = Color.white;
                }
                else
                {
                    iconImage.color = new Color(1, 1, 1, 0);
                }
            }
            else
            {
                iconImage.color = new Color(1, 1, 1, 0);
            }
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
                runtimeMoneyText.text = string.Format("{0:N0} VNÐ", money);
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
                    runtimePeriodText.text = "Bu?i sáng";
                    break;
                case TimeOfDay.Afternoon:
                    runtimePeriodText.text = "Bu?i chi?u";
                    break;
                case TimeOfDay.Evening:
                    runtimePeriodText.text = "Chi?u t?i";
                    break;
                case TimeOfDay.Night:
                    runtimePeriodText.text = "Ðêm bán";
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
