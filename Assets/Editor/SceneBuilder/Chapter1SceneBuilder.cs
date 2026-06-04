using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using GanhHangRong.Core;
using GanhHangRong.Player;
using GanhHangRong.NPC;
using GanhHangRong.Interaction;
using GanhHangRong.Weather;
using GanhHangRong.Audio;
using GanhHangRong.Economy;
using GanhHangRong.UI;
using System.Collections.Generic;

namespace GanhHangRong.Editor
{
    public static class Chapter1SceneBuilder
    {
        [MenuItem("Gánh Hàng Rong/Dựng Scene Chapter 1", false, 12)]
        public static void BuildChapter1Scene()
        {
            // Tự động nâng cấp vật liệu Standard sang URP Lit để tránh bị lỗi hồng (magenta)
            UpgradeMaterialsToURP();

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = "Chapter1";

            // TẠO VẬT LIỆU TẠM
            Material matToon = CreateMaterial("ToonMat", "GanhHangRong/ToonShading", Color.white);
            Material matPlayer = CreateMaterial("PlayerMat", "GanhHangRong/ToonShading", new Color(0.9f, 0.4f, 0.2f));
            Material matRoad = CreateMaterial("RoadMat", "GanhHangRong/WetGround", new Color(0.15f, 0.15f, 0.18f));
            Material matWater = CreateMaterial("WaterMat", "GanhHangRong/WaterSurface", new Color(0f, 0.3f, 0.5f));
            Material matCart = CreateMaterial("CartMat", "GanhHangRong/ToonShading", new Color(0.6f, 0.4f, 0.2f));
            Material matChair = CreateMaterial("ChairMat", "GanhHangRong/ToonShading", new Color(0.8f, 0.1f, 0.1f));
            Material matRailing = CreateMaterial("RailingMat", "GanhHangRong/ToonShading", new Color(0.3f, 0.3f, 0.35f));
            Material matStreetLight = CreateMaterial("StreetLightMat", "GanhHangRong/ToonShading", new Color(0.2f, 0.2f, 0.2f));
            Material matLightBulb = CreateMaterial("LightBulbMat", "Standard", new Color(1f, 0.9f, 0.5f));
            if (matLightBulb.HasProperty("_EmissionColor"))
            {
                matLightBulb.EnableKeyword("_EMISSION");
                matLightBulb.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.5f) * 2f);
            }
            Material matBoatWood = CreateMaterial("BoatWoodMat", "GanhHangRong/ToonShading", new Color(0.4f, 0.25f, 0.15f));
            Material matBoatCabin = CreateMaterial("BoatCabinMat", "GanhHangRong/ToonShading", new Color(0.85f, 0.85f, 0.85f));
            Material matMountain = CreateMaterial("MountainMat", "GanhHangRong/ToonShading", new Color(0.15f, 0.2f, 0.28f));

            // ==========================================
            // 1. ÁNH SÁNG
            // ==========================================
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.8f, 0.8f, 1f);
            dirLight.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // ==========================================
            // 2. CAMERA & POST PROCESSING (Giả lập)
            // ==========================================
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.orthographic = false;
            cam.fieldOfView = 50f; // Góc nhìn rộng hơn để thấy rõ toàn cảnh thành phố
            camObj.transform.position = new Vector3(-5f, 3f, -9f);
            camObj.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            var cinCam = camObj.AddComponent<CinematicCamera>();

            // ==========================================
            // 3. MÔI TRƯỜNG (Sử dụng Simple City Plain)
            // ==========================================
            GameObject envParent = new GameObject("Environment");

            // Lambda helper để spawn prefab và cấu hình Collider nếu thiếu
            System.Func<string, string, Vector3, Quaternion, Transform, GameObject> spawnPrefab = (fileName, objName, pos, rot, parent) => {
                string path = "Assets/Simple city plain/Prefabs/" + fileName;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    obj.name = objName;
                    obj.transform.position = pos;
                    obj.transform.rotation = rot;
                    if (parent != null) obj.transform.SetParent(parent);
                    
                    // Đảm bảo có Collider để tránh rơi tự do
                    if (obj.GetComponent<Collider>() == null && obj.GetComponentInChildren<Collider>() == null)
                    {
                        var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
                        if (meshFilters.Length > 0)
                        {
                            foreach (var mf in meshFilters)
                            {
                                if (mf.gameObject.GetComponent<Collider>() == null)
                                {
                                    mf.gameObject.AddComponent<MeshCollider>();
                                }
                            }
                        }
                        else
                        {
                            obj.AddComponent<BoxCollider>();
                        }
                    }
                    return obj;
                }
                else
                {
                    Debug.LogWarning($"[Gánh Hàng Rong] Không tìm thấy prefab tại {path}");
                    return null;
                }
            };

            // Đo đạc kích thước thực tế của prefab vỉa hè để điều chỉnh bước nhảy khít nhau hoàn toàn
            float stepX = 5f; // Fallback mặc định
            string sidewalkPath = "Assets/Simple city plain/Prefabs/Sideway Prefab.prefab";
            GameObject sidewalkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sidewalkPath);
            if (sidewalkPrefab != null)
            {
                var mf = sidewalkPrefab.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    // Do prefab con có thể xoay -90 độ quanh X, trục X cục bộ của mesh tương ứng trục X thế giới
                    stepX = mf.sharedMesh.bounds.size.x * mf.transform.localScale.x;
                    Debug.Log($"[Gánh Hàng Rong] Kích thước vỉa hè đo được: {stepX}");
                }
            }

            if (stepX < 0.1f) stepX = 5f;

            string stonePath = "Assets/Simple city plain/Prefabs/Stone Floor prefab.prefab";
            GameObject stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stonePath);

            // Đo độ cao thực tế của bề mặt vỉa hè và nền đá
            float sidewalkTopY = CalculateModelMaxY(sidewalkPrefab, Quaternion.identity, Vector3.one);
            float stoneTopY = CalculateModelMaxY(stonePrefab, Quaternion.identity, Vector3.one);
            Debug.Log($"[Gánh Hàng Rong] Độ cao bề mặt đo được: sidewalkTopY={sidewalkTopY}, stoneTopY={stoneTopY}");

            // Spawn một hàng vỉa hè (Sidewalk) nằm ngang duy nhất dọc theo trục X - tự động khít hoàn toàn và kéo dài sang phải
            for (float x = -40f; x <= 120f; x += stepX)
            {
                spawnPrefab("Sideway Prefab.prefab", $"Sidewalk_{x}", new Vector3(x, 0f, 0f), Quaternion.identity, envParent.transform);
            }

            // Đo đạc kích thước thực tế của prefab đường (Street)
            float stepStreetX = 5f;
            float stepStreetZ = 5f;
            string streetPath = "Assets/Simple city plain/Prefabs/Street 3 Prefab.prefab";
            GameObject streetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(streetPath);
            if (streetPrefab != null)
            {
                var mf = streetPrefab.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    stepStreetX = mf.sharedMesh.bounds.size.x * mf.transform.localScale.x;
                    stepStreetZ = mf.sharedMesh.bounds.size.y * mf.transform.localScale.y;
                    Debug.Log($"[Gánh Hàng Rong] Kích thước đường đo được: X={stepStreetX}, Z={stepStreetZ}");
                }
            }

            if (stepStreetX < 0.1f) stepStreetX = 5f;
            if (stepStreetZ < 0.1f) stepStreetZ = 5f;

            // Đo chiều sâu vỉa hè dọc trục Z
            float stepSidewalkZ = 2f;
            if (sidewalkPrefab != null)
            {
                var mf = sidewalkPrefab.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    stepSidewalkZ = mf.sharedMesh.bounds.size.y * mf.transform.localScale.y;
                }
            }
            float streetZ = -(stepSidewalkZ / 2f + stepStreetZ / 2f);

            // Spawn hàng đường (Street) thứ nhất song song ngay cạnh bên phải (phía trước camera) của vỉa hè
            for (float x = -40f; x <= 120f; x += stepStreetX)
            {
                spawnPrefab("Street 3 Prefab.prefab", $"Street_1_{x}", new Vector3(x, 0f, streetZ), Quaternion.identity, envParent.transform);
            }

            // Spawn hàng đường (Street) thứ hai kế tiếp song song ngay cạnh hàng thứ nhất (càng gần camera hơn)
            float streetZ2 = streetZ - stepStreetZ;
            for (float x = -40f; x <= 120f; x += stepStreetX)
            {
                spawnPrefab("Street 3 Prefab.prefab", $"Street_2_{x}", new Vector3(x, 0f, streetZ2), Quaternion.identity, envParent.transform);
            }

            // Spawn hàng vỉa hè (Sidewalk) thứ hai song song ở phía đối diện (phía trước camera) của đường
            float sidewalkZ2 = streetZ2 - (stepStreetZ / 2f + stepSidewalkZ / 2f);
            for (float x = -40f; x <= 120f; x += stepX)
            {
                spawnPrefab("Sideway Prefab.prefab", $"Sidewalk_Front_{x}", new Vector3(x, 0f, sidewalkZ2), Quaternion.identity, envParent.transform);
            }

            // Đo đạc kích thước thực tế của prefab nền đá (Stone Floor)
            float stepStoneX = 5f;
            float stepStoneZ = 5f;
            stonePath = "Assets/Simple city plain/Prefabs/Stone Floor prefab.prefab";
            stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stonePath);
            if (stonePrefab != null)
            {
                var mf = stonePrefab.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    stepStoneX = mf.sharedMesh.bounds.size.x * mf.transform.localScale.x;
                    stepStoneZ = mf.sharedMesh.bounds.size.y * mf.transform.localScale.y;
                    Debug.Log($"[Gánh Hàng Rong] Kích thước nền đá đo được: X={stepStoneX}, Z={stepStoneZ}");
                }
            }

            if (stepStoneX < 0.1f) stepStoneX = 5f;
            if (stepStoneZ < 0.1f) stepStoneZ = 5f;

            float stoneZ1 = sidewalkZ2 - (stepSidewalkZ / 2f + stepStoneZ / 2f);
            float stoneZ2 = stoneZ1 - stepStoneZ;

            // Spawn hai hàng nền đá (Stone Floor) song song tiếp nối vỉa hè thứ hai (gần sát camera nhất)
            for (float x = -40f; x <= 120f; x += stepStoneX)
            {
                spawnPrefab("Stone Floor prefab.prefab", $"StoneFloor_1_{x}", new Vector3(x, 0f, stoneZ1), Quaternion.identity, envParent.transform);
                spawnPrefab("Stone Floor prefab.prefab", $"StoneFloor_2_{x}", new Vector3(x, 0f, stoneZ2), Quaternion.identity, envParent.transform);
            }

            // Spawn một hàng nền đá (Stone Floor) chạy dọc ở bên trái vỉa hè người chơi (phía sau xe trà đá)
            float stoneLeftZ = (stepSidewalkZ / 2f) + (stepStoneZ / 2f);
            for (float x = -40f; x <= 120f; x += stepStoneX)
            {
                spawnPrefab("Stone Floor prefab.prefab", $"StoneFloor_Left_{x}", new Vector3(x, 0f, stoneLeftZ), Quaternion.identity, envParent.transform);
            }

            // Spawn 1 căn nhà Building_N_Prefab ở phía bên phải trên nền Stone Floor (Z = stoneZ2) quay mặt về phía đường
            spawnPrefab("Building_N_Prefab.prefab", "Building_N_Foreground", new Vector3(30f, 0f, stoneZ2), Quaternion.Euler(0f, 90f, 0f), envParent.transform);

            // Spawn 1 căn nhà Building_A1_prefab ở kế bên phải tòa Building_N_Prefab (X = 42f)
            spawnPrefab("Building_A1_prefab.prefab", "Building_A1_Foreground", new Vector3(42f, 0f, stoneZ2), Quaternion.Euler(0f, 90f, 0f), envParent.transform);

            // Spawn 1 máy bán nước ColaMachine prefab ở bên trái tòa Building_N_Prefab (X = 20f, dịch hẳn ra ngoài để không bị chìm vào trong tường nhà)
            spawnPrefab("ColaMachine prefab.prefab", "ColaMachine_Foreground", new Vector3(20f, 0f, stoneZ2), Quaternion.identity, envParent.transform);

            // ==========================================
            // 4. XE TRÀ ĐÁ & GHẾ NHỰA
            // ==========================================
            string cartModelPath = "Assets/ganhnuoc/Meshy_AI_Vietnam_Sugarcane_Jui_0602210551_texture.fbx";
            GameObject cartModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cartModelPath);
            GameObject cartObj = null;

            // DIAGNOSTICS LOGGING
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Loading FBX at: {cartModelPath}");
            sb.AppendLine($"Prefab is null? {cartModelPrefab == null}");
            if (cartModelPrefab != null)
            {
                sb.AppendLine($"Prefab Name: {cartModelPrefab.name}");
                var renderers = cartModelPrefab.GetComponentsInChildren<MeshRenderer>(true);
                sb.AppendLine($"Number of MeshRenderers: {renderers.Length}");
                foreach (var r in renderers)
                {
                    sb.AppendLine($"- Renderer: {r.name}, Active: {r.gameObject.activeInHierarchy}");
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        sb.AppendLine($"  Mesh Bounds: {mf.sharedMesh.bounds.ToString()}");
                    }
                }
            }
            try
            {
                System.IO.File.WriteAllText("fbx_inspect.txt", sb.ToString());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Gánh Hàng Rong] Lỗi ghi file fbx_inspect.txt: {ex.Message}");
            }

            if (cartModelPrefab != null)
            {
                cartObj = (GameObject)PrefabUtility.InstantiatePrefab(cartModelPrefab);
                cartObj.name = "TeaCart";

                // Bật active cho toàn bộ gameobject con và renderers (đặc biệt khi model bị ẩn mặc định)
                var renderers = cartObj.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    mr.gameObject.SetActive(true);
                    mr.enabled = true;
                }

                // Tính toán bounds thực tế của mô hình
                Bounds combinedBounds = new Bounds();
                bool hasBounds = false;
                foreach (var mr in renderers)
                {
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        if (!hasBounds)
                        {
                            combinedBounds = mf.sharedMesh.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            combinedBounds.Encapsulate(mf.sharedMesh.bounds);
                        }
                    }
                }

                // Mô hình FBX của bạn có kích thước Mesh rất nhỏ (extents 0.01 -> size 0.02)
                // Chúng ta sẽ scale tự động để xe cao khoảng 1.6m
                float targetHeight = 1.6f;
                float unscaledHeight = CalculateModelHeight(cartObj, Quaternion.Euler(-90f, 180f, 0f), out float bottomOffsetAtScale1);
                float scaleFactor = targetHeight / unscaledHeight;

                cartObj.transform.localScale = Vector3.one * scaleFactor;

                // Đặt vị trí sao cho đáy của xe khớp sát mặt vỉa hè
                float yPos = sidewalkTopY + (bottomOffsetAtScale1 * scaleFactor) + 0.002f;
                cartObj.transform.position = new Vector3(4f, yPos, 0f);
                cartObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f); // Xoay ngược lại 180 độ (yaw = 180)

                // Gán vật liệu chi tiết
                Material modelMat = CreateCartModelMaterial();
                foreach (var mr in renderers)
                {
                    mr.sharedMaterial = modelMat;
                }

                // Thêm collider để tương tác
                var boxCol = cartObj.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                if (hasBounds)
                {
                    boxCol.center = combinedBounds.center;
                    boxCol.size = combinedBounds.size * 1.2f;
                }
                else
                {
                    boxCol.center = new Vector3(0f, 0.75f, 0f);
                    boxCol.size = new Vector3(1.5f, 1.5f, 1.5f);
                }
                
                var teaCartComp = cartObj.AddComponent<TeaCart>();

                // --- Add Kettle Prop ---
                string kettleModelPath = "Assets/amnuoc/amdunnuoc.fbx";
                GameObject kettlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(kettleModelPath);
                if (kettlePrefab != null)
                {
                    GameObject kettleObj = (GameObject)PrefabUtility.InstantiatePrefab(kettlePrefab);
                    kettleObj.name = "KettleProp";
                    
                    float kettleTargetHeight = 0.22f; // Thu nhỏ lại vừa vặn hơn (22cm)
                    // Đo kích thước trước khi set parent để tránh bị ảnh hưởng bởi scale của cha
                    float kettleUnscaledHeight = CalculateModelHeight(kettleObj, Quaternion.identity, out float kettleOffset);
                    float kettleScaleFactor = kettleTargetHeight / kettleUnscaledHeight;
                    
                    kettleObj.transform.SetParent(cartObj.transform);
                    // Quy đổi tỉ lệ cục bộ tương đối với cha
                    kettleObj.transform.localScale = Vector3.one * (kettleScaleFactor / scaleFactor);
                    
                    float tableWorldY = sidewalkTopY + 0.63f; // Hạ độ cao xuống mặt bàn chính (khoảng 63cm trên vỉa hè)
                    kettleObj.transform.position = new Vector3(3.65f, tableWorldY + (kettleOffset * kettleScaleFactor), -0.12f);
                    kettleObj.transform.rotation = Quaternion.identity;
                    
                    Material kettleMat = CreateKettleMaterial();
                    var kettleRenderers = kettleObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in kettleRenderers) r.sharedMaterial = kettleMat;

                    // Đảm bảo vật phẩm đứng yên cố định (kinematic), không bị trượt hay rơi do vật lý
                    var rbs = kettleObj.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var rb in rbs)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    // Tự động gán CartItem component và Collider để tương tác ngắm tâm tròn click được luôn
                    var cartItem = kettleObj.AddComponent<CartItem>();
                    var so = new SerializedObject(cartItem);
                    so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.WaterKettle;
                    so.FindProperty("itemName").stringValue = "Ấm Nước (Bình Thủy)";
                    so.FindProperty("itemDescription").stringValue = "Ấm nước nóng để pha trà.";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (kettleObj.GetComponent<Collider>() == null)
                    {
                        var col = kettleObj.AddComponent<BoxCollider>();
                        var meshFilter = kettleObj.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            col.center = meshFilter.sharedMesh.bounds.center;
                            col.size = meshFilter.sharedMesh.bounds.size;
                        }
                    }
                }

                // --- Add Tea Tin Prop ---
                string tinModelPath = "Assets/binhtra/Meshy_AI_Red_Vintage_Tea_Tin_0603084617_texture.fbx";
                GameObject tinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tinModelPath);
                if (tinPrefab != null)
                {
                    GameObject tinObj = (GameObject)PrefabUtility.InstantiatePrefab(tinPrefab);
                    tinObj.name = "TeaTinProp";
                    
                    float tinTargetHeight = 0.20f; // Thu nhỏ lại vừa vặn hơn (20cm)
                    // Đo kích thước trước khi set parent theo góc đứng thẳng (-90 độ X, 180 độ Y) để khớp hoàn hảo
                    float tinUnscaledHeight = CalculateModelHeight(tinObj, Quaternion.Euler(-90f, 180f, 0f), out float tinOffset);
                    float tinScaleFactor = tinTargetHeight / tinUnscaledHeight;
                    
                    tinObj.transform.SetParent(cartObj.transform);
                    // Quy đổi tỉ lệ cục bộ tương đối với cha
                    tinObj.transform.localScale = Vector3.one * (tinScaleFactor / scaleFactor);
                    
                    float tableWorldY = sidewalkTopY + 0.63f; // Hạ độ cao xuống mặt bàn chính
                    tinObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f); // Xoay đứng thẳng (-90 độ X) trước để khớp pivot đứng
                    tinObj.transform.position = new Vector3(4.15f, tableWorldY + (tinOffset * tinScaleFactor), 0.12f);

                    
                    Material tinMat = CreateTeaTinMaterial();
                    var tinRenderers = tinObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in tinRenderers) r.sharedMaterial = tinMat;

                    // Đảm bảo vật phẩm đứng yên cố định (kinematic)
                    var rbs = tinObj.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var rb in rbs)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    // Tự động gán CartItem component và Collider
                    var cartItem = tinObj.AddComponent<CartItem>();
                    var so = new SerializedObject(cartItem);
                    so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.TeaTin;
                    so.FindProperty("itemName").stringValue = "Bình Trà";
                    so.FindProperty("itemDescription").stringValue = "Bình trà đỏ chứa lá trà.";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (tinObj.GetComponent<Collider>() == null)
                    {
                        var col = tinObj.AddComponent<BoxCollider>();
                        var meshFilter = tinObj.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            col.center = meshFilter.sharedMesh.bounds.center;
                            col.size = meshFilter.sharedMesh.bounds.size;
                        }
                    }
                }

                // --- Add Sugar Jar Prop ---
                string sugarModelPath = "Assets/huduong/Meshy_AI_Cracked_Vintage_Jar_w_0603195416_texture.fbx";
                GameObject sugarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sugarModelPath);
                if (sugarPrefab != null)
                {
                    GameObject sugarObj = (GameObject)PrefabUtility.InstantiatePrefab(sugarPrefab);
                    sugarObj.name = "SugarJarProp";
                    
                    float sugarTargetHeight = 0.18f; // 18cm
                    float sugarUnscaledHeight = CalculateModelHeight(sugarObj, Quaternion.Euler(-90f, 180f, 0f), out float sugarOffset);
                    float sugarScaleFactor = sugarTargetHeight / sugarUnscaledHeight;
                    
                    sugarObj.transform.SetParent(cartObj.transform);
                    sugarObj.transform.localScale = Vector3.one * (sugarScaleFactor / scaleFactor);
                    
                    float tableWorldY = sidewalkTopY + 0.63f;
                    sugarObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
                    sugarObj.transform.position = new Vector3(3.8f, tableWorldY + (sugarOffset * sugarScaleFactor), 0.12f);
                    
                    Material sugarMat = CreateSugarJarMaterial();
                    var sugarRenderers = sugarObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in sugarRenderers) r.sharedMaterial = sugarMat;

                    var rbs = sugarObj.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var rb in rbs)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    var cartItem = sugarObj.AddComponent<CartItem>();
                    var so = new SerializedObject(cartItem);
                    so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.SugarJar;
                    so.FindProperty("itemName").stringValue = "Hũ Đường";
                    so.FindProperty("itemDescription").stringValue = "Hũ đựng đường cát để pha chế.";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (sugarObj.GetComponent<Collider>() == null)
                    {
                        var col = sugarObj.AddComponent<BoxCollider>();
                        var meshFilter = sugarObj.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            col.center = meshFilter.sharedMesh.bounds.center;
                            col.size = meshFilter.sharedMesh.bounds.size;
                        }
                    }
                }

                // --- Add Gas Stove Prop ---
                string stoveModelPath = "Assets/bepga/Meshy_AI_Namilux_High_Power_Po_0603212351_texture.fbx";
                GameObject stovePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stoveModelPath);
                if (stovePrefab != null)
                {
                    GameObject stoveObj = (GameObject)PrefabUtility.InstantiatePrefab(stovePrefab);
                    stoveObj.name = "GasStoveProp";
                    
                    float stoveTargetHeight = 0.08f; // 8cm dẹt dẹp
                    float stoveUnscaledHeight = CalculateModelHeight(stoveObj, Quaternion.Euler(-90f, 180f, 0f), out float stoveOffset);
                    float stoveScaleFactor = stoveTargetHeight / stoveUnscaledHeight;
                    
                    stoveObj.transform.SetParent(cartObj.transform);
                    stoveObj.transform.localScale = Vector3.one * (stoveScaleFactor / scaleFactor);
                    
                    float tableWorldY = sidewalkTopY + 0.63f;
                    stoveObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
                    stoveObj.transform.position = new Vector3(3.95f, tableWorldY + (stoveOffset * stoveScaleFactor), -0.12f);
                    
                    Material stoveMat = CreateGasStoveMaterial();
                    var stoveRenderers = stoveObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in stoveRenderers) r.sharedMaterial = stoveMat;

                    var rbs = stoveObj.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var rb in rbs)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    var cartItem = stoveObj.AddComponent<CartItem>();
                    var so = new SerializedObject(cartItem);
                    so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.GasStove;
                    so.FindProperty("itemName").stringValue = "Bếp Ga";
                    so.FindProperty("itemDescription").stringValue = "Bếp ga mini Namilux.";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (stoveObj.GetComponent<Collider>() == null)
                    {
                        var col = stoveObj.AddComponent<BoxCollider>();
                        var meshFilter = stoveObj.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            col.center = meshFilter.sharedMesh.bounds.center;
                            col.size = meshFilter.sharedMesh.bounds.size;
                        }
                    }
                }

                // --- Add Coffee Jar Prop ---
                string coffeeModelPath = "Assets/caphe/Meshy_AI_Cà_Phê_Phổ_Cổ_V_0603204206_texture.fbx";
                GameObject coffeePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(coffeeModelPath);
                if (coffeePrefab != null)
                {
                    GameObject coffeeObj = (GameObject)PrefabUtility.InstantiatePrefab(coffeePrefab);
                    coffeeObj.name = "CoffeeProp";
                    
                    float coffeeTargetHeight = 0.18f; // 18cm
                    float coffeeUnscaledHeight = CalculateModelHeight(coffeeObj, Quaternion.Euler(-90f, 180f, 0f), out float coffeeOffset);
                    float coffeeScaleFactor = coffeeTargetHeight / coffeeUnscaledHeight;
                    
                    coffeeObj.transform.SetParent(cartObj.transform);
                    coffeeObj.transform.localScale = Vector3.one * (coffeeScaleFactor / scaleFactor);
                    
                    float tableWorldY = sidewalkTopY + 0.63f;
                    coffeeObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
                    coffeeObj.transform.position = new Vector3(4.35f, tableWorldY + (coffeeOffset * coffeeScaleFactor), -0.12f);
                    
                    Material coffeeMat = CreateCoffeeMaterial();
                    var coffeeRenderers = coffeeObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in coffeeRenderers) r.sharedMaterial = coffeeMat;

                    var rbs = coffeeObj.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var rb in rbs)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    var cartItem = coffeeObj.AddComponent<CartItem>();
                    var so = new SerializedObject(cartItem);
                    so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.Coffee;
                    so.FindProperty("itemName").stringValue = "Bình Cà Phê";
                    so.FindProperty("itemDescription").stringValue = "Bột cà phê nguyên chất.";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (coffeeObj.GetComponent<Collider>() == null)
                    {
                        var col = coffeeObj.AddComponent<BoxCollider>();
                        var meshFilter = coffeeObj.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            col.center = meshFilter.sharedMesh.bounds.center;
                            col.size = meshFilter.sharedMesh.bounds.size;
                        }
                    }
                }

                // --- Add Ice Cooler Prop ---
                string iceModelPath = "Assets/binhdungda/Meshy_AI_Open_Red_Cooler_with__0603212035_texture.fbx";
                GameObject icePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(iceModelPath);
                if (icePrefab != null)
                {
                    GameObject iceObj = (GameObject)PrefabUtility.InstantiatePrefab(icePrefab);
                    iceObj.name = "IceCoolerProp";
                    
                    float iceTargetHeight = 0.45f; // 45cm tall
                    float iceUnscaledHeight = CalculateModelHeight(iceObj, Quaternion.Euler(-90f, 180f, 0f), out float iceOffset);
                    float iceScaleFactor = iceTargetHeight / iceUnscaledHeight;
                    
                    iceObj.transform.SetParent(envParent.transform);
                    iceObj.transform.localScale = Vector3.one * iceScaleFactor;
                    
                    iceObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
                    iceObj.transform.position = new Vector3(3.0f, sidewalkTopY + (iceOffset * iceScaleFactor) + 0.002f, -0.6f);
                    
                    Material iceMat = CreateIceCoolerMaterial();
                    var iceRenderers = iceObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in iceRenderers) r.sharedMaterial = iceMat;

                    var rbs = iceObj.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var rb in rbs)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    var cartItem = iceObj.AddComponent<CartItem>();
                    var so = new SerializedObject(cartItem);
                    so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.IceCooler;
                    so.FindProperty("itemName").stringValue = "Bình Đựng Đá";
                    so.FindProperty("itemDescription").stringValue = "Thùng chứa đá lạnh.";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (iceObj.GetComponent<Collider>() == null)
                    {
                        var col = iceObj.AddComponent<BoxCollider>();
                        var meshFilter = iceObj.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            col.center = meshFilter.sharedMesh.bounds.center;
                            col.size = meshFilter.sharedMesh.bounds.size;
                        }
                    }
                }

                // --- Add Water Bottle Prop ---
                string waterModelPath = "Assets/binhnuoc/Meshy_AI_Sài_Gòn_Aquwa_Bottl_0603204228_texture.fbx";
                GameObject waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterModelPath);
                if (waterPrefab != null)
                {
                    GameObject waterObj = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);
                    waterObj.name = "WaterBottleProp";
                    
                    float waterTargetHeight = 0.40f; // 40cm tall
                    float waterUnscaledHeight = CalculateModelHeight(waterObj, Quaternion.Euler(-90f, 180f, 0f), out float waterOffset);
                    float waterScaleFactor = waterTargetHeight / waterUnscaledHeight;
                    
                    waterObj.transform.SetParent(envParent.transform);
                    waterObj.transform.localScale = Vector3.one * waterScaleFactor;
                    
                    waterObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
                    waterObj.transform.position = new Vector3(2.6f, sidewalkTopY + (waterOffset * waterScaleFactor) + 0.002f, -0.6f);
                    
                    Material waterMat = CreateWaterBottleMaterial();
                    var waterRenderers = waterObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in waterRenderers) r.sharedMaterial = waterMat;

                    var rbs = waterObj.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var rb in rbs)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    var cartItem = waterObj.AddComponent<CartItem>();
                    var so = new SerializedObject(cartItem);
                    so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.WaterBottle;
                    so.FindProperty("itemName").stringValue = "Bình Nước";
                    so.FindProperty("itemDescription").stringValue = "Bình nước lọc pha chế.";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (waterObj.GetComponent<Collider>() == null)
                    {
                        var col = waterObj.AddComponent<BoxCollider>();
                        var meshFilter = waterObj.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            col.center = meshFilter.sharedMesh.bounds.center;
                            col.size = meshFilter.sharedMesh.bounds.size;
                        }
                    }
                }

                // --- Add Water Cups ---
                string cupModelPath = "Assets/lynuoc/Meshy_AI_Steaming_Glass_Beer_M_0603223059_texture.fbx";
                GameObject cupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cupModelPath);
                if (cupPrefab != null)
                {
                    Vector3[] cupPositions = new Vector3[]
                    {
                        new Vector3(3.48f, 0f, 0.04f),
                        new Vector3(3.56f, 0f, 0.04f),
                        new Vector3(3.48f, 0f, 0.12f),
                        new Vector3(3.56f, 0f, 0.12f),
                        new Vector3(3.52f, 0f, 0.20f)
                    };

                    Material cupMat = CreateWaterCupMaterial();

                    for (int i = 0; i < cupPositions.Length; i++)
                    {
                        GameObject cupObj = (GameObject)PrefabUtility.InstantiatePrefab(cupPrefab);
                        cupObj.name = $"WaterCupProp_{i + 1}";

                        float cupTargetHeight = 0.10f; // 10cm tall (typical glass)
                        float cupUnscaledHeight = CalculateModelHeight(cupObj, Quaternion.Euler(-90f, 180f, 0f), out float cupOffset);
                        float cupScaleFactor = cupTargetHeight / cupUnscaledHeight;

                        cupObj.transform.SetParent(cartObj.transform);
                        cupObj.transform.localScale = Vector3.one * (cupScaleFactor / scaleFactor);

                        float tableWorldY = sidewalkTopY + 0.63f;
                        cupObj.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
                        cupObj.transform.position = new Vector3(cupPositions[i].x, tableWorldY + (cupOffset * cupScaleFactor), cupPositions[i].z);

                        var cupRenderers = cupObj.GetComponentsInChildren<MeshRenderer>(true);
                        foreach (var r in cupRenderers) r.sharedMaterial = cupMat;

                        var rbs = cupObj.GetComponentsInChildren<Rigidbody>(true);
                        foreach (var rb in rbs)
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }

                        var cartItem = cupObj.AddComponent<CartItem>();
                        var so = new SerializedObject(cartItem);
                        so.FindProperty("itemType").enumValueIndex = (int)CartItem.CartItemType.WaterCup;
                        so.FindProperty("itemName").stringValue = "Ly Nước";
                        so.FindProperty("itemDescription").stringValue = "Ly nước dùng để pha chế trà đá.";
                        so.ApplyModifiedPropertiesWithoutUndo();

                        if (cupObj.GetComponent<Collider>() == null)
                        {
                            var col = cupObj.AddComponent<BoxCollider>();
                            var meshFilter = cupObj.GetComponentInChildren<MeshFilter>();
                            if (meshFilter != null && meshFilter.sharedMesh != null)
                            {
                                col.center = meshFilter.sharedMesh.bounds.center;
                                col.size = meshFilter.sharedMesh.bounds.size;
                            }
                        }
                    }
                }

                // --- Add First Person Camera Point ---
                GameObject camPointObj = new GameObject("FirstPersonCameraPoint");
                camPointObj.transform.SetParent(cartObj.transform);
                camPointObj.transform.position = new Vector3(4.0f, sidewalkTopY + 1.55f, 0.9f);
                camPointObj.transform.rotation = Quaternion.Euler(30f, 180f, 0f);
                
                var camField = typeof(TeaCart).GetField("cameraViewPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (camField != null) camField.SetValue(teaCartComp, camPointObj.transform);
            }
            else
            {
                // Fallback nếu không có model
                cartObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cartObj.name = "TeaCart";
                cartObj.transform.position = new Vector3(4f, 1f, 0f);
                cartObj.transform.localScale = new Vector3(2f, 2f, 1f);
                cartObj.GetComponent<MeshRenderer>().sharedMaterial = matCart;
                var cartCollider = cartObj.GetComponent<BoxCollider>();
                if (cartCollider != null) cartCollider.isTrigger = true;
                cartObj.AddComponent<TeaCart>();
            }
            
            // Đèn dầu trên xe
            GameObject lanternObj = new GameObject("LanternLight");
            lanternObj.transform.SetParent(cartObj.transform);
            
            // Đặt vị trí world để tránh bị ảnh hưởng bởi scale cực lớn của cha
            float lanternWorldY = cartObj.transform.position.y + (1.6f * 0.75f); // Đặt trên mặt bàn xe đẩy
            lanternObj.transform.position = new Vector3(4f, lanternWorldY, 0f);
            Light lanternLight = lanternObj.AddComponent<Light>();
            lanternLight.type = LightType.Point;
            lanternLight.color = new Color(1f, 0.7f, 0.3f);
            lanternLight.intensity = 1.5f;
            lanternLight.range = 5f;

            // Spawn 4 cái bàn trà đá và 4 chiếc ghế cho mỗi bàn (tổng cộng 16 ghế)
            GameObject tablesGroup = new GameObject("Tables_Group");
            tablesGroup.transform.SetParent(envParent.transform);
            Material tableMat = CreateTableModelMaterial();

            GameObject chairsGroup = new GameObject("Chairs_Group");
            chairsGroup.transform.SetParent(envParent.transform);
            Material chairMat = CreateChairModelMaterial();

            // Dãy 1: Trên vỉa hè (Z = 0) bên cạnh xe đẩy (đã dịch chuyển sang X=4)
            Vector3 tablePos1 = new Vector3(-1.25f, sidewalkTopY, 0f);
            SpawnTable(tablePos1, tablesGroup, tableMat);
            SpawnChairsAroundTable(tablePos1, chairsGroup, chairMat);

            Vector3 tablePos2 = new Vector3(1.75f, sidewalkTopY, 0f);
            SpawnTable(tablePos2, tablesGroup, tableMat);
            SpawnChairsAroundTable(tablePos2, chairsGroup, chairMat);

            // Dãy 2: Trên nền đá bên trái (Z = stoneLeftZ) bên cạnh xe đẩy
            Vector3 tablePos3 = new Vector3(-1.25f, stoneTopY, stoneLeftZ);
            SpawnTable(tablePos3, tablesGroup, tableMat);
            SpawnChairsAroundTable(tablePos3, chairsGroup, chairMat);

            Vector3 tablePos4 = new Vector3(1.75f, stoneTopY, stoneLeftZ);
            SpawnTable(tablePos4, tablesGroup, tableMat);
            SpawnChairsAroundTable(tablePos4, chairsGroup, chairMat);

            // Đã xóa ghế đỏ cạnh xe đẩy theo yêu cầu

            GameObject playerObj = new GameObject("Player_HoangHon");
            playerObj.tag = "Player";
            playerObj.layer = LayerMask.NameToLayer("Default");
            playerObj.transform.position = new Vector3(0f, 1.5f, 0f); // Đặt trên vỉa hè
            
            // Trả lại đường dẫn model cũ của User
            string modelPath = "Assets/_Project/Art/Models/Player/Meshy_AI_Ripped_Jeans_Portrait_biped/Meshy_AI_Ripped_Jeans_Portrait_biped_Animation_Walking_withSkin.glb";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            
            if (modelPrefab != null)
            {
                GameObject visualObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                visualObj.transform.SetParent(playerObj.transform);
                // Vì mô hình GLB thường có gốc tọa độ ở tâm (hoặc khác chuẩn), ta đặt localPosition = 0
                // để tránh tình trạng nhân vật bị lún xuống đất một nửa.
                visualObj.transform.localPosition = Vector3.zero; 
                visualObj.transform.localRotation = Quaternion.identity; // Xoay hướng trước (3D di chuyển tự xoay)
                
                SetupPlayerAnimator(visualObj);
            }
            else
            {
                // Fallback nếu không có model
                GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visualObj.transform.SetParent(playerObj.transform);
                visualObj.transform.localPosition = new Vector3(0, 1f, 0);
                visualObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                Object.DestroyImmediate(visualObj.GetComponent<CapsuleCollider>());
                visualObj.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial("Mat_PlayerFallback", "Standard", Color.blue);
            }
            
            var col3d = playerObj.AddComponent<CapsuleCollider>();
            col3d.center = new Vector3(0, 0.9f, 0);
            col3d.height = 1.8f;
            col3d.radius = 0.4f;
            
            var rb3d = playerObj.AddComponent<Rigidbody>();
            rb3d.freezeRotation = true;
            rb3d.useGravity = true;
            
            playerObj.AddComponent<PlayerController>();
            playerObj.AddComponent<PlayerAnimator>();
            playerObj.AddComponent<PlayerStats>();
            cinCam.SetTarget(playerObj.transform); // Set camera target

            // ==========================================
            // 5. HIỆU ỨNG THỜI TIẾT (PARTICLES)
            // ==========================================
            GameObject weatherFxObj = new GameObject("WeatherFX");
            
            // Mưa
            GameObject rainObj = new GameObject("RainParticles");
            rainObj.transform.SetParent(weatherFxObj.transform);
            rainObj.transform.position = new Vector3(0, 8, 0); // Đặt trên cao
            var rainPs = rainObj.AddComponent<ParticleSystem>();
            var rainMain = rainPs.main;
            rainMain.loop = true;
            rainMain.startLifetime = 1.5f;
            rainMain.startSpeed = 15f;
            rainMain.startSize = 0.05f;
            rainMain.maxParticles = 2000;
            rainMain.simulationSpace = ParticleSystemSimulationSpace.World;
            rainMain.startColor = new Color(0.8f, 0.85f, 1f, 0.4f);
            rainMain.gravityModifier = 0.5f;
            
            var rainEmission = rainPs.emission;
            rainEmission.rateOverTime = 0; // Quản lý bởi WeatherManager
            
            var rainShape = rainPs.shape;
            rainShape.shapeType = ParticleSystemShapeType.Box;
            rainShape.scale = new Vector3(30, 0, 10);
            
            var rainRenderer = rainObj.GetComponent<ParticleSystemRenderer>();
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.lengthScale = 8f;
            rainRenderer.velocityScale = 0.1f;
            rainRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");

            // Sương mù
            GameObject fogObj = new GameObject("FogParticles");
            fogObj.transform.SetParent(weatherFxObj.transform);
            fogObj.transform.position = new Vector3(0, -0.5f, 5); // Đặt thấp, xa một chút
            var fogPs = fogObj.AddComponent<ParticleSystem>();
            var fogMain = fogPs.main;
            fogMain.loop = true;
            fogMain.startLifetime = 8f;
            fogMain.startSpeed = 1f;
            fogMain.startSize = new ParticleSystem.MinMaxCurve(3f, 8f);
            fogMain.maxParticles = 200;
            fogMain.simulationSpace = ParticleSystemSimulationSpace.World;
            fogMain.startColor = new Color(1f, 1f, 1f, 0.1f);
            
            var fogEmission = fogPs.emission;
            fogEmission.rateOverTime = 0;
            
            var fogShape = fogPs.shape;
            fogShape.shapeType = ParticleSystemShapeType.Box;
            fogShape.scale = new Vector3(40, 1, 15);
            
            var fogRenderer = fogObj.GetComponent<ParticleSystemRenderer>();
            fogRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

            // ==========================================
            // 6. CÁC HỆ THỐNG QUẢN LÝ (MANAGERS)
            // ==========================================
            GameObject managersObj = new GameObject("Managers");
            managersObj.AddComponent<GameManager>();
            
            // Weather
            var weatherMgr = managersObj.AddComponent<WeatherManager>();
            var rainSys = managersObj.AddComponent<RainSystem>();
            var windSys = managersObj.AddComponent<WindSystem>();
            var fogCtrl = managersObj.AddComponent<FogController>();
            
            // Gán Particle cho Weather systems
            var serializedRain = new SerializedObject(rainSys);
            serializedRain.FindProperty("rainParticles").objectReferenceValue = rainPs;
            serializedRain.ApplyModifiedPropertiesWithoutUndo();
            
            windSys.Initialize(rainPs);
            
            var serializedFog = new SerializedObject(fogCtrl);
            serializedFog.FindProperty("fogParticles").objectReferenceValue = fogPs;
            serializedFog.ApplyModifiedPropertiesWithoutUndo();

            // Tạo Weather Presets
            List<WeatherPreset> presets = new List<WeatherPreset>();
            presets.Add(CreateWeatherPreset("Clear", WeatherType.Clear, new Color(0.8f, 0.9f, 1f), 0.8f, 0, 0, 0.001f, 1f));
            presets.Add(CreateWeatherPreset("LightRain", WeatherType.LightRain, new Color(0.6f, 0.7f, 0.8f), 0.5f, 0.4f, 0.3f, 0.01f, 0.7f));
            presets.Add(CreateWeatherPreset("HeavyRain", WeatherType.HeavyRain, new Color(0.4f, 0.4f, 0.5f), 0.3f, 1f, 0.7f, 0.03f, 0.3f));
            presets.Add(CreateWeatherPreset("SeaWind", WeatherType.SeaWind, new Color(0.7f, 0.8f, 0.9f), 0.7f, 0.1f, 0.8f, 0.005f, 0.8f));
            presets.Add(CreateWeatherPreset("Foggy", WeatherType.Foggy, new Color(0.9f, 0.9f, 0.95f), 0.4f, 0, 0.1f, 0.04f, 0.6f));
            
            var serializedWeatherMgr = new SerializedObject(weatherMgr);
            var presetsProp = serializedWeatherMgr.FindProperty("weatherPresets");
            presetsProp.ClearArray();
            for (int i = 0; i < presets.Count; i++)
            {
                presetsProp.InsertArrayElementAtIndex(i);
                presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = presets[i];
            }
            serializedWeatherMgr.ApplyModifiedPropertiesWithoutUndo();

            managersObj.AddComponent<EconomyManager>();
            managersObj.AddComponent<DayNightCycle>();
            managersObj.AddComponent<GanhHangRong.Systems.TimeOfDayLighting>();
            managersObj.AddComponent<Narrative.DialogueManager>();
            managersObj.AddComponent<AudioManager>();
            managersObj.AddComponent<GanhHangRong.Systems.EmotionalFailureSystem>();
            managersObj.AddComponent<GanhHangRong.Systems.GameplayLoop>();
            var npcVisualFactory = managersObj.AddComponent<NPCVisualFactory>();
            
            // Tìm và gán model Grab rider (biped có animation đi bộ)
            string grabModelPath = "Assets/ronaldo/Meshy_AI_Grab_Delivery_Rider_biped_Animation_Running_withSkin.fbx";
            GameObject grabModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(grabModelPath);

            // Tạo/Load Animator Controller cho Grab rider
            string controllerPath = "Assets/_Project/Animations/NPC/NPCGrabAnimController.controller";
            RuntimeAnimatorController grabController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (grabController == null)
            {
                if (!System.IO.Directory.Exists("Assets/_Project/Animations"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Animations");
                if (!System.IO.Directory.Exists("Assets/_Project/Animations/NPC"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Animations/NPC");

                AnimationClip clip = null;
                var assets = AssetDatabase.LoadAllAssetsAtPath(grabModelPath);
                foreach (var asset in assets)
                {
                    if (asset is AnimationClip && !asset.name.StartsWith("__preview__"))
                    {
                        clip = (AnimationClip)asset;
                        break;
                    }
                }

                if (clip != null)
                {
                    var animController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    var rootStateMachine = animController.layers[0].stateMachine;
                    var state = rootStateMachine.AddState("WalkOrRun");
                    state.motion = clip;
                    grabController = animController;
                    Debug.Log($"[SceneBuilder] Created Animator Controller at {controllerPath} with clip {clip.name}");
                }
                else
                {
                    Debug.LogError($"[SceneBuilder] Could not find AnimationClip in FBX at {grabModelPath}");
                }
            }
            
            var serializedFactory = new SerializedObject(npcVisualFactory);
            serializedFactory.FindProperty("npcModelPrefab").objectReferenceValue = grabModelPrefab;
            
            // Tạo vật liệu Grab rider nếu chưa có
            Material grabMat = CreateGrabRiderMaterial();
            serializedFactory.FindProperty("npcModelMaterial").objectReferenceValue = grabMat;
            serializedFactory.FindProperty("npcModelAnimatorController").objectReferenceValue = grabController;
            
            serializedFactory.ApplyModifiedPropertiesWithoutUndo();

            // Thiết lập field cho WeatherManager
            weatherMgr.GetType().GetField("globalLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(weatherMgr, dirLight);

            // ==========================================
            // 7. HỆ THỐNG NPC SPAWNER + PROFILES
            // ==========================================
            // Tạo 5 NPCProfile
            List<NPCProfile> profiles = new List<NPCProfile>();
            profiles.Add(CreateNPCProfile("Fisherman", NPCType.Fisherman, 20, 40, 5, 10, 0.3f));
            profiles.Add(CreateNPCProfile("Worker", NPCType.Worker, 15, 30, 3, 8, 0.2f));
            profiles.Add(CreateNPCProfile("BusDriver", NPCType.BusDriver, 10, 25, 4, 7, 0.1f));
            profiles.Add(CreateNPCProfile("IslandTraveler", NPCType.IslandTraveler, 25, 50, 8, 15, 0.5f));
            profiles.Add(CreateNPCProfile("LocalResident", NPCType.LocalResident, 30, 60, 5, 12, 0.4f));

            GameObject spawnerObj = new GameObject("NPC_Spawner");
            var spawner = spawnerObj.AddComponent<NPCSpawner>();
            
            GameObject spawnLeft = new GameObject("SpawnPoint_Left");
            spawnLeft.transform.position = new Vector3(-20f, 1f, 0f);
            spawnLeft.transform.SetParent(spawnerObj.transform);
            
            GameObject spawnRight = new GameObject("SpawnPoint_Right");
            spawnRight.transform.position = new Vector3(20f, 1f, 0f);
            spawnRight.transform.SetParent(spawnerObj.transform);
            
            var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            spawner.GetType().GetField("spawnPoints", bf).SetValue(spawner, new Transform[] { spawnLeft.transform, spawnRight.transform });
            spawner.GetType().GetField("exitPoints", bf).SetValue(spawner, new Transform[] { spawnLeft.transform, spawnRight.transform });
            spawner.GetType().GetField("availableProfiles", bf).SetValue(spawner, profiles);

            // ==========================================
            // 8. HỆ THỐNG UI (REDESIGNED)
            // ==========================================
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            GameObject hudObj = new GameObject("HUD");
            hudObj.transform.SetParent(canvasObj.transform, false);
            var hudRect = hudObj.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero; hudRect.offsetMax = Vector2.zero;
            hudObj.AddComponent<CanvasGroup>();
            var gameplayHUD = hudObj.AddComponent<GameplayHUD>(); 
            
            // Tải Sprites
            Sprite sprAvatarBoard = LoadSprite("Assets/_Project/Art/UI/Items/khung_avatar.png");
            Sprite sprPriceTag = LoadSprite("Assets/_Project/Art/UI/Items/the_gia.png");
            Sprite sprCompass = LoadSprite("Assets/_Project/Art/UI/Items/dong_ho.png");
            Sprite sprCounter = LoadSprite("Assets/_Project/Art/UI/Items/mat_ban_inox.png");
            // Dùng icon đã crop để tránh dính chữ/giá mẫu từ mockup
            Sprite sprTea = LoadSprite("Assets/_Project/Art/UI/Cropped/small_4.png");
            Sprite sprSugar = LoadSprite("Assets/_Project/Art/UI/Cropped/small_3.png");
            Sprite sprCup = LoadSprite("Assets/_Project/Art/UI/Cropped/small_2.png");
            Sprite sprIce = LoadSprite("Assets/_Project/Art/UI/Cropped/small_1.png");

            // 1. Top Left - Avatar & Stats
            GameObject topLeftPanel = new GameObject("TopLeftPanel");
            topLeftPanel.transform.SetParent(hudObj.transform, false);
            var tlRect = topLeftPanel.AddComponent<RectTransform>();
            tlRect.anchorMin = new Vector2(0, 1); tlRect.anchorMax = new Vector2(0, 1);
            tlRect.pivot = new Vector2(0, 1);
            tlRect.anchoredPosition = new Vector2(50, -50);
            tlRect.sizeDelta = new Vector2(600, 200);
            
            if (sprAvatarBoard != null) {
                var tlImg = topLeftPanel.AddComponent<UnityEngine.UI.Image>();
                tlImg.sprite = sprAvatarBoard;
                tlImg.preserveAspect = true;
            }

            var nameText = new GameObject("NameText").AddComponent<TMPro.TextMeshProUGUI>();
            nameText.transform.SetParent(topLeftPanel.transform, false);
            nameText.rectTransform.anchoredPosition = new Vector2(40, 20);
            nameText.rectTransform.sizeDelta = new Vector2(300, 50);
            nameText.text = "NGUYỄN HOÀNG HÔN";
            nameText.fontSize = 28; nameText.alignment = TMPro.TextAlignmentOptions.Center;
            nameText.fontStyle = TMPro.FontStyles.Bold;
            nameText.color = new Color(0.9f, 0.8f, 0.7f); // Màu gỗ/kem

            // Năng lượng
            GameObject energyBg = new GameObject("EnergyBg");
            energyBg.transform.SetParent(topLeftPanel.transform, false);
            var ebgRect = energyBg.AddComponent<RectTransform>();
            ebgRect.anchoredPosition = new Vector2(40, -30); ebgRect.sizeDelta = new Vector2(250, 30);
            energyBg.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject energyFill = new GameObject("EnergyFill");
            energyFill.transform.SetParent(energyBg.transform, false);
            var efillRect = energyFill.AddComponent<RectTransform>();
            efillRect.anchorMin = new Vector2(0, 0); efillRect.anchorMax = new Vector2(1, 1);
            efillRect.offsetMin = Vector2.zero; efillRect.offsetMax = Vector2.zero;
            var energySliderImg = energyFill.AddComponent<UnityEngine.UI.Image>();
            energySliderImg.color = new Color(1f, 0.6f, 0f); // Cam vàng

            var energySlider = energyBg.AddComponent<UnityEngine.UI.Slider>();
            energySlider.fillRect = efillRect; energySlider.minValue = 0; energySlider.maxValue = 1; energySlider.value = 1;
            energySlider.interactable = false;

            // Stress
            GameObject stressBg = new GameObject("StressBg");
            stressBg.transform.SetParent(topLeftPanel.transform, false);
            var sbgRect = stressBg.AddComponent<RectTransform>();
            sbgRect.anchoredPosition = new Vector2(40, -65); sbgRect.sizeDelta = new Vector2(250, 10);
            stressBg.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject stressFill = new GameObject("StressFill");
            stressFill.transform.SetParent(stressBg.transform, false);
            var sfillRect = stressFill.AddComponent<RectTransform>();
            sfillRect.anchorMin = new Vector2(0, 0); sfillRect.anchorMax = new Vector2(1, 1);
            sfillRect.offsetMin = Vector2.zero; sfillRect.offsetMax = Vector2.zero;
            var stressSliderImg = stressFill.AddComponent<UnityEngine.UI.Image>();
            stressSliderImg.color = new Color(0.8f, 0.1f, 0.1f); // Đỏ

            var stressSlider = stressBg.AddComponent<UnityEngine.UI.Slider>();
            stressSlider.fillRect = sfillRect; stressSlider.minValue = 0; stressSlider.maxValue = 1; stressSlider.value = 0;
            stressSlider.interactable = false;

            // Avatar viền tròn
            GameObject avatarBox = new GameObject("AvatarBox");
            avatarBox.transform.SetParent(topLeftPanel.transform, false);
            var avBoxRect = avatarBox.AddComponent<RectTransform>();
            avBoxRect.anchoredPosition = new Vector2(-150, 0); avBoxRect.sizeDelta = new Vector2(120, 120);
            avatarBox.AddComponent<UnityEngine.UI.Image>().color = new Color(0,0,0,0);

            // 2. Top Right - Tiền
            GameObject topRightPanel = new GameObject("MoneyPanel");
            topRightPanel.transform.SetParent(hudObj.transform, false);
            var trRect = topRightPanel.AddComponent<RectTransform>();
            trRect.anchorMin = new Vector2(1, 1); trRect.anchorMax = new Vector2(1, 1);
            trRect.pivot = new Vector2(1, 1);
            trRect.anchoredPosition = new Vector2(-250, -50);
            trRect.sizeDelta = new Vector2(400, 120);
            
            if (sprPriceTag != null) {
                var trImg = topRightPanel.AddComponent<UnityEngine.UI.Image>();
                trImg.sprite = sprPriceTag;
                trImg.preserveAspect = true;
            }

            var moneyText = new GameObject("MoneyText").AddComponent<TMPro.TextMeshProUGUI>();
            moneyText.transform.SetParent(topRightPanel.transform, false);
            moneyText.rectTransform.anchoredPosition = new Vector2(30, 0);
            moneyText.rectTransform.sizeDelta = new Vector2(280, 80);
            moneyText.text = "50,000 VNĐ";
            moneyText.fontSize = 42; moneyText.alignment = TMPro.TextAlignmentOptions.Center;
            moneyText.color = new Color(0.3f, 0.2f, 0.1f);
            moneyText.fontStyle = TMPro.FontStyles.Bold;

            // 3. Top Right - Đồng hồ La Bàn
            GameObject clockPanel = new GameObject("ClockPanel");
            clockPanel.transform.SetParent(hudObj.transform, false);
            var clRect = clockPanel.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(1, 1); clRect.anchorMax = new Vector2(1, 1);
            clRect.pivot = new Vector2(1, 1);
            clRect.anchoredPosition = new Vector2(-50, -50);
            clRect.sizeDelta = new Vector2(150, 150);
            
            if (sprCompass != null) {
                var clImg = clockPanel.AddComponent<UnityEngine.UI.Image>();
                clImg.sprite = sprCompass;
                clImg.preserveAspect = true;
            }

            var clockText = new GameObject("ClockText").AddComponent<TMPro.TextMeshProUGUI>();
            clockText.transform.SetParent(clockPanel.transform, false);
            clockText.rectTransform.anchoredPosition = new Vector2(0, 0);
            clockText.rectTransform.sizeDelta = new Vector2(150, 40);
            clockText.text = "17:00";
            clockText.fontSize = 28; clockText.alignment = TMPro.TextAlignmentOptions.Center;
            clockText.color = Color.white;
            clockText.fontStyle = TMPro.FontStyles.Bold;
            var clockOutline = clockText.gameObject.AddComponent<UnityEngine.UI.Outline>();
            clockOutline.effectColor = Color.black; clockOutline.effectDistance = new Vector2(2, -2);

            // Gán reference cho GameplayHUD qua Reflection
            gameplayHUD.GetType().GetField("playerNameText", bf).SetValue(gameplayHUD, nameText);
            gameplayHUD.GetType().GetField("energySlider", bf).SetValue(gameplayHUD, energySlider);
            gameplayHUD.GetType().GetField("stressSlider", bf).SetValue(gameplayHUD, stressSlider);
            gameplayHUD.GetType().GetField("moneyText", bf).SetValue(gameplayHUD, moneyText);
            gameplayHUD.GetType().GetField("clockText", bf).SetValue(gameplayHUD, clockText);
            gameplayHUD.GetType().GetField("teaCountText", bf).SetValue(gameplayHUD, null);
            gameplayHUD.GetType().GetField("sugarCountText", bf).SetValue(gameplayHUD, null);
            gameplayHUD.GetType().GetField("cupCountText", bf).SetValue(gameplayHUD, null);
            gameplayHUD.GetType().GetField("iceLevelText", bf).SetValue(gameplayHUD, null);

            GameObject promptObj = new GameObject("InteractionPrompt");
            promptObj.transform.SetParent(canvasObj.transform, false);
            var pRect = promptObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0, 150); // Nâng lên khỏi xe đẩy
            pRect.sizeDelta = new Vector2(160, 60);
            
            // Viền đỏ cho nút E
            promptObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.7f);
            var pOutline = promptObj.AddComponent<UnityEngine.UI.Outline>();
            pOutline.effectColor = Color.red; pOutline.effectDistance = new Vector2(3, 3);

            promptObj.AddComponent<CanvasGroup>();
            var promptUI = promptObj.AddComponent<InteractionPromptUI>();

            GameObject pTextObj = new GameObject("Text");
            pTextObj.transform.SetParent(promptObj.transform, false);
            var promptText = pTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            promptText.rectTransform.anchorMin = Vector2.zero; promptText.rectTransform.anchorMax = Vector2.one;
            promptText.rectTransform.sizeDelta = Vector2.zero;
            promptText.text = "Nhấn F";
            promptText.fontSize = 28; promptText.color = Color.white;
            promptText.alignment = TMPro.TextAlignmentOptions.Center;
            promptUI.GetType().GetField("promptText", bf).SetValue(promptUI, promptText);
            promptUI.GetType().GetField("canvasGroup", bf).SetValue(promptUI, promptObj.GetComponent<CanvasGroup>());

            // Day Summary UI (ẩn ban đầu)
            GameObject summaryObj = new GameObject("DaySummary");
            summaryObj.transform.SetParent(canvasObj.transform, false);
            var sumRect = summaryObj.AddComponent<RectTransform>();
            sumRect.anchorMin = Vector2.zero; sumRect.anchorMax = Vector2.one;
            sumRect.offsetMin = Vector2.zero; sumRect.offsetMax = Vector2.zero;
            summaryObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.85f);
            var sumCG = summaryObj.AddComponent<CanvasGroup>();
            sumCG.alpha = 0; sumCG.interactable = false; sumCG.blocksRaycasts = false;
            var summaryUI = summaryObj.AddComponent<DaySummaryUI>();

            var sumTitle = new GameObject("Title").AddComponent<TMPro.TextMeshProUGUI>();
            sumTitle.transform.SetParent(summaryObj.transform, false);
            sumTitle.rectTransform.anchoredPosition = new Vector2(0, 100);
            sumTitle.rectTransform.sizeDelta = new Vector2(400, 60);
            sumTitle.text = "Tổng Kết Ngày"; sumTitle.fontSize = 42;
            sumTitle.alignment = TMPro.TextAlignmentOptions.Center;

            var sumCustomers = new GameObject("Customers").AddComponent<TMPro.TextMeshProUGUI>();
            sumCustomers.transform.SetParent(summaryObj.transform, false);
            sumCustomers.rectTransform.anchoredPosition = new Vector2(0, 30);
            sumCustomers.rectTransform.sizeDelta = new Vector2(400, 40);
            sumCustomers.fontSize = 28; sumCustomers.alignment = TMPro.TextAlignmentOptions.Center;

            var sumMoney = new GameObject("Money").AddComponent<TMPro.TextMeshProUGUI>();
            sumMoney.transform.SetParent(summaryObj.transform, false);
            sumMoney.rectTransform.anchoredPosition = new Vector2(0, -20);
            sumMoney.rectTransform.sizeDelta = new Vector2(400, 40);
            sumMoney.fontSize = 28; sumMoney.alignment = TMPro.TextAlignmentOptions.Center;

            var sumStress = new GameObject("Stress").AddComponent<TMPro.TextMeshProUGUI>();
            sumStress.transform.SetParent(summaryObj.transform, false);
            sumStress.rectTransform.anchoredPosition = new Vector2(0, -70);
            sumStress.rectTransform.sizeDelta = new Vector2(400, 40);
            sumStress.fontSize = 28; sumStress.alignment = TMPro.TextAlignmentOptions.Center;

            var sumBtn = new GameObject("ContinueBtn");
            sumBtn.transform.SetParent(summaryObj.transform, false);
            var btnRect = sumBtn.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -140); btnRect.sizeDelta = new Vector2(200, 50);
            sumBtn.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.6f, 0.3f);
            var btn = sumBtn.AddComponent<UnityEngine.UI.Button>();
            var btnText = new GameObject("Text").AddComponent<TMPro.TextMeshProUGUI>();
            btnText.transform.SetParent(sumBtn.transform, false);
            btnText.rectTransform.anchorMin = Vector2.zero; btnText.rectTransform.anchorMax = Vector2.one;
            btnText.rectTransform.sizeDelta = Vector2.zero;
            btnText.text = "Ngày Tiếp Theo"; btnText.fontSize = 22;
            btnText.alignment = TMPro.TextAlignmentOptions.Center; btnText.color = Color.white;

            summaryUI.GetType().GetField("canvasGroup", bf).SetValue(summaryUI, sumCG);
            summaryUI.GetType().GetField("titleText", bf).SetValue(summaryUI, sumTitle);
            summaryUI.GetType().GetField("customersServedText", bf).SetValue(summaryUI, sumCustomers);
            summaryUI.GetType().GetField("moneyEarnedText", bf).SetValue(summaryUI, sumMoney);
            summaryUI.GetType().GetField("stressLevelText", bf).SetValue(summaryUI, sumStress);
            summaryUI.GetType().GetField("continueButton", bf).SetValue(summaryUI, btn);


            string sceneDir = "Assets/_Project/Scenes/Chapter1";
            if (!AssetDatabase.IsValidFolder(sceneDir))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Chapter1");
            }
            string scenePath = $"{sceneDir}/Chapter1.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"[Gánh Hàng Rong] Đã tạo thành công {scenePath} - BẤM PLAY ĐỂ CHƠI!");
        }

        private static Material CreateMaterial(string name, string shaderName, Color color)
        {
            Shader shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            
            string path = $"Assets/_Project/Art/Materials/{name}.mat";
            if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateCartModelMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/TeaCartModelMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // Gán Albedo
            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ganhnuoc/Meshy_AI_Vietnam_Sugarcane_Jui_0602210551_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            // Gán Normal map và cấu hình TextureImporter
            string normalPath = "Assets/ganhnuoc/Meshy_AI_Vietnam_Sugarcane_Jui_0602210551_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material CreateTableModelMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/TeaTableModelMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);

                // Gán Albedo
                Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Ban_fbx/Meshy_AI_tạo_cho_tui_một_b_0531144352_texture.png");
                if (albedoTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                    else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
                }

                // Gán Normal map và cấu hình TextureImporter
                string normalPath = "Assets/Ban_fbx/Meshy_AI_tạo_cho_tui_một_b_0531144352_texture_normal.png";
                var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normalTex != null)
                {
                    if (mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", normalTex);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                }

                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");

                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static void SpawnTable(Vector3 position, GameObject parent, Material tableMat)
        {
            string modelPath = "Assets/Ban_fbx/Meshy_AI_tạo_cho_tui_một_b_0531144352_texture.fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null) return;

            GameObject tableObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            tableObj.name = "TeaTable";
            tableObj.transform.SetParent(parent.transform);

            // Bật active cho toàn bộ lưới con và renderers
            var renderers = tableObj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                mr.gameObject.SetActive(true);
                mr.enabled = true;
                mr.sharedMaterial = tableMat;
            }

            // Tính toán bounds
            Bounds combinedBounds = new Bounds();
            bool hasBounds = false;
            foreach (var mr in renderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = mf.sharedMesh.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(mf.sharedMesh.bounds);
                    }
                }
            }

            // Chiều cao bàn nước tiêu chuẩn (khoảng 0.65m)
            float targetHeight = 0.65f;
            float unscaledHeight = CalculateModelHeight(tableObj, Quaternion.Euler(-90f, 0f, 0f), out float bottomOffsetAtScale1);
            float scaleFactor = targetHeight / unscaledHeight;

            tableObj.transform.localScale = Vector3.one * scaleFactor;

            // Tính toán vị trí Y sao cho đáy bàn chạm đất
            float yPos = position.y + (bottomOffsetAtScale1 * scaleFactor) + 0.002f;
            tableObj.transform.position = new Vector3(position.x, yPos, position.z);
            tableObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // Đứng thẳng tương tự xe đẩy

            // Thêm BoxCollider vật lý để người chơi và NPC không đi xuyên qua
            var boxCol = tableObj.AddComponent<BoxCollider>();
            if (hasBounds)
            {
                boxCol.center = combinedBounds.center;
                boxCol.size = combinedBounds.size;
            }
            else
            {
                boxCol.center = Vector3.zero;
                boxCol.size = new Vector3(1f, 1f, 1f);
            }
        }

        private static Material CreateChairModelMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/TeaChairModelMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);

                // Gán Albedo
                Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ghe_fbx/Meshy_AI_Red_plastic_step_stoo_0531144332_texture.png");
                if (albedoTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                    else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
                }

                // Gán Normal map và cấu hình TextureImporter
                string normalPath = "Assets/ghe_fbx/Meshy_AI_Red_plastic_step_stoo_0531144332_texture_normal.png";
                var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normalTex != null)
                {
                    if (mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", normalTex);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                }

                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");

                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static GameObject SpawnChair(Vector3 position, float rotationY, GameObject parent, Material chairMat)
        {
            string modelPath = "Assets/ghe_fbx/Meshy_AI_Red_plastic_step_stoo_0531144332_texture.fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null) return null;

            GameObject chairObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            chairObj.name = "ChairSeat";
            chairObj.transform.SetParent(parent.transform);

            // Bật active cho toàn bộ MeshRenderer con
            var renderers = chairObj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                mr.gameObject.SetActive(true);
                mr.enabled = true;
                mr.sharedMaterial = chairMat;
            }

            // Tính toán bounds
            Bounds combinedBounds = new Bounds();
            bool hasBounds = false;
            foreach (var mr in renderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = mf.sharedMesh.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(mf.sharedMesh.bounds);
                    }
                }
            }

            // Chiều cao ghế nhựa đỏ tiêu chuẩn (khoảng 0.35m)
            float targetHeight = 0.35f;
            float unscaledHeight = CalculateModelHeight(chairObj, Quaternion.Euler(-90f, rotationY, 0f), out float bottomOffsetAtScale1);
            float scaleFactor = targetHeight / unscaledHeight;

            chairObj.transform.localScale = Vector3.one * scaleFactor;

            // Tính toán vị trí Y sao cho đáy ghế chạm đất
            float yPos = position.y + (bottomOffsetAtScale1 * scaleFactor) + 0.002f;
            chairObj.transform.position = new Vector3(position.x, yPos, position.z);
            chairObj.transform.rotation = Quaternion.Euler(-90f, rotationY, 0f); // Xoay dọc theo hướng chỉ định

            // Thêm Trigger BoxCollider với kích thước cố định (world-space) để đảm bảo detect được
            // Không dùng mesh bounds vì sau khi scale bounds.center bị lệch sang local space
            var boxCol = chairObj.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.center = new Vector3(0f, 0.2f / chairObj.transform.localScale.y, 0f); // ~0.2m trên mặt đất
            boxCol.size = new Vector3(0.6f / chairObj.transform.localScale.x,
                                     0.4f / chairObj.transform.localScale.y,
                                     0.6f / chairObj.transform.localScale.z);

            // Thêm script để khách hoặc người chơi ngồi
            var seat = chairObj.AddComponent<CustomerSeat>();
            var field = typeof(Interactable).GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(seat, "Ngồi nghỉ");

            return chairObj;
        }

        private static void SpawnChairsAroundTable(Vector3 tablePos, GameObject parent, Material chairMat)
        {
            float offset = 0.7f;
            // Trái (hướng Đông)
            SpawnChair(new Vector3(tablePos.x - offset, tablePos.y, tablePos.z), 90f, parent, chairMat);
            // Phải (hướng Tây)
            SpawnChair(new Vector3(tablePos.x + offset, tablePos.y, tablePos.z), -90f, parent, chairMat);
            // Trước (hướng Bắc)
            SpawnChair(new Vector3(tablePos.x, tablePos.y, tablePos.z - offset), 0f, parent, chairMat);
            // Sau (hướng Nam)
            SpawnChair(new Vector3(tablePos.x, tablePos.y, tablePos.z + offset), 180f, parent, chairMat);
        }

        private static Material CreateGrabRiderMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/GrabRiderModelMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);

                // Gán Albedo (dùng texture từ folder ronaldo)
                Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ronaldo/Meshy_AI_Grab_Delivery_Rider_biped_texture_0.png");
                if (albedoTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                    else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
                }

                // Gán Normal map và cấu hình TextureImporter
                string normalPath = "Assets/ronaldo/Meshy_AI_Grab_Delivery_Rider_biped_texture_0_normal.png";
                var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normalTex != null)
                {
                    if (mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", normalTex);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                }

                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");

                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static WeatherPreset CreateWeatherPreset(string name, WeatherType type, Color lightColor, float intensity, float rain, float wind, float fog, float customerMod)
        {
            WeatherPreset preset = ScriptableObject.CreateInstance<WeatherPreset>();
            preset.weatherType = type;
            preset.ambientLightColor = lightColor;
            preset.ambientLightIntensity = intensity;
            preset.rainIntensity = rain;
            preset.windStrength = wind;
            preset.fogDensity = fog;
            preset.customerSpawnModifier = customerMod;
            preset.iceMeltModifier = (rain > 0.5f) ? 0.5f : 1f;

            string path = $"Assets/_Project/ScriptableObjects/Weather/{name}.asset";
            if (!System.IO.Directory.Exists("Assets/_Project/ScriptableObjects/Weather"))
                System.IO.Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Weather");

            AssetDatabase.CreateAsset(preset, path);
            return preset;
        }

        private static NPCProfile CreateNPCProfile(string name, NPCType type, float minPat, float maxPat, float minDrink, float maxDrink, float tip)
        {
            NPCProfile profile = ScriptableObject.CreateInstance<NPCProfile>();
            profile.npcType = type;
            profile.npcName = name;
            profile.minPatience = minPat;
            profile.maxPatience = maxPat;
            profile.minDrinkTime = minDrink;
            profile.maxDrinkTime = maxDrink;
            profile.tipChance = tip;
            
            // Random color tint for variety
            profile.possibleColorTints = new Color[] { 
                Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f),
                Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f) 
            };

            string path = $"Assets/_Project/ScriptableObjects/NPC/{name}.asset";
            if (!System.IO.Directory.Exists("Assets/_Project/ScriptableObjects/NPC"))
                System.IO.Directory.CreateDirectory("Assets/_Project/ScriptableObjects/NPC");

            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static void SetupPlayerAnimator(GameObject visualObj)
        {
            var animator = visualObj.GetComponent<Animator>();
            if (animator == null) animator = visualObj.AddComponent<Animator>();

            string controllerPath = "Assets/_Project/Animations/Player/PlayerAnimController.controller";
            
            // Luôn tạo lại controller để cập nhật animation mới
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Animations/Player"))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/_Project/Animations/Player");
                AssetDatabase.Refresh();
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var rootStateMachine = controller.layers[0].stateMachine;
            var idleState = rootStateMachine.AddState("Idle");
            var walkState = rootStateMachine.AddState("Walk");
            var runState = rootStateMachine.AddState("Run");
            var pushState = rootStateMachine.AddState("Pushing");
            var serveState = rootStateMachine.AddState("Serving");

            string modelDir = "Assets/_Project/Art/Models/Player/Meshy_AI_Ripped_Jeans_Portrait_biped";

            // Walk - dùng Walking animation
            AnimationClip walkClip = FindAnimClip($"{modelDir}/Meshy_AI_Ripped_Jeans_Portrait_biped_Animation_Walking_withSkin.glb");
            if (walkClip != null) walkState.motion = walkClip;

            // Run - dùng Running animation
            AnimationClip runClip = FindAnimClip($"{modelDir}/Meshy_AI_Ripped_Jeans_Portrait_biped_Animation_Running_withSkin.glb");
            if (runClip == null) runClip = FindAnimClip($"{modelDir}/Meshy_AI_Ripped_Jeans_Portrait_biped_Animation_Run_03_withSkin.glb");
            if (runClip != null) runState.motion = runClip;

            // Push Cart - dùng Unsteady Walk
            AnimationClip pushClip = FindAnimClip($"{modelDir}/Meshy_AI_Ripped_Jeans_Portrait_biped_Animation_Unsteady_Walk_withSkin.glb");
            if (pushClip != null) pushState.motion = pushClip;

            // Idle - dùng Walking clip ở speed 0 nếu không có idle riêng (hoặc sẽ dùng procedural)
            // Serving - procedural

            // Tạo Transitions MƯỢT MÀ
            // hasExitTime = false -> chuyển state ngay lập tức
            // transitionDuration = 0.15 -> blend mượt trong 0.15 giây
            var anyToIdle = rootStateMachine.AddAnyStateTransition(idleState);
            anyToIdle.AddCondition(AnimatorConditionMode.Equals, 0, "State");
            anyToIdle.hasExitTime = false;
            anyToIdle.duration = 0.15f;
            anyToIdle.canTransitionToSelf = false;

            var anyToWalk = rootStateMachine.AddAnyStateTransition(walkState);
            anyToWalk.AddCondition(AnimatorConditionMode.Equals, 1, "State");
            anyToWalk.hasExitTime = false;
            anyToWalk.duration = 0.15f;
            anyToWalk.canTransitionToSelf = false;

            var anyToRun = rootStateMachine.AddAnyStateTransition(runState);
            anyToRun.AddCondition(AnimatorConditionMode.Equals, 2, "State");
            anyToRun.hasExitTime = false;
            anyToRun.duration = 0.15f;
            anyToRun.canTransitionToSelf = false;

            var anyToPush = rootStateMachine.AddAnyStateTransition(pushState);
            anyToPush.AddCondition(AnimatorConditionMode.Equals, 3, "State");
            anyToPush.hasExitTime = false;
            anyToPush.duration = 0.2f;
            anyToPush.canTransitionToSelf = false;

            var anyToServe = rootStateMachine.AddAnyStateTransition(serveState);
            anyToServe.AddCondition(AnimatorConditionMode.Equals, 4, "State");
            anyToServe.hasExitTime = false;
            anyToServe.duration = 0.1f;
            anyToServe.canTransitionToSelf = false;
            
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
        }

        private static void CreateRailing(GameObject parent, Material mat)
        {
            GameObject railingGroup = new GameObject("Railing_Group");
            railingGroup.transform.SetParent(parent.transform);

            float startX = -30f;
            float endX = 30f;
            float step = 3f;
            float zPos = 2.4f;

            // Cột lan can đứng
            for (float x = startX; x <= endX; x += step)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Post_{x}";
                post.transform.SetParent(railingGroup.transform);
                post.transform.position = new Vector3(x, 0.4f, zPos);
                post.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f); // Cao 1.2m
                post.GetComponent<MeshRenderer>().sharedMaterial = mat;
                Object.DestroyImmediate(post.GetComponent<CapsuleCollider>());
            }

            // Thanh lan can ngang trên
            GameObject topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "TopRail";
            topRail.transform.SetParent(railingGroup.transform);
            topRail.transform.position = new Vector3(0f, 0.95f, zPos);
            topRail.transform.localScale = new Vector3(60f, 0.04f, 0.04f);
            topRail.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(topRail.GetComponent<BoxCollider>());

            // Thanh lan can ngang dưới
            GameObject bottomRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomRail.name = "BottomRail";
            bottomRail.transform.SetParent(railingGroup.transform);
            bottomRail.transform.position = new Vector3(0f, 0.5f, zPos);
            bottomRail.transform.localScale = new Vector3(60f, 0.03f, 0.03f);
            bottomRail.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(bottomRail.GetComponent<BoxCollider>());
        }

        private static void CreateStreetLights(GameObject parent, Material poleMat, Material lightMat)
        {
            GameObject lightsGroup = new GameObject("StreetLights_Group");
            lightsGroup.transform.SetParent(parent.transform);

            float[] xPositions = { -22f, -8f, 6f, 20f };
            float zPos = 2.4f;

            foreach (float x in xPositions)
            {
                GameObject streetLight = new GameObject($"StreetLight_{x}");
                streetLight.transform.SetParent(lightsGroup.transform);

                // Cột đứng (Cylinder)
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(streetLight.transform);
                pole.transform.position = new Vector3(x, 2f, zPos);
                pole.transform.localScale = new Vector3(0.12f, 2f, 0.12f); // Cao 4m
                pole.GetComponent<MeshRenderer>().sharedMaterial = poleMat;
                Object.DestroyImmediate(pole.GetComponent<CapsuleCollider>());

                // Thanh ngang đỡ đèn (Cube)
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Arm";
                arm.transform.SetParent(streetLight.transform);
                arm.transform.position = new Vector3(x - 0.4f, 3.9f, zPos);
                arm.transform.localScale = new Vector3(0.8f, 0.08f, 0.08f);
                arm.GetComponent<MeshRenderer>().sharedMaterial = poleMat;
                Object.DestroyImmediate(arm.GetComponent<BoxCollider>());

                // Bóng đèn phát sáng (Sphere)
                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Bulb";
                bulb.transform.SetParent(streetLight.transform);
                bulb.transform.position = new Vector3(x - 0.8f, 3.8f, zPos);
                bulb.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                bulb.GetComponent<MeshRenderer>().sharedMaterial = lightMat;
                Object.DestroyImmediate(bulb.GetComponent<SphereCollider>());

                // Nguồn sáng thực tế (Point Light)
                GameObject lightSource = new GameObject("LightSource");
                lightSource.transform.SetParent(streetLight.transform);
                lightSource.transform.position = new Vector3(x - 0.8f, 3.6f, zPos);
                
                Light light = lightSource.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.85f, 0.6f); // Vàng ấm
                light.intensity = 2f;
                light.range = 8f;
                light.shadows = LightShadows.Soft;
            }
        }

        private static void CreateBoats(GameObject parent, Material woodMat, Material cabinMat)
        {
            GameObject boatsGroup = new GameObject("Boats_Group");
            boatsGroup.transform.SetParent(parent.transform);

            // Các vị trí ngẫu nhiên tương đối giống concept mockup
            Vector3[] positions = {
                new Vector3(-18f, -0.2f, 15f),
                new Vector3(-9f, -0.2f, 22f),
                new Vector3(2f, -0.2f, 18f),
                new Vector3(12f, -0.2f, 13f),
                new Vector3(22f, -0.2f, 20f)
            };

            Vector3[] scales = {
                new Vector3(3.2f, 0.8f, 1.4f),
                new Vector3(2.5f, 0.6f, 1.2f),
                new Vector3(2.8f, 0.7f, 1.3f),
                new Vector3(3.5f, 0.9f, 1.5f),
                new Vector3(2.6f, 0.6f, 1.2f)
            };

            float[] rotations = { -15f, 10f, 5f, -8f, 12f };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject boat = new GameObject($"FishingBoat_{i}");
                boat.transform.SetParent(boatsGroup.transform);
                boat.transform.position = positions[i];
                boat.transform.rotation = Quaternion.Euler(0f, rotations[i], 0f);

                // Thân thuyền (Cube)
                GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hull.name = "Hull";
                hull.transform.SetParent(boat.transform);
                hull.transform.localPosition = Vector3.zero;
                hull.transform.localScale = scales[i];
                hull.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                Object.DestroyImmediate(hull.GetComponent<BoxCollider>());

                // Cabin thuyền (Cube nhỏ hơn màu trắng/kem)
                GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cabin.name = "Cabin";
                cabin.transform.SetParent(boat.transform);
                cabin.transform.localPosition = new Vector3(-0.2f * scales[i].x, scales[i].y * 0.8f, 0f);
                cabin.transform.localScale = new Vector3(scales[i].x * 0.4f, scales[i].y * 1.2f, scales[i].z * 0.8f);
                cabin.GetComponent<MeshRenderer>().sharedMaterial = cabinMat;
                Object.DestroyImmediate(cabin.GetComponent<BoxCollider>());

                // Cột buồm (Cylinder)
                GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mast.name = "Mast";
                mast.transform.SetParent(boat.transform);
                mast.transform.localPosition = new Vector3(0.2f * scales[i].x, scales[i].y * 1.5f, 0f);
                mast.transform.localScale = new Vector3(0.06f, scales[i].y * 1.5f, 0.06f);
                mast.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                Object.DestroyImmediate(mast.GetComponent<CapsuleCollider>());
                
                // Đèn định vị của thuyền đêm
                GameObject boatLight = new GameObject("LightSource");
                boatLight.transform.SetParent(boat.transform);
                boatLight.transform.localPosition = new Vector3(-0.2f * scales[i].x, scales[i].y * 1.6f, 0f);
                Light bLight = boatLight.AddComponent<Light>();
                bLight.type = LightType.Point;
                bLight.color = new Color(1f, 0.9f, 0.5f);
                bLight.intensity = 0.5f;
                bLight.range = 3f;
            }
        }

        private static void CreateMountains(GameObject parent, Material mat)
        {
            GameObject mountainsGroup = new GameObject("Mountains_Group");
            mountainsGroup.transform.SetParent(parent.transform);

            // Dãy núi nhấp nhô xa mờ
            Vector3[] positions = {
                new Vector3(-35f, -2f, 40f),
                new Vector3(-10f, -4f, 48f),
                new Vector3(15f, -3f, 42f),
                new Vector3(40f, -5f, 45f)
            };

            Vector3[] scales = {
                new Vector3(30f, 15f, 15f),
                new Vector3(40f, 18f, 20f),
                new Vector3(35f, 14f, 18f),
                new Vector3(25f, 10f, 12f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                // Dùng Sphere dẹt/kéo giãn để làm núi nhấp nhô tự nhiên
                GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mountain.name = $"Mountain_{i}";
                mountain.transform.SetParent(mountainsGroup.transform);
                mountain.transform.position = positions[i];
                mountain.transform.localScale = scales[i];
                mountain.GetComponent<MeshRenderer>().sharedMaterial = mat;
                Object.DestroyImmediate(mountain.GetComponent<SphereCollider>());
            }
        }

        private static AnimationClip FindAnimClip(string glbPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(glbPath);
            if (assets == null) return null;
            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview"))
                    return c;
            }
            return null;
        }

        private static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void UpgradeMaterialsToURP()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                Debug.LogWarning("[Gánh Hàng Rong] Không tìm thấy URP Lit Shader. Bỏ qua nâng cấp vật liệu.");
                return;
            }

            // Tìm toàn bộ file vật liệu (.mat) trong thư mục Simple city plain
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Simple city plain" });
            int upgradedCount = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != null)
                {
                    string sName = mat.shader.name;
                    if (sName == "Standard" || sName.Contains("Standard") || sName.Contains("Built-in"))
                    {
                        Texture mainTex = mat.mainTexture;
                        Color color = mat.color;

                        // Đổi shader sang URP Lit
                        mat.shader = urpShader;

                        // Khôi phục map texture & màu sắc cho URP
                        if (mat.HasProperty("_BaseMap") && mainTex != null)
                            mat.SetTexture("_BaseMap", mainTex);
                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", color);

                        EditorUtility.SetDirty(mat);
                        upgradedCount++;
                    }
                }
            }

            if (upgradedCount > 0)
            {
                Debug.Log($"[Gánh Hàng Rong] Đã tự động nâng cấp {upgradedCount} vật liệu sang URP Lit để sửa lỗi màn hình hồng!");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static Material CreateKettleMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/KettleMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = new Color(0.8f, 0.8f, 0.8f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.9f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
                if (mat.HasProperty("_Roughness")) mat.SetFloat("_Roughness", 0.2f);
                
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static Material CreateTeaTinMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/TeaTinMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // Gán Albedo
            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/binhtra/Meshy_AI_Red_Vintage_Tea_Tin_0603084617_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            // Gán Normal map
            string normalPath = "Assets/binhtra/Meshy_AI_Red_Vintage_Tea_Tin_0603084617_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            // Gán Metallic
            string metallicPath = "Assets/binhtra/Meshy_AI_Red_Vintage_Tea_Tin_0603084617_texture_metallic.png";
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material CreateSugarJarMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/SugarJarMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/huduong/Meshy_AI_Cracked_Vintage_Jar_w_0603195416_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            string normalPath = "Assets/huduong/Meshy_AI_Cracked_Vintage_Jar_w_0603195416_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            string metallicPath = "Assets/huduong/Meshy_AI_Cracked_Vintage_Jar_w_0603195416_texture_metallic.png";
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material CreateGasStoveMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/GasStoveMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/bepga/Meshy_AI_Namilux_High_Power_Po_0603212351_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            string normalPath = "Assets/bepga/Meshy_AI_Namilux_High_Power_Po_0603212351_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            string metallicPath = "Assets/bepga/Meshy_AI_Namilux_High_Power_Po_0603212351_texture_metallic.png";
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material CreateCoffeeMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/CoffeeMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/caphe/Meshy_AI_Cà_Phê_Phổ_Cổ_V_0603204206_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            string normalPath = "Assets/caphe/Meshy_AI_Cà_Phê_Phổ_Cổ_V_0603204206_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            string metallicPath = "Assets/caphe/Meshy_AI_Cà_Phê_Phổ_Cổ_V_0603204206_texture_metallic.png";
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material CreateIceCoolerMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/IceCoolerMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/binhdungda/Meshy_AI_Open_Red_Cooler_with__0603212035_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            string normalPath = "Assets/binhdungda/Meshy_AI_Open_Red_Cooler_with__0603212035_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            string metallicPath = "Assets/binhdungda/Meshy_AI_Open_Red_Cooler_with__0603212035_texture_metallic.png";
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material CreateWaterBottleMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/WaterBottleMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/binhnuoc/Meshy_AI_Sài_Gòn_Aquwa_Bottl_0603204228_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            string normalPath = "Assets/binhnuoc/Meshy_AI_Sài_Gòn_Aquwa_Bottl_0603204228_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            string metallicPath = "Assets/binhnuoc/Meshy_AI_Sài_Gòn_Aquwa_Bottl_0603204228_texture_metallic.png";
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Material CreateWaterCupMaterial()
        {
            string matPath = "Assets/_Project/Art/Materials/WaterCupMat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (!System.IO.Directory.Exists("Assets/_Project/Art/Materials"))
                    System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/lynuoc/Meshy_AI_Steaming_Glass_Beer_M_0603223059_texture.png");
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            string normalPath = "Assets/lynuoc/Meshy_AI_Steaming_Glass_Beer_M_0603223059_texture_normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            string metallicPath = "Assets/lynuoc/Meshy_AI_Steaming_Glass_Beer_M_0603223059_texture_metallic.png";
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static float CalculateModelHeight(GameObject obj, Quaternion rotation, out float bottomOffsetAtScale1)
        {
            Bounds worldBounds = new Bounds();
            bool hasBounds = false;

            Matrix4x4 rootToWorld = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);

            var renderers = obj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    // Compute localToRoot matrix manually to bypass stale world transform issues in Editor
                    Matrix4x4 localToRoot = Matrix4x4.identity;
                    Transform t = mr.transform;
                    while (t != obj.transform && t != null)
                    {
                        localToRoot = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * localToRoot;
                        t = t.parent;
                    }

                    Matrix4x4 childToWorld = rootToWorld * localToRoot;

                    Bounds localBounds = mf.sharedMesh.bounds;
                    Vector3[] corners = new Vector3[8];
                    Vector3 min = localBounds.min;
                    Vector3 max = localBounds.max;

                    corners[0] = new Vector3(min.x, min.y, min.z);
                    corners[1] = new Vector3(min.x, min.y, max.z);
                    corners[2] = new Vector3(min.x, max.y, min.z);
                    corners[3] = new Vector3(min.x, max.y, max.z);
                    corners[4] = new Vector3(max.x, min.y, min.z);
                    corners[5] = new Vector3(max.x, min.y, max.z);
                    corners[6] = new Vector3(max.x, max.y, min.z);
                    corners[7] = new Vector3(max.x, max.y, max.z);

                    foreach (var corner in corners)
                    {
                        Vector3 worldCorner = childToWorld.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            worldBounds = new Bounds(worldCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            worldBounds.Encapsulate(worldCorner);
                        }
                    }
                }
            }

            var skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinnedRenderers)
            {
                if (smr.sharedMesh != null)
                {
                    // Compute localToRoot matrix manually
                    Matrix4x4 localToRoot = Matrix4x4.identity;
                    Transform t = smr.transform;
                    while (t != obj.transform && t != null)
                    {
                        localToRoot = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * localToRoot;
                        t = t.parent;
                    }

                    Matrix4x4 childToWorld = rootToWorld * localToRoot;

                    Bounds localBounds = smr.sharedMesh.bounds;
                    Vector3[] corners = new Vector3[8];
                    Vector3 min = localBounds.min;
                    Vector3 max = localBounds.max;

                    corners[0] = new Vector3(min.x, min.y, min.z);
                    corners[1] = new Vector3(min.x, min.y, max.z);
                    corners[2] = new Vector3(min.x, max.y, min.z);
                    corners[3] = new Vector3(min.x, max.y, max.z);
                    corners[4] = new Vector3(max.x, min.y, min.z);
                    corners[5] = new Vector3(max.x, min.y, max.z);
                    corners[6] = new Vector3(max.x, max.y, min.z);
                    corners[7] = new Vector3(max.x, max.y, max.z);

                    foreach (var corner in corners)
                    {
                        Vector3 worldCorner = childToWorld.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            worldBounds = new Bounds(worldCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            worldBounds.Encapsulate(worldCorner);
                        }
                    }
                }
            }

            if (hasBounds)
            {
                bottomOffsetAtScale1 = -worldBounds.min.y;
                return worldBounds.size.y;
            }

            bottomOffsetAtScale1 = 0f;
            return 1f; // Fallback
        }

        private static float CalculateModelMaxY(GameObject prefab, Quaternion rotation, Vector3 scale)
        {
            if (prefab == null) return 0f;

            GameObject temp = Object.Instantiate(prefab);
            temp.transform.position = Vector3.zero;
            temp.transform.rotation = rotation;
            temp.transform.localScale = scale;

            Bounds worldBounds = new Bounds();
            bool hasBounds = false;

            var renderers = temp.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds localBounds = mf.sharedMesh.bounds;
                    Vector3[] corners = new Vector3[8];
                    Vector3 min = localBounds.min;
                    Vector3 max = localBounds.max;

                    corners[0] = new Vector3(min.x, min.y, min.z);
                    corners[1] = new Vector3(min.x, min.y, max.z);
                    corners[2] = new Vector3(min.x, max.y, min.z);
                    corners[3] = new Vector3(min.x, max.y, max.z);
                    corners[4] = new Vector3(max.x, min.y, min.z);
                    corners[5] = new Vector3(max.x, min.y, max.z);
                    corners[6] = new Vector3(max.x, max.y, min.z);
                    corners[7] = new Vector3(max.x, max.y, max.z);

                    foreach (var corner in corners)
                    {
                        Vector3 worldCorner = mr.transform.TransformPoint(corner);
                        if (!hasBounds)
                        {
                            worldBounds = new Bounds(worldCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            worldBounds.Encapsulate(worldCorner);
                        }
                    }
                }
            }

            float maxY = hasBounds ? worldBounds.max.y : 0f;
            Object.DestroyImmediate(temp);
            return maxY;
        }
    }
}
