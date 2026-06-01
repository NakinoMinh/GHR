using UnityEditor;
using UnityEngine;
using System.IO;

namespace GanhHangRong.Editor
{
    /// <summary>
    /// Editor tool để tạo cấu trúc thư mục chuyên nghiệp cho dự án Gánh Hàng Rong.
    /// Chạy từ menu: Gánh Hàng Rong > Tạo Cấu Trúc Thư Mục
    /// </summary>
    public static class FolderStructureCreator
    {
        [MenuItem("Gánh Hàng Rong/Tạo Cấu Trúc Thư Mục", false, 0)]
        public static void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                // Art
                "Assets/_Project/Art/Characters",
                "Assets/_Project/Art/Environment",
                "Assets/_Project/Art/Props",
                "Assets/_Project/Art/Materials",
                "Assets/_Project/Art/Textures",
                "Assets/_Project/Art/VFX",
                "Assets/_Project/Art/UI",

                // Audio
                "Assets/_Project/Audio/Ambient",
                "Assets/_Project/Audio/Music",
                "Assets/_Project/Audio/SFX",
                "Assets/_Project/Audio/Voice",

                // Prefabs
                "Assets/_Project/Prefabs/Characters",
                "Assets/_Project/Prefabs/Environment",
                "Assets/_Project/Prefabs/Props",
                "Assets/_Project/Prefabs/UI",
                "Assets/_Project/Prefabs/Systems",

                // Scenes
                "Assets/_Project/Scenes/Bootstrap",
                "Assets/_Project/Scenes/MainMenu",
                "Assets/_Project/Scenes/Chapter1",
                "Assets/_Project/Scenes/TestScenes",

                // Scripts
                "Assets/_Project/Scripts/Core",
                "Assets/_Project/Scripts/Player",
                "Assets/_Project/Scripts/NPC",
                "Assets/_Project/Scripts/Interaction",
                "Assets/_Project/Scripts/Systems",
                "Assets/_Project/Scripts/UI",
                "Assets/_Project/Scripts/Audio",
                "Assets/_Project/Scripts/Weather",
                "Assets/_Project/Scripts/Narrative",

                // Shaders
                "Assets/_Project/Shaders",

                // ScriptableObjects
                "Assets/_Project/ScriptableObjects/Items",
                "Assets/_Project/ScriptableObjects/Recipes",
                "Assets/_Project/ScriptableObjects/Dialogue",
                "Assets/_Project/ScriptableObjects/Weather",

                // Animations
                "Assets/_Project/Animations/Player",
                "Assets/_Project/Animations/NPC",
                "Assets/_Project/Animations/Environment",

                // Resources & Settings
                "Assets/_Project/Resources",
                "Assets/_Project/Settings",

                // Third Party & Plugins
                "Assets/ThirdParty",
                "Assets/Plugins",
            };

            foreach (string folder in folders)
            {
                string fullPath = Path.Combine(Application.dataPath, "..", folder);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[Gánh Hàng Rong] ✅ Đã tạo xong cấu trúc thư mục dự án!");
            EditorUtility.DisplayDialog(
                "Gánh Hàng Rong",
                "Đã tạo xong cấu trúc thư mục dự án!",
                "OK"
            );
        }
    }
}
