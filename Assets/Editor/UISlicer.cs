using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GanhHangRong.EditorScripts
{
    public class UISlicer : EditorWindow
    {
        [MenuItem("Gánh Hàng Rong/Auto Slice UserUI")]
        public static void SliceUserUI()
        {
            string path = "Assets/_Project/Art/UI/UserUI.png";
            
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("UserUI.png not found at " + path);
                return;
            }

            importer.isReadable = true;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return;

            // Generate Rects
            Rect[] rects = UnityEditorInternal.InternalSpriteUtility.GenerateAutomaticSpriteRectangles(tex, 10, 0);
            
            List<SpriteMetaData> metaData = new List<SpriteMetaData>();
            for (int i = 0; i < rects.Length; i++)
            {
                SpriteMetaData smd = new SpriteMetaData();
                smd.rect = rects[i];
                smd.name = "UISlice_" + i;
                smd.pivot = new Vector2(0.5f, 0.5f);
                smd.alignment = 9; // Custom pivot
                metaData.Add(smd);
            }

            importer.spritesheet = metaData.ToArray();
            importer.isReadable = false; // reset
            importer.SaveAndReimport();

            Debug.Log("Sliced UserUI.png into " + rects.Length + " sprites!");
        }
    }
}
