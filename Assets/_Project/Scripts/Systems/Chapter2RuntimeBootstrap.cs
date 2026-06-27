using System.Collections.Generic;
using System.Reflection;
using GanhHangRong.Audio;
using GanhHangRong.Core;
using GanhHangRong.Economy;
using GanhHangRong.Interaction;
using GanhHangRong.Narrative;
using GanhHangRong.NPC;
using GanhHangRong.Player;
using GanhHangRong.UI;
using GanhHangRong.Weather;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GanhHangRong.Systems
{
    public class Chapter2RuntimeBootstrap : MonoBehaviour
    {
        private const string Chapter2MusicResourcePath = "Music/Chapter1_ChuyenTauDem";
        private static bool isSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            if (!isSubscribed)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                isSubscribed = true;
            }

            TryInstall(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall(scene);
        }

        private static void TryInstall(Scene scene)
        {
            if (scene.name != Constants.CHAPTER2_SCENE_NAME) return;
            if (FindAnyObjectByType<Chapter2RuntimeBootstrap>() != null) return;

            GameObject host = GameObject.Find("Chapter2Bootstrap");
            if (host == null) host = new GameObject("Chapter2Bootstrap");
            host.AddComponent<Chapter2RuntimeBootstrap>();
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != Constants.CHAPTER2_SCENE_NAME)
            {
                Destroy(this);
                return;
            }

            Transform managers = FindOrCreateRoot("Managers").transform;

            EnsureGameManager();
            EnsureAudio(managers);
            EnsureClockAndWeather(managers);
            EnsureDialogueManager(managers);

            GameObject player = EnsurePlayer();
            EnsureGroundCollider(player.transform.position);
            EnsureCamera(player.transform);
            TeaCart cart = EnsureFoodCart(player.transform);
            EnsureSeats();
            EnsureNPCSpawner(managers);
            EnsureGameplayUI();

            if (cart != null && player != null)
            {
                player.transform.position = GetPlayerSpawnPosition(cart.transform);
                player.transform.rotation = Quaternion.LookRotation(Flatten(cart.transform.position - player.transform.position, Vector3.forward), Vector3.up);
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetCurrentChapter(2, true);
                if (GameManager.Instance.CurrentPhase != GamePhase.Cutscene &&
                    GameManager.Instance.CurrentPhase != GamePhase.Dialogue)
                {
                    GameManager.Instance.SetGamePhase(GamePhase.Playing);
                }
            }
        }

        private static void EnsureGameManager()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            if (manager == null)
            {
                GameObject managerObject = new GameObject("GameManager");
                manager = managerObject.AddComponent<GameManager>();
            }

            manager.SetCurrentChapter(2, true);
        }

        private static void EnsureAudio(Transform managers)
        {
            AudioManager audio = FindAnyObjectByType<AudioManager>();
            if (audio == null)
            {
                GameObject audioObject = new GameObject("AudioManager");
                audioObject.transform.SetParent(managers, false);
                audio = audioObject.AddComponent<AudioManager>();
            }

            SceneMusicPlayer sceneMusic = FindAnyObjectByType<SceneMusicPlayer>();
            if (sceneMusic == null)
            {
                sceneMusic = audio.gameObject.AddComponent<SceneMusicPlayer>();
                SetSerializedField(sceneMusic, "musicResourcePath", Chapter2MusicResourcePath);
                SetSerializedField(sceneMusic, "fadeDuration", 1.5f);
                SetSerializedField(sceneMusic, "loop", true);
            }
        }

        private static void EnsureClockAndWeather(Transform managers)
        {
            DayNightCycle cycle = FindAnyObjectByType<DayNightCycle>();
            if (cycle == null)
            {
                GameObject clockObject = new GameObject("DayNightCycle");
                clockObject.transform.SetParent(managers, false);
                cycle = clockObject.AddComponent<DayNightCycle>();
            }

            cycle.SkipToHour(18.15f);

            WeatherManager weather = FindAnyObjectByType<WeatherManager>();
            if (weather == null)
            {
                GameObject weatherObject = new GameObject("WeatherManager");
                weatherObject.transform.SetParent(managers, false);
                weather = weatherObject.AddComponent<WeatherManager>();
            }

            List<WeatherPreset> presets = CreateRuntimeWeatherPresets();
            SetSerializedField(weather, "weatherPresets", presets);
            if (presets.Count > 0)
            {
                SetSerializedField(weather, "currentPreset", presets[0]);
                ApplyWeatherPresetVisuals(presets[0]);
            }
        }

        private static void EnsureDialogueManager(Transform managers)
        {
            if (FindAnyObjectByType<DialogueManager>() != null) return;

            GameObject dialogueObject = new GameObject("DialogueManager");
            dialogueObject.transform.SetParent(managers, false);
            dialogueObject.AddComponent<DialogueManager>();
        }

        private static GameObject EnsurePlayer()
        {
            GameObject player = FindPlayerObject();
            if (player == null)
            {
                player = new GameObject("Player_HoangHon");
                TrySetTag(player, Constants.TAG_PLAYER);
                player.transform.position = GetMarkerPosition("PlayerStart_Chapter2", new Vector3(-2f, 0.05f, -2f));

                Rigidbody rb = player.AddComponent<Rigidbody>();
                rb.mass = 70f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                CapsuleCollider collider = player.AddComponent<CapsuleCollider>();
                collider.height = 1.75f;
                collider.radius = 0.32f;
                collider.center = new Vector3(0f, 0.88f, 0f);

                player.AddComponent<PlayerStats>();
                player.AddComponent<PlayerController>();
                player.AddComponent<PlayerAnimator>();
                CreateFallbackPlayerVisual(player.transform);
            }

            return player;
        }

        private static void EnsureGroundCollider(Vector3 nearPosition)
        {
            if (GameObject.Find("Chapter2RuntimeGroundCollider") != null) return;

            GameObject ground = new GameObject("Chapter2RuntimeGroundCollider");
            ground.transform.position = new Vector3(nearPosition.x, nearPosition.y - 0.55f, nearPosition.z);
            BoxCollider collider = ground.AddComponent<BoxCollider>();
            collider.size = new Vector3(180f, 1f, 140f);
        }

        private static void EnsureCamera(Transform player)
        {
            Camera camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = player.position + new Vector3(0f, 2.2f, -4f);
                camera.transform.LookAt(player.position + Vector3.up * 1.2f);
            }

            TrySetTag(camera.gameObject, "MainCamera");
            if (camera.GetComponent<AudioListener>() == null && FindAnyObjectByType<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            CinematicCamera cinematic = camera.GetComponent<CinematicCamera>();
            if (cinematic == null) cinematic = camera.gameObject.AddComponent<CinematicCamera>();
            cinematic.SetTarget(player);
        }

        private static TeaCart EnsureFoodCart(Transform player)
        {
            TeaCart cart = FindAnyObjectByType<TeaCart>();
            if (cart == null)
            {
                GameObject cartObject = new GameObject("NightMarketFoodCart_Runtime");
                Transform marker = GameObject.Find("TeaCartStart_Chapter2")?.transform;
                if (marker != null)
                {
                    cartObject.transform.SetPositionAndRotation(marker.position, marker.rotation);
                }
                else
                {
                    cartObject.transform.position = player.position + new Vector3(2.4f, 0f, 1.2f);
                    cartObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }

                CreateFoodCartVisual(cartObject.transform);
                BoxCollider trigger = cartObject.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 1.05f, 0f);
                trigger.size = new Vector3(4.1f, 2.2f, 2.9f);
                cart = cartObject.AddComponent<TeaCart>();
            }

            EnsureFoodItems(cart.transform);
            return cart;
        }

        private static void EnsureFoodItems(Transform cart)
        {
            if (cart.GetComponentInChildren<Chapter2FoodItem>(true) != null) return;

            CreateFoodItem(cart, ChapterOrderCatalog.BanhMiMuoiOt, "Bánh mì nướng muối ớt", new Vector3(-0.72f, 1.24f, 0.18f), new Color(0.96f, 0.55f, 0.18f, 1f));
            CreateFoodItem(cart, ChapterOrderCatalog.BanhTrangNuong, "Bánh tráng nướng", new Vector3(0f, 1.25f, 0.2f), new Color(0.95f, 0.75f, 0.27f, 1f));
            CreateFoodItem(cart, ChapterOrderCatalog.HaiSanXien, "Hải sản xiên que", new Vector3(0.72f, 1.24f, 0.18f), new Color(1f, 0.38f, 0.22f, 1f));
        }

        private static void EnsureSeats()
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            foreach (Transform item in transforms)
            {
                if (item == null || !item.name.StartsWith("PlasticChair_")) continue;
                if (item.GetComponent<CustomerSeat>() == null)
                {
                    item.gameObject.AddComponent<CustomerSeat>();
                }

                Collider collider = item.GetComponent<Collider>();
                if (collider == null)
                {
                    BoxCollider box = item.gameObject.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 0.26f, 0f);
                    box.size = new Vector3(0.65f, 0.55f, 0.65f);
                    box.isTrigger = true;
                }
            }
        }

        private static void EnsureNPCSpawner(Transform managers)
        {
            NPCVisualFactory visualFactory = FindAnyObjectByType<NPCVisualFactory>();
            if (visualFactory == null)
            {
                visualFactory = managers.gameObject.AddComponent<NPCVisualFactory>();
            }

            NPCSpawner spawner = FindAnyObjectByType<NPCSpawner>();
            if (spawner == null)
            {
                GameObject spawnerObject = new GameObject("NPC_Spawner_Chapter2_Runtime");
                spawnerObject.transform.SetParent(managers, false);
                spawner = spawnerObject.AddComponent<NPCSpawner>();
            }

            SetSerializedField(spawner, "spawnPoints", FindOrCreateMarkers("CustomerSpawn_", new[]
            {
                new Vector3(-7f, 0.05f, -2f),
                new Vector3(7f, 0.05f, -2f)
            }));

            SetSerializedField(spawner, "exitPoints", FindOrCreateMarkers("CustomerExit_", new[]
            {
                new Vector3(-12f, 0.05f, 5f),
                new Vector3(12f, 0.05f, 5f)
            }));

            SetSerializedField(spawner, "availableProfiles", CreateChapter2Profiles());
        }

        private static void EnsureGameplayUI()
        {
            EnsureEventSystem();

            GameObject canvasObject = GameObject.Find("Chapter2HUD") ?? new GameObject("Chapter2HUD");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
                canvasObject.AddComponent<GraphicRaycaster>();

            if (canvas.GetComponent<GameplayHUD>() == null)
            {
                canvas.gameObject.AddComponent<GameplayHUD>();
            }

            EnsurePromptUI(canvas.transform);
            EnsureDialogueUI(canvas.transform);
        }

        private static void EnsurePromptUI(Transform canvas)
        {
            if (FindAnyObjectByType<InteractionPromptUI>() != null) return;

            RectTransform rect = CreateUIRect("InteractionPrompt", canvas);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(440f, 78f);

            Image bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.65f, 0.05f, 0.03f, 0.9f);

            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI text = CreateUIText("PromptText", rect, 34, FontStyles.Bold, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(16f, 8f);
            text.rectTransform.offsetMax = new Vector2(-16f, -8f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.color = Color.white;
            text.outlineWidth = 0.14f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
            text.extraPadding = true;

            InteractionPromptUI prompt = rect.gameObject.AddComponent<InteractionPromptUI>();
            SetSerializedField(prompt, "promptText", text);
            SetSerializedField(prompt, "canvasGroup", group);
            SetSerializedField(prompt, "followPlayer", false);
        }

        private static void EnsureDialogueUI(Transform canvas)
        {
            if (FindAnyObjectByType<DialogueUI>() != null) return;

            RectTransform panel = CreateUIRect("DialoguePanel", canvas);
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 34f);
            panel.sizeDelta = new Vector2(980f, 170f);

            Image bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.05f, 0.04f, 0.92f);

            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI speaker = CreateUIText("SpeakerName", panel, 24, FontStyles.Bold, TextAlignmentOptions.Left);
            speaker.rectTransform.anchorMin = new Vector2(0f, 1f);
            speaker.rectTransform.anchorMax = new Vector2(1f, 1f);
            speaker.rectTransform.offsetMin = new Vector2(26f, -52f);
            speaker.rectTransform.offsetMax = new Vector2(-26f, -12f);
            speaker.color = new Color(1f, 0.72f, 0.34f, 1f);

            TextMeshProUGUI body = CreateUIText("DialogueText", panel, 25, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            body.rectTransform.anchorMin = new Vector2(0f, 0f);
            body.rectTransform.anchorMax = new Vector2(1f, 1f);
            body.rectTransform.offsetMin = new Vector2(26f, 22f);
            body.rectTransform.offsetMax = new Vector2(-26f, -62f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.color = new Color(0.94f, 0.9f, 0.82f, 1f);

            DialogueUI dialogue = panel.gameObject.AddComponent<DialogueUI>();
            SetSerializedField(dialogue, "dialoguePanel", panel.gameObject);
            SetSerializedField(dialogue, "speakerNameText", speaker);
            SetSerializedField(dialogue, "dialogueText", body);
            SetSerializedField(dialogue, "canvasGroup", group);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static List<NPCProfile> CreateChapter2Profiles()
        {
            return new List<NPCProfile>
            {
                CreateProfile(NPCType.IslandTraveler, "Khách du lịch", 24f, 46f, 0.35f),
                CreateProfile(NPCType.LocalResident, "Người đi chợ đêm", 28f, 54f, 0.2f),
                CreateProfile(NPCType.Worker, "Người lao động", 34f, 62f, 0.16f),
                CreateProfile(NPCType.BusDriver, "Tài xế ghé chợ", 26f, 48f, 0.24f)
            };
        }

        private static NPCProfile CreateProfile(NPCType type, string npcName, float minPatience, float maxPatience, float tipChance)
        {
            NPCProfile profile = ScriptableObject.CreateInstance<NPCProfile>();
            profile.npcType = type;
            profile.npcName = npcName;
            profile.minPatience = minPatience;
            profile.maxPatience = maxPatience;
            profile.tipChance = tipChance;
            profile.minDrinkTime = 6f;
            profile.maxDrinkTime = 14f;
            return profile;
        }

        private static List<WeatherPreset> CreateRuntimeWeatherPresets()
        {
            return new List<WeatherPreset>
            {
                CreatePreset(WeatherType.Clear, 1f, 0f, 1f, new Color(0.84f, 0.78f, 0.66f, 1f), new Color(0.38f, 0.48f, 0.56f, 1f), 0.006f),
                CreatePreset(WeatherType.LightRain, 0.82f, 0.35f, Constants.RAIN_CUSTOMER_MODIFIER_LIGHT, new Color(0.62f, 0.68f, 0.78f, 1f), new Color(0.28f, 0.36f, 0.42f, 1f), 0.012f),
                CreatePreset(WeatherType.HeavyRain, 0.55f, 0.8f, Constants.RAIN_CUSTOMER_MODIFIER_HEAVY, new Color(0.44f, 0.5f, 0.62f, 1f), new Color(0.18f, 0.24f, 0.31f, 1f), 0.02f),
                CreatePreset(WeatherType.SeaWind, 0.88f, 0f, Constants.WIND_CUSTOMER_MODIFIER, new Color(0.7f, 0.78f, 0.9f, 1f), new Color(0.34f, 0.46f, 0.54f, 1f), 0.009f)
            };
        }

        private static WeatherPreset CreatePreset(WeatherType type, float lightIntensity, float rain, float customerModifier, Color lightColor, Color fogColor, float fogDensity)
        {
            WeatherPreset preset = ScriptableObject.CreateInstance<WeatherPreset>();
            preset.weatherType = type;
            preset.ambientLightIntensity = lightIntensity;
            preset.rainIntensity = rain;
            preset.customerSpawnModifier = customerModifier;
            preset.ambientLightColor = lightColor;
            preset.fogColor = fogColor;
            preset.fogDensity = fogDensity;
            return preset;
        }

        private static void ApplyWeatherPresetVisuals(WeatherPreset preset)
        {
            if (preset == null) return;

            Light light = FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.color = preset.ambientLightColor;
                light.intensity = preset.ambientLightIntensity;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = preset.fogColor;
            RenderSettings.fogDensity = preset.fogDensity;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            EventManager.TriggerRainIntensityChanged(preset.rainIntensity);
        }

        private static Transform[] FindOrCreateMarkers(string prefix, Vector3[] fallbackPositions)
        {
            List<Transform> result = new List<Transform>();
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            foreach (Transform item in transforms)
            {
                if (item != null && item.name.StartsWith(prefix))
                    result.Add(item);
            }

            if (result.Count > 0) return result.ToArray();

            for (int i = 0; i < fallbackPositions.Length; i++)
            {
                GameObject marker = new GameObject(prefix + (char)('A' + i));
                marker.transform.position = fallbackPositions[i];
                result.Add(marker.transform);
            }

            return result.ToArray();
        }

        private static void CreateFoodCartVisual(Transform parent)
        {
            CreateBox(parent, "CartBody", new Vector3(0f, 0.65f, 0f), new Vector3(2.8f, 0.95f, 1.35f), new Color(0.38f, 0.28f, 0.18f, 1f));
            CreateBox(parent, "CartCounter", new Vector3(0f, 1.16f, 0f), new Vector3(3.05f, 0.16f, 1.55f), new Color(0.68f, 0.48f, 0.3f, 1f));
            CreateBox(parent, "CharcoalGrill", new Vector3(0f, 1.3f, -0.36f), new Vector3(1.35f, 0.16f, 0.42f), new Color(0.08f, 0.08f, 0.075f, 1f));
            CreateBox(parent, "Sign", new Vector3(0f, 0.38f, -0.72f), new Vector3(1.65f, 0.4f, 0.06f), new Color(0.88f, 0.74f, 0.47f, 1f));

            for (int i = -1; i <= 1; i += 2)
            {
                CreateBox(parent, "Post_" + i, new Vector3(i * 1.3f, 1.75f, -0.55f), new Vector3(0.07f, 1.25f, 0.07f), new Color(0.16f, 0.18f, 0.16f, 1f));
                CreateBox(parent, "BackPost_" + i, new Vector3(i * 1.3f, 1.75f, 0.55f), new Vector3(0.07f, 1.25f, 0.07f), new Color(0.16f, 0.18f, 0.16f, 1f));
            }

            CreateBox(parent, "Awning", new Vector3(0f, 2.42f, 0f), new Vector3(3.25f, 0.08f, 1.95f), new Color(0.95f, 0.48f, 0.18f, 1f));
            CreateBox(parent, "AwningStripe", new Vector3(0f, 2.47f, 0f), new Vector3(0.38f, 0.085f, 2.0f), new Color(1f, 0.86f, 0.55f, 1f));

            for (int i = -1; i <= 1; i += 2)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel_" + i;
                wheel.transform.SetParent(parent, false);
                wheel.transform.localPosition = new Vector3(i * 0.9f, 0.22f, -0.72f);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.32f, 0.09f, 0.32f);
                Object.Destroy(wheel.GetComponent<Collider>());
                SetColor(wheel, new Color(0.06f, 0.055f, 0.05f, 1f));
            }
        }

        private static void CreateFoodItem(Transform parent, int orderId, string itemName, Vector3 localPosition, Color color)
        {
            GameObject item = new GameObject(itemName);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localRotation = Quaternion.identity;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plate.transform.SetParent(item.transform, false);
            plate.transform.localScale = new Vector3(0.34f, 0.035f, 0.34f);
            Object.Destroy(plate.GetComponent<Collider>());
            SetColor(plate, new Color(0.9f, 0.84f, 0.68f, 1f));

            GameObject food = GameObject.CreatePrimitive(orderId == ChapterOrderCatalog.HaiSanXien ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            food.transform.SetParent(item.transform, false);
            food.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            food.transform.localScale = orderId == ChapterOrderCatalog.HaiSanXien
                ? new Vector3(0.7f, 0.06f, 0.08f)
                : new Vector3(0.26f, 0.04f, 0.26f);
            food.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            Object.Destroy(food.GetComponent<Collider>());
            SetColor(food, color);

            Chapter2FoodItem foodItem = item.AddComponent<Chapter2FoodItem>();
            foodItem.Configure(orderId, itemName);
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            Object.Destroy(box.GetComponent<Collider>());
            SetColor(box, color);
            return box;
        }

        private static void CreateFallbackPlayerVisual(Transform parent)
        {
            GameObject visual = new GameObject("VisualRoot");
            visual.transform.SetParent(parent, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            body.transform.localScale = new Vector3(0.38f, 0.58f, 0.38f);
            Object.Destroy(body.GetComponent<Collider>());
            SetColor(body, new Color(0.27f, 0.27f, 0.26f, 1f));

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            head.transform.localScale = Vector3.one * 0.34f;
            Object.Destroy(head.GetComponent<Collider>());
            SetColor(head, new Color(0.86f, 0.62f, 0.44f, 1f));

            CreateBox(visual.transform, "LeftArm", new Vector3(-0.36f, 1.05f, 0f), new Vector3(0.12f, 0.72f, 0.12f), new Color(0.86f, 0.62f, 0.44f, 1f));
            CreateBox(visual.transform, "RightArm", new Vector3(0.36f, 1.05f, 0f), new Vector3(0.12f, 0.72f, 0.12f), new Color(0.86f, 0.62f, 0.44f, 1f));
            CreateBox(visual.transform, "LeftLeg", new Vector3(-0.13f, 0.25f, 0f), new Vector3(0.14f, 0.58f, 0.14f), new Color(0.28f, 0.45f, 0.66f, 1f));
            CreateBox(visual.transform, "RightLeg", new Vector3(0.13f, 0.25f, 0f), new Vector3(0.14f, 0.58f, 0.14f), new Color(0.28f, 0.45f, 0.66f, 1f));
        }

        private static RectTransform CreateUIRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TextMeshProUGUI CreateUIText(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateUIRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void SetColor(GameObject obj, Color color)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            renderer.material = material;
        }

        private static GameObject FindOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            return existing != null ? existing : new GameObject(name);
        }

        private static GameObject FindPlayerObject()
        {
            try
            {
                GameObject tagged = GameObject.FindGameObjectWithTag(Constants.TAG_PLAYER);
                if (tagged != null) return tagged;
            }
            catch (UnityException)
            {
                // Tag may not exist in early project states.
            }

            return GameObject.Find("Player_HoangHon");
        }

        private static Vector3 GetPlayerSpawnPosition(Transform cart)
        {
            Transform marker = GameObject.Find("PlayerStart_Chapter2")?.transform;
            if (marker != null) return marker.position;

            Vector3 right = Flatten(cart.right, Vector3.right);
            Vector3 forward = Flatten(cart.forward, Vector3.forward);
            return cart.position - right * 1.8f - forward * 1.2f + Vector3.up * 0.05f;
        }

        private static Vector3 GetMarkerPosition(string markerName, Vector3 fallback)
        {
            Transform marker = GameObject.Find(markerName)?.transform;
            return marker != null ? marker.position : fallback;
        }

        private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = fallback;
                direction.y = 0f;
            }

            return direction.normalized;
        }

        private static void TrySetTag(GameObject obj, string tag)
        {
            try
            {
                obj.tag = tag;
            }
            catch (UnityException)
            {
                // Ignore missing tags; components also use direct references/fallback lookups.
            }
        }

        private static void SetSerializedField(object target, string fieldName, object value)
        {
            if (target == null) return;

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
