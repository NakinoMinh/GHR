using UnityEngine;

namespace GanhHangRong.Economy
{
    [CreateAssetMenu(fileName = "NewRecipeData", menuName = "Gánh Hàng Rong/Economy/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        public string recipeName;
        public int requiredTea = 1;
        public int requiredIce = 1;
        public int requiredSugar = 1;
        
        public float preparationTime = 2f;
        public int baseSellPrice = 5000;
    }
}
