using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GanhHangRong.Environment
{
    public class EnvironmentSetupHelper : MonoBehaviour
    {
        [Header("Root moi truong")]
        [SerializeField] private Transform generatedRoot;
        [SerializeField] private string generatedRootName = "GHR_MienTay_Environment";

        [Header("Layout")]
        [SerializeField] private Vector3 origin = new Vector3(76f, 0f, -18f);
        [SerializeField] private float roadLength = 72f;
        [SerializeField] private float roadWidth = 8f;
        [SerializeField] private float sidewalkWidth = 2.5f;
        [SerializeField] private float canalWidth = 12f;

        [Header("He thong thoi gian")]
        [SerializeField] private TimeOfDayManager timeManager;
        [SerializeField] private DayNightCycleController dayNightController;
        [SerializeField] private LightAutoToggle streetLightToggle;

        private Material roadMaterial;
        private Material sidewalkMaterial;
        private Material stripeMaterial;
        private Material waterMaterial;
        private Material woodMaterial;
        private Material leafMaterial;
        private Material trunkMaterial;
        private Material warmLampMaterial;

        [ContextMenu("Build Mien Tay Environment Preview")]
        public void BuildEnvironmentPreview()
        {
            PrepareRoot();
            PrepareMaterials();
            BuildRoadAndSidewalk();
            BuildCanalAndDock();
            BuildBoats();
            BuildMarketProps();
            BuildTropicalPlants();
            BuildPolesAndLights();
            BuildDayNightSystems();
            CreateTimeHudIfMissing();
        }

        [ContextMenu("Collect Scene References")]
        public void CollectSceneReferences()
        {
            timeManager = FindAnyObjectByType<TimeOfDayManager>();
            dayNightController = FindAnyObjectByType<DayNightCycleController>();
            streetLightToggle = FindAnyObjectByType<LightAutoToggle>();
        }

        private void PrepareRoot()
        {
            if (generatedRoot == null)
            {
                GameObject existing = GameObject.Find(generatedRootName);
                generatedRoot = existing != null ? existing.transform : new GameObject(generatedRootName).transform;
            }

            for (int i = generatedRoot.childCount - 1; i >= 0; i--)
            {
                DestroyObject(generatedRoot.GetChild(i).gameObject);
            }
        }

        private void PrepareMaterials()
        {
            roadMaterial = CreateMaterial("Mat_Road_Asphalt", new Color(0.12f, 0.12f, 0.115f));
            sidewalkMaterial = CreateMaterial("Mat_Sidewalk_WornConcrete", new Color(0.44f, 0.42f, 0.36f));
            stripeMaterial = CreateMaterial("Mat_Road_WhitePaint", new Color(0.9f, 0.88f, 0.78f));
            waterMaterial = CreateMaterial("Mat_Canal_MurkyGreen", new Color(0.18f, 0.38f, 0.34f, 0.82f));
            woodMaterial = CreateMaterial("Mat_OldDockWood", new Color(0.42f, 0.25f, 0.13f));
            leafMaterial = CreateMaterial("Mat_TropicalLeaves", new Color(0.18f, 0.42f, 0.18f));
            trunkMaterial = CreateMaterial("Mat_PalmTrunk", new Color(0.42f, 0.29f, 0.16f));
            warmLampMaterial = CreateMaterial("Mat_WarmLampGlass", new Color(1f, 0.68f, 0.32f));
            warmLampMaterial.EnableKeyword("_EMISSION");
            warmLampMaterial.SetColor("_EmissionColor", new Color(1f, 0.48f, 0.16f));
        }

        private void BuildRoadAndSidewalk()
        {
            CreateCube("Road_Asphalt_Main", origin, new Vector3(roadWidth, 0.12f, roadLength), roadMaterial);
            CreateCube("Sidewalk_MarketSide", origin + new Vector3(-(roadWidth + sidewalkWidth) * 0.5f, 0.08f, 0f), new Vector3(sidewalkWidth, 0.18f, roadLength), sidewalkMaterial);
            CreateCube("Sidewalk_CanalSide", origin + new Vector3((roadWidth + sidewalkWidth) * 0.5f, 0.08f, 0f), new Vector3(sidewalkWidth, 0.18f, roadLength), sidewalkMaterial);
            CreateCube("Curb_MarketSide", origin + new Vector3(-roadWidth * 0.5f, 0.18f, 0f), new Vector3(0.22f, 0.28f, roadLength), sidewalkMaterial);
            CreateCube("Curb_CanalSide", origin + new Vector3(roadWidth * 0.5f, 0.18f, 0f), new Vector3(0.22f, 0.28f, roadLength), sidewalkMaterial);

            for (int i = -5; i <= 5; i++)
            {
                CreateCube($"LaneMark_{i:00}", origin + new Vector3(0f, 0.17f, i * 6f), new Vector3(0.22f, 0.02f, 3.2f), stripeMaterial);
            }

            for (int i = 0; i < 8; i++)
            {
                CreateCube($"ZebraCrossing_{i:00}", origin + new Vector3(-3.4f + i * 0.95f, 0.19f, -9f), new Vector3(0.45f, 0.025f, 5.8f), stripeMaterial);
            }

            for (int i = -3; i <= 3; i++)
            {
                CreateCube($"DrainCover_{i:00}", origin + new Vector3(3.2f, 0.22f, i * 9f), new Vector3(0.65f, 0.04f, 0.9f), CreateMaterial("Mat_Drain_DarkMetal", new Color(0.08f, 0.08f, 0.075f)));
            }
        }

        private void BuildCanalAndDock()
        {
            GameObject waterRoot = new GameObject("WaterArea");
            waterRoot.transform.SetParent(generatedRoot);

            Vector3 canalCenter = origin + new Vector3(roadWidth * 0.5f + sidewalkWidth + canalWidth * 0.5f + 1.5f, -0.18f, 0f);
            GameObject water = CreateCube("Canal_Water_Surface", canalCenter, new Vector3(canalWidth, 0.04f, roadLength), waterMaterial, waterRoot.transform);
            WaterAreaController waterArea = waterRoot.AddComponent<WaterAreaController>();
            AssignPrivateField(waterArea, "waterRenderer", water.GetComponent<Renderer>());

            CreateCube("Canal_Embankment_Market", canalCenter + new Vector3(-canalWidth * 0.5f - 0.35f, 0.05f, 0f), new Vector3(0.7f, 0.55f, roadLength), woodMaterial, waterRoot.transform);
            CreateCube("Canal_Embankment_Far", canalCenter + new Vector3(canalWidth * 0.5f + 0.35f, 0.05f, 0f), new Vector3(0.7f, 0.55f, roadLength), woodMaterial, waterRoot.transform);

            Vector3 dockBase = canalCenter + new Vector3(-canalWidth * 0.35f, 0.28f, -7f);
            CreateCube("WoodDock_Platform", dockBase, new Vector3(4.8f, 0.22f, 9f), woodMaterial, waterRoot.transform);
            for (int i = 0; i < 7; i++)
            {
                CreateCylinder($"Dock_Pile_{i:00}", dockBase + new Vector3(i % 2 == 0 ? -2.1f : 2.1f, -0.3f, -3.8f + i * 1.25f), new Vector3(0.22f, 1.2f, 0.22f), woodMaterial, waterRoot.transform);
            }
        }

        private void BuildBoats()
        {
            GameObject boatGroup = new GameObject("BoatGroup");
            boatGroup.transform.SetParent(generatedRoot);

            Vector3 canalStart = origin + new Vector3(roadWidth * 0.5f + sidewalkWidth + 5.5f, 0f, 0f);
            CreateBoat("GheChoHang_NearDock", canalStart + new Vector3(0f, 0.05f, -8f), 8f, new Color(0.58f, 0.22f, 0.13f), false, boatGroup.transform);
            CreateBoat("XuongNho_Floating", canalStart + new Vector3(3.4f, 0.03f, 8f), -15f, new Color(0.18f, 0.38f, 0.45f), false, boatGroup.transform);
            CreateBoat("ThuyenBuom_DiemNhan", canalStart + new Vector3(1.6f, 0.04f, 22f), -6f, new Color(0.68f, 0.3f, 0.12f), true, boatGroup.transform);
        }

        private void BuildMarketProps()
        {
            Vector3 marketSide = origin + new Vector3(-roadWidth * 0.5f - sidewalkWidth - 1.6f, 0.28f, -7f);
            for (int i = 0; i < 5; i++)
            {
                CreateCube($"Market_Table_{i:00}", marketSide + new Vector3(0f, 0f, i * 3.2f), new Vector3(2.2f, 0.22f, 1.1f), woodMaterial);
                CreateCylinder($"Market_Basket_{i:00}", marketSide + new Vector3(-0.65f, 0.35f, i * 3.2f), new Vector3(0.65f, 0.34f, 0.65f), CreateMaterial("Mat_Basket_Wicker", new Color(0.62f, 0.42f, 0.2f)));
            }

            CreateCube("Old_Local_Sign_RachGia", marketSide + new Vector3(-0.8f, 1.7f, -4.2f), new Vector3(3.8f, 1.1f, 0.12f), CreateMaterial("Mat_FadedBlueSign", new Color(0.15f, 0.34f, 0.55f)));
        }

        private void BuildTropicalPlants()
        {
            for (int i = 0; i < 7; i++)
            {
                float z = -30f + i * 10f;
                CreatePalmTree(origin + new Vector3(roadWidth * 0.5f + sidewalkWidth + canalWidth + 3.2f, 0f, z), 2.7f + (i % 3) * 0.35f);
            }

            for (int i = 0; i < 5; i++)
            {
                CreateBananaCluster(origin + new Vector3(-roadWidth * 0.5f - sidewalkWidth - 3.4f, 0f, -24f + i * 12f));
            }
        }

        private void BuildPolesAndLights()
        {
            GameObject lightRoot = new GameObject("StreetLights");
            lightRoot.transform.SetParent(generatedRoot);
            StreetLightGroup group = lightRoot.AddComponent<StreetLightGroup>();

#if UNITY_EDITOR
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/cotden/Meshy_AI_Lone_Streetlight_0701084055_texture.fbx");
            Material poleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/cotden/Meshy_AI_Lone_Streetlight_Mat.mat");
#else
            GameObject modelPrefab = null;
            Material poleMat = null;
#endif

            for (int i = 0; i < 7; i++)
            {
                Vector3 basePos = origin + new Vector3(roadWidth * 0.5f + 0.85f, 0f, -30f + i * 10f);
                if (modelPrefab != null && poleMat != null)
                {
                    GameObject pole = Instantiate(modelPrefab, lightRoot.transform);
                    pole.name = $"PowerPole_{i:00}";
                    pole.transform.position = basePos + new Vector3(0f, 1.711082f, 0f);
                    pole.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
                    pole.transform.localScale = new Vector3(180f, 180f, 180f);

                    var mr = pole.GetComponentInChildren<MeshRenderer>(true);
                    if (mr != null)
                    {
                        mr.sharedMaterial = poleMat;
                        group.RegisterEmissiveRenderer(mr);
                    }

                    GameObject lampHead = new GameObject($"LampHead_{i:00}");
                    lampHead.transform.SetParent(pole.transform);
                    lampHead.transform.position = basePos + new Vector3(-0.38f, 3.35f, 0f);

                    Light lamp = lampHead.AddComponent<Light>();
                    lamp.type = LightType.Point;
                    lamp.color = new Color(1f, 0.68f, 0.42f);
                    lamp.range = 8f;
                    lamp.intensity = 0f;
                    lamp.enabled = false;
                    lamp.shadows = LightShadows.Soft;
                    group.RegisterLight(lamp);
                }
                else
                {
                    CreateCylinder($"PowerPole_{i:00}", basePos + new Vector3(0f, 1.7f, 0f), new Vector3(0.18f, 3.4f, 0.18f), trunkMaterial, lightRoot.transform);
                    GameObject lampHead = CreateCube($"LampHead_{i:00}", basePos + new Vector3(-0.55f, 3.15f, 0f), new Vector3(0.45f, 0.18f, 0.45f), warmLampMaterial, lightRoot.transform);
                    Light lamp = lampHead.AddComponent<Light>();
                    lamp.type = LightType.Point;
                    lamp.color = new Color(1f, 0.68f, 0.42f);
                    lamp.range = 7f;
                    lamp.intensity = 0f;
                    lamp.enabled = false;
                    group.RegisterLight(lamp);
                    group.RegisterEmissiveRenderer(lampHead.GetComponent<Renderer>());
                }
            }

            DrawWire("PowerWire_A", lightRoot.transform, new Vector3(roadWidth * 0.5f + 0.85f, 3.25f, -30f), new Vector3(roadWidth * 0.5f + 0.85f, 3.25f, 30f));
            DrawWire("PowerWire_B", lightRoot.transform, new Vector3(roadWidth * 0.5f + 1.05f, 3.05f, -30f), new Vector3(roadWidth * 0.5f + 1.05f, 3.05f, 30f));

            streetLightToggle = GetOrCreateSystemObject<LightAutoToggle>("StreetLights_AutoToggle");
            streetLightToggle.RegisterGroup(group);
            streetLightToggle.RegisterGroup(BuildMarketLightGroup());
        }

        private void BuildDayNightSystems()
        {
            timeManager = GetOrCreateSystemObject<TimeOfDayManager>("TimeSystem");
            dayNightController = GetOrCreateSystemObject<DayNightCycleController>("DayNightSystem");

            Light sun = FindLightByName("Sun_Light");
            if (sun == null)
            {
                sun = FindLightByName("Directional Light");
            }

            if (sun == null)
            {
                GameObject sunObj = new GameObject("Sun_Light");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            Light moon = FindLightByName("Moon_Light");
            if (moon == null)
            {
                GameObject moonObj = new GameObject("Moon_Light");
                moon = moonObj.AddComponent<Light>();
                moon.type = LightType.Directional;
            }

            AssignPrivateField(dayNightController, "timeManager", timeManager);
            AssignPrivateField(dayNightController, "sunLight", sun);
            AssignPrivateField(dayNightController, "moonLight", moon);
            AssignPrivateField(streetLightToggle, "timeManager", timeManager);
        }

        private void CreateTimeHudIfMissing()
        {
            if (GameObject.Find("GHR_TimeHUD") != null)
            {
                return;
            }

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject hud = new GameObject("GHR_TimeHUD", typeof(RectTransform));
            hud.transform.SetParent(canvas.transform, false);
            RectTransform hudRect = hud.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(1f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(1f, 1f);
            hudRect.anchoredPosition = new Vector2(-24f, -24f);
            hudRect.sizeDelta = new Vector2(180f, 90f);

            TextMeshProUGUI time = CreateHudText("TimeText", hud.transform, new Vector2(0f, -4f), 26, FontStyles.Bold);
            TextMeshProUGUI day = CreateHudText("DayText", hud.transform, new Vector2(0f, -36f), 18, FontStyles.Normal);
            TextMeshProUGUI period = CreateHudText("PeriodText", hud.transform, new Vector2(0f, -62f), 18, FontStyles.Normal);

            TimeUIController uiController = hud.AddComponent<TimeUIController>();
            AssignPrivateField(uiController, "timeManager", timeManager);
            AssignPrivateField(uiController, "timeText", time);
            AssignPrivateField(uiController, "dayText", day);
            AssignPrivateField(uiController, "periodText", period);
        }

        private StreetLightGroup BuildMarketLightGroup()
        {
            GameObject marketLights = new GameObject("MarketLights");
            marketLights.transform.SetParent(generatedRoot);
            StreetLightGroup group = marketLights.AddComponent<StreetLightGroup>();

            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = origin + new Vector3(-roadWidth * 0.5f - sidewalkWidth - 1.4f, 2.2f, -16f + i * 8f);
                GameObject bulb = CreateCube($"MarketWarmBulb_{i:00}", pos, new Vector3(0.32f, 0.18f, 0.32f), warmLampMaterial, marketLights.transform);
                Light light = bulb.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.62f, 0.34f);
                light.range = 5.5f;
                light.intensity = 0f;
                light.enabled = false;
                group.RegisterLight(light);
                group.RegisterEmissiveRenderer(bulb.GetComponent<Renderer>());
            }

            return group;
        }

        private GameObject CreateBoat(string name, Vector3 position, float yaw, Color hullColor, bool withSail = false, Transform parent = null)
        {
            GameObject boat = new GameObject(name);
            boat.transform.SetParent(parent != null ? parent : generatedRoot);
            boat.transform.position = position;
            boat.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            boat.AddComponent<BoatFloatSimple>();

            Material hull = CreateMaterial($"Mat_{name}_Hull", hullColor);
            CreateCube("Hull", boat.transform.position, new Vector3(1.2f, 0.35f, 4.2f), hull, boat.transform);
            CreateCube("Cargo", boat.transform.position + boat.transform.forward * -0.35f + Vector3.up * 0.35f, new Vector3(0.9f, 0.45f, 1.2f), woodMaterial, boat.transform);

            if (withSail)
            {
                CreateCylinder("Mast", boat.transform.position + Vector3.up * 1.15f, new Vector3(0.08f, 2.2f, 0.08f), woodMaterial, boat.transform);
                CreateCube("SmallSail", boat.transform.position + boat.transform.right * 0.02f + Vector3.up * 1.35f, new Vector3(0.06f, 1.35f, 1.05f), stripeMaterial, boat.transform);
            }

            return boat;
        }

        private void CreatePalmTree(Vector3 position, float height)
        {
            GameObject tree = new GameObject("CoconutPalm");
            tree.transform.SetParent(generatedRoot);
            CreateCylinder("Trunk", position + Vector3.up * height * 0.5f, new Vector3(0.24f, height, 0.24f), trunkMaterial, tree.transform);
            for (int i = 0; i < 6; i++)
            {
                GameObject leaf = CreateCube($"PalmLeaf_{i:00}", position + Vector3.up * (height + 0.3f), new Vector3(0.22f, 0.08f, 2.1f), leafMaterial, tree.transform);
                leaf.transform.rotation = Quaternion.Euler(12f, i * 60f, 0f);
            }
        }

        private void CreateBananaCluster(Vector3 position)
        {
            GameObject cluster = new GameObject("BananaCluster");
            cluster.transform.SetParent(generatedRoot);
            for (int i = 0; i < 4; i++)
            {
                CreateCylinder($"BananaStem_{i:00}", position + new Vector3(i * 0.22f, 0.65f, i * 0.12f), new Vector3(0.12f, 1.3f, 0.12f), trunkMaterial, cluster.transform);
                GameObject leaf = CreateCube($"BananaLeaf_{i:00}", position + new Vector3(i * 0.22f, 1.35f, i * 0.12f), new Vector3(0.32f, 0.08f, 1.6f), leafMaterial, cluster.transform);
                leaf.transform.rotation = Quaternion.Euler(18f, i * 80f, 0f);
            }
        }

        private GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent != null ? parent : generatedRoot);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            SetMaterial(obj, material);
            return obj;
        }

        private GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent != null ? parent : generatedRoot);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            SetMaterial(obj, material);
            return obj;
        }

        private void DrawWire(string name, Transform parent, Vector3 start, Vector3 end)
        {
            GameObject wire = new GameObject(name);
            wire.transform.SetParent(parent);
            LineRenderer line = wire.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, origin + start);
            line.SetPosition(1, origin + end);
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.material = CreateMaterial("Mat_PowerWire", new Color(0.035f, 0.032f, 0.03f));
        }

        private TextMeshProUGUI CreateHudText(string name, Transform parent, Vector2 anchoredPosition, int fontSize, FontStyles style)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(180f, 28f);

            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Right;
            text.color = new Color(1f, 0.94f, 0.78f);
            text.text = name;
            return text;
        }

        private T GetOrCreateSystemObject<T>(string objectName) where T : Component
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                obj = new GameObject(objectName);
            }

            T component = obj.GetComponent<T>();
            return component != null ? component : obj.AddComponent<T>();
        }

        private Light FindLightByName(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            return obj != null ? obj.GetComponent<Light>() : null;
        }

        private Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private void SetMaterial(GameObject obj, Material material)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private void AssignPrivateField(Object target, string fieldName, Object value)
        {
            if (target == null || value == null)
            {
                return;
            }

            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private void DestroyObject(GameObject obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(obj);
                return;
            }
#endif
            Destroy(obj);
        }
    }
}
