using System;
using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Economy
{
    public class RecipeUnlockManager : MonoBehaviour
    {
        public static RecipeUnlockManager Instance { get; private set; }

        private const string SaveKey = "GHR_UnlockedRecipes";

        [Header("Công thức mở sẵn")]
        [SerializeField] private List<RecipeData> defaultUnlockedRecipes = new List<RecipeData>();

        private readonly HashSet<string> unlockedRecipeIds = new HashSet<string>();

        public event Action<string> RecipeUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Đã có RecipeUnlockManager khác trong scene. Object này sẽ tự hủy.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadData();
        }

        public bool UnlockRecipe(RecipeData recipe)
        {
            if (recipe == null)
            {
                Debug.LogWarning("Không thể unlock recipe vì RecipeData bị null.", this);
                return false;
            }

            return UnlockRecipe(recipe.Id);
        }

        public bool UnlockRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                Debug.LogWarning("Không thể unlock recipe vì recipeId rỗng.", this);
                return false;
            }

            if (!unlockedRecipeIds.Add(recipeId))
            {
                return false;
            }

            SaveData();
            RecipeUnlocked?.Invoke(recipeId);
            return true;
        }

        public bool IsRecipeUnlocked(RecipeData recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            if (!recipe.RequiresRecipeBook)
            {
                return true;
            }

            return IsRecipeUnlocked(recipe.Id);
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            return !string.IsNullOrWhiteSpace(recipeId) && unlockedRecipeIds.Contains(recipeId);
        }

        public void SaveData()
        {
            PlayerPrefs.SetString(SaveKey, string.Join("|", unlockedRecipeIds));
            PlayerPrefs.Save();
        }

        public void LoadData()
        {
            unlockedRecipeIds.Clear();

            foreach (RecipeData recipe in defaultUnlockedRecipes)
            {
                if (recipe != null && !string.IsNullOrWhiteSpace(recipe.Id))
                {
                    unlockedRecipeIds.Add(recipe.Id);
                }
            }

            string raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            string[] ids = raw.Split('|');
            foreach (string id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    unlockedRecipeIds.Add(id);
                }
            }
        }
    }
}
