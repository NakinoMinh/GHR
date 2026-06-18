using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace GanhHangRong.Editor
{
    public static class SceneToBuilderExporter
    {
        [MenuItem("Gánh Hàng Rong/1. Lưu Môi Trường Mới Thành Prefab", false, 14)]
        public static void ExportNewEnvironment()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            
            // Tìm các object cần xuất
            List<GameObject> objectsToExport = new List<GameObject>();
            foreach(var root in roots)
            {
                // Bỏ qua các object đã được tạo bởi Builder mặc định
                string name = root.name;
                if (name == "Directional Light" || name == "Main Camera" || 
                    name == "Environment" || name == "TeaCart" || name.Contains("Player") ||
                    name == "WeatherFX" || name == "Managers" || name == "NPC_Spawner" || 
                    name == "Canvas" || name == "EventSystem") 
                {
                    continue;
                }
                
                objectsToExport.Add(root);
            }
            
            if (objectsToExport.Count == 0)
            {
                Debug.LogWarning("[Gánh Hàng Rong] Không tìm thấy object mới nào để lưu!");
                return;
            }
            
            // Gom nhóm lại
            GameObject exportRoot = new GameObject("Chapter1_Extensions");
            
            // Lưu lại parent gốc để trả về nếu cần
            Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();
            
            foreach(var obj in objectsToExport)
            {
                originalParents[obj] = obj.transform.parent;
                obj.transform.SetParent(exportRoot.transform);
            }
            
            // Đảm bảo thư mục tồn tại
            string dir = "Assets/_Project/Art/Environment/Generated";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            // Lưu thành Prefab
            string path = $"{dir}/Chapter1_Extensions.prefab";
            
            // Xóa file cũ nếu có để ghi đè
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
            
            PrefabUtility.SaveAsPrefabAsset(exportRoot, path);
            Debug.Log($"[Gánh Hàng Rong] Đã lưu {objectsToExport.Count} object(s) vào Prefab {path}");
            
            // Xóa object tạm và trả các object về scene để không làm hỏng scene đang làm việc
            foreach(var obj in objectsToExport)
            {
                obj.transform.SetParent(originalParents[obj]);
            }
            Object.DestroyImmediate(exportRoot);
            
            Debug.Log("[Gánh Hàng Rong] Quá trình xuất Prefab hoàn tất! Bây giờ Chapter1SceneBuilder.cs sẽ tự động sinh ra chúng.");
            EditorUtility.DisplayDialog("Thành Công", $"Đã lưu {objectsToExport.Count} objects mới thành Prefab.\n\nBây giờ bạn có thể thử chạy lại [Dựng Scene Chapter 1]!", "OK");
        }
    }
}
