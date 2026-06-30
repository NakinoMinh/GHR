using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Economy
{
    [CreateAssetMenu(fileName = "NewRecipeData", menuName = "Gánh Hàng Rong/Economy/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        [Header("Thông tin công thức")]
        public string recipeId;
        public string recipeName;
        public Sprite recipeIcon;
        public List<IngredientRequirement> ingredients = new List<IngredientRequirement>();

        [Header("Mở khóa bằng sách")]
        public string recipeBookIdRequired;
        public bool isLockedByRecipeBook;

        [Header("Kinh tế")]
        public int baseSellPrice = 5000;

        [Header("Legacy - giữ để không hỏng asset cũ")]
        public int requiredTea = 1;
        public int requiredIce = 1;
        public int requiredSugar = 1;
        public float preparationTime = 2f;

        public string Id => string.IsNullOrWhiteSpace(recipeId) ? name : recipeId;
        public string DisplayName => string.IsNullOrWhiteSpace(recipeName) ? name : recipeName;
        public bool RequiresRecipeBook => isLockedByRecipeBook || !string.IsNullOrWhiteSpace(recipeBookIdRequired);

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                recipeId = name;
            }

            if (!string.IsNullOrWhiteSpace(recipeBookIdRequired))
            {
                isLockedByRecipeBook = true;
            }
        }
    }
}
