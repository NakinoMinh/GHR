using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace GanhHangRong.Editor
{
    public static class SetupBuildSettings
    {
        [MenuItem("Gánh Hàng Rong/Cấu hình Build Settings (Fix lỗi chuyển Scene)", false, 100)]
        public static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            
            // Tìm tất cả các file Scene (.unity) trong thư mục dự án của chúng ta
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes" });
            
            // Đảm bảo MainMenu luôn nằm đầu tiên (Index 0)
            string mainMenuPath = "";
            List<string> otherPaths = new List<string>();

            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("MainMenu"))
                    mainMenuPath = path;
                else
                    otherPaths.Add(path);
            }

            // Thêm Main Menu vào trước
            if (!string.IsNullOrEmpty(mainMenuPath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(mainMenuPath, true));
                Debug.Log($"[Gánh Hàng Rong] Đã thêm Scene vào Build (Index 0): {mainMenuPath}");
            }

            // Thêm các Scene còn lại (như Chapter 1)
            foreach (string path in otherPaths)
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"[Gánh Hàng Rong] Đã thêm Scene vào Build: {path}");
            }
            
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[Gánh Hàng Rong] Hoàn tất cấu hình Build Settings! Bạn đã có thể chuyển Scene bình thường.");
        }
    }
}
