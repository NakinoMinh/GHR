using System.Collections.Generic;
using UnityEngine;
using GanhHangRong.Economy;
using GanhHangRong.Interaction;

namespace GanhHangRong.Systems
{
    public static class ShopRuntimeSetup
    {
        private static bool isInitialized = false;
        private static readonly Dictionary<string, ItemData> runtimeItems = new Dictionary<string, ItemData>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            isInitialized = false;
            EnsureAllShopItems();
        }

        public static void EnsureAllShopItems()
        {
            if (isInitialized && runtimeItems.Count >= 8) return;

            CreateOrUpdateItem("hu_ca_phe", "Hũ Cà Phê Phố Cổ", "Cà phê bột rang xay thơm lừng. Mua 1 hũ (+150g cà phê) để pha chế.", 15000, "hu_tra");
            CreateOrUpdateItem("tra", "Hũ Trà Thượng Hạng", "Trà lài đậm vị truyền thống. Mua 1 hũ (+100g trà) để nấu trà sữa/trà đá.", 5000, "hu_tra");
            CreateOrUpdateItem("nuoc_sach", "Bình Nước Sài Gòn Aquwa", "Nước sạch tinh khiết 30L. Mua để nạp đầy nước cho bình lọc đun trà.", 3000, "ice_box");
            CreateOrUpdateItem("duong", "Hũ Đường", "Đường ngọt thanh dịu. Mua 1 hũ (+200g đường) dùng cho pha chế.", 2000, "hu_duong");
            CreateOrUpdateItem("ly_nuoc_sach", "Lốc 10 Ly Nước Sạch", "Ly nhựa sạch dùng một lần/tái sử dụng. Mua 1 lốc (+10 ly sạch).", 2000, "ly_cups");
            CreateOrUpdateItem("ban_doi", "Bộ Bàn Đôi (2 Ghế)", "Bộ bàn trà nhỏ kèm 2 ghế nhựa đẩu. Mua về bấm ĐẶT BÀN để bố trí cho khách ngồi!", 20000, "mat_ban_inox");
            CreateOrUpdateItem("ban_bon", "Bộ Bàn Lớn (4 Ghế)", "Bộ bàn trà lớn kèm 4 ghế nhựa đẩu cho nhóm khách đông. Bấm ĐẶT BÀN để bố trí!", 35000, "mat_ban_inox");
            CreateOrUpdateItem("ghe_nhua", "Ghế Nhựa Đẩu Đơn", "Ghế nhựa đẩu bổ sung cho quán trà đá. Bấm ĐẶT BÀN để sắp xếp!", 5000, "the_gia");

            isInitialized = true;

            // Apply to all shops in loaded resources/scenes
            ShopData[] shops = Resources.FindObjectsOfTypeAll<ShopData>();
            foreach (var shop in shops)
            {
                if (shop != null && (shop.name.Contains("tap_hoa") || shop.DisplayName.Contains("Tạp Hóa")))
                {
                    EnsureTapHoaItems(shop);
                }
            }
        }

        public static void EnsureTapHoaItems(ShopData shop)
        {
            if (shop == null) return;
            EnsureAllShopItems();

            if (shop.itemsForSale == null)
            {
                shop.itemsForSale = new List<ShopStockItem>();
            }

            string[] targetIds = { "hu_ca_phe", "tra", "nuoc_sach", "duong", "ly_nuoc_sach", "ban_doi", "ban_bon", "ghe_nhua" };

            foreach (string id in targetIds)
            {
                if (!runtimeItems.TryGetValue(id, out ItemData itemData) || itemData == null) continue;

                bool exists = false;
                foreach (var stock in shop.itemsForSale)
                {
                    if (stock != null && stock.item != null && (stock.item.Id == id || stock.item.name == id))
                    {
                        exists = true;
                        // Update price if needed
                        stock.priceOverride = itemData.price;
                        break;
                    }
                }

                if (!exists)
                {
                    ShopStockItem newStock = new ShopStockItem
                    {
                        item = itemData,
                        priceOverride = itemData.price,
                        stockAmount = -1 // Unlimited stock
                    };
                    shop.itemsForSale.Add(newStock);
                }
            }
        }

        private static void CreateOrUpdateItem(string id, string displayName, string desc, int price, string iconName)
        {
            if (!runtimeItems.TryGetValue(id, out ItemData item) || item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                item.name = id;
                item.itemId = id;
                runtimeItems[id] = item;
            }

            item.displayName = displayName;
            item.description = desc;
            item.price = price;
            item.buyPrice = price;
            item.itemType = id.StartsWith("ban_") || id.StartsWith("ghe_") ? ItemType.Tool : ItemType.Ingredient;

            if (item.icon == null && !string.IsNullOrEmpty(iconName))
            {
                Sprite foundSprite = FindSpriteByName(iconName);
                if (foundSprite != null)
                {
                    item.icon = foundSprite;
                }
            }
        }

        private static Sprite FindSpriteByName(string spriteName)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets(spriteName + " t:Sprite");
            if (guids == null || guids.Length == 0)
            {
                guids = UnityEditor.AssetDatabase.FindAssets(spriteName + " t:Texture2D");
            }
            if (guids != null && guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;

                Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
#endif
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (Sprite s in allSprites)
            {
                if (s != null && (s.name.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase) || s.name.Contains(spriteName)))
                {
                    return s;
                }
            }
            return null;
        }
    }
}
