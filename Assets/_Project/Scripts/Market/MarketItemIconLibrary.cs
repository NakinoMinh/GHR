using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Economy
{
    public static class MarketItemIconLibrary
    {
        private static readonly string[] AtlasResourcePaths =
        {
            "UI/Market/market_items_atlas",
            "UI/Market/market_recipes_atlas",
            "UI/Market/market_expansion_atlas"
        };
        private static Dictionary<string, Sprite> iconsById;

        public static Sprite GetIcon(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            EnsureLoaded();
            string key = itemId.Trim().ToLowerInvariant();
            if (!iconsById.TryGetValue(key, out Sprite icon))
            {
                LoadAtlases();
                iconsById.TryGetValue(key, out icon);
            }
            return icon;
        }

        private static void EnsureLoaded()
        {
            if (iconsById != null)
            {
                return;
            }

            iconsById = new Dictionary<string, Sprite>();
            LoadAtlases();
        }

        private static void LoadAtlases()
        {
            foreach (string atlasPath in AtlasResourcePaths)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(atlasPath);
                foreach (Sprite sprite in sprites)
                {
                    if (sprite != null)
                    {
                        iconsById[sprite.name.ToLowerInvariant()] = sprite;
                    }
                }
            }
        }
    }
}
