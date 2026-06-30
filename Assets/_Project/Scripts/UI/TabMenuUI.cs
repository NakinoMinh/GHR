using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using GanhHangRong.Core;

namespace GanhHangRong.UI
{
    /// <summary>
    /// Sổ Tay UI — 3 tab: MENU (Thực Đơn), CÔNG THỨC, ĐIỆN THOẠI.
    /// Tự xây dựng toàn bộ UI khi game chạy, không cần kéo thả Inspector.
    /// Nhấn Tab để mở/đóng.
    /// </summary>
    public class TabMenuUI : MonoBehaviour
    {
        // ======== Nội bộ ========
        private Canvas canvas;
        private GameObject menuRoot;
        private GameObject menuPanel;
        private GameObject recipePanel;
        private GameObject phonePanel;
        private Image menuTabBg, recipeTabBg, phoneTabBg;
        private bool isOpen = false;
        private int activeTab = 0;

        // ======== Bảng màu (theo ảnh gốc — phong cách khung gỗ tối) ========
        static readonly Color COL_FRAME       = new Color(0.30f, 0.20f, 0.12f, 1f);    // viền khung gỗ
        static readonly Color COL_BG          = new Color(0.12f, 0.14f, 0.18f, 0.97f);  // nền chính (xanh đen)
        static readonly Color COL_PANEL       = new Color(0.16f, 0.18f, 0.22f, 1f);     // nền panel nội dung
        static readonly Color COL_CARD        = new Color(0.20f, 0.22f, 0.28f, 1f);     // nền mỗi ô món ăn
        static readonly Color COL_CARD_HOVER  = new Color(0.24f, 0.26f, 0.32f, 1f);
        static readonly Color COL_TAB_ACTIVE  = new Color(0.22f, 0.58f, 0.32f, 1f);     // xanh lá tab chọn
        static readonly Color COL_TAB_INACTIVE= new Color(0.18f, 0.20f, 0.24f, 1f);
        static readonly Color COL_TITLE       = new Color(1f, 0.92f, 0.70f, 1f);        // vàng kem
        static readonly Color COL_TEXT        = new Color(0.92f, 0.93f, 0.95f, 1f);
        static readonly Color COL_PRICE       = new Color(0.70f, 0.85f, 0.70f, 1f);     // xanh lá nhạt
        static readonly Color COL_BTN_BUY     = new Color(0.20f, 0.60f, 0.35f, 1f);     // nút MUA
        static readonly Color COL_CLOSE       = new Color(0.75f, 0.18f, 0.18f, 1f);

        // ======== Dữ liệu các món (từ ChapterOrderCatalog + ảnh mô tả) ========
        private struct MenuItem
        {
            public string name;
            public int price;
            public string emoji;
            public string recipe;
            public MenuItem(string n, int p, string e, string r)
            { name = n; price = p; emoji = e; recipe = r; }
        }

        private static readonly MenuItem[] allItems = new MenuItem[]
        {
            new MenuItem("Trà Đá\nNguyên Chất", Constants.TRA_DA_SELL_PRICE, "🍵",
                "Công thức:\n1. Lấy ly sạch\n2. Cho 50g trà\n3. Rót 200ml nước sôi\n4. Thêm đá\n5. Phục vụ"),
            new MenuItem("Cà Phê\nĐen Đá", Constants.COFFEE_SELL_PRICE, "☕",
                "Công thức:\n1. Lấy ly sạch\n2. Cho 30g cà phê\n3. Rót 200ml nước sôi\n4. Thêm đá\n5. Phục vụ"),
            new MenuItem("Cà Phê\nSữa Đá", 20000, "☕",
                "Công thức:\n1. Lấy ly sạch\n2. Cho 30g cà phê\n3. Rót nước sôi\n4. Thêm sữa đặc\n5. Thêm đá"),
            new MenuItem("Nước\nSâm Lạnh", 10000, "🧃",
                "Công thức:\n1. Nấu sâm\n2. Để nguội\n3. Rót vào ly\n4. Thêm đá"),
            new MenuItem("Bánh Mì\nThịt Kẹp", 18000, "🥖",
                "Công thức:\n1. Kẹp bánh mì lên vỉ\n2. Phết sa tế muối ớt\n3. Nướng vàng 2 mặt\n4. Rắc chà bông"),
            new MenuItem("Món Ăn\nKèm", 15000, "🍪",
                "Các loại bánh và snack đi kèm thức uống")
        };

        // ================================================================
        //  LIFECYCLE
        // ================================================================
        private void Awake()
        {
            BuildFullUI();
            CloseMenu();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }
            // Đang mở mà nhấn Escape thì đóng
            if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseMenu();
            }
        }

        public void ToggleMenu()
        {
            if (isOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            isOpen = true;
            menuRoot.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SwitchTab(0);
        }

        public void CloseMenu()
        {
            isOpen = false;
            if (menuRoot != null) menuRoot.SetActive(false);
            if (GameManager.HasInstance && GameManager.Instance.IsPlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void SwitchTab(int idx)
        {
            activeTab = idx;
            menuPanel.SetActive(idx == 0);
            recipePanel.SetActive(idx == 1);
            phonePanel.SetActive(idx == 2);

            menuTabBg.color   = idx == 0 ? COL_TAB_ACTIVE : COL_TAB_INACTIVE;
            recipeTabBg.color = idx == 1 ? COL_TAB_ACTIVE : COL_TAB_INACTIVE;
            phoneTabBg.color  = idx == 2 ? COL_TAB_ACTIVE : COL_TAB_INACTIVE;
        }

        // ================================================================
        //  XÂY DỰNG UI
        // ================================================================
        private void BuildFullUI()
        {
            // --- Canvas ---
            var cGO = new GameObject("[SoTay] Canvas");
            DontDestroyOnLoad(cGO);
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = cGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            cGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                DontDestroyOnLoad(es);
            }

            // --- Overlay mờ ---
            var overlay = MakeRect(cGO, "Overlay");
            StretchFull(overlay);
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.45f);
            overlayImg.raycastTarget = true;

            // --- MenuRoot (toàn bộ cửa sổ Sổ Tay) ---
            menuRoot = MakePanel(overlay.gameObject, "SoTayRoot", COL_FRAME,
                0.12f, 0.06f, 0.88f, 0.94f);

            // --- Viền trong ---
            var innerBg = MakePanel(menuRoot, "InnerBg", COL_BG,
                0.01f, 0.01f, 0.99f, 0.99f);

            // ─── TIÊU ĐỀ "Sổ Tay" ───
            var titleBg = MakePanel(innerBg, "TitleBg", new Color(0.30f, 0.22f, 0.14f, 0.9f),
                0.30f, 0.90f, 0.70f, 0.99f);
            MakeText(titleBg, "Sổ Tay", 40, COL_TITLE, TextAlignmentOptions.Center, FontStyles.Bold,
                0f, 0f, 1f, 1f);

            // ─── NÚT ĐÓNG (X) ───
            BuildCloseButton(innerBg);

            // ─── THANH TAB ───
            BuildTabBar(innerBg);

            // ─── KHU VỰC NỘI DUNG ───
            var contentArea = MakeRect(innerBg, "ContentArea");
            SetAnchors(contentArea, 0.02f, 0.02f, 0.98f, 0.74f);

            menuPanel   = BuildMenuTabContent(contentArea.gameObject);
            recipePanel = BuildRecipeTabContent(contentArea.gameObject);
            phonePanel  = BuildPhoneTabContent(contentArea.gameObject);
        }

        // ─── THANH TAB 3 NÚT ───
        private void BuildTabBar(GameObject parent)
        {
            var tabBar = MakeRect(parent, "TabBar");
            SetAnchors(tabBar, 0.02f, 0.74f, 0.98f, 0.88f);

            var hLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.padding = new RectOffset(4, 4, 4, 4);

            menuTabBg   = MakeTabBtn(tabBar.gameObject, "📋  MENU", 0);
            recipeTabBg = MakeTabBtn(tabBar.gameObject, "🍜  CÔNG THỨC", 1);
            phoneTabBg  = MakeTabBtn(tabBar.gameObject, "📱  ĐIỆN THOẠI", 2);
        }

        private Image MakeTabBtn(GameObject parent, string label, int tabIdx)
        {
            var go = new GameObject("Tab_" + tabIdx);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = tabIdx == 0 ? COL_TAB_ACTIVE : COL_TAB_INACTIVE;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            int idx = tabIdx;
            btn.onClick.AddListener(() => SwitchTab(idx));

            // Hover effect
            var colors = btn.colors;
            colors.highlightedColor = new Color(
                img.color.r + 0.1f, img.color.g + 0.1f, img.color.b + 0.1f, 1f);
            colors.pressedColor = new Color(
                img.color.r - 0.05f, img.color.g - 0.05f, img.color.b - 0.05f, 1f);
            btn.colors = colors;

            // Text
            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.color = COL_TEXT;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return img;
        }

        // ─── NÚT ĐÓNG ───
        private void BuildCloseButton(GameObject parent)
        {
            var closeBg = MakePanel(parent, "CloseBtn", COL_CLOSE,
                0.93f, 0.90f, 0.99f, 0.99f);
            MakeText(closeBg, "✕", 28, Color.white, TextAlignmentOptions.Center, FontStyles.Bold,
                0f, 0f, 1f, 1f);
            var btn = closeBg.AddComponent<Button>();
            btn.targetGraphic = closeBg.GetComponent<Image>();
            btn.onClick.AddListener(CloseMenu);
        }

        // ================================================================
        //  TAB 1: MENU — Grid 3x2 các món
        // ================================================================
        private GameObject BuildMenuTabContent(GameObject parent)
        {
            var panel = new GameObject("MenuPanel");
            panel.transform.SetParent(parent.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.sizeDelta = Vector2.zero;

            // Tiêu đề "Thực Đơn"
            MakeText(panel, "Thực Đơn", 36, COL_TITLE, TextAlignmentOptions.Center, FontStyles.Bold,
                0f, 0.88f, 1f, 1f);

            // Grid 3 cột x 2 hàng
            var gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(panel.transform, false);
            var grt = gridGO.AddComponent<RectTransform>();
            SetAnchors(grt, 0.03f, 0.02f, 0.97f, 0.86f);

            var grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(380, 260);
            grid.spacing = new Vector2(16, 16);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperCenter;

            // ContentSizeFitter
            var csf = gridGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < allItems.Length; i++)
            {
                BuildFoodCard(gridGO, allItems[i]);
            }

            return panel;
        }

        private void BuildFoodCard(GameObject parent, MenuItem item)
        {
            // Card container
            var card = new GameObject("Card_" + item.name.Replace("\n", ""));
            card.transform.SetParent(parent.transform, false);
            card.AddComponent<RectTransform>();
            var cardImg = card.AddComponent<Image>();
            cardImg.color = COL_CARD;

            // ─── Emoji (thay ảnh) ───
            MakeText(card, item.emoji, 60, Color.white, TextAlignmentOptions.Center, FontStyles.Normal,
                0.05f, 0.30f, 0.50f, 0.90f);

            // ─── Tên món ───
            MakeText(card, item.name, 20, COL_TEXT, TextAlignmentOptions.Left, FontStyles.Bold,
                0.50f, 0.55f, 0.98f, 0.92f);

            // ─── Giá ───
            string priceStr = string.Format("Giá: {0:N0} VNĐ", item.price);
            MakeText(card, priceStr, 16, COL_PRICE, TextAlignmentOptions.Left, FontStyles.Normal,
                0.50f, 0.38f, 0.98f, 0.55f);

            // ─── Nút MUA ───
            var buyBtnGO = MakePanel(card, "BuyBtn", COL_BTN_BUY,
                0.50f, 0.08f, 0.85f, 0.35f);
            MakeText(buyBtnGO, "MUA", 18, Color.white, TextAlignmentOptions.Center, FontStyles.Bold,
                0f, 0f, 1f, 1f);
            var buyBtn = buyBtnGO.AddComponent<Button>();
            buyBtn.targetGraphic = buyBtnGO.GetComponent<Image>();
            // Hover effect cho nút MUA
            var bc = buyBtn.colors;
            bc.highlightedColor = new Color(0.25f, 0.70f, 0.40f, 1f);
            bc.pressedColor = new Color(0.15f, 0.50f, 0.28f, 1f);
            buyBtn.colors = bc;
        }

        // ================================================================
        //  TAB 2: CÔNG THỨC — Danh sách công thức pha chế
        // ================================================================
        private GameObject BuildRecipeTabContent(GameObject parent)
        {
            var panel = new GameObject("RecipePanel");
            panel.transform.SetParent(parent.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.sizeDelta = Vector2.zero;

            // Tiêu đề
            MakeText(panel, "Công Thức Pha Chế", 36, COL_TITLE,
                TextAlignmentOptions.Center, FontStyles.Bold,
                0f, 0.88f, 1f, 1f);

            // Scroll Area
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(panel.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            SetAnchors(scrollRT, 0.03f, 0.02f, 0.97f, 0.86f);
            scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f); // Raycast target
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport (Mask)
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content container
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewport.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            scrollRect.viewport = vpRT;

            // Build recipe cards
            for (int i = 0; i < allItems.Length; i++)
            {
                BuildRecipeCard(contentGO, allItems[i]);
            }

            return panel;
        }

        private void BuildRecipeCard(GameObject parent, MenuItem item)
        {
            var card = new GameObject("Recipe_" + item.name.Replace("\n", ""));
            card.transform.SetParent(parent.transform, false);
            var rt = card.AddComponent<RectTransform>();
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 140;
            le.preferredHeight = 150;

            card.AddComponent<Image>().color = COL_CARD;

            // Emoji
            MakeText(card, item.emoji, 50, Color.white,
                TextAlignmentOptions.Center, FontStyles.Normal,
                0.02f, 0.10f, 0.15f, 0.90f);

            // Tên món
            string cleanName = item.name.Replace("\n", " ");
            MakeText(card, cleanName, 24, COL_TITLE,
                TextAlignmentOptions.Left, FontStyles.Bold,
                0.16f, 0.65f, 0.98f, 0.95f);

            // Giá
            string priceStr = string.Format("Giá: {0:N0} VNĐ", item.price);
            MakeText(card, priceStr, 16, COL_PRICE,
                TextAlignmentOptions.Left, FontStyles.Normal,
                0.16f, 0.45f, 0.98f, 0.65f);

            // Công thức chi tiết
            MakeText(card, item.recipe, 15, COL_TEXT,
                TextAlignmentOptions.Left, FontStyles.Normal,
                0.16f, 0.02f, 0.98f, 0.48f);
        }

        // ================================================================
        //  TAB 3: ĐIỆN THOẠI
        // ================================================================
        private GameObject BuildPhoneTabContent(GameObject parent)
        {
            var panel = new GameObject("PhonePanel");
            panel.transform.SetParent(parent.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.sizeDelta = Vector2.zero;

            // Tiêu đề
            MakeText(panel, "📱  Điện Thoại", 36, COL_TITLE,
                TextAlignmentOptions.Center, FontStyles.Bold,
                0f, 0.88f, 1f, 1f);

            // Phone frame
            var phoneBg = MakePanel(panel, "PhoneBg", new Color(0.10f, 0.10f, 0.14f, 1f),
                0.20f, 0.05f, 0.80f, 0.85f);

            // Scroll Area cho danh bạ
            var scrollGO = new GameObject("PhoneScroll");
            scrollGO.transform.SetParent(phoneBg.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.sizeDelta = Vector2.zero;
            scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewport.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(20, 20, 20, 20);

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            scrollRect.viewport = vpRT;

            // Danh bạ
            BuildContactEntry(contentGO, "📞", "Chị Ba", "0912 345 678", "Cung cấp nguyên liệu trà, cà phê");
            BuildContactEntry(contentGO, "📞", "Nhà Cung Cấp Trà", "0987 654 321", "Giao trà lá tươi mỗi sáng");
            BuildContactEntry(contentGO, "🔧", "Thợ Sửa Xe", "0901 222 333", "Sửa xe gánh hàng rong khi bị hỏng");
            BuildContactEntry(contentGO, "🏥", "Trạm Y Tế", "0909 111 222", "Khám sức khoẻ khi mệt");
            BuildContactEntry(contentGO, "👮", "Công An Phường", "0908 888 999", "Liên hệ khi gặp sự cố");

            // Tin nhắn cuối
            var msgCard = new GameObject("Msg");
            msgCard.transform.SetParent(contentGO.transform, false);
            msgCard.AddComponent<RectTransform>();
            msgCard.AddComponent<LayoutElement>().minHeight = 60;
            msgCard.AddComponent<Image>().color = new Color(0.18f, 0.28f, 0.22f, 1f);
            MakeText(msgCard, "💬  Hôm nay trời đẹp, bán buôn thuận lợi nhé!", 17, COL_TEXT,
                TextAlignmentOptions.Center, FontStyles.Italic,
                0.05f, 0.05f, 0.95f, 0.95f);

            return panel;
        }

        private void BuildContactEntry(GameObject parent, string icon, string name, string phone, string desc)
        {
            var card = new GameObject("Contact_" + name);
            card.transform.SetParent(parent.transform, false);
            card.AddComponent<RectTransform>();
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 80;
            le.preferredHeight = 85;
            card.AddComponent<Image>().color = COL_CARD;

            // Icon
            MakeText(card, icon, 32, Color.white,
                TextAlignmentOptions.Center, FontStyles.Normal,
                0.02f, 0.15f, 0.12f, 0.85f);

            // Name
            MakeText(card, name, 22, COL_TEXT,
                TextAlignmentOptions.Left, FontStyles.Bold,
                0.13f, 0.50f, 0.60f, 0.90f);

            // Phone
            MakeText(card, phone, 20, COL_TITLE,
                TextAlignmentOptions.Left, FontStyles.Normal,
                0.60f, 0.50f, 0.98f, 0.90f);

            // Description
            MakeText(card, desc, 14, new Color(0.65f, 0.68f, 0.72f, 1f),
                TextAlignmentOptions.Left, FontStyles.Italic,
                0.13f, 0.05f, 0.98f, 0.48f);
        }

        // ================================================================
        //  HELPER
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

        private static GameObject MakePanel(GameObject parent, string name, Color color,
            float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static TextMeshProUGUI MakeText(GameObject parent, string content, int fontSize,
            Color color, TextAlignmentOptions align, FontStyles style,
            float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
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
    }
}
