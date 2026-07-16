using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GanhHangRong.Economy;

namespace GanhHangRong.UI
{
    public class ShopCartItemUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI quantityText;

        [Header("Buttons")]
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;

        private ShopUIController controller;
        private ShopStockItem stockItem;

        private void Awake()
        {
            AutoWire();
            WireButtons();
        }

        public void Initialize(ShopUIController owner, ShopStockItem stock, int currentQuantity)
        {
            controller = owner;
            stockItem = stock;
            AutoWire();
            WireButtons();
            RefreshUI(currentQuantity);
        }

        public void AutoWire()
        {
            if (iconImage == null)
            {
                Transform icon = transform.Find("ItemIcon");
                iconImage = icon != null ? icon.GetComponent<Image>() : null;
            }

            if (nameText == null)
            {
                nameText = FindText("ItemName");
            }
            if (priceText == null)
            {
                priceText = FindText("PriceText");
            }
            if (quantityText == null)
            {
                quantityText = FindText("QuantityText");
            }
            if (increaseButton == null)
            {
                increaseButton = FindButton("PlusButton");
            }
            if (decreaseButton == null)
            {
                decreaseButton = FindButton("MinusButton");
            }

            if (iconImage == null || nameText == null || priceText == null || quantityText == null)
            {
                AutoWireLegacyNames();
            }
        }

        public void RefreshUI(int currentQuantity)
        {
            if (stockItem == null || stockItem.item == null)
            {
                return;
            }

            ItemData item = stockItem.item;
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
                int totalPrice = Mathf.Max(0, stockItem.GetPrice()) * currentQuantity;
                priceText.text = $"{totalPrice:N0} VNĐ";
            }
            if (quantityText != null)
            {
                quantityText.text = currentQuantity.ToString();
            }

            SetInteractable(increaseButton, !item.IsBook);
        }

        private void WireButtons()
        {
            if (increaseButton != null)
            {
                increaseButton.onClick.RemoveListener(OnIncreaseClicked);
                increaseButton.onClick.AddListener(OnIncreaseClicked);
            }
            if (decreaseButton != null)
            {
                decreaseButton.onClick.RemoveListener(OnDecreaseClicked);
                decreaseButton.onClick.AddListener(OnDecreaseClicked);
            }
        }

        private void OnIncreaseClicked()
        {
            if (controller != null && stockItem != null)
            {
                controller.AddToCart(stockItem, 1);
            }
        }

        private void OnDecreaseClicked()
        {
            if (controller != null && stockItem != null)
            {
                controller.RemoveFromCart(stockItem, 1);
            }
        }

        private TextMeshProUGUI FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private Button FindButton(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void AutoWireLegacyNames()
        {
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (iconImage == null && image != null && image.gameObject != gameObject && image.name.ToLowerInvariant().Contains("icon"))
                {
                    iconImage = image;
                }
            }

            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text == null) continue;
                string lowerName = text.name.ToLowerInvariant();
                if (nameText == null && lowerName.Contains("name")) nameText = text;
                else if (priceText == null && (lowerName.Contains("price") || lowerName.Contains("cost"))) priceText = text;
                else if (quantityText == null && (lowerName.Contains("qty") || lowerName.Contains("quantity") || lowerName.Contains("count"))) quantityText = text;
            }

            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                string lowerName = button.name.ToLowerInvariant();
                if (increaseButton == null && (lowerName.Contains("plus") || lowerName.Contains("inc") || lowerName.Contains("add"))) increaseButton = button;
                else if (decreaseButton == null && (lowerName.Contains("minus") || lowerName.Contains("dec") || lowerName.Contains("sub"))) decreaseButton = button;
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
