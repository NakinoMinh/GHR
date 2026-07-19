using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GanhHangRong.Economy;

namespace GanhHangRong.UI
{
    public class ShopItemUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI selectedQuantityText;
        [SerializeField] private TextMeshProUGUI ownedAmountText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Buttons")]
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button buyButton;

        [Header("Settings")]
        [SerializeField] private int maxQuantity = 99;

        private ShopUIController controller;
        private ShopStockItem stockItem;
        private bool visualStyleApplied;
        private Button rowButton;

        public int MaxQuantity => Mathf.Max(1, maxQuantity);

        private void Awake()
        {
            WireButtons();
        }

        public void Initialize(ShopUIController owner, ShopStockItem stock)
        {
            controller = owner;
            stockItem = stock;
            WireButtons();
            ApplyVisualStyle();
            RefreshState();
        }

        public void RefreshState()
        {
            if (stockItem == null || stockItem.item == null)
            {
                return;
            }

            ApplyVisualStyle();
            ItemData item = stockItem.item;
            bool ownedBook = controller != null && controller.IsRecipeBookOwned(item);
            bool canAddToCart = !ownedBook && stockItem.stockAmount != 0;

            if (iconImage != null)
            {
                Sprite icon = item.icon != null ? item.icon : MarketItemIconLibrary.GetIcon(item.Id);
                iconImage.sprite = icon;
                iconImage.color = icon != null ? Color.white : new Color(0.22f, 0.34f, 0.29f, 1f);
                iconImage.preserveAspect = true;
            }

            if (nameText != null)
            {
                nameText.text = item.DisplayName;
            }

            if (priceText != null)
            {
                priceText.text = $"{Mathf.Max(0, stockItem.GetPrice()):N0} VNĐ";
            }

            if (ownedAmountText != null)
            {
                int owned = controller != null ? controller.GetOwnedAmount(item) : 0;
                ownedAmountText.text = item.IsBook
                    ? (ownedBook ? "ĐÃ SỞ HỮU" : "SÁCH CÔNG THỨC")
                    : $"Đang có: {owned}";
                ownedAmountText.color = ownedBook
                    ? new Color(0.42f, 0.82f, 0.57f)
                    : new Color(0.70f, 0.76f, 0.72f);
            }

            SetInteractable(increaseButton, canAddToCart);
            SetInteractable(buyButton, canAddToCart);
            SetInteractable(rowButton, canAddToCart);
        }

        public void AddToCart()
        {
            if (controller == null || stockItem == null)
            {
                Debug.LogWarning("ShopItemUI thiếu dữ liệu cửa hàng.", this);
                return;
            }

            controller.AddToCart(stockItem, 1, MaxQuantity);
        }

        private void WireButtons()
        {
            ResolveButtonReferences();

            if (increaseButton != null)
            {
                increaseButton.onClick.RemoveListener(AddToCart);
                increaseButton.onClick.AddListener(AddToCart);
            }

            if (buyButton != null && buyButton != increaseButton)
            {
                buyButton.onClick.RemoveListener(AddToCart);
                buyButton.onClick.AddListener(AddToCart);
            }

            rowButton = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            rowButton.targetGraphic = GetComponent<Image>();
            rowButton.onClick.RemoveListener(AddToCart);
            rowButton.onClick.AddListener(AddToCart);
        }

        private void ResolveButtonReferences()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null) continue;

                string buttonName = button.name.ToLowerInvariant();
                if (increaseButton == null && (buttonName == "+" || buttonName.Contains("plus") || buttonName.Contains("add")))
                {
                    increaseButton = button;
                }
                else if (decreaseButton == null && (buttonName == "-" || buttonName.Contains("minus") || buttonName.Contains("remove")))
                {
                    decreaseButton = button;
                }
                else if (buyButton == null && (buttonName.Contains("mua") || buttonName.Contains("buy")))
                {
                    buyButton = button;
                }
            }
        }

        private void ApplyVisualStyle()
        {
            if (visualStyleApplied)
            {
                return;
            }

            visualStyleApplied = true;
            Image background = GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.10f, 0.14f, 0.125f, 1f);
            }

            LayoutElement rootLayout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = 82f;
            rootLayout.minHeight = 82f;

            HorizontalLayoutGroup horizontal = GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
            {
                horizontal.padding = new RectOffset(10, 10, 8, 8);
                horizontal.spacing = 8f;
                horizontal.childControlWidth = false;
                horizontal.childControlHeight = true;
                horizontal.childForceExpandWidth = false;
                horizontal.childForceExpandHeight = true;
                horizontal.childAlignment = TextAnchor.MiddleLeft;
            }

            ConfigureElement(iconImage, 64f, 64f, false);
            ConfigureText(nameText, 170f, 18f, Color.white, true, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ConfigureText(priceText, 105f, 16f, new Color(1f, 0.79f, 0.37f), false, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            ConfigureText(ownedAmountText, 118f, 13f, new Color(0.70f, 0.76f, 0.72f), false, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

            if (selectedQuantityText != null) selectedQuantityText.gameObject.SetActive(false);
            if (decreaseButton != null) decreaseButton.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);

            Button primaryButton = buyButton != null ? buyButton : increaseButton;
            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(true);
            }
            if (increaseButton != null && increaseButton != primaryButton)
            {
                increaseButton.gameObject.SetActive(false);
            }

            if (primaryButton != null)
            {
                LayoutElement buttonLayout = primaryButton.GetComponent<LayoutElement>() ?? primaryButton.gameObject.AddComponent<LayoutElement>();
                buttonLayout.preferredWidth = 82f;
                buttonLayout.minWidth = 74f;
                Image buttonImage = primaryButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(0.13f, 0.55f, 0.36f, 1f);
                }

                TextMeshProUGUI buttonLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (buttonLabel != null)
                {
                    buttonLabel.text = "THÊM";
                    buttonLabel.fontSize = 14f;
                    buttonLabel.fontStyle = FontStyles.Bold;
                    buttonLabel.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private static void ConfigureElement(Graphic graphic, float width, float height, bool flexible)
        {
            if (graphic == null)
            {
                return;
            }

            LayoutElement layout = graphic.GetComponent<LayoutElement>() ?? graphic.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = flexible ? 60f : width;
            layout.preferredHeight = height;
            layout.flexibleWidth = flexible ? 1f : 0f;
        }

        private static void ConfigureText(TextMeshProUGUI text, float width, float size, Color color, bool flexible, TextAlignmentOptions alignment, FontStyles style)
        {
            if (text == null)
            {
                return;
            }

            LayoutElement layout = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = flexible ? 100f : Mathf.Min(width, 72f);
            layout.flexibleWidth = flexible ? 1f : 0f;

            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = size;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            if (text.font != null)
            {
                text.font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }
    }
}
