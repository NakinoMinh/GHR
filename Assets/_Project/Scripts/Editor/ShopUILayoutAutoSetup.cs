#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GanhHangRong.UI;

namespace GanhHangRong.EditorTools
{
    public static class ShopUILayoutAutoSetup
    {
        [MenuItem("GanhHangRong/Setup Shopping Cart UI (Dual Column)")]
        public static void SetupDualColumnShopUI()
        {
            ShopUIController shopController = Object.FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
            if (shopController == null)
            {
                Debug.LogError("Không tìm thấy ShopUIController trong Scene hiện tại!");
                return;
            }

            Undo.RecordObject(shopController.gameObject, "Setup Dual Column Shop UI");

            // Fix Vietnamese Fonts
            TextMeshProUGUI[] allTexts = shopController.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTexts)
            {
                if (t != null && t.font != null)
                {
                    Undo.RecordObject(t.font, "Enable Dynamic SDF");
                    t.font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    EditorUtility.SetDirty(t.font);
                }
            }

            Transform panel = shopController.transform;

            // Find existing ScrollRect for Left Side
            ScrollRect leftScroll = shopController.GetComponentInChildren<ScrollRect>(true);
            if (leftScroll != null)
            {
                Undo.RecordObject(leftScroll.GetComponent<RectTransform>(), "Resize Left Scroll");
                RectTransform leftRt = leftScroll.GetComponent<RectTransform>();
                leftRt.anchorMin = new Vector2(0.03f, 0.12f);
                leftRt.anchorMax = new Vector2(0.48f, 0.82f);
                leftRt.offsetMin = Vector2.zero;
                leftRt.offsetMax = Vector2.zero;

                // Header Left
                CreateOrUpdateText(panel, "Header_AvailableItems", "CỬA HÀNG (AVAILABLE ITEMS)", new Vector2(0.03f, 0.83f), new Vector2(0.48f, 0.90f), 18, TextAlignmentOptions.Left);
            }

            // Header Right
            CreateOrUpdateText(panel, "Header_ShoppingCart", "GIỎ HÀNG (SHOPPING CART)", new Vector2(0.52f, 0.83f), new Vector2(0.97f, 0.90f), 18, TextAlignmentOptions.Left);

            // Create Right ScrollView for Cart if missing
            Transform cartContent = null;
            Transform existingCartScroll = panel.Find("CartScrollView");
            if (existingCartScroll == null && leftScroll != null)
            {
                GameObject cartScrollGO = Object.Instantiate(leftScroll.gameObject, panel);
                cartScrollGO.name = "CartScrollView";
                Undo.RegisterCreatedObjectUndo(cartScrollGO, "Create CartScrollView");

                RectTransform cartRt = cartScrollGO.GetComponent<RectTransform>();
                cartRt.anchorMin = new Vector2(0.52f, 0.28f);
                cartRt.anchorMax = new Vector2(0.97f, 0.82f);
                cartRt.offsetMin = Vector2.zero;
                cartRt.offsetMax = Vector2.zero;

                ScrollRect cartSR = cartScrollGO.GetComponent<ScrollRect>();
                cartContent = cartSR.content;

                // Clear any leftover left items in cloned content
                for (int i = cartContent.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(cartContent.GetChild(i).gameObject);
                }
            }
            else if (existingCartScroll != null)
            {
                cartContent = existingCartScroll.GetComponent<ScrollRect>()?.content;
            }

            // Total Price Text
            TextMeshProUGUI totalTxt = CreateOrUpdateText(panel, "TotalPriceText", "Tổng cộng:                  0 VND", new Vector2(0.52f, 0.18f), new Vector2(0.97f, 0.26f), 20, TextAlignmentOptions.Right);

            // Buttons
            Button checkoutBtn = CreateOrUpdateButton(panel, "CheckoutButton", "THANH TOÁN", new Vector2(0.52f, 0.04f), new Vector2(0.74f, 0.16f), new Color(0.12f, 0.54f, 0.24f));
            Button clearBtn = CreateOrUpdateButton(panel, "ClearCartButton", "XÓA TẤT CẢ", new Vector2(0.76f, 0.04f), new Vector2(0.97f, 0.16f), new Color(0.2f, 0.2f, 0.2f));

            // Assign via SerializedObject
            SerializedObject so = new SerializedObject(shopController);
            if (cartContent != null) so.FindProperty("cartListContent").objectReferenceValue = cartContent;
            if (totalTxt != null) so.FindProperty("totalPriceText").objectReferenceValue = totalTxt;
            if (checkoutBtn != null) so.FindProperty("checkoutButton").objectReferenceValue = checkoutBtn;
            if (clearBtn != null) so.FindProperty("clearCartButton").objectReferenceValue = clearBtn;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(shopController);
            Debug.Log("Đã setup thành công giao diện 2 cột Giỏ Hàng cho ShopUIController!");
        }

        private static TextMeshProUGUI CreateOrUpdateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, TextAlignmentOptions align)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }

            RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            if (tmp.font != null) tmp.font.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            return tmp;
        }

        private static Button CreateOrUpdateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }

            RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = bgColor;

            Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();

            CreateOrUpdateText(go.transform, "Text", label, Vector2.zero, Vector2.one, 18, TextAlignmentOptions.Center);
            return btn;
        }
    }
}
#endif
