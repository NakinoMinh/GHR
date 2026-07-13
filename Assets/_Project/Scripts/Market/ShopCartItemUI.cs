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
            if (increaseButton != null)
            {
                increaseButton.onClick.AddListener(OnIncreaseClicked);
            }
            if (decreaseButton != null)
            {
                decreaseButton.onClick.AddListener(OnDecreaseClicked);
            }
        }

        public void Initialize(ShopUIController owner, ShopStockItem stock, int currentQuantity)
        {
            controller = owner;
            stockItem = stock;
            AutoWire();
            RefreshUI(currentQuantity);
        }

        public void AutoWire()
        {
            if (iconImage == null)
            {
                Image[] imgs = GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img != null && img.gameObject != this.gameObject && (img.name.Contains("Icon") || img.name.Contains("icon")))
                    {
                        iconImage = img;
                        break;
                    }
                }
            }

            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp == null) continue;
                if (tmp.font != null) tmp.font.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;

                string n = tmp.name.ToLowerInvariant();
                if (nameText == null && (n.Contains("name") || n.Contains("title"))) nameText = tmp;
                else if (priceText == null && (n.Contains("price") || n.Contains("cost") || tmp.text.Contains("VND"))) priceText = tmp;
                else if (quantityText == null && (n.Contains("qty") || n.Contains("quantity") || n.Contains("amount") || n.Contains("count"))) quantityText = tmp;
            }

            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var btn in btns)
            {
                if (btn == null) continue;
                TextMeshProUGUI btnTxt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                string txt = btnTxt != null ? btnTxt.text.Trim() : "";
                string bName = btn.name.ToLowerInvariant();

                if (increaseButton == null && (txt == "+" || bName.Contains("inc") || bName.Contains("add") || bName.Contains("plus")))
                {
                    increaseButton = btn;
                    increaseButton.onClick.RemoveAllListeners();
                    increaseButton.onClick.AddListener(OnIncreaseClicked);
                }
                else if (decreaseButton == null && (txt == "-" || bName.Contains("dec") || bName.Contains("sub") || bName.Contains("minus")))
                {
                    decreaseButton = btn;
                    decreaseButton.onClick.RemoveAllListeners();
                    decreaseButton.onClick.AddListener(OnDecreaseClicked);
                }
            }
        }

        public void RefreshUI(int currentQuantity)
        {
            if (stockItem == null || stockItem.item == null) return;
            ItemData item = stockItem.item;

            if (iconImage != null && item.icon != null)
            {
                iconImage.sprite = item.icon;
            }

            if (nameText != null)
            {
                nameText.text = $"{currentQuantity}x {item.DisplayName}";
            }

            if (priceText != null)
            {
                int totalPrice = Mathf.Max(0, stockItem.GetPrice()) * currentQuantity;
                priceText.text = $"{totalPrice:N0} VND";
            }

            if (quantityText != null)
            {
                quantityText.text = currentQuantity.ToString();
            }

            // Books can only have quantity 1
            bool isBook = item.IsBook;
            SetInteractable(increaseButton, !isBook);
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

        private void SetInteractable(Button btn, bool interactable)
        {
            if (btn != null)
            {
                btn.interactable = interactable;
            }
        }
    }
}
