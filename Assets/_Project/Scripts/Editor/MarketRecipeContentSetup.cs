#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GanhHangRong.Economy;

namespace GanhHangRong.EditorTools
{
    public static class MarketRecipeContentSetup
    {
        private const string ItemRoot = "Assets/_Project/ScriptableObjects/MarketGenerated/Items";
        private const string RecipeRoot = "Assets/_Project/Resources/Recipes";
        private const string SeafoodShopPath = "Assets/_Project/ScriptableObjects/MarketGenerated/Shops/shop_hai_san_pho.asset";
        private const string SpecialtyShopPath = "Assets/_Project/ScriptableObjects/MarketGenerated/Shops/shop_dac_san_kien_giang.asset";
        private const string GroceryShopPath = "Assets/_Project/ScriptableObjects/MarketGenerated/Shops/shop_tap_hoa.asset";

        [InitializeOnLoadMethod]
        private static void QueueContentSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && NeedsSetup())
                {
                    EnsureContent();
                    Debug.Log("[MarketRecipeContentSetup] Added missing market recipes and recipe books.");
                }
            };
        }

        [MenuItem("GanhHangRong/Setup Market Recipes")]
        public static void EnsureContent()
        {
            EnsureFolder(ItemRoot);
            EnsureFolder(RecipeRoot);

            ItemData caLoc = EnsureItem("ca_loc", "Cá lóc tươi", "Cá lóc đồng làm sạch, dùng nấu nước lèo bún cá.", 24000, "ca_loc");
            ItemData ghe = EnsureItem("ghe", "Ghẹ xanh", "Ghẹ xanh tươi, thịt chắc và ngọt.", 32000, "ghe");
            ItemData bunTuoi = EnsureItem("bun_tuoi", "Bún tươi", "Một phần bún gạo tươi cho một tô bún cá.", 6000, "bun_tuoi");
            ItemData banhCanh = EnsureItem("banh_canh", "Sợi bánh canh", "Sợi bánh canh bột gạo dày, dùng cho một tô.", 7000, "banh_canh");
            ItemData miaCay = EnsureItem("mia_cay", "Mía cây", "Mía tươi đã làm sạch, ép được một phần nước ngọt mát.", 4000, "mia_cay");
            ItemData chanhTuoi = EnsureItem("chanh_tuoi", "Chanh tươi", "Chanh xanh miền Tây, tạo vị chua thanh cho đồ uống.", 3000, "chanh_tuoi");
            ItemData laTra = EnsureItem("la_tra", "Lá trà", "Lá trà khô dùng để pha một ly trà đậm vị.", 4000, "la_tra");
            ItemData duaTuoi = EnsureItem("dua_tuoi", "Dừa tươi", "Dừa non nhiều nước, dùng phục vụ trực tiếp.", 8000, "dua_tuoi");

            ItemData nuocMam = LoadItem("nuoc_mam_phu_quoc");
            ItemData otBot = LoadItem("ot_bot");
            ItemData muoi = LoadItem("muoi");
            ItemData dauAn = LoadItem("dau_an");
            ItemData duongThotNot = LoadItem("duong_thot_not");
            ItemData tom = LoadItem("tom");
            ItemData muc = LoadItem("muc");
            ItemData ngheu = LoadItem("ngheu");

            ShopData seafoodShop = AssetDatabase.LoadAssetAtPath<ShopData>(SeafoodShopPath);
            ShopData specialtyShop = AssetDatabase.LoadAssetAtPath<ShopData>(SpecialtyShopPath);
            ShopData groceryShop = AssetDatabase.LoadAssetAtPath<ShopData>(GroceryShopPath);
            AddStock(seafoodShop, caLoc, 24000);
            AddStock(seafoodShop, ghe, 32000);
            AddStock(specialtyShop, bunTuoi, 6000);
            AddStock(specialtyShop, banhCanh, 7000);
            AddStock(specialtyShop, miaCay, 4000);
            AddStock(specialtyShop, duaTuoi, 8000);
            AddStock(groceryShop, chanhTuoi, 3000);
            AddStock(groceryShop, laTra, 4000);

            RecipeData bunCa = EnsureRecipe(
                "bun_ca_kien_giang",
                "Bún Cá Kiên Giang",
                42000,
                "bun_ca_kien_giang",
                "sach_bun_ca_kien_giang",
                new IngredientRequirement { item = caLoc, amount = 1 },
                new IngredientRequirement { item = bunTuoi, amount = 1 },
                new IngredientRequirement { item = nuocMam, amount = 1 },
                new IngredientRequirement { item = otBot, amount = 1 });

            RecipeData banhCanhGhe = EnsureRecipe(
                "banh_canh_ghe",
                "Bánh Canh Ghẹ",
                52000,
                "banh_canh_ghe",
                "sach_banh_canh_ghe",
                new IngredientRequirement { item = ghe, amount = 1 },
                new IngredientRequirement { item = banhCanh, amount = 1 },
                new IngredientRequirement { item = nuocMam, amount = 1 },
                new IngredientRequirement { item = muoi, amount = 1 });

            RecipeData tomRim = EnsureRecipe(
                "tom_rim_nuoc_mam", "Tôm Rim Nước Mắm", 65000, "tom_rim_nuoc_mam", "sach_tom_rim_nuoc_mam",
                new IngredientRequirement { item = tom, amount = 1 },
                new IngredientRequirement { item = nuocMam, amount = 1 },
                new IngredientRequirement { item = duongThotNot, amount = 1 },
                new IngredientRequirement { item = dauAn, amount = 1 });

            RecipeData mucNuong = EnsureRecipe(
                "muc_nuong_muoi_ot", "Mực Nướng Muối Ớt", 48000, "muc_nuong_muoi_ot", "sach_muc_nuong_muoi_ot",
                new IngredientRequirement { item = muc, amount = 1 },
                new IngredientRequirement { item = muoi, amount = 1 },
                new IngredientRequirement { item = otBot, amount = 1 },
                new IngredientRequirement { item = dauAn, amount = 1 });

            RecipeData ngheuXao = EnsureRecipe(
                "ngheu_xao_cay", "Nghêu Xào Cay", 52000, "ngheu_xao_cay", "sach_ngheu_xao_cay",
                new IngredientRequirement { item = ngheu, amount = 1 },
                new IngredientRequirement { item = nuocMam, amount = 1 },
                new IngredientRequirement { item = otBot, amount = 1 },
                new IngredientRequirement { item = dauAn, amount = 1 });

            EnsureRecipe(
                "nuoc_mia", "Nước Mía", 15000, "nuoc_mia", "sach_nuoc_mia",
                new IngredientRequirement { item = miaCay, amount = 2 });
            EnsureRecipe(
                "tra_chanh", "Trà Chanh", 28000, "tra_chanh", "sach_tra_chanh",
                new IngredientRequirement { item = laTra, amount = 1 },
                new IngredientRequirement { item = chanhTuoi, amount = 1 },
                new IngredientRequirement { item = duongThotNot, amount = 1 });
            EnsureRecipe(
                "nuoc_dua", "Nước Dừa", 18000, "nuoc_dua", "sach_nuoc_dua",
                new IngredientRequirement { item = duaTuoi, amount = 1 });

            AddStock(specialtyShop, ConfigureRecipeBook("sach_bun_ca_kien_giang", "Sách công thức Bún Cá Kiên Giang", "Mở khóa món Bún Cá Kiên Giang trong Tab Menu.", bunCa.Id, 30000), 30000);
            AddStock(specialtyShop, ConfigureRecipeBook("sach_banh_canh_ghe", "Sách công thức Bánh Canh Ghẹ", "Mở khóa món Bánh Canh Ghẹ trong Tab Menu.", banhCanhGhe.Id, 45000), 45000);
            AddStock(specialtyShop, ConfigureRecipeBook("sach_tom_rim_nuoc_mam", "Sách công thức Tôm Rim Nước Mắm", "Mở khóa món Tôm Rim Nước Mắm trong Tab Menu.", tomRim.Id, 35000), 35000);
            AddStock(specialtyShop, ConfigureRecipeBook("sach_muc_nuong_muoi_ot", "Sách công thức Mực Nướng Muối Ớt", "Mở khóa món Mực Nướng Muối Ớt trong Tab Menu.", mucNuong.Id, 32000), 32000);
            AddStock(specialtyShop, ConfigureRecipeBook("sach_ngheu_xao_cay", "Sách công thức Nghêu Xào Cay", "Mở khóa món Nghêu Xào Cay trong Tab Menu.", ngheuXao.Id, 28000), 28000);
            AddStock(specialtyShop, ConfigureRecipeBook("sach_nuoc_mia", "Sách công thức Nước Mía", "Mở khóa món Nước Mía trong Tab Menu.", "nuoc_mia", 20000), 20000);
            AddStock(specialtyShop, ConfigureRecipeBook("sach_tra_chanh", "Sách công thức Trà Chanh", "Mở khóa món Trà Chanh trong Tab Menu.", "tra_chanh", 25000), 25000);
            AddStock(specialtyShop, ConfigureRecipeBook("sach_nuoc_dua", "Sách công thức Nước Dừa", "Mở khóa món Nước Dừa trong Tab Menu.", "nuoc_dua", 22000), 22000);

            AssignMissingMarketIcons();
            AssetDatabase.SaveAssets();
        }

        private static ItemData EnsureItem(string id, string displayName, string description, int price, string iconId)
        {
            string path = $"{ItemRoot}/{id}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.name = id;
            item.itemId = id;
            item.displayName = displayName;
            item.description = description;
            item.itemType = ItemType.Ingredient;
            item.price = price;
            item.buyPrice = price;
            item.icon = MarketItemIconLibrary.GetIcon(iconId);
            EditorUtility.SetDirty(item);
            return item;
        }

        private static RecipeData EnsureRecipe(string id, string displayName, int sellPrice, string iconId, string recipeBookId, params IngredientRequirement[] ingredients)
        {
            string path = $"{RecipeRoot}/{id}.asset";
            RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<RecipeData>();
                AssetDatabase.CreateAsset(recipe, path);
            }

            recipe.name = id;
            recipe.recipeId = id;
            recipe.recipeName = displayName;
            recipe.recipeIcon = MarketItemIconLibrary.GetIcon(iconId);
            recipe.baseSellPrice = sellPrice;
            recipe.isLockedByRecipeBook = !string.IsNullOrWhiteSpace(recipeBookId);
            recipe.recipeBookIdRequired = recipeBookId ?? string.Empty;
            recipe.ingredients = new List<IngredientRequirement>(ingredients);
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        private static ItemData ConfigureRecipeBook(string assetName, string displayName, string description, string recipeId, int price)
        {
            ItemData book = LoadItem(assetName);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(book, $"{ItemRoot}/{assetName}.asset");
            }

            book.name = assetName;
            book.itemId = assetName;
            book.displayName = displayName;
            book.description = description;
            book.itemType = ItemType.RecipeBook;
            book.isRecipeBook = true;
            book.recipeIdToUnlock = recipeId;
            book.price = price;
            book.buyPrice = price;
            book.icon = MarketItemIconLibrary.GetIcon(assetName)
                ?? MarketItemIconLibrary.GetIcon(recipeId)
                ?? MarketItemIconLibrary.GetIcon("sach_bun_ca_kien_giang");
            EditorUtility.SetDirty(book);
            return book;
        }

        private static bool NeedsSetup()
        {
            string[] requiredItems =
            {
                "mia_cay", "chanh_tuoi", "la_tra", "dua_tuoi",
                "sach_tom_rim_nuoc_mam", "sach_muc_nuong_muoi_ot", "sach_ngheu_xao_cay",
                "sach_nuoc_mia", "sach_tra_chanh", "sach_nuoc_dua"
            };

            string[] requiredRecipes =
            {
                "tom_rim_nuoc_mam", "muc_nuong_muoi_ot", "ngheu_xao_cay",
                "nuoc_mia", "tra_chanh", "nuoc_dua"
            };

            foreach (string itemId in requiredItems)
            {
                if (AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemRoot}/{itemId}.asset") == null)
                {
                    return true;
                }
            }

            foreach (string recipeId in requiredRecipes)
            {
                if (AssetDatabase.LoadAssetAtPath<RecipeData>($"{RecipeRoot}/{recipeId}.asset") == null)
                {
                    return true;
                }
            }

            ShopData specialtyShop = AssetDatabase.LoadAssetAtPath<ShopData>(SpecialtyShopPath);
            return specialtyShop == null
                || specialtyShop.itemsForSale == null
                || !ContainsStock(specialtyShop, "sach_nuoc_mia")
                || !ContainsStock(specialtyShop, "sach_tra_chanh")
                || !ContainsStock(specialtyShop, "sach_nuoc_dua");
        }

        private static bool ContainsStock(ShopData shop, string itemId)
        {
            return shop.itemsForSale.Exists(stock =>
                stock != null && stock.item != null && stock.item.Id == itemId);
        }

        private static void AddStock(ShopData shop, ItemData item, int price)
        {
            if (shop == null || item == null)
            {
                return;
            }

            if (shop.itemsForSale == null)
            {
                shop.itemsForSale = new List<ShopStockItem>();
            }

            ShopStockItem existing = shop.itemsForSale.Find(stock => stock != null && stock.item != null && stock.item.Id == item.Id);
            if (existing == null)
            {
                shop.itemsForSale.Add(new ShopStockItem { item = item, priceOverride = price, stockAmount = -1 });
            }
            else
            {
                existing.priceOverride = price;
            }

            EditorUtility.SetDirty(shop);
        }

        private static void AssignMissingMarketIcons()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemRoot });
            foreach (string guid in guids)
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null) continue;
                Sprite icon = MarketItemIconLibrary.GetIcon(item.Id);
                if (icon != null && item.icon != icon)
                {
                    item.icon = icon;
                    EditorUtility.SetDirty(item);
                }
            }
        }

        private static ItemData LoadItem(string id)
        {
            return AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemRoot}/{id}.asset");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
