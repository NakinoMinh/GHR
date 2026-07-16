using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GanhHangRong.UI
{
    internal static class ShopUIRuntimeLayoutBuilder
    {
        internal sealed class Layout
        {
            public Transform CartListContent;
            public ShopCartItemUI CartItemPrefab;
            public TextMeshProUGUI TotalPriceText;
            public TextMeshProUGUI CartCountText;
            public TextMeshProUGUI CartEmptyText;
            public Button CheckoutButton;
            public Button ClearCartButton;
        }

        private static readonly Color PanelColor = new Color(0.055f, 0.075f, 0.07f, 0.985f);
        private static readonly Color SectionColor = new Color(0.085f, 0.115f, 0.105f, 0.98f);
        private static readonly Color ReceiptColor = new Color(0.07f, 0.09f, 0.085f, 0.98f);
        private static readonly Color AccentColor = new Color(0.87f, 0.58f, 0.19f, 1f);
        private static readonly Color GreenColor = new Color(0.12f, 0.58f, 0.38f, 1f);
        private static readonly Color MutedTextColor = new Color(0.70f, 0.75f, 0.71f, 1f);

        internal static Layout Build(
            GameObject shopPanel,
            Transform itemListContent,
            TextMeshProUGUI shopNameText,
            TextMeshProUGUI moneyText,
            TextMeshProUGUI transactionMessageText,
            Button closeButton)
        {
            if (shopPanel == null)
            {
                return null;
            }

            RectTransform panelRect = shopPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.07f, 0.055f);
            panelRect.anchorMax = new Vector2(0.93f, 0.945f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = shopPanel.GetComponent<Image>() ?? shopPanel.AddComponent<Image>();
            panelImage.color = PanelColor;

            Outline outline = shopPanel.GetComponent<Outline>() ?? shopPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.87f, 0.58f, 0.19f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            Transform root = shopPanel.transform;
            TMP_FontAsset font = shopNameText != null ? shopNameText.font : null;

            CreateBand(root, "MarketAccent", new Vector2(0f, 0.986f), Vector2.one, AccentColor, false);
            Image leftSection = CreateBand(root, "MarketGoodsSection", new Vector2(0.025f, 0.135f), new Vector2(0.635f, 0.80f), SectionColor, false);
            Image rightSection = CreateBand(root, "MarketCartSection", new Vector2(0.65f, 0.135f), new Vector2(0.975f, 0.80f), ReceiptColor, false);
            leftSection.transform.SetAsFirstSibling();
            rightSection.transform.SetAsFirstSibling();

            ConfigureText(shopNameText, new Vector2(0.04f, 0.885f), new Vector2(0.67f, 0.975f), 30f, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold);
            ConfigureText(moneyText, new Vector2(0.70f, 0.89f), new Vector2(0.925f, 0.972f), 21f, TextAlignmentOptions.MidlineRight, new Color(1f, 0.83f, 0.46f), FontStyles.Bold);
            ConfigureText(transactionMessageText, new Vector2(0.04f, 0.035f), new Vector2(0.635f, 0.115f), 16f, TextAlignmentOptions.MidlineLeft, new Color(0.94f, 0.76f, 0.42f), FontStyles.Normal);

            ConfigureCloseButton(closeButton);

            CreateText(root, "MarketSubtitle", "CHỢ VEN SÔNG", new Vector2(0.042f, 0.835f), new Vector2(0.28f, 0.88f), 13f, TextAlignmentOptions.MidlineLeft, AccentColor, FontStyles.Bold, font);
            CreateText(root, "GoodsHeader", "HÀNG HÓA", new Vector2(0.045f, 0.735f), new Vector2(0.36f, 0.79f), 19f, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold, font);
            CreateText(root, "GoodsSubheader", "Nguyên liệu và vật dụng", new Vector2(0.30f, 0.738f), new Vector2(0.615f, 0.79f), 13f, TextAlignmentOptions.MidlineRight, MutedTextColor, FontStyles.Normal, font);
            TextMeshProUGUI cartCount = CreateText(root, "CartHeader", "GIỎ HÀNG  •  0 MÓN", new Vector2(0.675f, 0.735f), new Vector2(0.95f, 0.79f), 18f, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold, font);

            ScrollRect goodsScroll = itemListContent != null ? itemListContent.GetComponentInParent<ScrollRect>(true) : null;
            ConfigureScroll(goodsScroll, new Vector2(0.04f, 0.16f), new Vector2(0.62f, 0.73f));

            ScrollRect cartScroll = GetOrCreateCartScroll(root, goodsScroll);
            ConfigureScroll(cartScroll, new Vector2(0.67f, 0.39f), new Vector2(0.955f, 0.72f));

            TextMeshProUGUI emptyText = CreateText(root, "CartEmptyText", "Giỏ hàng đang trống", new Vector2(0.675f, 0.49f), new Vector2(0.95f, 0.61f), 16f, TextAlignmentOptions.Center, MutedTextColor, FontStyles.Italic, font);
            CreateText(root, "TotalLabel", "TỔNG THANH TOÁN", new Vector2(0.675f, 0.30f), new Vector2(0.82f, 0.37f), 13f, TextAlignmentOptions.MidlineLeft, MutedTextColor, FontStyles.Bold, font);
            TextMeshProUGUI totalText = CreateText(root, "TotalPriceText", "0 VNĐ", new Vector2(0.80f, 0.29f), new Vector2(0.95f, 0.37f), 24f, TextAlignmentOptions.MidlineRight, new Color(1f, 0.80f, 0.38f), FontStyles.Bold, font);

            Button checkout = CreateButton(root, "CheckoutButton", "THANH TOÁN", new Vector2(0.67f, 0.16f), new Vector2(0.83f, 0.265f), GreenColor, font);
            Button clear = CreateButton(root, "ClearCartButton", "XÓA GIỎ", new Vector2(0.845f, 0.16f), new Vector2(0.955f, 0.265f), new Color(0.25f, 0.28f, 0.27f, 1f), font);

            return new Layout
            {
                CartListContent = cartScroll != null ? cartScroll.content : null,
                CartItemPrefab = CreateCartItemPrototype(root, font),
                TotalPriceText = totalText,
                CartCountText = cartCount,
                CartEmptyText = emptyText,
                CheckoutButton = checkout,
                ClearCartButton = clear
            };
        }

        private static ScrollRect GetOrCreateCartScroll(Transform root, ScrollRect goodsScroll)
        {
            Transform existing = root.Find("CartScrollView");
            if (existing != null)
            {
                return existing.GetComponent<ScrollRect>();
            }

            if (goodsScroll == null)
            {
                return null;
            }

            GameObject clone = Object.Instantiate(goodsScroll.gameObject, root);
            clone.name = "CartScrollView";
            clone.SetActive(true);
            ScrollRect cartScroll = clone.GetComponent<ScrollRect>();
            if (cartScroll != null && cartScroll.content != null)
            {
                for (int i = cartScroll.content.childCount - 1; i >= 0; i--)
                {
                    Object.Destroy(cartScroll.content.GetChild(i).gameObject);
                }
            }

            return cartScroll;
        }

        private static void ConfigureScroll(ScrollRect scroll, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (scroll == null)
            {
                return;
            }

            RectTransform rect = scroll.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = scroll.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.035f, 0.05f, 0.047f, 0.72f);
            }

            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 26f;

            if (scroll.content != null)
            {
                VerticalLayoutGroup layout = scroll.content.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 6f;
                    layout.padding = new RectOffset(6, 6, 6, 6);
                    layout.childControlWidth = true;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = false;
                }

                ContentSizeFitter fitter = scroll.content.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }
        }

        private static ShopCartItemUI CreateCartItemPrototype(Transform root, TMP_FontAsset font)
        {
            Transform existing = root.Find("CartItemUIPrefab_Runtime");
            if (existing != null)
            {
                return existing.GetComponent<ShopCartItemUI>();
            }

            GameObject row = new GameObject("CartItemUIPrefab_Runtime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup), typeof(ShopCartItemUI));
            row.transform.SetParent(root, false);
            row.SetActive(false);
            row.GetComponent<Image>().color = new Color(0.11f, 0.14f, 0.13f, 1f);
            row.GetComponent<LayoutElement>().preferredHeight = 68f;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 7, 7);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            Image icon = CreateLayoutImage(row.transform, "ItemIcon", 52f);
            icon.preserveAspect = true;
            CreateLayoutText(row.transform, "ItemName", "Mặt hàng", 118f, 15f, TextAlignmentOptions.MidlineLeft, Color.white, font, true);
            CreateLayoutButton(row.transform, "MinusButton", "−", 34f, new Color(0.28f, 0.31f, 0.30f, 1f), font);
            CreateLayoutText(row.transform, "QuantityText", "1", 26f, 15f, TextAlignmentOptions.Center, Color.white, font, false);
            CreateLayoutButton(row.transform, "PlusButton", "+", 34f, new Color(0.16f, 0.48f, 0.33f, 1f), font);
            CreateLayoutText(row.transform, "PriceText", "0 VNĐ", 90f, 14f, TextAlignmentOptions.MidlineRight, new Color(1f, 0.80f, 0.38f), font, false);

            ShopCartItemUI cartItem = row.GetComponent<ShopCartItemUI>();
            cartItem.AutoWire();
            return cartItem;
        }

        private static Image CreateBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, bool raycastTarget)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style, TMP_FontAsset font)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                label.font = font;
                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }
            label.text = text;
            ConfigureText(label, anchorMin, anchorMax, fontSize, alignment, color, style);
            return label;
        }

        private static void ConfigureText(TextMeshProUGUI text, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(11f, fontSize - 5f);
            text.fontSizeMax = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            if (text.font != null)
            {
                text.font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }
        }

        private static void ConfigureCloseButton(Button closeButton)
        {
            if (closeButton == null)
            {
                return;
            }

            RectTransform rect = closeButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.94f, 0.90f);
            rect.anchorMax = new Vector2(0.985f, 0.97f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = closeButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.24f, 0.27f, 0.26f, 1f);
            }

            TextMeshProUGUI label = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "×";
                label.fontSize = 24f;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private static Button CreateButton(Transform parent, string name, string labelText, Vector2 anchorMin, Vector2 anchorMax, Color color, TMP_FontAsset font)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = color;
            Button button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            button.targetGraphic = image;
            ConfigureButtonColors(button);

            CreateText(go.transform, "Label", labelText, Vector2.zero, Vector2.one, 16f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold, font);
            return button;
        }

        private static Image CreateLayoutImage(Transform parent, string name, float width)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
            return go.GetComponent<Image>();
        }

        private static TextMeshProUGUI CreateLayoutText(Transform parent, string name, string text, float width, float fontSize, TextAlignmentOptions alignment, Color color, TMP_FontAsset font, bool flexible)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = flexible ? 80f : width;
            element.flexibleWidth = flexible ? 1f : 0f;

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateLayoutButton(Transform parent, string name, string labelText, float width, Color color, TMP_FontAsset font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;

            Image image = go.GetComponent<Image>();
            image.color = color;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ConfigureButtonColors(button);

            CreateText(go.transform, "Label", labelText, Vector2.zero, Vector2.one, 19f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold, font);
            return button;
        }

        private static void ConfigureButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.48f, 0.48f, 0.48f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }
    }
}
