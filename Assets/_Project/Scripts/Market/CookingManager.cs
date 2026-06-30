using System;
using UnityEngine;

namespace GanhHangRong.Economy
{
    public class CookingManager : MonoBehaviour
    {
        public static CookingManager Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private RecipeUnlockManager recipeUnlockManager;
        [SerializeField] private bool autoFindMissingReferences = true;

        public event Action<RecipeData> RecipeCooked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Đã có CookingManager khác trong scene. Object này sẽ tự hủy.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        public bool CanCookRecipe(RecipeData recipe, out string failureReason)
        {
            if (!CheckRecipeUnlocked(recipe, out failureReason))
            {
                return false;
            }

            return CheckIngredients(recipe, out failureReason);
        }

        public bool CheckRecipeUnlocked(RecipeData recipe, out string failureReason)
        {
            failureReason = string.Empty;

            if (recipe == null)
            {
                failureReason = "Công thức không tồn tại.";
                return false;
            }

            if (!recipe.RequiresRecipeBook)
            {
                return true;
            }

            ResolveReferences();
            if (recipeUnlockManager == null)
            {
                failureReason = "Thiếu RecipeUnlockManager trong scene.";
                Debug.LogWarning(failureReason, this);
                return false;
            }

            if (!recipeUnlockManager.IsRecipeUnlocked(recipe.Id))
            {
                failureReason = "Bạn cần mua sách công thức tại quầy Đặc Sản Kiên Giang.";
                return false;
            }

            return true;
        }

        public bool CheckIngredients(RecipeData recipe, out string failureReason)
        {
            failureReason = string.Empty;

            if (recipe == null)
            {
                failureReason = "Công thức không tồn tại.";
                return false;
            }

            ResolveReferences();
            if (playerInventory == null)
            {
                failureReason = "Thiếu PlayerInventory trong scene.";
                Debug.LogWarning(failureReason, this);
                return false;
            }

            if (recipe.ingredients == null || recipe.ingredients.Count == 0)
            {
                return true;
            }

            foreach (IngredientRequirement requirement in recipe.ingredients)
            {
                if (requirement == null || requirement.item == null)
                {
                    failureReason = $"Công thức {recipe.DisplayName} có nguyên liệu chưa cấu hình.";
                    return false;
                }

                int requiredAmount = Mathf.Max(1, requirement.amount);
                int ownedAmount = playerInventory.GetItemAmount(requirement.item);
                if (ownedAmount < requiredAmount)
                {
                    failureReason = $"Thiếu {requirement.item.DisplayName} x{requiredAmount - ownedAmount} (đang có {ownedAmount}/{requiredAmount}).";
                    return false;
                }
            }

            return true;
        }

        public bool ConsumeIngredients(RecipeData recipe, out string failureReason)
        {
            if (!CheckIngredients(recipe, out failureReason))
            {
                return false;
            }

            if (recipe == null || recipe.ingredients == null)
            {
                failureReason = "Công thức không tồn tại.";
                return false;
            }

            foreach (IngredientRequirement requirement in recipe.ingredients)
            {
                if (requirement == null || requirement.item == null)
                {
                    failureReason = $"Công thức {recipe.DisplayName} có nguyên liệu chưa cấu hình.";
                    return false;
                }

                int amount = Mathf.Max(1, requirement.amount);
                if (!playerInventory.RemoveItem(requirement.item, amount))
                {
                    failureReason = $"Không thể trừ nguyên liệu {requirement.item.DisplayName}.";
                    return false;
                }
            }

            playerInventory.SaveData();
            failureReason = string.Empty;
            return true;
        }

        public bool CookRecipe(RecipeData recipe, out string resultMessage)
        {
            if (!CanCookRecipe(recipe, out resultMessage))
            {
                return false;
            }

            if (!ConsumeIngredients(recipe, out resultMessage))
            {
                return false;
            }

            RecipeCooked?.Invoke(recipe);
            resultMessage = $"Đã nấu thành công: {recipe.DisplayName}.";
            return true;
        }

        private void ResolveReferences()
        {
            if (!autoFindMissingReferences)
            {
                return;
            }

            if (playerInventory == null)
            {
                playerInventory = PlayerInventory.Instance != null
                    ? PlayerInventory.Instance
                    : FindAnyObjectByType<PlayerInventory>();
            }

            if (recipeUnlockManager == null)
            {
                recipeUnlockManager = RecipeUnlockManager.Instance != null
                    ? RecipeUnlockManager.Instance
                    : FindAnyObjectByType<RecipeUnlockManager>();
            }
        }
    }
}
