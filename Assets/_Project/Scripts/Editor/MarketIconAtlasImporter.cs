#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GanhHangRong.EditorTools
{
    public sealed class MarketIconAtlasImporter : AssetPostprocessor
    {
        private const string ItemAtlasPath = "Assets/_Project/Resources/UI/Market/market_items_atlas.png";
        private const string RecipeAtlasPath = "Assets/_Project/Resources/UI/Market/market_recipes_atlas.png";
        private const string ExpansionAtlasPath = "Assets/_Project/Resources/UI/Market/market_expansion_atlas.png";

        private static readonly string[] SpriteNames =
        {
            "banh_mi", "banh_trang", "trung", "muoi",
            "ot_bot", "dau_an", "duong_thot_not", "nuoc_mam_phu_quoc",
            "thot_not", "tom", "muc", "ngheu",
            "so", "bach_tuoc", "sach_bun_ca_kien_giang", "sach_banh_canh_ghe"
        };

        private static readonly string[] RecipeSpriteNames =
        {
            "ca_loc", "ghe", "bun_tuoi",
            "banh_canh", "bun_ca_kien_giang", "banh_canh_ghe"
        };

        private static readonly string[] ExpansionSpriteNames =
        {
            "mia_cay", "chanh_tuoi", "la_tra", "dua_tuoi",
            "sach_tom_rim_nuoc_mam", "sach_muc_nuong_muoi_ot", "sach_ngheu_xao_cay", null,
            "tom_rim_nuoc_mam", "muc_nuong_muoi_ot", "ngheu_xao_cay", null,
            "nuoc_mia", "tra_chanh", "nuoc_dua", null
        };

        private void OnPreprocessTexture()
        {
            bool isItemAtlas = assetPath == ItemAtlasPath;
            bool isRecipeAtlas = assetPath == RecipeAtlasPath;
            bool isExpansionAtlas = assetPath == ExpansionAtlasPath;
            if (!isItemAtlas && !isRecipeAtlas && !isExpansionAtlas)
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;

#pragma warning disable 0618
            string[] names = isItemAtlas ? SpriteNames : (isRecipeAtlas ? RecipeSpriteNames : ExpansionSpriteNames);
            int columns = isRecipeAtlas ? 3 : 4;
            int rows = isRecipeAtlas ? 2 : 4;
            float textureWidth = isRecipeAtlas ? 1536f : 1254f;
            float textureHeight = isRecipeAtlas ? 1024f : 1254f;
            float edgeInset = isExpansionAtlas ? 2f : 0f;
            List<SpriteMetaData> slices = new List<SpriteMetaData>();
            float cellWidth = textureWidth / columns;
            float cellHeight = textureHeight / rows;

            for (int index = 0; index < names.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(names[index]))
                {
                    continue;
                }

                int rowFromTop = index / columns;
                int column = index % columns;
                slices.Add(new SpriteMetaData
                {
                    name = names[index],
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(
                        column * cellWidth + edgeInset,
                        textureHeight - ((rowFromTop + 1) * cellHeight) + edgeInset,
                        cellWidth - edgeInset * 2f,
                        cellHeight - edgeInset * 2f)
                });
            }

            importer.spritesheet = slices.ToArray();
#pragma warning restore 0618
        }

        [InitializeOnLoadMethod]
        private static void EnsureAtlasIsSliced()
        {
            EditorApplication.delayCall += () =>
            {
                EnsureAtlas(ItemAtlasPath, SpriteNames.Length);
                EnsureAtlas(RecipeAtlasPath, RecipeSpriteNames.Length);
                EnsureAtlas(ExpansionAtlasPath, ExpansionSpriteNames.Count(name => !string.IsNullOrWhiteSpace(name)));
            };
        }

        private static void EnsureAtlas(string path, int expectedSpriteCount)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            int spriteCount = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .Count();

            if (importer.spriteImportMode != SpriteImportMode.Multiple || spriteCount != expectedSpriteCount)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
#endif
