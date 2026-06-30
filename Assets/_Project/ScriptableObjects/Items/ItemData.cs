using UnityEngine;

namespace GanhHangRong.Economy
{
    public enum ItemType
    {
        Ingredient,
        RecipeBook,
        Tool,
        Food,
        Other
    }

    [CreateAssetMenu(fileName = "NewItemData", menuName = "Gánh Hàng Rong/Economy/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Thông tin item")]
        public string itemId;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public ItemType itemType = ItemType.Ingredient;
        public int price = 5000;

        [Header("Sách công thức")]
        public bool isRecipeBook;
        public string recipeIdToUnlock;

        [Header("Legacy - giữ để không hỏng asset cũ")]
        public string itemName;
        public int buyPrice;
        public int sellPrice;

        public string Id => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
        public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : (!string.IsNullOrWhiteSpace(itemName) ? itemName : name);
        public int BuyPrice => price > 0 ? price : buyPrice;
        public bool IsBook => isRecipeBook || itemType == ItemType.RecipeBook;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = name;
            }

            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(itemName))
            {
                displayName = itemName;
            }

            if (price <= 0 && buyPrice > 0)
            {
                price = buyPrice;
            }

            if (isRecipeBook)
            {
                itemType = ItemType.RecipeBook;
            }
        }
    }
}
