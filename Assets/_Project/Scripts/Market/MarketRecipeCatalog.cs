using System.Collections.Generic;
using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Economy
{
    public static class MarketRecipeCatalog
    {
        private static readonly Dictionary<string, RecipeData> RecipesById = new Dictionary<string, RecipeData>();

        public static RecipeData GetRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return null;
            }

            if (RecipesById.TryGetValue(recipeId, out RecipeData cached) && cached != null)
            {
                return cached;
            }

            RecipeData recipe = Resources.Load<RecipeData>("Recipes/" + recipeId);
            RecipesById[recipeId] = recipe;
            return recipe;
        }

        public static RecipeData GetRecipeForOrder(int orderId)
        {
            switch (orderId)
            {
                case ChapterOrderCatalog.BunCaKienGiang:
                    return GetRecipe("bun_ca_kien_giang");
                case ChapterOrderCatalog.BanhCanhGhe:
                    return GetRecipe("banh_canh_ghe");
                case ChapterOrderCatalog.TomRimNuocMam:
                    return GetRecipe("tom_rim_nuoc_mam");
                case ChapterOrderCatalog.MucNuongMuoiOt:
                    return GetRecipe("muc_nuong_muoi_ot");
                case ChapterOrderCatalog.NgheuXaoCay:
                    return GetRecipe("ngheu_xao_cay");
                case ChapterOrderCatalog.NuocMia:
                    return GetRecipe("nuoc_mia");
                case ChapterOrderCatalog.TraChanh:
                    return GetRecipe("tra_chanh");
                case ChapterOrderCatalog.NuocDua:
                    return GetRecipe("nuoc_dua");
                default:
                    return null;
            }
        }
    }
}
