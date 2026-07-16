#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GanhHangRong.EditorTools
{
    public static class ShipRuntimePrefabSetup
    {
        private const string ShipResourceFolder = "Assets/_Project/Resources/Ships";
        private const string FerrySource = "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture.fbx";
        private const string FishingBoatSource = "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.fbx";
        private const string FerryPrefab = ShipResourceFolder + "/PhuQuocExpress.prefab";
        private const string FishingBoatPrefab = ShipResourceFolder + "/FishingBoat.prefab";

        [InitializeOnLoadMethod]
        private static void QueueSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !NeedsSetup())
                {
                    return;
                }

                EnsureFolder(ShipResourceFolder);
                CreatePrefabIfMissing(FerrySource, FerryPrefab, "PhuQuocExpress");
                CreatePrefabIfMissing(FishingBoatSource, FishingBoatPrefab, "FishingBoat");
                AssetDatabase.SaveAssets();
                Debug.Log("[ShipRuntimePrefabSetup] Runtime ship prefabs are ready.");
            };
        }

        [MenuItem("GanhHangRong/Setup Runtime Ship Prefabs")]
        public static void RebuildPrefabs()
        {
            EnsureFolder(ShipResourceFolder);
            RebuildPrefab(FerrySource, FerryPrefab, "PhuQuocExpress");
            RebuildPrefab(FishingBoatSource, FishingBoatPrefab, "FishingBoat");
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsSetup()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(FerryPrefab) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(FishingBoatPrefab) == null;
        }

        private static void CreatePrefabIfMissing(string sourcePath, string prefabPath, string prefabName)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                RebuildPrefab(sourcePath, prefabPath, prefabName);
            }
        }

        private static void RebuildPrefab(string sourcePath, string prefabPath, string prefabName)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogError($"[ShipRuntimePrefabSetup] Missing ship model: {sourcePath}");
                return;
            }

            GameObject instance = Object.Instantiate(source);
            instance.name = prefabName;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;

            try
            {
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }
    }
}
#endif
