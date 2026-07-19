#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GanhHangRong.EditorTools
{
    public static class ShipRuntimePrefabSetup
    {
        private const string ShipResourceFolder = "Assets/_Project/Resources/Ships";
        private const string ShipMaterialFolder = ShipResourceFolder + "/Materials";
        private const string FerrySource = "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture.fbx";
        private const string FishingBoatSource = "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.fbx";
        private const string FerryAlbedo = "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture.png";
        private const string FerryNormal = "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture_normal.png";
        private const string FishingBoatAlbedo = "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.png";
        private const string FishingBoatNormal = "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture_normal.png";
        private const string FerryPrefab = ShipResourceFolder + "/PhuQuocExpress.prefab";
        private const string FishingBoatPrefab = ShipResourceFolder + "/FishingBoat.prefab";
        private const string FerryMaterial = ShipMaterialFolder + "/PhuQuocExpress_URP.mat";
        private const string FishingBoatMaterial = ShipMaterialFolder + "/FishingBoat_URP.mat";

        [InitializeOnLoadMethod]
        private static void QueueSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !NeedsSetup())
                {
                    return;
                }

                EnsureFolder(ShipMaterialFolder);
                Material ferryMaterial = EnsureMaterial(FerryMaterial, FerryAlbedo, FerryNormal);
                Material fishingBoatMaterial = EnsureMaterial(FishingBoatMaterial, FishingBoatAlbedo, FishingBoatNormal);
                RebuildPrefab(FerrySource, FerryPrefab, "PhuQuocExpress", ferryMaterial);
                RebuildPrefab(FishingBoatSource, FishingBoatPrefab, "FishingBoat", fishingBoatMaterial);
                AssetDatabase.SaveAssets();
                Debug.Log("[ShipRuntimePrefabSetup] Runtime ship prefabs and URP materials are ready.");
            };
        }

        [MenuItem("GanhHangRong/Setup Runtime Ship Prefabs")]
        public static void RebuildPrefabs()
        {
            EnsureFolder(ShipMaterialFolder);
            Material ferryMaterial = EnsureMaterial(FerryMaterial, FerryAlbedo, FerryNormal);
            Material fishingBoatMaterial = EnsureMaterial(FishingBoatMaterial, FishingBoatAlbedo, FishingBoatNormal);
            RebuildPrefab(FerrySource, FerryPrefab, "PhuQuocExpress", ferryMaterial);
            RebuildPrefab(FishingBoatSource, FishingBoatPrefab, "FishingBoat", fishingBoatMaterial);
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsSetup()
        {
            Material ferryMaterial = AssetDatabase.LoadAssetAtPath<Material>(FerryMaterial);
            Material fishingBoatMaterial = AssetDatabase.LoadAssetAtPath<Material>(FishingBoatMaterial);
            GameObject ferryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FerryPrefab);
            GameObject fishingBoatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FishingBoatPrefab);

            return ferryMaterial == null
                || fishingBoatMaterial == null
                || !PrefabUsesMaterial(ferryPrefab, ferryMaterial)
                || !PrefabUsesMaterial(fishingBoatPrefab, fishingBoatMaterial);
        }

        private static bool PrefabUsesMaterial(GameObject prefab, Material material)
        {
            if (prefab == null || material == null)
            {
                return false;
            }

            Renderer renderer = prefab.GetComponentInChildren<Renderer>(true);
            return renderer != null && renderer.sharedMaterial == material;
        }

        private static Material EnsureMaterial(string materialPath, string albedoPath, string normalPath)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                Debug.LogError("[ShipRuntimePrefabSetup] No compatible Lit shader was found.");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (albedo == null)
            {
                Debug.LogError($"[ShipRuntimePrefabSetup] Missing ship albedo: {albedoPath}");
            }

            material.name = System.IO.Path.GetFileNameWithoutExtension(materialPath);
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetFloat("_Metallic", 0.15f);
            material.SetFloat("_Smoothness", 0.32f);
            material.enableInstancing = true;

            if (normal != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void RebuildPrefab(string sourcePath, string prefabPath, string prefabName, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null || material == null)
            {
                Debug.LogError($"[ShipRuntimePrefabSetup] Cannot build ship prefab: {sourcePath}");
                return;
            }

            GameObject instance = Object.Instantiate(source);
            instance.name = prefabName;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }
                renderer.sharedMaterials = materials;
            }

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
