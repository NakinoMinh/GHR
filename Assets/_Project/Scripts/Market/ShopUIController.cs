using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using GanhHangRong.Economy;
using GanhHangRong.Player;

namespace GanhHangRong.UI
{
    public class ShopUIController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private RecipeUnlockManager recipeUnlockManager;
        [SerializeField] private SimplePlayerControlLock playerControlLock;
        [SerializeField] private bool autoFindMissingReferences = true;

        [Header("Panel")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private TextMeshProUGUI shopNameText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI transactionMessageText;
        [SerializeField] private Button closeButton;

        [Header("Item list")]
        [SerializeField] private Transform itemListContent;
        [SerializeField] private ShopItemUI itemUIPrefab;

        [Header("Shopping Cart")]
        [SerializeField] private Transform cartListContent;
        [SerializeField] private ShopCartItemUI cartItemPrefab;
        [SerializeField] private TextMeshProUGUI totalPriceText;
        [SerializeField] private Button checkoutButton;
        [SerializeField] private Button clearCartButton;

        private readonly Dictionary<ShopStockItem, int> shoppingCart = new Dictionary<ShopStockItem, int>();
        private readonly List<ShopCartItemUI> spawnedCartItems = new List<ShopCartItemUI>();

        private readonly List<ShopItemUI> spawnedItems = new List<ShopItemUI>();
        private ShopData currentShop;
        private bool isOpen;
        private TextMeshProUGUI cartCountText;
        private TextMeshProUGUI cartEmptyText;
        private CanvasGroup shopCanvasGroup;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            ResolveReferences();
            EnsureEventSystemReady();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }
            if (checkoutButton != null)
            {
                checkoutButton.onClick.AddListener(Checkout);
            }
            if (clearCartButton != null)
            {
                clearCartButton.onClick.AddListener(ClearCart);
            }

            SetPanelVisible(false);
            UpdateMoneyText();
            SetMessage(string.Empty);
        }

        private void OnEnable()
        {
            ShopInteractable.ShopOpenRequested += OpenShop;

            if (playerInventory != null)
            {
                playerInventory.MoneyChanged += HandleMoneyChanged;
                playerInventory.InventoryChanged += RefreshItemRows;
            }

            if (recipeUnlockManager != null)
            {
                recipeUnlockManager.RecipeUnlocked += HandleRecipeUnlocked;
            }
        }

        private void OnDisable()
        {
            ShopInteractable.ShopOpenRequested -= OpenShop;

            if (playerInventory != null)
            {
                playerInventory.MoneyChanged -= HandleMoneyChanged;
                playerInventory.InventoryChanged -= RefreshItemRows;
            }

            if (recipeUnlockManager != null)
            {
                recipeUnlockManager.RecipeUnlocked -= HandleRecipeUnlocked;
            }
        }

        private void Update()
        {
            if (isOpen && WasClosePressed())
            {
                CloseShop();
            }
        }

        public void OpenShop(ShopOpenRequest request)
        {
            if (request == null)
            {
                Debug.LogWarning("Không mở shop vì ShopOpenRequest bị null.", this);
                return;
            }

            OpenShop(request.ShopData, request.Source);
        }

        public void OpenShop(ShopData shopData, ShopInteractable source)
        {
            OpenShop(shopData);
        }

        public void OpenShop(ShopData shopData)
        {
            ResolveReferences();
            EnsureEventSystemReady();
            EnsureMarketLayoutRuntime();

            if (shopData == null)
            {
                Debug.LogWarning("Không mở shop vì ShopData bị null.", this);
                return;
            }

            if (shopPanel == null)
            {
                Debug.LogWarning("ShopUIController thiếu Shop Panel.", this);
                return;
            }

            currentShop = shopData;
            if (currentShop != null && (currentShop.name.Contains("tap_hoa") || currentShop.DisplayName.Contains("Tạp Hóa")))
            {
                Systems.ShopRuntimeSetup.EnsureTapHoaItems(currentShop);
            }
            isOpen = true;
            SetPanelVisible(true);
            SetMessage(string.Empty);

            if (shopNameText != null)
            {
                shopNameText.text = currentShop.DisplayName;
            }
            else
            {
                Debug.LogWarning("ShopUIController thiếu ShopNameText.", this);
            }

            UpdateMoneyText();
            RebuildItemList();
            ClearCart();

            if (playerControlLock != null)
            {
                playerControlLock.LockControls();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void CloseShop()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            currentShop = null;
            SetMessage(string.Empty);
            SetPanelVisible(false);
            ClearItemList();
            ClearCart();

            if (playerControlLock != null)
            {
                playerControlLock.UnlockControls();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public bool TryBuyItem(ShopStockItem stockItem, int quantity)
        {
            ResolveReferences();

            if (stockItem == null || stockItem.item == null)
            {
                SetMessage("Món hàng chưa được cấu hình.");
                return false;
            }

            if (playerInventory == null)
            {
                SetMessage("Thiếu PlayerInventory trong scene.");
                Debug.LogWarning("ShopUIController không tìm thấy PlayerInventory.", this);
                return false;
            }

            ItemData item = stockItem.item;
            int safeQuantity = Mathf.Max(1, quantity);

            if (item.IsBook)
            {
                safeQuantity = 1;
                if (IsRecipeBookOwned(item))
                {
                    SetMessage("Bạn đã sở hữu sách này.");
                    RefreshItemRows();
                    return false;
                }

                if (recipeUnlockManager == null)
                {
                    SetMessage("Thiếu RecipeUnlockManager trong scene.");
                    Debug.LogWarning("Không thể mua sách vì thiếu RecipeUnlockManager.", this);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(item.recipeIdToUnlock))
                {
                    SetMessage("Sách này chưa gán recipe cần mở khóa.");
                    return false;
                }
            }

            int unitPrice = Mathf.Max(0, stockItem.GetPrice());
            int totalPrice = unitPrice * safeQuantity;
            if (!playerInventory.SpendMoney(totalPrice))
            {
                SetMessage("Số tiền hàng lớn hơn tiền hiện có, không thể mua!");
                return false;
            }

            playerInventory.AddItem(item, safeQuantity);

            if (item.IsBook)
            {
                bool unlocked = recipeUnlockManager.UnlockRecipe(item.recipeIdToUnlock);
                string unlockedText = unlocked
                    ? $"Đã mở khóa công thức: {item.recipeIdToUnlock}"
                    : "Bạn đã sở hữu sách này.";

                SetMessage(unlockedText);
            }
            else
            {
                SetMessage($"Đã mua thành công: {item.DisplayName} x{safeQuantity}.");
            }

            playerInventory.SaveData();
            UpdateMoneyText();
            RefreshItemRows();
            return true;
        }

        public bool IsRecipeBookOwned(ItemData item)
        {
            if (item == null || !item.IsBook)
            {
                return false;
            }

            bool hasBookInInventory = playerInventory != null && playerInventory.HasItem(item.Id);
            bool hasUnlockedRecipe = recipeUnlockManager != null
                && !string.IsNullOrWhiteSpace(item.recipeIdToUnlock)
                && recipeUnlockManager.IsRecipeUnlocked(item.recipeIdToUnlock);

            return hasBookInInventory || hasUnlockedRecipe;
        }

        public int GetOwnedAmount(ItemData item)
        {
            if (playerInventory == null || item == null)
            {
                return 0;
            }

            return playerInventory.GetItemAmount(item);
        }

        private void RebuildItemList()
        {
            ClearItemList();

            if (currentShop == null)
            {
                return;
            }

            if (itemListContent == null || itemUIPrefab == null)
            {
                Debug.LogWarning("ShopUIController thiếu ItemListContent hoặc ItemUIPrefab.", this);
                SetMessage("Thiếu cấu hình danh sách hàng.");
                return;
            }

            UnityEngine.UI.ContentSizeFitter csf = itemListContent.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf != null) csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;

            RectTransform contentRt = itemListContent.GetComponent<RectTransform>();
            if (contentRt != null)
            {
                contentRt.anchorMin = new Vector2(0, 1);
                contentRt.anchorMax = new Vector2(1, 1);
                contentRt.sizeDelta = new Vector2(0, contentRt.sizeDelta.y);
            }

            UnityEngine.UI.VerticalLayoutGroup vlg = itemListContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
            }

            foreach (ShopStockItem stockItem in currentShop.itemsForSale)
            {
                if (stockItem == null || stockItem.item == null)
                {
                    continue;
                }

                ShopItemUI row = Instantiate(itemUIPrefab, itemListContent);
                row.gameObject.SetActive(true);
                row.Initialize(this, stockItem);
                spawnedItems.Add(row);
            }
        }

        private void ClearItemList()
        {
            foreach (ShopItemUI row in spawnedItems)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            spawnedItems.Clear();
        }

        private void RefreshItemRows()
        {
            UpdateMoneyText();

            foreach (ShopItemUI row in spawnedItems)
            {
                if (row != null)
                {
                    row.RefreshState();
                }
            }
        }

        private void HandleMoneyChanged(int money)
        {
            UpdateMoneyText();
        }

        private void HandleRecipeUnlocked(string recipeId)
        {
            RefreshItemRows();
        }

        private void UpdateMoneyText()
        {
            if (moneyText == null)
            {
                return;
            }

            int money = playerInventory != null ? playerInventory.CurrentMoney : 0;
            moneyText.text = $"TIỀN MẶT  •  {money:N0} VNĐ";
            UpdateCheckoutState();
        }

        private void SetMessage(string message)
        {
            if (transactionMessageText != null)
            {
                transactionMessageText.text = message;
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (shopPanel != null)
            {
                if (shopCanvasGroup == null || shopCanvasGroup.gameObject != shopPanel)
                {
                    shopCanvasGroup = shopPanel.GetComponent<CanvasGroup>();
                }

                // Unity's destroyed-object references are not CLR null, so the null-coalescing
                // operator can retain a missing component instead of creating a replacement.
                if (shopCanvasGroup == null)
                {
                    shopCanvasGroup = shopPanel.AddComponent<CanvasGroup>();
                }

                shopCanvasGroup.alpha = visible ? 1f : 0f;
                shopCanvasGroup.interactable = visible;
                shopCanvasGroup.blocksRaycasts = visible;
                shopPanel.SetActive(visible);
            }
        }

        public void AddToCart(ShopStockItem stockItem, int quantity, int maxQuantity = 99)
        {
            if (stockItem == null || stockItem.item == null || quantity <= 0) return;

            if (stockItem.stockAmount == 0)
            {
                SetMessage("Mặt hàng này hiện đã hết.");
                return;
            }
            
            if (stockItem.item.IsBook)
            {
                if (IsRecipeBookOwned(stockItem.item))
                {
                    SetMessage("Bạn đã sở hữu sách này.");
                    return;
                }
                shoppingCart[stockItem] = 1; // Books max 1
            }
            else
            {
                int currentQuantity = shoppingCart.TryGetValue(stockItem, out int current) ? current : 0;
                int stockLimit = stockItem.stockAmount >= 0 ? stockItem.stockAmount : int.MaxValue;
                int safeLimit = Mathf.Min(Mathf.Max(1, maxQuantity), stockLimit);
                shoppingCart[stockItem] = Mathf.Clamp(currentQuantity + quantity, 1, safeLimit);
            }
            
            RefreshCartUI();
            SetMessage($"Đã thêm {stockItem.item.DisplayName} vào giỏ hàng.");
        }

        public void RemoveFromCart(ShopStockItem stockItem, int quantity)
        {
            if (stockItem == null || !shoppingCart.ContainsKey(stockItem)) return;

            shoppingCart[stockItem] -= quantity;
            if (shoppingCart[stockItem] <= 0)
            {
                shoppingCart.Remove(stockItem);
            }

            RefreshCartUI();
        }

        public void ClearCart()
        {
            shoppingCart.Clear();
            RefreshCartUI();
        }

        public void Checkout()
        {
            if (shoppingCart.Count == 0)
            {
                SetMessage("Giỏ hàng đang trống.");
                return;
            }

            ResolveReferences();
            if (playerInventory == null)
            {
                SetMessage("Lỗi: Không tìm thấy túi đồ.");
                return;
            }

            foreach (var entry in shoppingCart)
            {
                ItemData item = entry.Key != null ? entry.Key.item : null;
                if (item == null)
                {
                    SetMessage("Giỏ hàng có món chưa được cấu hình.");
                    return;
                }

                if (!item.IsBook)
                {
                    continue;
                }

                if (recipeUnlockManager == null)
                {
                    SetMessage("Không thể mua sách: thiếu RecipeUnlockManager trong scene.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(item.recipeIdToUnlock))
                {
                    SetMessage("Sách công thức chưa được gán món cần mở khóa.");
                    return;
                }

                if (IsRecipeBookOwned(item))
                {
                    SetMessage("Bạn đã sở hữu một sách công thức trong giỏ.");
                    RefreshItemRows();
                    return;
                }
            }

            int totalPrice = 0;
            foreach (var kvp in shoppingCart)
            {
                totalPrice += Mathf.Max(0, kvp.Key.GetPrice()) * kvp.Value;
            }

            if (!playerInventory.SpendMoney(totalPrice))
            {
                SetMessage("Bạn không có đủ tiền để thanh toán!");
                return;
            }

            // Grant items
            int totalItems = 0;
            foreach (var kvp in shoppingCart)
            {
                ItemData item = kvp.Key.item;
                int qty = kvp.Value;
                playerInventory.AddItem(item, qty);
                totalItems += qty;

                if (item.IsBook && recipeUnlockManager != null)
                {
                    recipeUnlockManager.UnlockRecipe(item.recipeIdToUnlock);
                }
            }

            playerInventory.SaveData();
            UpdateMoneyText();
            SetMessage($"Thanh toán thành công {totalItems} món. Tổng: {totalPrice:N0} VND.");
            
            shoppingCart.Clear();
            RefreshCartUI();
            RefreshItemRows();
        }

        private void RefreshCartUI()
        {
            foreach (var row in spawnedCartItems)
            {
                if (row != null) Destroy(row.gameObject);
            }
            spawnedCartItems.Clear();

            if (cartListContent == null || cartItemPrefab == null) return;

            int totalPrice = 0;
            foreach (var kvp in shoppingCart)
            {
                ShopCartItemUI row = Instantiate(cartItemPrefab, cartListContent);
                row.gameObject.SetActive(true);
                row.Initialize(this, kvp.Key, kvp.Value);
                spawnedCartItems.Add(row);
                
                totalPrice += Mathf.Max(0, kvp.Key.GetPrice()) * kvp.Value;
            }

            if (totalPriceText != null)
            {
                totalPriceText.text = $"{totalPrice:N0} VNĐ";
            }

            int itemCount = 0;
            foreach (int quantity in shoppingCart.Values)
            {
                itemCount += quantity;
            }

            if (cartCountText != null)
            {
                cartCountText.text = $"GIỎ HÀNG  •  {itemCount} MÓN";
            }
            if (cartEmptyText != null)
            {
                cartEmptyText.gameObject.SetActive(shoppingCart.Count == 0);
            }

            UpdateCheckoutState();
        }

        private void EnsureMarketLayoutRuntime()
        {
            ShopUIRuntimeLayoutBuilder.Layout layout = ShopUIRuntimeLayoutBuilder.Build(
                shopPanel,
                itemListContent,
                shopNameText,
                moneyText,
                transactionMessageText,
                closeButton);

            if (layout == null)
            {
                return;
            }

            cartListContent = layout.CartListContent;
            cartItemPrefab = layout.CartItemPrefab;
            totalPriceText = layout.TotalPriceText;
            cartCountText = layout.CartCountText;
            cartEmptyText = layout.CartEmptyText;
            checkoutButton = layout.CheckoutButton;
            clearCartButton = layout.ClearCartButton;

            checkoutButton.onClick.RemoveAllListeners();
            checkoutButton.onClick.AddListener(Checkout);
            clearCartButton.onClick.RemoveAllListeners();
            clearCartButton.onClick.AddListener(ClearCart);
        }

        private void UpdateCheckoutState()
        {
            int total = 0;
            foreach (var entry in shoppingCart)
            {
                total += Mathf.Max(0, entry.Key.GetPrice()) * entry.Value;
            }

            bool hasItems = shoppingCart.Count > 0;
            bool canAfford = playerInventory != null && playerInventory.CurrentMoney >= total;
            if (checkoutButton != null)
            {
                checkoutButton.interactable = hasItems && canAfford;
            }
            if (clearCartButton != null)
            {
                clearCartButton.interactable = hasItems;
            }
        }

        private void EnsureDualColumnCartLayoutRuntime()
        {
            if (shopPanel == null) return;

            // 1. Ensure Vietnamese fonts use Dynamic mode
            TextMeshProUGUI[] allTexts = shopPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTexts)
            {
                if (t != null && t.font != null)
                {
                    t.font.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;
                }
            }

            // 2. Locate left scroll view
            ScrollRect leftScroll = itemListContent != null
                ? itemListContent.GetComponentInParent<ScrollRect>(true)
                : shopPanel.GetComponentInChildren<ScrollRect>(true);

            if (leftScroll != null)
            {
                RectTransform leftRt = leftScroll.GetComponent<RectTransform>();
                leftRt.anchorMin = new Vector2(0.03f, 0.12f);
                leftRt.anchorMax = new Vector2(0.48f, 0.82f);
                leftRt.offsetMin = Vector2.zero;
                leftRt.offsetMax = Vector2.zero;
                leftScroll.gameObject.SetActive(true);

                CreateRuntimeText(shopPanel.transform, "Header_Left", "CỬA HÀNG (AVAILABLE ITEMS)", new Vector2(0.03f, 0.83f), new Vector2(0.48f, 0.90f), 18, TextAlignmentOptions.Left);
                CreateRuntimeText(shopPanel.transform, "Header_Right", "GIỎ HÀNG (SHOPPING CART)", new Vector2(0.52f, 0.83f), new Vector2(0.97f, 0.90f), 18, TextAlignmentOptions.Left);

                Transform cartScrollGO = shopPanel.transform.Find("CartScrollView");
                if (cartScrollGO == null)
                {
                    GameObject clone = Instantiate(leftScroll.gameObject, shopPanel.transform);
                    clone.name = "CartScrollView";
                    clone.SetActive(true);

                    RectTransform cartRt = clone.GetComponent<RectTransform>();
                    cartRt.anchorMin = new Vector2(0.52f, 0.28f);
                    cartRt.anchorMax = new Vector2(0.97f, 0.82f);
                    cartRt.offsetMin = Vector2.zero;
                    cartRt.offsetMax = Vector2.zero;

                    ScrollRect cartSR = clone.GetComponent<ScrollRect>();
                    cartListContent = cartSR.content;
                    for (int i = cartListContent.childCount - 1; i >= 0; i--)
                    {
                        Destroy(cartListContent.GetChild(i).gameObject);
                    }
                }
                else
                {
                    cartScrollGO.gameObject.SetActive(true);
                    RectTransform cartRt = cartScrollGO.GetComponent<RectTransform>();
                    cartRt.anchorMin = new Vector2(0.52f, 0.28f);
                    cartRt.anchorMax = new Vector2(0.97f, 0.82f);
                    cartRt.offsetMin = Vector2.zero;
                    cartRt.offsetMax = Vector2.zero;

                    cartListContent = cartScrollGO.GetComponent<ScrollRect>()?.content;
                }
            }

            if (totalPriceText == null)
            {
                totalPriceText = CreateRuntimeText(shopPanel.transform, "TotalPriceText", "Tổng cộng: 0 VND", new Vector2(0.52f, 0.18f), new Vector2(0.97f, 0.26f), 20, TextAlignmentOptions.Right);
            }

            if (checkoutButton == null)
            {
                checkoutButton = CreateRuntimeButton(shopPanel.transform, "CheckoutButton", "THANH TOÁN", new Vector2(0.52f, 0.04f), new Vector2(0.74f, 0.16f), new Color(0.12f, 0.54f, 0.24f));
                checkoutButton.onClick.RemoveAllListeners();
                checkoutButton.onClick.AddListener(Checkout);
            }

            if (clearCartButton == null)
            {
                clearCartButton = CreateRuntimeButton(shopPanel.transform, "ClearCartButton", "XÓA TẤT CẢ", new Vector2(0.76f, 0.04f), new Vector2(0.97f, 0.16f), new Color(0.2f, 0.2f, 0.2f));
                clearCartButton.onClick.RemoveAllListeners();
                clearCartButton.onClick.AddListener(ClearCart);
            }

            if (cartItemPrefab == null && itemUIPrefab != null)
            {
                ShopItemUI oldItem = itemUIPrefab;
                GameObject cartPrefabGO = Instantiate(oldItem.gameObject, shopPanel.transform);
                cartPrefabGO.name = "CartItemUIPrefab_Runtime";
                cartPrefabGO.SetActive(false);

                ShopItemUI sItem = cartPrefabGO.GetComponent<ShopItemUI>();
                if (sItem != null) DestroyImmediate(sItem);

                ShopCartItemUI cartUI = cartPrefabGO.GetComponent<ShopCartItemUI>() ?? cartPrefabGO.AddComponent<ShopCartItemUI>();
                cartUI.AutoWire();
                cartItemPrefab = cartUI;
            }
        }

        private static TextMeshProUGUI CreateRuntimeText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, TextAlignmentOptions align)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                if (go.GetComponent<RectTransform>() == null)
                {
                    Destroy(go);
                    go = new GameObject(name, typeof(RectTransform));
                    go.transform.SetParent(parent, false);
                }
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            if (tmp.font != null) tmp.font.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;

            return tmp;
        }

        private static Button CreateRuntimeButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                if (go.GetComponent<RectTransform>() == null)
                {
                    Destroy(go);
                    go = new GameObject(name, typeof(RectTransform));
                    go.transform.SetParent(parent, false);
                }
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = bgColor;

            Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();

            CreateRuntimeText(go.transform, "Text", label, Vector2.zero, Vector2.one, 18, TextAlignmentOptions.Center);
            return btn;
        }

        private void ResolveReferences()
        {
            if (!autoFindMissingReferences)
            {
                return;
            }

            if (playerInventory == null)
            {
                playerInventory = PlayerInventory.Instance != null
                    ? PlayerInventory.Instance
                    : FindAnyObjectByType<PlayerInventory>();
            }

            if (recipeUnlockManager == null)
            {
                recipeUnlockManager = RecipeUnlockManager.Instance != null
                    ? RecipeUnlockManager.Instance
                    : FindAnyObjectByType<RecipeUnlockManager>();
            }

            if (playerControlLock == null)
            {
                playerControlLock = FindAnyObjectByType<SimplePlayerControlLock>();
            }
        }

        private static void EnsureEventSystemReady()
        {
            EventSystem eventSystem = EventSystem.current != null
                ? EventSystem.current
                : FindAnyObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem_Auto");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private static bool WasClosePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
