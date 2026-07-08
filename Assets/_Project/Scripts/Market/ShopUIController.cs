using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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

        private readonly List<ShopItemUI> spawnedItems = new List<ShopItemUI>();
        private ShopData currentShop;
        private bool isOpen;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            ResolveReferences();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
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
            moneyText.text = $"{money:N0} VND";
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
                shopPanel.SetActive(visible);
            }
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
