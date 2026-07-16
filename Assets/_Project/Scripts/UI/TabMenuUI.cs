using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using GanhHangRong.Core;
using GanhHangRong.Economy;
using GanhHangRong.Interaction;
using System.Collections.Generic;

namespace GanhHangRong.UI
{
    public class TabMenuUI : MonoBehaviour
    {
        private const int InitialMenuCapacity = 3;

        private static TabMenuUI instance;

        public static TabMenuUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<TabMenuUI>();
                }

                return instance;
            }
            private set => instance = value;
        }
        public static bool IsAnyMenuOpen => Instance != null && Instance.isOpen;
        public static bool HasSavedServingMenu { get; private set; }

        [Header("Runtime State")]
        [SerializeField] private bool isOpen = false;

        // --- UI Root References ---
        private GameObject menuRoot;
        private GameObject mainWindow;
        private GameObject tabMenuContent;
        private GameObject tabRecipeContent;
        private GameObject tabContactContent;

        // --- Tab Buttons ---
        private Button tabBtnMenu;
        private Button tabBtnRecipe;
        private Button tabBtnContact;
        private Button tabBtnCart;
        private TextMeshProUGUI tabTxtMenu;
        private TextMeshProUGUI tabTxtRecipe;
        private TextMeshProUGUI tabTxtContact;
        private TextMeshProUGUI tabTxtCart;
        private GameObject tabCartContent;
        private GameObject cartTabContainer;

        // --- Serving Menu UI References ---
        private List<MenuItem> servingMenu = new List<MenuItem>();
        private GameObject leftServingContainer;
        private TextMeshProUGUI servingCountText;
        private GameObject rightInventoryGrid;
        private Button businessActionButton;
        private TextMeshProUGUI businessActionButtonText;
        private TextMeshProUGUI headerStatusText;
        private GameObject recipeListContent;

        // ================================================================
        //  BẢNG MÀU SỔ BÁN HÀNG
        // ================================================================
        private static readonly Color COL_BG_OVERLAY       = new Color(0.05f, 0.06f, 0.055f, 0.82f);
        private static readonly Color COL_WINDOW_BG        = new Color(0.13f, 0.15f, 0.14f, 1f);
        private static readonly Color COL_TAB_ACTIVE       = new Color(0.16f, 0.48f, 0.34f, 1f);
        private static readonly Color COL_TAB_INACTIVE     = new Color(0.23f, 0.25f, 0.24f, 1f);
        private static readonly Color COL_LEFT_PANEL       = new Color(0.12f, 0.16f, 0.14f, 1f);
        private static readonly Color COL_LEFT_CARD        = new Color(0.19f, 0.23f, 0.21f, 1f);
        private static readonly Color COL_LEFT_CARD_ACTIVE = new Color(0.20f, 0.36f, 0.28f, 1f);
        private static readonly Color COL_RIGHT_PANEL      = new Color(0.91f, 0.92f, 0.90f, 1f);
        private static readonly Color COL_RIGHT_CARD       = new Color(0.98f, 0.98f, 0.96f, 1f);
        private static readonly Color COL_RIGHT_LOCKED     = new Color(0.78f, 0.80f, 0.78f, 1f);
        private static readonly Color COL_BTN_SERVE        = new Color(0.16f, 0.48f, 0.34f, 1f);
        private static readonly Color COL_BTN_SELECTED     = new Color(0.34f, 0.40f, 0.37f, 1f);
        private static readonly Color COL_BTN_LOCKED       = new Color(0.47f, 0.49f, 0.48f, 1f);
        private static readonly Color COL_BTN_DANGER       = new Color(0.70f, 0.28f, 0.20f, 1f);
        private static readonly Color COL_TEXT_DARK        = new Color(0.10f, 0.13f, 0.12f, 1f);
        private static readonly Color COL_TEXT_LIGHT       = new Color(0.95f, 0.97f, 0.95f, 1f);
        private static readonly Color COL_TEXT_MUTED       = new Color(0.48f, 0.52f, 0.50f, 1f);
        private static readonly Color COL_PRICE_GOLD       = new Color(0.72f, 0.46f, 0.10f, 1f);
        private static readonly Color COL_IMAGE_WELL       = new Color(0.86f, 0.89f, 0.86f, 1f);

        // ======== Dữ liệu các món ========
        private struct MenuItem
        {
            public string name;
            public int price;
            public string iconKey;
            public string category;
            public string recipe;
            public bool unlockedByDefault;
            public string recipeId;
            public int orderId;
            public MenuItem(string n, int p, string icon, string group, string r, bool unlocked = true, int id = -1, string unlockRecipeId = null)
            { name = n; price = p; iconKey = icon; category = group; recipe = r; unlockedByDefault = unlocked; orderId = id; recipeId = unlockRecipeId; }
        }

        private static readonly MenuItem[] allItems = new MenuItem[]
        {
            new MenuItem("Cà Phê Đen Đá", Constants.COFFEE_SELL_PRICE, "coffee_black_iced", "ĐỒ UỐNG",
                "Công thức:\n1. Lấy ly sạch\n2. Cho 30g cà phê\n3. Rót 200ml nước sôi\n4. Thêm đá\n5. Phục vụ", true, 1),
            new MenuItem("Trà Đá Nguyên Chất", Constants.TRA_DA_SELL_PRICE, "tea_iced", "ĐỒ UỐNG",
                "Công thức:\n1. Lấy ly sạch\n2. Cho 50g trà\n3. Rót 200ml nước sôi\n4. Thêm đá\n5. Phục vụ", true, 0),
            new MenuItem("Bún Cá Kiên Giang", 42000, "bun_ca_kien_giang", "MÓN ĂN",
                "Cá lóc x1 • Bún tươi x1 • Nước mắm x1 • Ớt bột x1", false, ChapterOrderCatalog.BunCaKienGiang, "bun_ca_kien_giang"),
            new MenuItem("Bánh Canh Ghẹ", 52000, "banh_canh_ghe", "MÓN ĂN",
                "Ghẹ xanh x1 • Sợi bánh canh x1 • Nước mắm x1 • Muối x1", false, ChapterOrderCatalog.BanhCanhGhe, "banh_canh_ghe"),
            new MenuItem("Tôm Rim Nước Mắm", 65000, "tom_rim_nuoc_mam", "MÓN ĂN",
                "Tôm x1 • Nước mắm x1 • Đường thốt nốt x1 • Dầu ăn x1", false, ChapterOrderCatalog.TomRimNuocMam, "tom_rim_nuoc_mam"),
            new MenuItem("Mực Nướng Muối Ớt", 48000, "muc_nuong_muoi_ot", "MÓN ĂN",
                "Mực x1 • Muối x1 • Ớt bột x1 • Dầu ăn x1", false, ChapterOrderCatalog.MucNuongMuoiOt, "muc_nuong_muoi_ot"),
            new MenuItem("Nghêu Xào Cay", 52000, "ngheu_xao_cay", "MÓN ĂN",
                "Nghêu x1 • Nước mắm x1 • Ớt bột x1 • Dầu ăn x1", false, ChapterOrderCatalog.NgheuXaoCay, "ngheu_xao_cay"),
            new MenuItem("Nước Mía", 15000, "nuoc_mia", "ĐỒ UỐNG",
                "Mía cây x2", false, ChapterOrderCatalog.NuocMia, "nuoc_mia"),
            new MenuItem("Trà Chanh", 28000, "tra_chanh", "ĐỒ UỐNG",
                "Lá trà x1 • Chanh tươi x1 • Đường thốt nốt x1", false, ChapterOrderCatalog.TraChanh, "tra_chanh"),
            new MenuItem("Nước Dừa", 18000, "nuoc_dua", "ĐỒ UỐNG",
                "Dừa tươi x1", false, ChapterOrderCatalog.NuocDua, "nuoc_dua")
        };

        private static readonly Dictionary<string, Sprite> itemSpriteCache = new Dictionary<string, Sprite>();

        private static Sprite GetItemSprite(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;
            if (itemSpriteCache.TryGetValue(iconKey, out Sprite cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>("UI/TabMenu/" + iconKey);
            if (sprite == null)
            {
                sprite = MarketItemIconLibrary.GetIcon(iconKey);
            }
            itemSpriteCache[iconKey] = sprite;
            return sprite;
        }

        private static bool IsItemUnlocked(MenuItem item)
        {
            if (item.unlockedByDefault || string.IsNullOrWhiteSpace(item.recipeId))
            {
                return item.unlockedByDefault;
            }

            RecipeUnlockManager manager = RecipeUnlockManager.Instance != null
                ? RecipeUnlockManager.Instance
                : FindAnyObjectByType<RecipeUnlockManager>();
            return manager != null && manager.IsRecipeUnlocked(item.recipeId);
        }

        // ================================================================
        //  HỆ THỐNG SPRITE BO GÓC TỰ ĐỘNG (PROCEDURAL ROUNDED SPRITES)
        // ================================================================
        private static Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();

        private static Sprite GetRoundedSprite(int radius = 16)
        {
            if (spriteCache.TryGetValue(radius, out var s) && s != null) return s;
            int size = radius * 2 + 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x < radius ? radius - x : (x > size - 1 - radius ? x - (size - 1 - radius) : 0);
                    int dy = y < radius ? radius - y : (y > size - 1 - radius ? y - (size - 1 - radius) : 0);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            DontDestroyOnLoad(tex);
            spriteCache[radius] = s;
            return s;
        }

        // ================================================================
        //  LIFECYCLE & AUTO CREATE
        // ================================================================
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            TryCreateForGameplayScene();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreateForGameplayScene();
        }

        private static void TryCreateForGameplayScene()
        {
            if (FindAnyObjectByType<GanhHangRong.Interaction.TeaCart>(FindObjectsInactive.Include) == null) return;

            if (FindAnyObjectByType<TabMenuUI>() == null)
            {
                var go = new GameObject("TabMenuUI_Auto");
                go.AddComponent<TabMenuUI>();
            }
        }

        public static HashSet<int> ActiveServingOrderIds = new HashSet<int>();
        private static bool isServingInitialized = false;

        public static HashSet<int> GetActiveServingOrderIds()
        {
            if (!isServingInitialized)
            {
                ActiveServingOrderIds.Add(0); // Trà Đá
                ActiveServingOrderIds.Add(1); // Cà Phê Đá
                isServingInitialized = true;
            }
            return ActiveServingOrderIds;
        }

        public static int[] ExportActiveServingOrderIds()
        {
            int[] result = new int[GetActiveServingOrderIds().Count];
            GetActiveServingOrderIds().CopyTo(result);
            return result;
        }

        public static void RestoreActiveServingOrderIds(int[] orderIds, bool wasSaved)
        {
            ActiveServingOrderIds.Clear();
            bool restoredAnySavedItem = false;
            if (orderIds != null)
            {
                for (int i = 0; i < orderIds.Length; i++)
                {
                    for (int itemIndex = 0; itemIndex < allItems.Length; itemIndex++)
                    {
                        MenuItem item = allItems[itemIndex];
                        if (item.orderId == orderIds[i] && IsItemUnlocked(item))
                        {
                            restoredAnySavedItem |= ActiveServingOrderIds.Add(item.orderId);
                            break;
                        }
                    }
                }
            }

            if (ActiveServingOrderIds.Count == 0)
            {
                ActiveServingOrderIds.Add(0);
                ActiveServingOrderIds.Add(1);
            }

            isServingInitialized = true;
            HasSavedServingMenu = wasSaved && restoredAnySavedItem;
            if (Instance != null)
            {
                Instance.ApplyActiveMenuToDraft();
                Instance.RefreshServingMenuUI();
                Instance.RefreshInventoryCardsUI();
            }
        }

        public static void MarkMenuNeedsDailySave()
        {
            HasSavedServingMenu = false;
            if (Instance != null && Instance.isOpen)
            {
                Instance.UpdateBusinessActionButton();
            }
        }

        private void Awake()
        {
            Instance = this;
            EnsureEventSystem();

            GetActiveServingOrderIds();
            ApplyActiveMenuToDraft();
            if (servingMenu.Count == 0)
            {
                foreach (var item in allItems)
                    if (IsItemUnlocked(item)) servingMenu.Add(item);
            }

            BuildFullUI();
            CloseMenu();
        }

        private void OnEnable()
        {
            RecipeUnlockManager manager = RecipeUnlockManager.Instance;
            if (manager != null)
            {
                manager.RecipeUnlocked -= HandleRecipeUnlocked;
                manager.RecipeUnlocked += HandleRecipeUnlocked;
            }
        }

        private void OnDisable()
        {
            RecipeUnlockManager manager = RecipeUnlockManager.Instance;
            if (manager != null)
            {
                manager.RecipeUnlocked -= HandleRecipeUnlocked;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var esGO = new GameObject("EventSystem_Auto");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (GameplayPauseMenuUI.IsOpen) return;
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }
            if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseMenu();
            }
        }

        // ================================================================
        //  PUBLIC CONTROLS
        // ================================================================
        public void ToggleMenu()
        {
            if (isOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            if (isOpen) return;
            isOpen = true;
            EnsureEventSystem();
            if (menuRoot != null) menuRoot.SetActive(true);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.PauseGame();
            }
            else
            {
                Time.timeScale = 0f;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SwitchTab(0);
            RefreshHeaderStatus();
            RefreshServingMenuUI();
            RefreshInventoryCardsUI();
            RefreshRecipeCardsUI();
            RefreshCartTabUI();
        }

        private void HandleRecipeUnlocked(string recipeId)
        {
            HasSavedServingMenu = false;
            RefreshInventoryCardsUI();
            RefreshRecipeCardsUI();
        }

        public void CloseMenu()
        {
            if (!isOpen)
            {
                if (menuRoot != null) menuRoot.SetActive(false);
                return;
            }
            isOpen = false;
            if (menuRoot != null) menuRoot.SetActive(false);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.ResumeGame();
            }
            else
            {
                Time.timeScale = 1f;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ================================================================
        //  BUILD FULL UI (OVERLAY & WINDOW)
        // ================================================================
        private void BuildFullUI()
        {
            var canvasGO = new GameObject("[SoTay] Canvas");
            canvasGO.transform.SetParent(this.transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Đảm bảo nổi trên cùng
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Nền tối mờ toàn màn hình
            menuRoot = MakePanel(canvasGO, "MenuRoot", COL_BG_OVERLAY, 0f, 0f, 1f, 1f);
            
            // Bảng vận hành chính, giữ biên đủ rộng cho cả màn hình 16:9 và 16:10.
            mainWindow = MakeRoundedPanel(menuRoot, "MainWindow", COL_WINDOW_BG, 8, 0.035f, 0.04f, 0.965f, 0.96f);
            var mainShadow = mainWindow.AddComponent<Shadow>();
            mainShadow.effectColor = new Color(0, 0, 0, 0.4f);
            mainShadow.effectDistance = new Vector2(0, -10);

            BuildTopHeader(mainWindow);

            // Container cho các Tab nội dung
            var contentArea = MakeRect(mainWindow, "ContentArea");
            SetAnchors(contentArea, 0.018f, 0.02f, 0.982f, 0.835f);

            tabMenuContent = BuildMenuTabContent(contentArea.gameObject);
            tabRecipeContent = BuildRecipeTabContent(contentArea.gameObject);
            tabContactContent = BuildContactTabContent(contentArea.gameObject);
            tabCartContent = BuildCartTabContent(contentArea.gameObject);

            SwitchTab(0);
        }

        private void BuildTopHeader(GameObject parent)
        {
            var header = MakeRect(parent, "TopHeader");
            SetAnchors(header, 0.018f, 0.845f, 0.982f, 0.985f);

            MakeText(header.gameObject, "SỔ BÁN HÀNG", 28, COL_TEXT_LIGHT, TextAlignmentOptions.Left, FontStyles.Bold, 0.01f, 0.50f, 0.34f, 0.93f);
            headerStatusText = MakeText(header.gameObject, "Đang chuẩn bị  |  08:00", 14,
                new Color(0.68f, 0.76f, 0.71f, 1f), TextAlignmentOptions.Left, FontStyles.Normal,
                0.01f, 0.14f, 0.34f, 0.50f);

            var closeBtnGO = MakeRoundedPanel(header.gameObject, "CloseBtn", COL_TAB_INACTIVE, 6, 0.955f, 0.28f, 0.992f, 0.78f);
            MakeText(closeBtnGO, "X", 16, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            closeBtnGO.AddComponent<Button>().onClick.AddListener(CloseMenu);

            var tabArea = MakeRect(header.gameObject, "TabArea");
            SetAnchors(tabArea, 0.36f, 0.20f, 0.94f, 0.80f);
            
            var hlg = tabArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childControlWidth = true; hlg.childControlHeight = true; hlg.childForceExpandWidth = true;

            tabBtnMenu = CreatePillTabButton(tabArea.gameObject, "THỰC ĐƠN", () => SwitchTab(0), out tabTxtMenu);
            tabBtnRecipe = CreatePillTabButton(tabArea.gameObject, "CÔNG THỨC", () => SwitchTab(1), out tabTxtRecipe);
            tabBtnContact = CreatePillTabButton(tabArea.gameObject, "LIÊN HỆ", () => SwitchTab(2), out tabTxtContact);
            tabBtnCart = CreatePillTabButton(tabArea.gameObject, "KHO HÀNG", () => SwitchTab(3), out tabTxtCart);
        }

        private Button CreatePillTabButton(GameObject parent, string label, UnityEngine.Events.UnityAction action, out TextMeshProUGUI txtRef)
        {
            var btnGO = MakeRoundedPanel(parent, "TabBtn_" + label, COL_TAB_INACTIVE, 6, 0, 0, 1, 1);
            txtRef = MakeText(btnGO, label, 15, COL_TEXT_LIGHT, TextAlignmentOptions.Center, FontStyles.Bold, 0.03f, 0f, 0.97f, 1f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnGO.GetComponent<Image>();
            btn.onClick.AddListener(action);
            return btn;
        }

        private void SwitchTab(int index)
        {
            if (tabMenuContent != null) tabMenuContent.SetActive(index == 0);
            if (tabRecipeContent != null) tabRecipeContent.SetActive(index == 1);
            if (tabContactContent != null) tabContactContent.SetActive(index == 2);
            if (tabCartContent != null) tabCartContent.SetActive(index == 3);

            UpdateTabVisual(tabBtnMenu, tabTxtMenu, index == 0);
            UpdateTabVisual(tabBtnRecipe, tabTxtRecipe, index == 1);
            UpdateTabVisual(tabBtnContact, tabTxtContact, index == 2);
            UpdateTabVisual(tabBtnCart, tabTxtCart, index == 3);

            if (index == 2)
            {
                RefreshIceVendorCard();
            }
            if (index == 3)
            {
                RefreshCartTabUI();
            }
        }

        private void UpdateTabVisual(Button btn, TextMeshProUGUI txt, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = isActive ? COL_TAB_ACTIVE : COL_TAB_INACTIVE;
            if (txt != null) txt.color = isActive ? Color.white : new Color(0.80f, 0.84f, 0.81f, 1f);
        }

        private void RefreshHeaderStatus()
        {
            if (headerStatusText == null) return;

            string phaseLabel = "Tự do";
            if (Systems.BusinessDayController.HasInstance && Systems.BusinessDayController.Instance.IsManagingGameLoop)
            {
                switch (Systems.BusinessDayController.Instance.CurrentPhase)
                {
                    case BusinessDayPhase.PreOpen: phaseLabel = "Trước giờ mở"; break;
                    case BusinessDayPhase.Preparation: phaseLabel = "Đang chuẩn bị"; break;
                    case BusinessDayPhase.Trading: phaseLabel = "Đang bán hàng"; break;
                    case BusinessDayPhase.Closing: phaseLabel = "Đang đóng quán"; break;
                    case BusinessDayPhase.AfterHours: phaseLabel = "Sau giờ bán"; break;
                    case BusinessDayPhase.DaySummary: phaseLabel = "Tổng kết ngày"; break;
                }
            }

            Economy.DayNightCycle cycle = FindAnyObjectByType<Economy.DayNightCycle>();
            float hour = cycle != null ? cycle.CurrentHour : 0f;
            int wholeHour = Mathf.FloorToInt(hour) % 24;
            int minute = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
            headerStatusText.text = $"{phaseLabel}  |  {wholeHour:00}:{minute:00}";
        }

        // ================================================================
        //  TAB 1: MENU — Trái: Thực đơn phục vụ / Phải: Kho món ăn
        // ================================================================
        private GameObject BuildMenuTabContent(GameObject parent)
        {
            var panel = new GameObject("MenuTabContent");
            panel.transform.SetParent(parent.transform, false);
            StretchFull(panel.AddComponent<RectTransform>());

            var leftPanel = MakeRoundedPanel(panel, "LeftPanel", COL_LEFT_PANEL, 8, 0.005f, 0.005f, 0.35f, 0.995f);
            MakeText(leftPanel, "THỰC ĐƠN HÔM NAY", 20, COL_TEXT_LIGHT, TextAlignmentOptions.Left, FontStyles.Bold, 0.055f, 0.91f, 0.94f, 0.97f);
            servingCountText = MakeText(leftPanel, "Đã chọn 0/3 món", 14,
                new Color(0.66f, 0.73f, 0.68f, 1f), TextAlignmentOptions.Left, FontStyles.Normal,
                0.055f, 0.855f, 0.94f, 0.91f);

            var leftScrollArea = MakeRect(leftPanel, "ScrollArea");
            SetAnchors(leftScrollArea, 0.045f, 0.145f, 0.955f, 0.84f);
            var leftScroll = leftScrollArea.gameObject.AddComponent<ScrollRect>();
            leftScroll.vertical = true; leftScroll.horizontal = false;
            
            var leftViewport = MakeRect(leftScrollArea.gameObject, "Viewport");
            StretchFull(leftViewport);
            leftViewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            leftViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            leftServingContainer = MakeRect(leftViewport.gameObject, "Content").gameObject;
            var lRT = leftServingContainer.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0, 1); lRT.anchorMax = new Vector2(1, 1);
            lRT.pivot = new Vector2(0.5f, 1); lRT.anchoredPosition = Vector2.zero; lRT.sizeDelta = Vector2.zero;
            
            var vlg = leftServingContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
            leftServingContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            leftScroll.content = lRT; leftScroll.viewport = leftViewport;

            var actionGO = MakeRoundedPanel(leftPanel, "BusinessActionButton", COL_BTN_SERVE, 6, 0.045f, 0.03f, 0.955f, 0.115f);
            businessActionButtonText = MakeText(actionGO, "LƯU THỰC ĐƠN", 16, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            businessActionButton = actionGO.AddComponent<Button>();
            businessActionButton.targetGraphic = actionGO.GetComponent<Image>();
            businessActionButton.onClick.AddListener(HandleBusinessActionClicked);

            var rightPanel = MakeRoundedPanel(panel, "RightPanel", COL_RIGHT_PANEL, 8, 0.365f, 0.005f, 0.995f, 0.995f);
            MakeText(rightPanel, "DANH SÁCH MÓN", 22, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.035f, 0.91f, 0.60f, 0.97f);
            MakeText(rightPanel, "Chọn món để đưa vào thực đơn bán hàng", 14, COL_TEXT_MUTED, TextAlignmentOptions.Left, FontStyles.Normal, 0.035f, 0.855f, 0.72f, 0.91f);
            MakeText(rightPanel, "TỐI ĐA 3 MÓN", 13, COL_TAB_ACTIVE, TextAlignmentOptions.Right, FontStyles.Bold, 0.74f, 0.875f, 0.965f, 0.95f);

            var rightScrollArea = MakeRect(rightPanel, "ScrollArea");
            SetAnchors(rightScrollArea, 0.018f, 0.018f, 0.982f, 0.845f);
            var rightScroll = rightScrollArea.gameObject.AddComponent<ScrollRect>();
            rightScroll.vertical = true; rightScroll.horizontal = false;
            
            var rightViewport = MakeRect(rightScrollArea.gameObject, "Viewport");
            StretchFull(rightViewport);
            rightViewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            rightViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            rightInventoryGrid = MakeRect(rightViewport.gameObject, "Content").gameObject;
            var rRT = rightInventoryGrid.GetComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0, 1); rRT.anchorMax = new Vector2(1, 1);
            rRT.pivot = new Vector2(0.5f, 1); rRT.anchoredPosition = Vector2.zero; rRT.sizeDelta = Vector2.zero;
            
            var grid = rightInventoryGrid.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3; grid.cellSize = new Vector2(300, 250);
            grid.spacing = new Vector2(12, 12); grid.padding = new RectOffset(12, 12, 12, 12);
            rightInventoryGrid.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rightScroll.content = rRT; rightScroll.viewport = rightViewport;

            RefreshServingMenuUI();
            RefreshInventoryCardsUI();
            return panel;
        }

        private void RefreshInventoryCardsUI()
        {
            if (rightInventoryGrid == null) return;
            foreach (Transform child in rightInventoryGrid.transform)
                Destroy(child.gameObject);

            foreach (var item in allItems)
                BuildInventoryCard(rightInventoryGrid, item);
        }

        private void BuildInventoryCard(GameObject parent, MenuItem item)
        {
            bool canEdit = CanEditServingMenu();
            bool isAlreadyServing = servingMenu.Exists(m => m.name == item.name);
            bool isUnlocked = IsItemUnlocked(item);
            Color cardColor = isUnlocked ? (isAlreadyServing ? new Color(0.91f, 0.96f, 0.92f, 1f) : COL_RIGHT_CARD) : COL_RIGHT_LOCKED;
            var card = MakeRoundedPanel(parent, "InvCard_" + item.name.Replace(" ", ""), cardColor, 8, 0, 0, 1, 1);
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.10f); shadow.effectDistance = new Vector2(0, -2);

            var imageWell = MakeRoundedPanel(card, "ImageWell", COL_IMAGE_WELL, 6, 0.045f, 0.37f, 0.955f, 0.955f);
            MakeIcon(imageWell, item.iconKey, isUnlocked ? Color.white : new Color(0.52f, 0.55f, 0.53f, 0.75f), 0.04f, 0.04f, 0.96f, 0.96f);
            MakeText(imageWell, item.category, 11, isUnlocked ? COL_TAB_ACTIVE : COL_TEXT_MUTED,
                TextAlignmentOptions.TopLeft, FontStyles.Bold, 0.04f, 0.77f, 0.72f, 0.97f);
            if (!isUnlocked)
            {
                MakeText(imageWell, "MUA SÁCH Ở CHỢ", 13, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0.08f, 0.38f, 0.92f, 0.64f);
            }

            Color primaryText = isUnlocked ? COL_TEXT_DARK : new Color(0.32f, 0.35f, 0.33f, 1f);
            MakeText(card, item.name, 16, primaryText, TextAlignmentOptions.Left, FontStyles.Bold, 0.055f, 0.22f, 0.70f, 0.36f);
            MakeText(card, $"{item.price:N0} đ", 15, isUnlocked ? COL_PRICE_GOLD : COL_TEXT_MUTED,
                TextAlignmentOptions.Right, FontStyles.Bold, 0.68f, 0.22f, 0.945f, 0.36f);

            Color btnColor = !isUnlocked ? COL_BTN_LOCKED : (isAlreadyServing || !canEdit ? COL_BTN_SELECTED : COL_BTN_SERVE);
            string btnText = !isUnlocked ? "CHƯA CÓ CÔNG THỨC" : (isAlreadyServing ? "ĐÃ CHỌN" : (canEdit ? "THÊM MÓN" : "KHÓA TRONG CA"));
            var btnGO = MakeRoundedPanel(card, "MenuActionBtn", btnColor, 6, 0.05f, 0.035f, 0.95f, 0.19f);
            MakeText(btnGO, btnText, 13, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);

            if (isUnlocked && !isAlreadyServing && canEdit)
            {
                var btn = btnGO.AddComponent<Button>();
                btn.targetGraphic = btnGO.GetComponent<Image>();
                btn.onClick.AddListener(() =>
                {
                    if (servingMenu.Count >= InitialMenuCapacity || servingMenu.Exists(m => m.name == item.name)) return;
                    servingMenu.Add(item);
                    HasSavedServingMenu = false;
                    RefreshServingMenuUI();
                    RefreshInventoryCardsUI();
                });
            }
        }

        private void RefreshServingMenuUI()
        {
            if (servingCountText != null)
                servingCountText.text = $"Đã chọn {servingMenu.Count}/{InitialMenuCapacity} món";

            UpdateBusinessActionButton();
            
            if (leftServingContainer == null) return;

            foreach (Transform child in leftServingContainer.transform)
                Destroy(child.gameObject);

            if (servingMenu.Count == 0)
            {
                var emptyRow = MakeRect(leftServingContainer, "EmptyText").gameObject;
                emptyRow.AddComponent<LayoutElement>().minHeight = 100;
                MakeText(emptyRow, "Chưa có món trong thực đơn.\nChọn món từ danh sách bên phải.", 15,
                    new Color(0.58f, 0.65f, 0.60f, 1f), TextAlignmentOptions.Center, FontStyles.Normal, 0.05f, 0f, 0.95f, 1f);
                return;
            }

            foreach (var item in servingMenu)
            {
                var row = MakeRoundedPanel(leftServingContainer, "ServeItem", COL_LEFT_CARD, 8, 0, 0, 1, 1);
                row.AddComponent<LayoutElement>().minHeight = 96;
                MakeIcon(row, item.iconKey, Color.white, 0.025f, 0.12f, 0.23f, 0.88f);
                MakeText(row, item.name, 16, COL_TEXT_LIGHT, TextAlignmentOptions.Left, FontStyles.Bold, 0.25f, 0.50f, 0.83f, 0.88f);
                MakeText(row, $"{item.price:N0} đ", 14, COL_PRICE_GOLD, TextAlignmentOptions.Left, FontStyles.Bold, 0.25f, 0.18f, 0.68f, 0.49f);

                bool canEdit = CanEditServingMenu();
                var removeBtnGO = MakeRoundedPanel(row, "RemoveBtn", canEdit ? COL_BTN_DANGER : COL_BTN_SELECTED, 6, 0.84f, 0.27f, 0.965f, 0.73f);
                MakeText(removeBtnGO, canEdit ? "X" : "-", 14, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
                
                if (canEdit)
                {
                    var capturedItem = item;
                    var removeButton = removeBtnGO.AddComponent<Button>();
                    removeButton.targetGraphic = removeBtnGO.GetComponent<Image>();
                    removeButton.onClick.AddListener(() => {
                        servingMenu.Remove(capturedItem);
                        HasSavedServingMenu = false;
                        RefreshServingMenuUI();
                        RefreshInventoryCardsUI();
                    });
                }
            }
        }

        public void SaveServingMenu()
        {
            if (!CanEditServingMenu()) return;
            if (servingMenu.Count == 0)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Cần chọn ít nhất 1 món trước khi lưu thực đơn.");
                return;
            }

            isServingInitialized = true;
            ActiveServingOrderIds.Clear();
            foreach (MenuItem item in servingMenu)
            {
                if (item.orderId >= 0) ActiveServingOrderIds.Add(item.orderId);
            }
            HasSavedServingMenu = true;
            UpdateBusinessActionButton();
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã lưu {ActiveServingOrderIds.Count} món cho ngày hôm nay.");
        }

        private void ApplyActiveMenuToDraft()
        {
            servingMenu.Clear();
            HashSet<int> activeIds = GetActiveServingOrderIds();
            foreach (MenuItem item in allItems)
            {
                if (IsItemUnlocked(item) && activeIds.Contains(item.orderId))
                {
                    servingMenu.Add(item);
                }
            }
        }

        private static bool CanEditServingMenu()
        {
            return !Systems.BusinessDayController.HasInstance ||
                   !Systems.BusinessDayController.Instance.IsManagingGameLoop ||
                   Systems.BusinessDayController.Instance.CanEditMenu;
        }

        private void HandleBusinessActionClicked()
        {
            if (!Systems.BusinessDayController.HasInstance ||
                !Systems.BusinessDayController.Instance.IsManagingGameLoop)
            {
                SaveServingMenu();
                return;
            }

            Systems.BusinessDayController controller = Systems.BusinessDayController.Instance;
            if (controller.CurrentPhase == BusinessDayPhase.Preparation)
            {
                SaveServingMenu();
            }
            else if (controller.CurrentPhase == BusinessDayPhase.Trading && controller.RequestEarlyCloseFromMenu())
            {
                CloseMenu();
            }
        }

        private void UpdateBusinessActionButton()
        {
            if (businessActionButton == null || businessActionButtonText == null) return;

            bool interactable = true;
            Color color = COL_BTN_SERVE;
            string label = HasSavedServingMenu ? "ĐÃ LƯU THỰC ĐƠN" : "LƯU THỰC ĐƠN";

            if (Systems.BusinessDayController.HasInstance &&
                Systems.BusinessDayController.Instance.IsManagingGameLoop)
            {
                BusinessDayPhase phase = Systems.BusinessDayController.Instance.CurrentPhase;
                if (phase == BusinessDayPhase.Trading)
                {
                    label = "ĐÓNG QUÁN SỚM";
                    color = new Color(0.68f, 0.22f, 0.16f, 1f);
                }
                else if (phase != BusinessDayPhase.Preparation)
                {
                    label = "CHỈNH KHI CHUẨN BỊ";
                    color = COL_BTN_SELECTED;
                    interactable = false;
                }
            }

            businessActionButton.interactable = interactable;
            businessActionButtonText.text = label;
            Image image = businessActionButton.GetComponent<Image>();
            if (image != null) image.color = color;
        }

        // ================================================================
        //  TAB 2: CÔNG THỨC & TAB 3: ĐIỆN THOẠI (DESIGN BO GÓC SANG TRỌNG)
        // ================================================================
        private GameObject BuildRecipeTabContent(GameObject parent)
        {
            var panel = MakeRoundedPanel(parent, "RecipeTabContent", COL_RIGHT_PANEL, 8, 0.005f, 0.005f, 0.995f, 0.995f);
            MakeText(panel, "SỔ CÔNG THỨC", 22, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.025f, 0.91f, 0.55f, 0.97f);
            MakeText(panel, "Tra nhanh nguyên liệu và thứ tự pha chế", 14, COL_TEXT_MUTED, TextAlignmentOptions.Left, FontStyles.Normal, 0.025f, 0.855f, 0.70f, 0.91f);

            var scrollArea = MakeRect(panel, "ScrollArea");
            SetAnchors(scrollArea, 0.018f, 0.02f, 0.982f, 0.845f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = MakeRect(scrollArea.gameObject, "Viewport");
            StretchFull(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = MakeRect(viewport.gameObject, "Content").gameObject;
            recipeListContent = contentGO;
            var cRT = contentGO.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.padding = new RectOffset(12, 12, 12, 12);
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT; scroll.viewport = viewport;

            RefreshRecipeCardsUI();
            return panel;
        }

        private void RefreshRecipeCardsUI()
        {
            if (recipeListContent == null)
            {
                return;
            }

            foreach (Transform child in recipeListContent.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (MenuItem item in allItems)
            {
                BuildRecipeCard(recipeListContent, item);
            }
        }

        private void BuildRecipeCard(GameObject parent, MenuItem item)
        {
            bool isUnlocked = IsItemUnlocked(item);
            bool isFoodRecipe = !string.IsNullOrWhiteSpace(item.recipeId);
            RecipeData recipeData = isFoodRecipe ? MarketRecipeCatalog.GetRecipe(item.recipeId) : null;
            bool canCook = false;
            string cookStatus = string.Empty;
            if (isFoodRecipe && isUnlocked && CookingManager.Instance != null && recipeData != null)
            {
                canCook = CookingManager.Instance.CanCookRecipe(recipeData, out cookStatus);
            }

            Color cardColor = isUnlocked ? COL_RIGHT_CARD : COL_RIGHT_LOCKED;
            GameObject card = MakeRoundedPanel(parent, "Recipe_" + item.name, cardColor, 8, 0, 0, 1, 1);
            LayoutElement layout = card.AddComponent<LayoutElement>();
            layout.minHeight = 132f;
            layout.preferredHeight = 132f;
            Shadow shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.10f);
            shadow.effectDistance = new Vector2(0, -2);

            GameObject imageWell = MakeRoundedPanel(card, "RecipeImage", COL_IMAGE_WELL, 6, 0.012f, 0.08f, 0.105f, 0.92f);
            MakeIcon(imageWell, item.iconKey, isUnlocked ? Color.white : new Color(0.5f, 0.52f, 0.5f, 0.7f), 0.06f, 0.06f, 0.94f, 0.94f);
            MakeText(card, item.category, 11, isUnlocked ? COL_TAB_ACTIVE : COL_TEXT_MUTED, TextAlignmentOptions.Left, FontStyles.Bold, 0.125f, 0.70f, 0.30f, 0.91f);
            MakeText(card, item.name, 18, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.125f, 0.43f, 0.42f, 0.72f);
            MakeText(card, $"{item.price:N0} đ", 15, COL_PRICE_GOLD, TextAlignmentOptions.Left, FontStyles.Bold, 0.125f, 0.16f, 0.32f, 0.43f);

            string recipeText = isUnlocked
                ? item.recipe
                : "Mua sách công thức tại quầy Đặc Sản Kiên Giang để mở khóa.";
            MakeText(card, recipeText, 14,
                isUnlocked ? new Color(0.28f, 0.32f, 0.30f, 1f) : COL_TEXT_MUTED,
                TextAlignmentOptions.Left, isUnlocked ? FontStyles.Normal : FontStyles.Italic,
                0.36f, 0.18f, isFoodRecipe ? 0.76f : 0.975f, 0.86f);

            if (!isFoodRecipe)
            {
                return;
            }

            string buttonLabel = !isUnlocked
                ? "CHƯA MỞ KHÓA"
                : (canCook
                    ? (item.category == "ĐỒ UỐNG" ? "PHA 1 LY" : "NẤU 1 PHẦN")
                    : "THIẾU NGUYÊN LIỆU");
            Color buttonColor = !isUnlocked || !canCook ? COL_BTN_LOCKED : COL_BTN_SERVE;
            GameObject cookButtonObject = MakeRoundedPanel(card, "CookButton", buttonColor, 6, 0.79f, 0.22f, 0.97f, 0.56f);
            MakeText(cookButtonObject, buttonLabel, 13, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0.03f, 0.03f, 0.97f, 0.97f);

            string statusText = isUnlocked
                ? (canCook ? "Đủ nguyên liệu trong kho" : cookStatus)
                : "Cần sách công thức";
            MakeText(card, statusText, 11, canCook ? COL_TAB_ACTIVE : COL_TEXT_MUTED,
                TextAlignmentOptions.Center, FontStyles.Normal, 0.79f, 0.07f, 0.97f, 0.20f);

            if (isUnlocked && canCook)
            {
                Button cookButton = cookButtonObject.AddComponent<Button>();
                cookButton.targetGraphic = cookButtonObject.GetComponent<Image>();
                MenuItem captured = item;
                cookButton.onClick.AddListener(() => TryCookMenuItem(captured));
            }
        }

        private void TryCookMenuItem(MenuItem item)
        {
            if (CartItem.HasPreparedTea)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đang cầm {CartItem.PreparedDrinkName}. Hãy phục vụ trước khi nấu món khác.");
                return;
            }

            RecipeData recipe = MarketRecipeCatalog.GetRecipe(item.recipeId);
            if (recipe == null || CookingManager.Instance == null)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Không tìm thấy dữ liệu công thức trong scene.");
                return;
            }

            if (!CookingManager.Instance.CookRecipe(recipe, out string result))
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", result);
                RefreshRecipeCardsUI();
                return;
            }

            CartItem.PrepareReadyOrder(item.orderId);
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"{result} Món đã sẵn sàng để phục vụ.");
            RefreshRecipeCardsUI();
            RefreshCartTabUI();
        }

        // ── Ice Vendor dialogue overlay references ──
        private GameObject iceVendorDialogueOverlay;
        private TextMeshProUGUI iceVendorDialogueTxt;
        private Button iceVendorCallBtn;
        private TextMeshProUGUI iceVendorIceTxt;
        private int iceDialogueStep = 0;

        private GameObject BuildContactTabContent(GameObject parent)
        {
            var panel = MakeRoundedPanel(parent, "ContactTabContent", COL_RIGHT_PANEL, 8, 0.005f, 0.005f, 0.995f, 0.995f);
            MakeText(panel, "DANH BẠ NHÀ CUNG CẤP", 22, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.025f, 0.91f, 0.62f, 0.97f);
            MakeText(panel, "Liên hệ dịch vụ hỗ trợ hoạt động bán hàng", 14, COL_TEXT_MUTED, TextAlignmentOptions.Left, FontStyles.Normal, 0.025f, 0.855f, 0.72f, 0.91f);

            var scrollArea = MakeRect(panel, "ScrollArea");
            SetAnchors(scrollArea, 0.018f, 0.02f, 0.982f, 0.845f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = MakeRect(scrollArea.gameObject, "Viewport");
            StretchFull(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = MakeRect(viewport.gameObject, "Content").gameObject;
            var cRT = contentGO.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.padding = new RectOffset(12, 12, 12, 12);
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT; scroll.viewport = viewport;

            // ─── Ông Ba Bán Đá (Special card) ───
            BuildIceVendorCard(contentGO);

            BuildContactEntry(contentGO, "NL", "Chị Ba", "0912 345 678", "Cung cấp nguyên liệu trà, cà phê mỗi sáng");
            BuildContactEntry(contentGO, "TR", "Nhà Cung Cấp Trà", "0987 654 321", "Giao trà lá tươi, giá sỉ ưu đãi");
            BuildContactEntry(contentGO, "SC", "Thợ Sửa Xe", "0901 222 333", "Sửa chữa gánh hàng rong khi bị hỏng");
            BuildContactEntry(contentGO, "YT", "Trạm Y Tế Phường", "0909 111 222", "Khám sức khoẻ và phục hồi thể lực khi mệt");
            BuildContactEntry(contentGO, "CA", "Công An Phường", "0908 888 999", "Hỗ trợ an ninh đường phố và trật tự đô thị");

            // Dialogue overlay (hidden by default)
            BuildIceVendorDialogue(panel);

            return panel;
        }

        private void BuildIceVendorCard(GameObject parent)
        {
            // Background xanh lam nhạt đặc biệt cho ông Ba
            Color cardBg = new Color(0.88f, 0.95f, 1.00f, 1f);
            Color iceBlueDark = new Color(0.10f, 0.45f, 0.80f, 1f);
            Color iceBlueLight = new Color(0.20f, 0.60f, 0.95f, 1f);

            var card = MakeRoundedPanel(parent, "Contact_OngBa", cardBg, 8, 0, 0, 1, 1);
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 130; le.preferredHeight = 130;
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.18f); shadow.effectDistance = new Vector2(0, -4);

            var iconWell = MakeRoundedPanel(card, "IceVendorIcon", new Color(0.78f, 0.91f, 0.98f, 1f), 6, 0.015f, 0.14f, 0.125f, 0.88f);
            MakeIcon(iconWell, "ingredient_ice", Color.white, 0.08f, 0.08f, 0.92f, 0.92f);

            // Tên + số
            MakeText(card, "Ông Ba Bán Đá", 20, iceBlueDark, TextAlignmentOptions.Left, FontStyles.Bold, 0.15f, 0.62f, 0.60f, 0.92f);
            MakeText(card, "0918 123 456", 17, iceBlueLight, TextAlignmentOptions.Left, FontStyles.Bold, 0.60f, 0.62f, 0.98f, 0.92f);
            MakeText(card, "Giao đá nhanh — 5.000đ/thùng (100%)", 14, new Color(0.3f, 0.3f, 0.4f, 1f), TextAlignmentOptions.Left, FontStyles.Italic, 0.15f, 0.42f, 0.99f, 0.60f);

            // Thanh đá hiện tại
            iceVendorIceTxt = MakeText(card, "Đá: ...%", 15, iceBlueDark, TextAlignmentOptions.Left, FontStyles.Bold, 0.15f, 0.22f, 0.60f, 0.40f);

            // Nút GỌI
            var btnGO = MakeRoundedPanel(card, "CallBtn_OngBa", iceBlueLight, 6, 0.63f, 0.10f, 0.97f, 0.45f);
            MakeText(btnGO, "GỌI GIAO ĐÁ  5.000đ", 14, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0.02f, 0f, 0.98f, 1f);
            iceVendorCallBtn = btnGO.AddComponent<Button>();
            iceVendorCallBtn.targetGraphic = btnGO.GetComponent<Image>();
            iceVendorCallBtn.onClick.AddListener(OnCallOngBa);
        }

        private void BuildIceVendorDialogue(GameObject parent)
        {
            // Full-panel overlay mờ
            iceVendorDialogueOverlay = MakeRoundedPanel(parent, "IceVendorDialogue", new Color(0.05f, 0.10f, 0.20f, 0.92f), 24, 0.10f, 0.25f, 0.90f, 0.75f);

            MakeText(iceVendorDialogueOverlay, "ÔNG BA BÁN ĐÁ", 22, new Color(0.5f, 0.85f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.72f, 1f, 0.95f);
            iceVendorDialogueTxt = MakeText(iceVendorDialogueOverlay, "", 18, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0.04f, 0.35f, 0.96f, 0.70f);
            iceVendorDialogueTxt.enableWordWrapping = true;

            var okBtnGO = MakeRoundedPanel(iceVendorDialogueOverlay, "OkBtn", new Color(0.20f, 0.60f, 0.95f, 1f), 16, 0.30f, 0.06f, 0.70f, 0.30f);
            MakeText(okBtnGO, "OK", 20, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            okBtnGO.AddComponent<Button>().onClick.AddListener(OnIceVendorDialogueNext);

            iceVendorDialogueOverlay.SetActive(false);
        }

        private void OnCallOngBa()
        {
            // Kiểm tra điều kiện
            var stats = FindAnyObjectByType<Player.PlayerStats>();
            if (stats == null) return;

            float icePct = stats.IceLevel / Core.Constants.ICE_MAX * 100f;
            if (icePct > 5f)
            {
                ShowIceVendorLine($"Đá còn {icePct:F0}%, chưa cần gọi tôi đâu!");
                return;
            }

            if (Systems.IceVendorManager.Instance != null && Systems.IceVendorManager.Instance.IsDelivering)
            {
                ShowIceVendorLine("Tôi đang trên đường rồi, chờ tôi chút nhé!");
                return;
            }

            if (!stats.CanAfford(5000))
            {
                ShowIceVendorLine("Trời ơi, anh không đủ 5.000đ à? Tôi bận lắm đó!");
                return;
            }

            // Bắt đầu hội thoại
            iceDialogueStep = 0;
            ShowIceVendorLine("Ờ, anh cần đá hả? Tôi đang chạy trên đường rồi, chờ tôi chút xíu nhé!");
        }

        private void OnIceVendorDialogueNext()
        {
            iceDialogueStep++;
            switch (iceDialogueStep)
            {
                case 1:
                    // Gọi ông Ba thật sự
                    bool success = Systems.IceVendorManager.Instance != null &&
                                   Systems.IceVendorManager.Instance.CallIceVendor();
                    if (success)
                        ShowIceVendorLine("Ổng đang trên đường rồi! Vài phút nữa tới chỗ anh đó, đợi chút!");
                    else
                        ShowIceVendorLine("Ủa... có vẻ có lỗi gì rồi, thử lại sau nhé!");
                    break;
                case 2:
                    iceVendorDialogueOverlay.SetActive(false);
                    break;
                default:
                    iceVendorDialogueOverlay.SetActive(false);
                    break;
            }
        }

        private void ShowIceVendorLine(string text)
        {
            if (iceVendorDialogueOverlay == null) return;
            iceVendorDialogueOverlay.SetActive(true);
            if (iceVendorDialogueTxt != null) iceVendorDialogueTxt.text = text;
        }

        private void RefreshIceVendorCard()
        {
            if (iceVendorIceTxt == null || iceVendorCallBtn == null) return;

            var stats = FindAnyObjectByType<Player.PlayerStats>();
            if (stats == null) return;

            float icePct = stats.IceLevel / Core.Constants.ICE_MAX * 100f;
            iceVendorIceTxt.text = $"Đá hiện tại: {icePct:F0}%";

            bool isDelivering = Systems.IceVendorManager.Instance != null &&
                                Systems.IceVendorManager.Instance.IsDelivering;
            bool canCall = icePct <= 5f && !isDelivering && stats.CanAfford(5000);

            Color iceBlue = new Color(0.20f, 0.60f, 0.95f, 1f);
            Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            var img = iceVendorCallBtn.GetComponent<Image>();
            if (img != null) img.color = canCall ? iceBlue : disabledColor;
            iceVendorCallBtn.interactable = canCall;

            if (iceVendorDialogueTxt != null && iceVendorDialogueOverlay != null &&
                !iceVendorDialogueOverlay.activeSelf)
            {
                // Hiển thị trạng thái ông Ba đang giao
                if (isDelivering)
                    iceVendorIceTxt.text += " - Ông Ba đang trên đường!";
            }
        }

        private void BuildContactEntry(GameObject parent, string icon, string name, string phone, string desc)
        {
            var card = MakeRoundedPanel(parent, "Contact_" + name, COL_RIGHT_CARD, 8, 0, 0, 1, 1);
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 90; le.preferredHeight = 90;
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.15f); shadow.effectDistance = new Vector2(0, -3);

            var iconWell = MakeRoundedPanel(card, "ContactMark", COL_TAB_ACTIVE, 6, 0.025f, 0.18f, 0.105f, 0.82f);
            MakeText(iconWell, icon, 15, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            MakeText(card, name, 20, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.14f, 0.55f, 0.55f, 0.90f);
            MakeText(card, phone, 18, COL_TAB_ACTIVE, TextAlignmentOptions.Left, FontStyles.Bold, 0.55f, 0.55f, 0.98f, 0.90f);
            MakeText(card, desc, 15, new Color(0.4f, 0.4f, 0.4f, 1f), TextAlignmentOptions.Left, FontStyles.Italic, 0.14f, 0.10f, 0.98f, 0.50f);
        }

        // ================================================================
        //  HELPER FUNCTIONS FOR PROCEDURAL ROUNDED UI
        // ================================================================
        private static RectTransform MakeRect(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<RectTransform>();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static GameObject MakePanel(GameObject parent, string name, Color color, float xMin, float yMin, float xMax, float yMax)
        {
            var go = MakeRect(parent, name).gameObject;
            SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject MakeRoundedPanel(GameObject parent, string name, Color color, int radius, float xMin, float yMin, float xMax, float yMax)
        {
            var go = MakeRect(parent, name).gameObject;
            SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return go;
        }

        private static Image MakeIcon(GameObject parent, string iconKey, Color tint, float xMin, float yMin, float xMax, float yMax)
        {
            var go = MakeRect(parent, "Icon_" + iconKey).gameObject;
            SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var image = go.AddComponent<Image>();
            image.sprite = GetItemSprite(iconKey);
            image.color = image.sprite != null ? tint : Color.clear;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI MakeText(GameObject parent, string content, int fontSize, Color color, TextAlignmentOptions align, FontStyles style, float xMin, float yMin, float xMax, float yMax)
        {
            var go = MakeRect(parent, "Txt_" + content.Substring(0, Mathf.Min(content.Length, 8))).gameObject;
            SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ================================================================
        //  TAB 4: GIỎ HÀNG — Quản lý hiển thị nguyên liệu & đồ đã mua
        // ================================================================
        private GameObject BuildCartTabContent(GameObject parent)
        {
            var panel = MakeRoundedPanel(parent, "CartTabContent", COL_RIGHT_PANEL, 8, 0.005f, 0.005f, 0.995f, 0.995f);
            MakeText(panel, "KHO NGUYÊN LIỆU", 22, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.025f, 0.91f, 0.55f, 0.97f);
            MakeText(panel, "Theo dõi lượng hàng đang có trên xe", 14, COL_TEXT_MUTED, TextAlignmentOptions.Left, FontStyles.Normal, 0.025f, 0.855f, 0.70f, 0.91f);

            var scrollArea = MakeRect(panel, "ScrollArea");
            SetAnchors(scrollArea, 0.018f, 0.02f, 0.982f, 0.845f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = MakeRect(scrollArea.gameObject, "Viewport");
            StretchFull(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            cartTabContainer = MakeRect(viewport.gameObject, "Content").gameObject;
            var cRT = cartTabContainer.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;

            var vlg = cartTabContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.padding = new RectOffset(12, 12, 12, 12);
            cartTabContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT; scroll.viewport = viewport;

            return panel;
        }

        private void RefreshCartTabUI()
        {
            if (cartTabContainer == null) return;

            // Xóa sạch card cũ
            for (int i = cartTabContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(cartTabContainer.transform.GetChild(i).gameObject);
            }

            Player.PlayerStats stats = FindAnyObjectByType<Player.PlayerStats>();

            // 1. Tiêu đề: NGUYÊN LIỆU TRÊN XE ĐẨY
            var titleGO = MakeRect(cartTabContainer, "Title_Cart").gameObject;
            var leTitle = titleGO.AddComponent<LayoutElement>();
            leTitle.minHeight = 35; leTitle.preferredHeight = 35;
            MakeText(titleGO, "TRÊN XE ĐẨY", 18, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0f, 0f, 1f, 1f);

            if (stats != null)
            {
                BuildIngredientCartCard("ingredient_tea", "Trà lài", $"{stats.TeaSupply} g");
                BuildIngredientCartCard("ingredient_sugar", "Đường cát", $"{stats.SugarSupply} g");
                BuildIngredientCartCard("ingredient_coffee", "Cà phê rang", $"{stats.CoffeeSupply} g");
                BuildIngredientCartCard("ingredient_cups", "Ly nhựa sạch", $"{stats.CupSupply} cái");
                float icePct = stats.IceLevel / Core.Constants.ICE_MAX * 100f;
                BuildIngredientCartCard("ingredient_ice", "Đá viên", $"{icePct:F0}%");
                BuildIngredientCartCard("ingredient_water", "Nước đun sôi", $"{GanhHangRong.Interaction.CartItem.BottleWater:F1} L");
            }

            // Dãn cách
            var spacer = MakeRect(cartTabContainer, "Spacer").gameObject;
            var leSpacer = spacer.AddComponent<LayoutElement>();
            leSpacer.minHeight = 20; leSpacer.preferredHeight = 20;

            // 2. Tiêu đề: VẬT PHẨM ĐÃ MUA (GIỎ HÀNG)
            var title2GO = MakeRect(cartTabContainer, "Title_Bought").gameObject;
            var leTitle2 = title2GO.AddComponent<LayoutElement>();
            leTitle2.minHeight = 35; leTitle2.preferredHeight = 35;
            MakeText(title2GO, "VẬT PHẨM ĐÃ MUA", 18, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0f, 0f, 1f, 1f);

            if (GanhHangRong.Economy.PlayerInventory.Instance != null && GanhHangRong.Economy.PlayerInventory.Instance.Items != null)
            {
                int count = 0;
                foreach (var stack in GanhHangRong.Economy.PlayerInventory.Instance.Items)
                {
                    if (stack == null || stack.item == null || stack.amount <= 0) continue;
                    BuildInventoryCartCard(stack);
                    count++;
                }

                if (count == 0)
                {
                    var emptyGO = MakeRect(cartTabContainer, "Empty_Cart").gameObject;
                    var leEmpty = emptyGO.AddComponent<LayoutElement>();
                    leEmpty.minHeight = 60; leEmpty.preferredHeight = 60;
                    MakeText(emptyGO, "Chưa mua vật phẩm nào từ cửa hàng.", 18, Color.gray, TextAlignmentOptions.Center, FontStyles.Italic, 0f, 0f, 1f, 1f);
                }
            }
        }

        private void BuildIngredientCartCard(string iconKey, string displayName, string amountStr)
        {
            var card = MakeRoundedPanel(cartTabContainer, "Ingredient_" + displayName, COL_RIGHT_PANEL, 8, 0, 0, 1, 1);
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 80; le.preferredHeight = 80;
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.15f); shadow.effectDistance = new Vector2(0, -3);

            var iconWell = MakeRoundedPanel(card, "IconWell", COL_IMAGE_WELL, 6, 0.015f, 0.10f, 0.105f, 0.90f);
            MakeIcon(iconWell, iconKey, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);

            MakeText(card, displayName, 18, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.125f, 0.5f, 0.6f, 0.9f);
            MakeText(card, "Còn lại trên xe", 14, Color.gray, TextAlignmentOptions.Left, FontStyles.Normal, 0.125f, 0.15f, 0.6f, 0.5f);
            MakeText(card, amountStr, 22, COL_TAB_ACTIVE, TextAlignmentOptions.Right, FontStyles.Bold, 0.6f, 0.15f, 0.96f, 0.85f);
        }

        private void BuildInventoryCartCard(GanhHangRong.Economy.InventoryItemStack stack)
        {
            var card = MakeRoundedPanel(cartTabContainer, "Item_" + stack.item.DisplayName, COL_RIGHT_PANEL, 8, 0, 0, 1, 1);
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 80; le.preferredHeight = 80;
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.15f); shadow.effectDistance = new Vector2(0, -3);

            // Icon Image
            var iconGO = MakeRect(card, "Icon");
            SetAnchors(iconGO, 0.02f, 0.1f, 0.12f, 0.9f);
            var img = iconGO.gameObject.AddComponent<Image>();
            img.preserveAspect = true;
            if (stack.item.icon != null)
            {
                img.sprite = stack.item.icon;
            }

            MakeText(card, stack.item.DisplayName, 18, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.15f, 0.5f, 0.6f, 0.9f);
            MakeText(card, stack.item.description, 13, Color.gray, TextAlignmentOptions.Left, FontStyles.Normal, 0.15f, 0.15f, 0.6f, 0.5f);
            MakeText(card, $"x{stack.amount}", 22, COL_TAB_ACTIVE, TextAlignmentOptions.Right, FontStyles.Bold, 0.6f, 0.15f, 0.96f, 0.85f);
        }
    }
}
