using UnityEngine;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Sinh ra các con tàu trên biển và gán kịch bản đi tuần tra cho chúng.
    /// </summary>
    public class ShipManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<ShipManager>() != null) return;
            var go = new GameObject("[ShipManager]");
            go.AddComponent<ShipManager>();
        }

        private void Start()
        {
            // 1. Tàu Màu Đỏ (Phu Quoc Express Ferry) - Lane Z=30
            SpawnShip(
                "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture.fbx",
                "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture.png",
                "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture_normal.png",
                "TauMauDo_1",
                new Vector3(-50f, -1.4f, 30f),
                new Vector3(60f, -1.4f, 30f),
                3.5f,
                18.0f,
                new Vector3(-90f, -90f, 0f)
            );

            // 2. Thuyền Đánh Cá (Blue Vietnamese Fishing Boat) - Lane Z=22
            SpawnShip(
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.fbx",
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.png",
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture_normal.png",
                "ThuyenDanhCa_1",
                new Vector3(50f, -1.4f, 22f),
                new Vector3(-40f, -1.4f, 22f),
                2.0f,
                10.0f,
                new Vector3(-90f, -90f, 0f)
            );

            // 3. Thuyền Đánh Cá 2 - Lane Z=38
            SpawnShip(
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.fbx",
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.png",
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture_normal.png",
                "ThuyenDanhCa_2",
                new Vector3(-60f, -1.4f, 38f),
                new Vector3(40f, -1.4f, 38f),
                2.2f,
                10.0f,
                new Vector3(-90f, -90f, 0f)
            );

            // 4. Thuyền Đánh Cá 3 - Lane Z=46
            SpawnShip(
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.fbx",
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture.png",
                "Assets/thuyendanhca/Meshy_AI_Blue_Vietnamese_Fishi_0713000008_texture_normal.png",
                "ThuyenDanhCa_3",
                new Vector3(70f, -1.4f, 46f),
                new Vector3(-70f, -1.4f, 46f),
                1.8f,
                10.0f,
                new Vector3(-90f, -90f, 0f)
            );

            // 5. Tàu Màu Đỏ 2 - Lane Z=54
            SpawnShip(
                "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture.fbx",
                "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture.png",
                "Assets/taumaudo/Meshy_AI_Phu_Quoc_Express_Ferr_0712235919_texture_normal.png",
                "TauMauDo_2",
                new Vector3(80f, -1.4f, 54f),
                new Vector3(-80f, -1.4f, 54f),
                4.0f,
                18.0f,
                new Vector3(-90f, -90f, 0f)
            );
        }

        private void SpawnShip(string fbxPath, string texPath, string normalPath, string name, Vector3 start, Vector3 end, float speed, float targetHeight, Vector3 childRotation)
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[ShipManager] Không tìm thấy FBX tại {fbxPath}");
                return;
            }

            Texture2D albedo = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Texture2D normalTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                            ? Shader.Find("Universal Render Pipeline/Lit")
                            : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            if (shader.name.Contains("Universal"))
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTexture("_BumpMap", normalTex);
            }
            else
            {
                mat.mainTexture = albedo;
                mat.SetTexture("_BumpMap", normalTex);
            }
            if (normalTex != null) mat.EnableKeyword("_NORMALMAP");

            GameObject parent = new GameObject(name);
            parent.transform.position = start;

            GameObject visual = Instantiate(prefab, parent.transform);
            visual.name = "Visual";
            visual.transform.localRotation = Quaternion.Euler(childRotation);

            Bounds b = new Bounds(visual.transform.position, Vector3.zero);
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                r.material = mat;
                b.Encapsulate(r.bounds);
            }

            if (b.size.y > 0.001f)
            {
                float scale = targetHeight / b.size.y;
                visual.transform.localScale *= scale;
            }

            // Căn chỉnh để đáy thuyền nằm ngay trục Y của parent
            b = new Bounds(visual.transform.position, Vector3.zero);
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
            float yOffset = parent.transform.position.y - b.min.y;
            visual.transform.localPosition = new Vector3(0, yOffset - 0.2f, 0); // Trừ 0.2f để thuyền hơi chìm dưới nước một xíu

            var patrol = parent.AddComponent<Environment.ShipPatrol>();
            patrol.startPos = start;
            patrol.endPos = end;
            patrol.speed = speed;
#else
            Debug.LogWarning("[ShipManager] Việc load FBX trực tiếp bằng đường dẫn chỉ hoạt động trong Editor.");
#endif
        }
    }
}
