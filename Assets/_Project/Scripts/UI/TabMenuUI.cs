using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using TMPro;
using GanhHangRong.Core;
using System.Collections.Generic;

namespace GanhHangRong.UI
{
    public class TabMenuUI : MonoBehaviour
    {
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

        // ================================================================
        //  BẢNG MÀU CHUẨN KEM VÀNG & XANH LÁ (THEO ẢNH THAM KHẢO)
        // ================================================================
        private static readonly Color COL_BG_OVERLAY       = new Color(0.10f, 0.08f, 0.06f, 0.75f); // Nền đen mờ ấm
        private static readonly Color COL_WINDOW_BG        = new Color(0.55f, 0.48f, 0.42f, 1f);    // Khung viền sổ tay nâu xám
        private static readonly Color COL_TAB_ACTIVE       = new Color(0.18f, 0.63f, 0.26f, 1f);    // Xanh lá nổi bật (#2EA01A)
        private static readonly Color COL_TAB_INACTIVE     = new Color(0.35f, 0.25f, 0.18f, 1f);    // Nâu gỗ tối
        private static readonly Color COL_LEFT_PANEL       = new Color(0.28f, 0.18f, 0.12f, 1f);    // Nâu gỗ trầm (Thực đơn phục vụ)
        private static readonly Color COL_LEFT_CARD        = new Color(0.20f, 0.13f, 0.08f, 1f);    // Nâu tối cho card bên trái
        private static readonly Color COL_LEFT_CARD_ACTIVE = new Color(0.15f, 0.35f, 0.20f, 1f);    // Viền/Nền xanh khi đang chọn
        private static readonly Color COL_RIGHT_PANEL      = new Color(0.95f, 0.93f, 0.89f, 1f);    // Kem sáng ấm (Kho món ăn)
        private static readonly Color COL_RIGHT_CARD       = new Color(1.00f, 1.00f, 1.00f, 1f);    // Trắng sữa cho card bên phải
        private static readonly Color COL_RIGHT_LOCKED     = new Color(0.85f, 0.85f, 0.85f, 0.9f);  // Xám mờ cho món bị khóa
        private static readonly Color COL_BTN_SERVE        = new Color(0.18f, 0.63f, 0.26f, 1f);    // Nút + PHỤC VỤ màu xanh
        private static readonly Color COL_BTN_SELECTED     = new Color(0.35f, 0.45f, 0.38f, 1f);    // Nút khi đã phục vụ rồi
        private static readonly Color COL_BTN_LOCKED       = new Color(0.60f, 0.60f, 0.60f, 1f);    // Nút Khóa
        private static readonly Color COL_TEXT_DARK        = new Color(0.20f, 0.15f, 0.10f, 1f);    // Chữ đậm trên nền sáng
        private static readonly Color COL_TEXT_LIGHT       = new Color(0.98f, 0.95f, 0.90f, 1f);    // Chữ sáng trên nền tối
        private static readonly Color COL_PRICE_GOLD       = new Color(0.80f, 0.60f, 0.10f, 1f);    // Chữ giá tiền màu vàng đồng

        // ======== Dữ liệu các món ========
        private struct MenuItem
        {
            public string name;
            public int price;
            public string emoji;
            public string recipe;
            public bool isUnlocked;
            public int orderId;
            public MenuItem(string n, int p, string e, string r, bool unlocked = true, int id = -1)
            { name = n; price = p; emoji = e; recipe = r; isUnlocked = unlocked; orderId = id; }
        }

        private static readonly MenuItem[] allItems = new MenuItem[]
        {
            new MenuItem("Cà Phê Đen Đá", Constants.COFFEE_SELL_PRICE, "☕",
                "Công thức:\n1. Lấy ly sạch\n2. Cho 30g cà phê\n3. Rót 200ml nước sôi\n4. Thêm đá\n5. Phục vụ", true, 1),
            new MenuItem("Trà Đá Nguyên Chất", Constants.TRA_DA_SELL_PRICE, "🍵",
                "Công thức:\n1. Lấy ly sạch\n2. Cho 50g trà\n3. Rót 200ml nước sôi\n4. Thêm đá\n5. Phục vụ", true, 0),
            new MenuItem("Nước Chanh Đá", 12000, "🍹", "Mở khóa sau", false, 2),
            new MenuItem("Nước Mía Tươi", 10000, "🎋", "Mở khóa sau", false, 3),
            new MenuItem("Trà Sữa Trân Châu", 25000, "🧋", "Mở khóa sau", false, 4),
            new MenuItem("Cơm Tấm Sườn Bì", 45000, "🍛", "Mở khóa sau", false, 5),
            new MenuItem("Phở Bò Hà Nội", 40000, "🍜", "Mở khóa sau", false, 6),
            new MenuItem("Gỏi Cuốn Tôm Thịt", 15000, "🥗", "Mở khóa sau", false, 7),
            new MenuItem("Dừa Tươi Ướp Lạnh", 20000, "🥥", "Mở khóa sau", false, 8)
        };

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
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<TabMenuUI>() == null)
            {
                var go = new GameObject("TabMenuUI_Auto");
                DontDestroyOnLoad(go);
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

        private void UpdateActiveServingOrderIds()
        {
            isServingInitialized = true;
            ActiveServingOrderIds.Clear();
            foreach (var item in servingMenu)
            {
                if (item.orderId >= 0) ActiveServingOrderIds.Add(item.orderId);
            }
        }

        private void Awake()
        {
            EnsureEventSystem();
            // Khởi tạo thực đơn mặc định ban đầu là 2 món mở khóa
            if (servingMenu.Count == 0)
            {
                foreach (var item in allItems)
                    if (item.isUnlocked) servingMenu.Add(item);
            }
            UpdateActiveServingOrderIds();

            BuildFullUI();
            CloseMenu();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var esGO = new GameObject("EventSystem_Auto");
            DontDestroyOnLoad(esGO);
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
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
            RefreshServingMenuUI();
            RefreshInventoryCardsUI();
            RefreshCartTabUI();
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
            
            // Khung Sổ Tay chính bo tròn 28px
            mainWindow = MakeRoundedPanel(menuRoot, "MainWindow", COL_WINDOW_BG, 28, 0.05f, 0.05f, 0.95f, 0.95f);
            var mainShadow = mainWindow.AddComponent<Shadow>();
            mainShadow.effectColor = new Color(0, 0, 0, 0.4f);
            mainShadow.effectDistance = new Vector2(0, -10);

            BuildTopHeader(mainWindow);

            // Container cho các Tab nội dung
            var contentArea = MakeRect(mainWindow, "ContentArea");
            SetAnchors(contentArea, 0.02f, 0.02f, 0.98f, 0.86f);

            tabMenuContent = BuildMenuTabContent(contentArea.gameObject);
            tabRecipeContent = BuildRecipeTabContent(contentArea.gameObject);
            tabContactContent = BuildContactTabContent(contentArea.gameObject);
            tabCartContent = BuildCartTabContent(contentArea.gameObject);

            SwitchTab(0);
        }

        private void BuildTopHeader(GameObject parent)
        {
            var header = MakeRect(parent, "TopHeader");
            SetAnchors(header, 0.02f, 0.86f, 0.98f, 0.98f);

            // Tiêu đề "MENU CHÍNH" ở giữa
            MakeText(header.gameObject, "MENU CHÍNH", 32, COL_TEXT_LIGHT, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.55f, 1f, 0.95f);

            // Nút đóng ✕ góc trên phải
            var closeBtnGO = MakeRoundedPanel(header.gameObject, "CloseBtn", COL_LEFT_CARD, 16, 0.95f, 0.55f, 0.99f, 0.95f);
            MakeText(closeBtnGO, "✕", 22, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            closeBtnGO.AddComponent<Button>().onClick.AddListener(CloseMenu);

            // --- Tab Buttons Area (dạng viên thuốc Pill Buttons) ---
            var tabArea = MakeRect(header.gameObject, "TabArea");
            SetAnchors(tabArea, 0.05f, 0.02f, 0.95f, 0.48f);
            
            var hlg = tabArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20; hlg.childControlWidth = true; hlg.childControlHeight = true; hlg.childForceExpandWidth = true;

            tabBtnMenu = CreatePillTabButton(tabArea.gameObject, "MENU", () => SwitchTab(0), out tabTxtMenu);
            tabBtnRecipe = CreatePillTabButton(tabArea.gameObject, "📖 CÔNG THỨC", () => SwitchTab(1), out tabTxtRecipe);
            tabBtnContact = CreatePillTabButton(tabArea.gameObject, "📱 ĐIỆN THOẠI", () => SwitchTab(2), out tabTxtContact);
            tabBtnCart = CreatePillTabButton(tabArea.gameObject, "🛒 GIỎ HÀNG", () => SwitchTab(3), out tabTxtCart);
        }

        private Button CreatePillTabButton(GameObject parent, string label, UnityEngine.Events.UnityAction action, out TextMeshProUGUI txtRef)
        {
            var btnGO = MakeRoundedPanel(parent, "TabBtn_" + label, COL_TAB_INACTIVE, 24, 0, 0, 1, 1);
            var shadow = btnGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.2f); shadow.effectDistance = new Vector2(0, -3);
            
            txtRef = MakeText(btnGO, label, 20, COL_TEXT_LIGHT, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
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
            if (txt != null) txt.color = isActive ? Color.white : new Color(0.85f, 0.80f, 0.75f, 1f);
        }

        // ================================================================
        //  TAB 1: MENU — Trái: Thực đơn phục vụ / Phải: Kho món ăn
        // ================================================================
        private GameObject BuildMenuTabContent(GameObject parent)
        {
            var panel = new GameObject("MenuTabContent");
            panel.transform.SetParent(parent.transform, false);
            StretchFull(panel.AddComponent<RectTransform>());

            // --- Panel Trái (THỰC ĐƠN PHỤC VỤ - 35%) ---
            var leftPanel = MakeRoundedPanel(panel, "LeftPanel", COL_LEFT_PANEL, 20, 0.01f, 0.01f, 0.35f, 0.99f);
            var leftShadow = leftPanel.AddComponent<Shadow>();
            leftShadow.effectColor = new Color(0, 0, 0, 0.25f); leftShadow.effectDistance = new Vector2(0, -4);

            MakeText(leftPanel, "THỰC ĐƠN PHỤC VỤ", 22, COL_TEXT_LIGHT, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.91f, 1f, 0.98f);
            servingCountText = MakeText(leftPanel, "Hiện tại: 0/5 món", 16, new Color(0.8f, 0.8f, 0.8f, 1f), TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0.86f, 1f, 0.91f);

            var leftScrollArea = MakeRect(leftPanel, "ScrollArea");
            SetAnchors(leftScrollArea, 0.04f, 0.02f, 0.96f, 0.85f);
            var leftScroll = leftScrollArea.gameObject.AddComponent<ScrollRect>();
            leftScroll.vertical = true; leftScroll.horizontal = false;
            
            var leftViewport = MakeRect(leftScrollArea.gameObject, "Viewport");
            StretchFull(leftViewport);
            leftViewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            leftViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            leftServingContainer = MakeRect(leftViewport.gameObject, "Content").gameObject;
            var lRT = leftServingContainer.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0, 1); lRT.anchorMax = new Vector2(1, 1);
            lRT.pivot = new Vector2(0.5f, 1); lRT.anchoredPosition = Vector2.zero;
            
            var vlg = leftServingContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
            leftServingContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            leftScroll.content = lRT; leftScroll.viewport = leftViewport;

            // --- Panel Phải (KHO MÓN ĂN - 65%) ---
            var rightPanel = MakeRoundedPanel(panel, "RightPanel", COL_RIGHT_PANEL, 24, 0.37f, 0.01f, 0.99f, 0.99f);
            var rightShadow = rightPanel.AddComponent<Shadow>();
            rightShadow.effectColor = new Color(0, 0, 0, 0.25f); rightShadow.effectDistance = new Vector2(0, -4);

            MakeText(rightPanel, "KHO MÓN ĂN", 24, COL_TEXT_DARK, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.91f, 1f, 0.98f);
            MakeText(rightPanel, "Tất cả món đã học", 15, Color.gray, TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0.86f, 1f, 0.91f);

            // Trang trí góc trên phải
            var cfgBtn = MakeRoundedPanel(rightPanel, "CfgBtn", Color.white, 12, 0.88f, 0.89f, 0.93f, 0.96f);
            MakeText(cfgBtn, "⚙", 18, COL_TEXT_DARK, TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0f, 1f, 1f);
            var filterBtn = MakeRoundedPanel(rightPanel, "FilterBtn", Color.white, 12, 0.94f, 0.89f, 0.98f, 0.96f);
            MakeText(filterBtn, "▼", 14, COL_TEXT_DARK, TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0f, 1f, 1f);

            var rightScrollArea = MakeRect(rightPanel, "ScrollArea");
            SetAnchors(rightScrollArea, 0.02f, 0.02f, 0.98f, 0.85f);
            var rightScroll = rightScrollArea.gameObject.AddComponent<ScrollRect>();
            rightScroll.vertical = true; rightScroll.horizontal = false;
            
            var rightViewport = MakeRect(rightScrollArea.gameObject, "Viewport");
            StretchFull(rightViewport);
            rightViewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            rightViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            rightInventoryGrid = MakeRect(rightViewport.gameObject, "Content").gameObject;
            var rRT = rightInventoryGrid.GetComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0, 1); rRT.anchorMax = new Vector2(1, 1);
            rRT.pivot = new Vector2(0.5f, 1); rRT.anchoredPosition = Vector2.zero;
            
            var grid = rightInventoryGrid.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3; grid.cellSize = new Vector2(240, 280);
            grid.spacing = new Vector2(18, 18); grid.padding = new RectOffset(16, 16, 16, 16);
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
            var card = MakeRoundedPanel(parent, "InvCard_" + item.name.Replace(" ", ""), item.isUnlocked ? COL_RIGHT_CARD : COL_RIGHT_LOCKED, 20, 0, 0, 1, 1);
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.12f); shadow.effectDistance = new Vector2(0, -3);

            if (item.isUnlocked)
            {
                MakeText(card, item.emoji, 65, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0.42f, 1f, 0.90f);
                MakeText(card, "⭐⭐⭐", 14, COL_PRICE_GOLD, TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0.36f, 1f, 0.46f);
                MakeText(card, item.name, 18, COL_TEXT_DARK, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.22f, 1f, 0.36f);
                MakeText(card, $"🟡 {item.price:N0} đ", 16, COL_PRICE_GOLD, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.13f, 1f, 0.24f);

                bool isAlreadyServing = servingMenu.Exists(m => m.name == item.name);
                Color btnColor = isAlreadyServing ? COL_BTN_SELECTED : COL_BTN_SERVE;
                string btnText = isAlreadyServing ? "✓ ĐANG PHỤC VỤ" : "+ PHỤC VỤ";

                var btnGO = MakeRoundedPanel(card, "ServeBtn", btnColor, 20, 0.08f, 0.02f, 0.92f, 0.13f);
                MakeText(btnGO, btnText, 15, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
                
                if (!isAlreadyServing)
                {
                    var btn = btnGO.AddComponent<Button>();
                    btn.onClick.AddListener(() => {
                        if (servingMenu.Count < 5 && !servingMenu.Exists(m => m.name == item.name)) {
                            servingMenu.Add(item);
                            RefreshServingMenuUI();
                            RefreshInventoryCardsUI();
                        }
                    });
                }
            }
            else
            {
                MakeText(card, "🔒", 60, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0.40f, 1f, 0.88f);
                MakeText(card, item.name, 17, new Color(0.4f, 0.4f, 0.4f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.22f, 1f, 0.38f);
                MakeText(card, $"🟡 {item.price:N0} đ", 15, Color.gray, TextAlignmentOptions.Center, FontStyles.Normal, 0f, 0.13f, 1f, 0.23f);

                var btnGO = MakeRoundedPanel(card, "LockBtn", COL_BTN_LOCKED, 20, 0.08f, 0.02f, 0.92f, 0.13f);
                MakeText(btnGO, "Khóa", 15, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            }
        }

        private void RefreshServingMenuUI()
        {
            UpdateActiveServingOrderIds();
            if (servingCountText != null)
                servingCountText.text = $"Hiện tại: {servingMenu.Count}/5 món";
            
            if (leftServingContainer == null) return;

            foreach (Transform child in leftServingContainer.transform)
                Destroy(child.gameObject);

            if (servingMenu.Count == 0)
            {
                var emptyRow = MakeRect(leftServingContainer, "EmptyText").gameObject;
                emptyRow.AddComponent<LayoutElement>().minHeight = 100;
                MakeText(emptyRow, "Chưa chọn món nào để bán.\nHãy nhấn '+ PHỤC VỤ' bên phải ->", 15, Color.gray, TextAlignmentOptions.Center, FontStyles.Italic, 0f, 0f, 1f, 1f);
                return;
            }

            foreach (var item in servingMenu)
            {
                var row = MakeRoundedPanel(leftServingContainer, "ServeItem", COL_LEFT_CARD, 16, 0, 0, 1, 1);
                row.AddComponent<LayoutElement>().minHeight = 110;
                
                // Viền sáng bóng bên trong thẻ phục vụ
                var outline = MakeRoundedPanel(row, "Border", COL_LEFT_CARD_ACTIVE, 16, 0.01f, 0.03f, 0.99f, 0.97f);
                outline.transform.SetAsFirstSibling();

                // Emoji món
                MakeText(row, item.emoji, 50, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0.02f, 0.15f, 0.28f, 0.85f);
                
                // Tên món
                MakeText(row, $"[{item.name}]", 18, COL_TEXT_LIGHT, TextAlignmentOptions.Left, FontStyles.Bold, 0.30f, 0.55f, 0.95f, 0.92f);
                
                // Giá tiền kèm icon vàng
                MakeText(row, $"🟡 {item.price:N0} đ", 15, COL_PRICE_GOLD, TextAlignmentOptions.Left, FontStyles.Bold, 0.30f, 0.32f, 0.95f, 0.55f);

                // Huy hiệu "ĐANG PHỤC VỤ" màu xanh lá
                var badgeGO = MakeRoundedPanel(row, "Badge", COL_TAB_ACTIVE, 14, 0.30f, 0.05f, 0.72f, 0.28f);
                MakeText(badgeGO, "ĐANG PHỤC VỤ", 12, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);

                // Nút BỎ món (màu đỏ gạch nhỏ ở góc dưới phải)
                var removeBtnGO = MakeRoundedPanel(row, "RemoveBtn", new Color(0.7f, 0.2f, 0.2f, 1f), 14, 0.75f, 0.05f, 0.96f, 0.28f);
                MakeText(removeBtnGO, "✕ BỎ", 13, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
                
                var capturedItem = item;
                removeBtnGO.AddComponent<Button>().onClick.AddListener(() => {
                    servingMenu.Remove(capturedItem);
                    RefreshServingMenuUI();
                    RefreshInventoryCardsUI();
                });
            }
        }

        // ================================================================
        //  TAB 2: CÔNG THỨC & TAB 3: ĐIỆN THOẠI (DESIGN BO GÓC SANG TRỌNG)
        // ================================================================
        private GameObject BuildRecipeTabContent(GameObject parent)
        {
            var panel = new GameObject("RecipeTabContent");
            panel.transform.SetParent(parent.transform, false);
            StretchFull(panel.AddComponent<RectTransform>());

            var scrollArea = MakeRect(panel, "ScrollArea");
            SetAnchors(scrollArea, 0.02f, 0.02f, 0.98f, 0.98f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = MakeRect(scrollArea.gameObject, "Viewport");
            StretchFull(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = MakeRect(viewport.gameObject, "Content").gameObject;
            var cRT = contentGO.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1); cRT.anchoredPosition = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.padding = new RectOffset(20, 20, 20, 20);
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT; scroll.viewport = viewport;

            foreach (var item in allItems)
            {
                var card = MakeRoundedPanel(contentGO, "Recipe_" + item.name, COL_RIGHT_PANEL, 20, 0, 0, 1, 1);
                var le = card.AddComponent<LayoutElement>();
                le.minHeight = 110; le.preferredHeight = 110;
                var shadow = card.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.15f); shadow.effectDistance = new Vector2(0, -3);

                MakeText(card, item.emoji, 50, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0.02f, 0.1f, 0.15f, 0.9f);
                MakeText(card, item.name, 20, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.16f, 0.65f, 0.50f, 0.95f);
                MakeText(card, $"🟡 {item.price:N0} VNĐ", 16, COL_PRICE_GOLD, TextAlignmentOptions.Left, FontStyles.Bold, 0.16f, 0.40f, 0.50f, 0.65f);
                MakeText(card, item.recipe, 15, new Color(0.3f, 0.3f, 0.3f, 1f), TextAlignmentOptions.Left, FontStyles.Normal, 0.50f, 0.05f, 0.98f, 0.95f);
            }
            return panel;
        }

        // ── Ice Vendor dialogue overlay references ──
        private GameObject iceVendorDialogueOverlay;
        private TextMeshProUGUI iceVendorDialogueTxt;
        private Button iceVendorCallBtn;
        private TextMeshProUGUI iceVendorIceTxt;
        private int iceDialogueStep = 0;

        private GameObject BuildContactTabContent(GameObject parent)
        {
            var panel = new GameObject("ContactTabContent");
            panel.transform.SetParent(parent.transform, false);
            StretchFull(panel.AddComponent<RectTransform>());

            var scrollArea = MakeRect(panel, "ScrollArea");
            SetAnchors(scrollArea, 0.02f, 0.02f, 0.98f, 0.98f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = MakeRect(scrollArea.gameObject, "Viewport");
            StretchFull(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = MakeRect(viewport.gameObject, "Content").gameObject;
            var cRT = contentGO.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1); cRT.anchoredPosition = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.padding = new RectOffset(20, 20, 20, 20);
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT; scroll.viewport = viewport;

            // ─── Ông Ba Bán Đá (Special card) ───
            BuildIceVendorCard(contentGO);

            BuildContactEntry(contentGO, "📞", "Chị Ba", "0912 345 678", "Cung cấp nguyên liệu trà, cà phê mỗi sáng");
            BuildContactEntry(contentGO, "📞", "Nhà Cung Cấp Trà", "0987 654 321", "Giao trà lá tươi, giá sỉ ưu đãi");
            BuildContactEntry(contentGO, "🔧", "Thợ Sửa Xe", "0901 222 333", "Sửa chữa gánh hàng rong khi bị hỏng");
            BuildContactEntry(contentGO, "🏥", "Trạm Y Tế Phường", "0909 111 222", "Khám sức khoẻ và phục hồi thể lực khi mệt");
            BuildContactEntry(contentGO, "👮", "Công An Phường", "0908 888 999", "Hỗ trợ an ninh đường phố và trật tự đô thị");

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

            var card = MakeRoundedPanel(parent, "Contact_OngBa", cardBg, 20, 0, 0, 1, 1);
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 130; le.preferredHeight = 130;
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.18f); shadow.effectDistance = new Vector2(0, -4);

            // Icon đá
            MakeText(card, "🧊", 44, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0.01f, 0.15f, 0.13f, 0.88f);

            // Tên + số
            MakeText(card, "Ông Ba Bán Đá", 20, iceBlueDark, TextAlignmentOptions.Left, FontStyles.Bold, 0.15f, 0.62f, 0.60f, 0.92f);
            MakeText(card, "0918 123 456", 17, iceBlueLight, TextAlignmentOptions.Left, FontStyles.Bold, 0.60f, 0.62f, 0.98f, 0.92f);
            MakeText(card, "Giao đá nhanh — 5.000đ/thùng (100%)", 14, new Color(0.3f, 0.3f, 0.4f, 1f), TextAlignmentOptions.Left, FontStyles.Italic, 0.15f, 0.42f, 0.99f, 0.60f);

            // Thanh đá hiện tại
            iceVendorIceTxt = MakeText(card, "Đá: ...%", 15, iceBlueDark, TextAlignmentOptions.Left, FontStyles.Bold, 0.15f, 0.22f, 0.60f, 0.40f);

            // Nút GỌI
            var btnGO = MakeRoundedPanel(card, "CallBtn_OngBa", iceBlueLight, 20, 0.63f, 0.10f, 0.97f, 0.45f);
            MakeText(btnGO, "📞 Gọi (5.000đ)", 15, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            iceVendorCallBtn = btnGO.AddComponent<Button>();
            iceVendorCallBtn.targetGraphic = btnGO.GetComponent<Image>();
            iceVendorCallBtn.onClick.AddListener(OnCallOngBa);
        }

        private void BuildIceVendorDialogue(GameObject parent)
        {
            // Full-panel overlay mờ
            iceVendorDialogueOverlay = MakeRoundedPanel(parent, "IceVendorDialogue", new Color(0.05f, 0.10f, 0.20f, 0.92f), 24, 0.10f, 0.25f, 0.90f, 0.75f);

            MakeText(iceVendorDialogueOverlay, "🧊 Ông Ba Bán Đá", 22, new Color(0.5f, 0.85f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0.72f, 1f, 0.95f);
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
                    iceVendorIceTxt.text += " — Ông Ba đang trên đường! 🚴";
            }
        }

        private void BuildContactEntry(GameObject parent, string icon, string name, string phone, string desc)
        {
            var card = MakeRoundedPanel(parent, "Contact_" + name, COL_RIGHT_PANEL, 20, 0, 0, 1, 1);
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 90; le.preferredHeight = 90;
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.15f); shadow.effectDistance = new Vector2(0, -3);

            MakeText(card, icon, 40, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0.02f, 0.15f, 0.12f, 0.85f);
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
            var panel = new GameObject("CartTabContent");
            panel.transform.SetParent(parent.transform, false);
            StretchFull(panel.AddComponent<RectTransform>());

            var scrollArea = MakeRect(panel, "ScrollArea");
            SetAnchors(scrollArea, 0.02f, 0.02f, 0.98f, 0.98f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = MakeRect(scrollArea.gameObject, "Viewport");
            StretchFull(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            cartTabContainer = MakeRect(viewport.gameObject, "Content").gameObject;
            var cRT = cartTabContainer.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1); cRT.anchoredPosition = Vector2.zero;

            var vlg = cartTabContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.padding = new RectOffset(20, 20, 20, 20);
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
            var titleGO = new GameObject("Title_Cart");
            titleGO.transform.SetParent(cartTabContainer.transform, false);
            var leTitle = titleGO.AddComponent<LayoutElement>();
            leTitle.minHeight = 35; leTitle.preferredHeight = 35;
            MakeText(titleGO, "📦 NGUYÊN LIỆU TRÊN XE ĐẨY", 22, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0f, 0f, 1f, 1f);

            if (stats != null)
            {
                BuildIngredientCartCard("hu_tra", "Hũ Trà Lài", $"{stats.TeaSupply} g");
                BuildIngredientCartCard("hu_duong", "Hũ Đường Cát", $"{stats.SugarSupply} g");
                BuildIngredientCartCard("hu_tra", "Hũ Cà Phê Phố Cổ", $"{stats.CoffeeSupply} g");
                BuildIngredientCartCard("ly_cups", "Lốc Ly Nhựa Sạch", $"{stats.CupSupply} cái");
                BuildIngredientCartCard("ice_box", "Bình Nước Đun Sôi", $"{GanhHangRong.Interaction.CartItem.BottleWater:F1} L");
            }

            // Dãn cách
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(cartTabContainer.transform, false);
            var leSpacer = spacer.AddComponent<LayoutElement>();
            leSpacer.minHeight = 20; leSpacer.preferredHeight = 20;

            // 2. Tiêu đề: VẬT PHẨM ĐÃ MUA (GIỎ HÀNG)
            var title2GO = new GameObject("Title_Bought");
            title2GO.transform.SetParent(cartTabContainer.transform, false);
            var leTitle2 = title2GO.AddComponent<LayoutElement>();
            leTitle2.minHeight = 35; leTitle2.preferredHeight = 35;
            MakeText(title2GO, "🛒 VẬT PHẨM ĐÃ MUA (GIỎ HÀNG)", 22, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0f, 0f, 1f, 1f);

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
                    var emptyGO = new GameObject("Empty_Cart");
                    emptyGO.transform.SetParent(cartTabContainer.transform, false);
                    var leEmpty = emptyGO.AddComponent<LayoutElement>();
                    leEmpty.minHeight = 60; leEmpty.preferredHeight = 60;
                    MakeText(emptyGO, "Chưa mua vật phẩm nào từ cửa hàng.", 18, Color.gray, TextAlignmentOptions.Center, FontStyles.Italic, 0f, 0f, 1f, 1f);
                }
            }
        }

        private void BuildIngredientCartCard(string spriteName, string displayName, string amountStr)
        {
            var card = MakeRoundedPanel(cartTabContainer, "Ingredient_" + displayName, COL_RIGHT_PANEL, 20, 0, 0, 1, 1);
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 80; le.preferredHeight = 80;
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.15f); shadow.effectDistance = new Vector2(0, -3);

            // Icon Image
            var iconGO = MakeRect(card, "Icon");
            SetAnchors(iconGO, 0.02f, 0.1f, 0.12f, 0.9f);
            var img = iconGO.gameObject.AddComponent<Image>();
            img.preserveAspect = true;

            // Load Sprite
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets(spriteName + " t:Sprite");
            if (guids != null && guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
#endif
            if (img.sprite == null)
            {
                Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (Sprite s in allSprites)
                {
                    if (s != null && s.name.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        img.sprite = s;
                        break;
                    }
                }
            }

            MakeText(card, displayName, 18, COL_TEXT_DARK, TextAlignmentOptions.Left, FontStyles.Bold, 0.15f, 0.5f, 0.6f, 0.9f);
            MakeText(card, "Còn lại trên xe:", 14, Color.gray, TextAlignmentOptions.Left, FontStyles.Normal, 0.15f, 0.15f, 0.6f, 0.5f);
            MakeText(card, amountStr, 22, COL_TAB_ACTIVE, TextAlignmentOptions.Right, FontStyles.Bold, 0.6f, 0.15f, 0.96f, 0.85f);
        }

        private void BuildInventoryCartCard(GanhHangRong.Economy.InventoryItemStack stack)
        {
            var card = MakeRoundedPanel(cartTabContainer, "Item_" + stack.item.DisplayName, COL_RIGHT_PANEL, 20, 0, 0, 1, 1);
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
