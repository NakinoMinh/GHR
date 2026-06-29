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
        private int selectedQuantity = 1;

        private ItemData Item => stockItem != null ? stockItem.item : null;

        private void Awake()
        {
            if (increaseButton != null)
            {
                increaseButton.onClick.AddListener(IncreaseQuantity);
            }

            if (decreaseButton != null)
            {
                decreaseButton.onClick.AddListener(DecreaseQuantity);
            }

            if (buyButton != null)
            {
                buyButton.onClick.AddListener(Buy);
            }
        }

        public void Initialize(ShopUIController owner, ShopStockItem stock)
        {
            controller = owner;
            stockItem = stock;
            selectedQuantity = 1;
            RefreshState();
        }

        public void IncreaseQuantity()
        {
            if (Item != null && Item.IsBook)
            {
                selectedQuantity = 1;
            }
            else
            {
                selectedQuantity = Mathf.Clamp(selectedQuantity + 1, 1, Mathf.Max(1, maxQuantity));
            }

            RefreshState();
        }

        public void DecreaseQuantity()
        {
            selectedQuantity = Mathf.Max(1, selectedQuantity - 1);
            RefreshState();
        }

        public void Buy()
        {
            if (controller == null)
            {
                Debug.LogWarning("ShopItemUI thiếu ShopUIController.", this);
                return;
            }

            controller.TryBuyItem(stockItem, selectedQuantity);
        }

        public void RefreshState()
        {
            ItemData item = Item;
            if (item == null)
            {
                SetText(nameText, "Item chưa cấu hình");
                SetText(priceText, "0 VND");
                SetText(selectedQuantityText, "x1");
                SetText(ownedAmountText, "Đang có: 0");
                SetText(statusText, "Thiếu ItemData");
                SetInteractable(buyButton, false);
                SetInteractable(increaseButton, false);
                SetInteractable(decreaseButton, false);
                return;
            }

            bool isBook = item.IsBook;
            bool ownedBook = isBook && controller != null && controller.IsRecipeBookOwned(item);
            int ownedAmount = controller != null ? controller.GetOwnedAmount(item) : 0;

            if (isBook)
            {
                selectedQuantity = 1;
            }

            SetIcon(item.icon);
            SetText(nameText, item.DisplayName);
            SetText(priceText, $"{stockItem.GetPrice():N0} VND");
            SetText(selectedQuantityText, $"x{selectedQuantity}");
            SetText(ownedAmountText, $"Đang có: {ownedAmount}");

            if (ownedBook)
            {
                SetText(statusText, "Đã sở hữu");
            }
            else if (isBook)
            {
                SetText(statusText, "Sách công thức");
            }
            else
            {
                SetText(statusText, string.Empty);
            }

            SetInteractable(buyButton, !ownedBook);
            SetInteractable(increaseButton, !isBook);
            SetInteractable(decreaseButton, !isBook && selectedQuantity > 1);
        }

        private void SetIcon(Sprite icon)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
