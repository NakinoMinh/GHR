using UnityEngine;
using UnityEditor;
using GanhHangRong.Interaction;

namespace GanhHangRong.Editor
{
    /// <summary>
    /// Editor tool — Tự động tạo Bồn Rửa Chén bên phải xe đẩy trong Scene.
    /// Menu: GanhHangRong > Thêm Bồn Rửa Chén
    /// </summary>
    public static class SetupDishwashingStation
    {
        [MenuItem("GanhHangRong/Thêm Bồn Rửa Chén bên phải xe đẩy")]
        public static void AddDishwashingStation()
        {
            // 1. Tìm TeaCart trong Scene
            var teaCartObj = GameObject.Find("TeaCart");
            if (teaCartObj == null)
            {
                EditorUtility.DisplayDialog("Không tìm thấy TeaCart",
                    "Không tìm thấy GameObject 'TeaCart' trong Scene.\nHãy đảm bảo xe đẩy đã được đặt vào Scene rồi mới chạy lại.",
                    "OK");
                return;
            }

            // 2. Kiểm tra đã có bồn rửa chưa
            var existing = GameObject.Find("DishwashingStation");
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog("Đã có Bồn Rửa Chén",
                    "Scene đã có GameObject 'DishwashingStation'.\nBạn có muốn tạo thêm một cái mới không?",
                    "Tạo thêm", "Hủy");
                if (!replace) return;
            }

            // 3. Load FBX model bồn rửa chén
            const string fbxPath = "Assets/cho_rua_chen/Meshy_AI_Dishwashing_Station_0611012056_texture.fbx";
            var fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxPrefab == null)
            {
                EditorUtility.DisplayDialog("Không tìm thấy FBX",
                    $"Không tìm thấy file:\n{fbxPath}\n\nHãy đảm bảo file FBX đã được import vào project.",
                    "OK");
                return;
            }

            // 4. Instantiate mô hình
            var stationGO = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            Undo.RegisterCreatedObjectUndo(stationGO, "Add Dishwashing Station");
            stationGO.name = "DishwashingStation";

            // 5. Tính vị trí bên TRÁI GÓC TRONG xe đẩy
            //    "Bên trái" = -transform.right của xe
            //    "Góc trong" = lùi vào phía sau xe (-transform.forward một chút)
            Vector3 cartPos = teaCartObj.transform.position;

            // Hướng "trái" của xe đẩy trong world space (âm của right)
            Vector3 leftDir = -teaCartObj.transform.right;
            leftDir.y = 0f;
            if (leftDir.sqrMagnitude < 0.001f) leftDir = Vector3.left;
            leftDir.Normalize();

            // Hướng "sau" của xe (góc trong) để thụt vào trong
            Vector3 backDir = -teaCartObj.transform.forward;
            backDir.y = 0f;
            if (backDir.sqrMagnitude < 0.001f) backDir = Vector3.back;
            backDir.Normalize();

            // Đặt bên trái 1.5m + lùi vào trong 0.5m → nằm gọn ở góc trái trong xe
            Vector3 stationPos = cartPos + leftDir * 1.5f + backDir * 0.5f;
            stationPos.y = cartPos.y;
            stationGO.transform.position = stationPos;

            // Xoay cùng hướng với xe đẩy
            stationGO.transform.rotation = teaCartObj.transform.rotation;

            // Scale nhỏ hơn xe đẩy cho phù hợp (bồn rửa thường nhỏ hơn xe)
            stationGO.transform.localScale = teaCartObj.transform.localScale * 0.6f;

            // 6. Thêm DishwashingStation component
            var stationComp = stationGO.GetComponent<DishwashingStation>();
            if (stationComp == null)
                stationComp = Undo.AddComponent<DishwashingStation>(stationGO);

            // 6.5. Gán material URP Lit với texture để tránh bị mất màu (magenta/trắng xóa)
            var fbxRenderers = stationGO.GetComponentsInChildren<MeshRenderer>(true);
            if (fbxRenderers.Length > 0)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (urpShader != null)
                {
                    // Load texture từ thư mục FBX
                    Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        "Assets/cho_rua_chen/Meshy_AI_Dishwashing_Station_0611012056_texture.png");
                    Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        "Assets/cho_rua_chen/Meshy_AI_Dishwashing_Station_0611012056_texture_normal.png");

                    // Fix normal map import type
                    if (normalTex != null)
                    {
                        var nImporter = AssetImporter.GetAtPath(
                            "Assets/cho_rua_chen/Meshy_AI_Dishwashing_Station_0611012056_texture_normal.png")
                            as TextureImporter;
                        if (nImporter != null && nImporter.textureType != TextureImporterType.NormalMap)
                        {
                            nImporter.textureType = TextureImporterType.NormalMap;
                            nImporter.SaveAndReimport();
                            normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                                "Assets/cho_rua_chen/Meshy_AI_Dishwashing_Station_0611012056_texture_normal.png");
                        }
                    }

                    Material stationMat = new Material(urpShader);
                    if (albedo != null)
                    {
                        if (stationMat.HasProperty("_BaseMap")) stationMat.SetTexture("_BaseMap", albedo);
                        else if (stationMat.HasProperty("_MainTex")) stationMat.SetTexture("_MainTex", albedo);
                        stationMat.color = Color.white;
                        if (stationMat.HasProperty("_BaseColor")) stationMat.SetColor("_BaseColor", Color.white);
                    }
                    else
                    {
                        // Fallback: màu inox xám bạc
                        stationMat.color = new Color(0.75f, 0.75f, 0.78f);
                        if (stationMat.HasProperty("_BaseColor")) stationMat.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.78f));
                        if (stationMat.HasProperty("_Metallic")) stationMat.SetFloat("_Metallic", 0.7f);
                        if (stationMat.HasProperty("_Smoothness")) stationMat.SetFloat("_Smoothness", 0.6f);
                    }
                    if (normalTex != null && stationMat.HasProperty("_BumpMap"))
                    {
                        stationMat.SetTexture("_BumpMap", normalTex);
                        stationMat.EnableKeyword("_NORMALMAP");
                    }

                    // Lưu material asset
                    string matDir = "Assets/_Project/Art/Materials";
                    if (!System.IO.Directory.Exists(matDir))
                        System.IO.Directory.CreateDirectory(matDir);
                    string matPath = matDir + "/DishwashingStationMat.mat";
                    var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (existingMat == null)
                        AssetDatabase.CreateAsset(stationMat, matPath);
                    else
                        stationMat = existingMat;

                    foreach (var r in fbxRenderers)
                        r.sharedMaterial = stationMat;

                    AssetDatabase.SaveAssets();
                }
            }

            // 7. Thêm BoxCollider nếu chưa có
            if (stationGO.GetComponent<Collider>() == null)
            {
                var col = Undo.AddComponent<BoxCollider>(stationGO);
                col.isTrigger = false;

                // Fit collider vào bounds của mesh
                var meshFilters = stationGO.GetComponentsInChildren<MeshFilter>();
                if (meshFilters.Length > 0)
                {
                    Bounds combinedBounds = meshFilters[0].sharedMesh != null
                        ? meshFilters[0].sharedMesh.bounds
                        : new Bounds();

                    foreach (var mf in meshFilters)
                    {
                        if (mf.sharedMesh != null)
                            combinedBounds.Encapsulate(mf.sharedMesh.bounds);
                    }

                    col.center = combinedBounds.center;
                    col.size = combinedBounds.size;
                }
            }

            // 8. Đánh dấu scene đã thay đổi
            EditorUtility.SetDirty(stationGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            // 9. Focus vào object vừa tạo
            Selection.activeGameObject = stationGO;
            SceneView.FrameLastActiveSceneView();

            Debug.Log($"[SetupDishwashingStation] Đã tạo bồn rửa chén tại {stationPos} (bên trái góc trong TeaCart).");
            EditorUtility.DisplayDialog("Đã hoàn tất!",
                $"Đã tạo 'DishwashingStation' bên trái góc trong xe đẩy.\n\n" +
                $"Vị trí: {stationPos}\n\n" +
                "Hãy kiểm tra và điều chỉnh vị trí/scale nếu cần trong Scene View.\n" +
                "Nhớ Save Scene (Ctrl+S)!",
                "OK");
        }
    }
}
