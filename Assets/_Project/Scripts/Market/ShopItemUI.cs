using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GanhHangRong.Economy;

namespace GanhHangRong.UI
{
    public class ShopItemUI : MonoBehaviour, IPointerClickHandler
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

        private ItemData Item => stockItem != null ? stockItem.item : null;

        private const string VietnameseChars = "AĂÂÁẮẤÀẰẦẢẲẨÃẴẪẠẶẬEÊÉẾÈỀẺỂẼỄẸỆIÍÌỈĨỊOÔƠÓỐỚÒỒỜỎỔỞÕỖỠỌỘỢUƯÚỨÙỪỦỬŨỮỤỰYÝỲỶỸỴaăâáắấàằầảẩãẵẫạặậeêéếèềẻểẽễẹệiíìỉĩịoôơóốớòồờỏổởõỗỡọộợuưúứùừủửũữụựyýỳỷỹỵđĐ";

        private void Awake()
        {
            if (increaseButton != null)
            {
                increaseButton.onClick.RemoveAllListeners();
                increaseButton.onClick.AddListener(AddToCart);
            }

            // Đảm bảo click vào cả thẻ item cũng thêm vào giỏ hàng
            Button rootBtn = GetComponent<Button>();
            if (rootBtn != null)
            {
                rootBtn.onClick.RemoveAllListeners();
                rootBtn.onClick.AddListener(AddToCart);
            }
        }

        public void Initialize(ShopUIController owner, ShopStockItem stock)
        {
            controller = owner;
            stockItem = stock;

            // Nếu prefab chưa có nút +, tự động tạo một nút "+ Mua" bên phải
            if (increaseButton == null)
            {
                Transform existingBtn = transform.Find("AddButton_Runtime");
                if (existingBtn == null)
                {
                    GameObject btnGO = new GameObject("AddButton_Runtime", typeof(RectTransform));
                    btnGO.transform.SetParent(transform, false);
                    RectTransform rt = btnGO.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1, 0.5f);
                    rt.anchorMax = new Vector2(1, 0.5f);
                    rt.sizeDelta = new Vector2(70, 32);
                    rt.anchoredPosition = new Vector2(-45, 0);

                    Image img = btnGO.AddComponent<Image>();
                    img.color = new Color(0.15f, 0.55f, 0.25f);

                    Button btn = btnGO.AddComponent<Button>();
                    btn.onClick.AddListener(AddToCart);
                    increaseButton = btn;

                    GameObject txtGO = new GameObject("Text", typeof(RectTransform));
                    txtGO.transform.SetParent(btnGO.transform, false);
                    RectTransform txtRt = txtGO.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.offsetMin = Vector2.zero;
                    txtRt.offsetMax = Vector2.zero;

                    TextMeshProUGUI tmpTxt = txtGO.AddComponent<TextMeshProUGUI>();
                    tmpTxt.text = "+ Mua";
                    tmpTxt.fontSize = 16;
                    tmpTxt.alignment = TextAlignmentOptions.Center;
                    tmpTxt.color = Color.white;
                }
            }

            RefreshState();
        }

        public void RefreshState()
        {
            if (stockItem == null || stockItem.item == null) return;

            ItemData item = stockItem.item;
            bool isBook = item.IsBook;
            bool ownedBook = controller != null && controller.IsRecipeBookOwned(item);

            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t != null && t.font != null)
                {
                    t.font.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;
                }
            }

            if (iconImage != null && item.icon != null)
            {
                iconImage.sprite = item.icon;
            }

            if (nameText != null)
            {
                nameText.text = item.DisplayName;
            }

            if (priceText != null)
            {
                int price = Mathf.Max(0, stockItem.GetPrice());
                priceText.text = $"{price:N0} VND";
            }

            if (ownedAmountText != null)
            {
                int owned = controller != null ? controller.GetOwnedAmount(item) : 0;
                ownedAmountText.text = $"Còn: {owned}";
            }

            // Hide old single-item checkout controls
            if (selectedQuantityText != null) selectedQuantityText.gameObject.SetActive(false);
            if (decreaseButton != null) decreaseButton.gameObject.SetActive(false);
            if (buyButton != null) buyButton.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);

            SetInteractable(increaseButton, !ownedBook);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AddToCart();
        }

        public void AddToCart()
        {
            if (controller == null)
            {
                Debug.LogWarning("ShopItemUI thiếu ShopUIController.", this);
                return;
            }

            controller.AddToCart(stockItem, 1);
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
