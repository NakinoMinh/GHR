using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace GanhHangRong.Editor
{
    public static class MainMenuSceneBuilder
    {
        [MenuItem("Gánh Hàng Rong/Dựng Scene Main Menu", false, 11)]
        public static void BuildMainMenuScene()
        {
            // Tạo scene mới
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = "MainMenu";

            // ═══════════════════════════════════════════
            // 1. CAMERA
            // ═══════════════════════════════════════════
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;

            // ═══════════════════════════════════════════
            // 2. EVENT SYSTEM (Input System mới)
            // ═══════════════════════════════════════════
            GameObject evtSystem = new GameObject("EventSystem");
            evtSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evtSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // ═══════════════════════════════════════════
            // 3. CANVAS CHÍNH
            // ═══════════════════════════════════════════
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // ═══════════════════════════════════════════
            // 4. BACKGROUND (Ảnh nền + Parallax)
            // ═══════════════════════════════════════════
            string bgPath = "Assets/_Project/Art/UI/MainMenuBG.jpg";
            AssetDatabase.Refresh();
            TextureImporter importer = AssetImporter.GetAtPath(bgPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.preserveAspect = false;

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
            }
            else
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(bgPath);
                if (tex != null)
                {
                    bgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    bgImg.sprite = bgSprite;
                }
                else
                {
                    bgImg.color = new Color(0.05f, 0.08f, 0.12f);
                }
            }

            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(1980, 1140); // Lớn hơn 1920x1080 để Parallax không lộ viền

            // ═══════════════════════════════════════════
            // 5. UI LAYER GROUP (chứa Title + Buttons)
            // ═══════════════════════════════════════════
            GameObject uiLayer = new GameObject("UILayer");
            uiLayer.transform.SetParent(canvasObj.transform, false);
            var uiLayerRect = uiLayer.AddComponent<RectTransform>();
            uiLayerRect.anchorMin = Vector2.zero;
            uiLayerRect.anchorMax = Vector2.one;
            uiLayerRect.sizeDelta = Vector2.zero;
            var uiLayerGroup = uiLayer.AddComponent<CanvasGroup>();

            // --- Gradient overlay nửa trên ---
            GameObject topGradient = new GameObject("TopGradient");
            topGradient.transform.SetParent(uiLayer.transform, false);
            var gradImg = topGradient.AddComponent<Image>();
            gradImg.color = new Color(0, 0, 0, 0.5f);
            gradImg.raycastTarget = false;
            var gradRect = topGradient.GetComponent<RectTransform>();
            gradRect.anchorMin = new Vector2(0, 0.6f);
            gradRect.anchorMax = new Vector2(1, 1f);
            gradRect.sizeDelta = Vector2.zero;

            // --- Gradient overlay nửa dưới (cho nút rõ hơn) ---
            GameObject botGradient = new GameObject("BottomGradient");
            botGradient.transform.SetParent(uiLayer.transform, false);
            var botGradImg = botGradient.AddComponent<Image>();
            botGradImg.color = new Color(0, 0, 0, 0.4f);
            botGradImg.raycastTarget = false;
            var botGradRect = botGradient.GetComponent<RectTransform>();
            botGradRect.anchorMin = new Vector2(0, 0f);
            botGradRect.anchorMax = new Vector2(1, 0.18f);
            botGradRect.sizeDelta = Vector2.zero;

            // ═══════════════════════════════════════════
            // 6. BẢNG HIỆU GỖ + TIÊU ĐỀ
            // ═══════════════════════════════════════════
            // Bảng gỗ
            GameObject signObj = new GameObject("WoodenSign");
            signObj.transform.SetParent(uiLayer.transform, false);
            var signImg = signObj.AddComponent<Image>();
            signImg.color = new Color(0.42f, 0.27f, 0.15f, 0.92f); // Màu gỗ
            signImg.raycastTarget = false;
            var signOutline = signObj.AddComponent<Outline>();
            signOutline.effectColor = new Color(0.24f, 0.13f, 0.06f, 1f);
            signOutline.effectDistance = new Vector2(4, -4);
            var signRect = signObj.GetComponent<RectTransform>();
            signRect.anchorMin = new Vector2(0.5f, 1f);
            signRect.anchorMax = new Vector2(0.5f, 1f);
            signRect.anchoredPosition = new Vector2(0, -140);
            signRect.sizeDelta = new Vector2(900, 170);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(signObj.transform, false);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "GÁNH HÀNG RONG";
            titleText.fontSize = 110;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.8f, 0.5f); // Vàng ấm
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 40;
            titleText.fontSizeMax = 110;
            titleText.raycastTarget = false;
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.sizeDelta = Vector2.zero;

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(uiLayer.transform, false);
            var subText = subObj.AddComponent<TextMeshProUGUI>();
            subText.text = "Ánh Đèn Đêm Miền Tây | Hành Trình Mưu Sinh Nơi Miền Biển\nDành cả tuổi trẻ chỉ để giữ một ánh đèn còn sáng giữa đêm.";
            subText.fontSize = 28;
            subText.fontStyle = FontStyles.Italic;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            subText.raycastTarget = false;
            var subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -280);
            subRect.sizeDelta = new Vector2(1400, 80);

            // ═══════════════════════════════════════════
            // 7. BẢNG NÚT BẤM (Horizontal Layout)
            // ═══════════════════════════════════════════
            GameObject panelObj = new GameObject("ButtonPanel");
            panelObj.transform.SetParent(uiLayer.transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 75);
            panelRect.sizeDelta = new Vector2(1400, 90);

            var hLayout = panelObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 15;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;

            // Tạo nút — gán sự kiện cho script sau khi AddComponent
            var menuUI = canvasObj.AddComponent<UI.MainMenuUI>();

            CreateMenuButton(panelObj.transform, "BẮT ĐẦU CHƠI", menuUI, "OnPlayClicked");
            CreateMenuButton(panelObj.transform, "TIẾP TỤC", menuUI, "OnContinueClicked");
            CreateMenuButton(panelObj.transform, "CÀI ĐẶT", menuUI, "OnSettingsClicked");
            CreateMenuButton(panelObj.transform, "THÀNH TÍCH", menuUI, "OnAchievementsClicked");
            CreateMenuButton(panelObj.transform, "VỀ GAME", menuUI, "OnAboutClicked");
            CreateMenuButton(panelObj.transform, "THOÁT", menuUI, "OnQuitClicked");

            // Watermark
            GameObject watermark = new GameObject("Watermark");
            watermark.transform.SetParent(uiLayer.transform, false);
            var wmText = watermark.AddComponent<TextMeshProUGUI>();
            wmText.text = "GÁNH HÀNG RONG\nKIÊN GIANG\n2018";
            wmText.fontSize = 18;
            wmText.fontStyle = FontStyles.Bold;
            wmText.alignment = TextAlignmentOptions.BottomRight;
            wmText.color = new Color(1f, 0.72f, 0.3f, 0.7f);
            wmText.raycastTarget = false;
            var wmRect = watermark.GetComponent<RectTransform>();
            wmRect.anchorMin = new Vector2(1, 0);
            wmRect.anchorMax = new Vector2(1, 0);
            wmRect.anchoredPosition = new Vector2(-30, 25);
            wmRect.sizeDelta = new Vector2(200, 80);

            // ═══════════════════════════════════════════
            // 8. HỆ THỐNG MƯA (Particle System)
            // ═══════════════════════════════════════════
            GameObject rainObj = new GameObject("RainParticles");
            var rain = rainObj.AddComponent<ParticleSystem>();
            var rainMain = rain.main;
            rainMain.loop = true;
            rainMain.startLifetime = 1.5f;
            rainMain.startSpeed = 15f;
            rainMain.startSize = 0.05f;
            rainMain.maxParticles = 500;
            rainMain.simulationSpace = ParticleSystemSimulationSpace.World;
            rainMain.startColor = new Color(0.8f, 0.85f, 1f, 0.3f);
            rainMain.gravityModifier = 0.5f;

            var rainEmission = rain.emission;
            rainEmission.rateOverTime = 300;

            var rainShape = rain.shape;
            rainShape.shapeType = ParticleSystemShapeType.Box;
            rainShape.scale = new Vector3(25, 0, 1);
            rainShape.rotation = new Vector3(0, 0, 15f); // Mưa bay chéo do gió biển

            rainObj.transform.position = new Vector3(0, 8, 0); // Phía trên camera

            // Renderer mưa
            var rainRenderer = rainObj.GetComponent<ParticleSystemRenderer>();
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.lengthScale = 8f;
            rainRenderer.velocityScale = 0.1f;
            // Dùng Default-Line material
            rainRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");

            // ═══════════════════════════════════════════
            // 9. TRANSITION OVERLAY (Màn đen chuyển cảnh)
            // ═══════════════════════════════════════════
            GameObject overlayCanvas = new GameObject("TransitionCanvas");
            Canvas oCvs = overlayCanvas.AddComponent<Canvas>();
            oCvs.renderMode = RenderMode.ScreenSpaceOverlay;
            oCvs.sortingOrder = 100; // Luôn trên cùng
            var oScaler = overlayCanvas.AddComponent<CanvasScaler>();
            oScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            oScaler.referenceResolution = new Vector2(1920, 1080);
            overlayCanvas.AddComponent<GraphicRaycaster>().enabled = false; // Không chặn input

            // Panel đen
            GameObject overlayPanel = new GameObject("OverlayPanel");
            overlayPanel.transform.SetParent(overlayCanvas.transform, false);
            var overlayImg = overlayPanel.AddComponent<Image>();
            overlayImg.color = Color.black;
            overlayImg.raycastTarget = false;
            var overlayRect = overlayPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // CanvasGroup cho fade
            var overlayGroup = overlayPanel.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;

            // Chapter text
            GameObject chapterObj = new GameObject("ChapterText");
            chapterObj.transform.SetParent(overlayPanel.transform, false);
            var chText = chapterObj.AddComponent<TextMeshProUGUI>();
            chText.text = "Chương 1: Tiếng Rao Đêm";
            chText.fontSize = 72;
            chText.alignment = TextAlignmentOptions.Center;
            chText.color = Color.white;
            chText.alpha = 0f;
            chText.raycastTarget = false;
            var chRect = chapterObj.GetComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.5f, 0.5f);
            chRect.anchorMax = new Vector2(0.5f, 0.5f);
            chRect.sizeDelta = new Vector2(1200, 120);

            // ═══════════════════════════════════════════
            // 10. GÁN SERIALIZED FIELDS VÀO SCRIPT
            // ═══════════════════════════════════════════
            var so = new SerializedObject(menuUI);
            so.FindProperty("backgroundRect").objectReferenceValue = bgRect;
            so.FindProperty("uiLayerGroup").objectReferenceValue = uiLayerGroup;
            so.FindProperty("transitionOverlay").objectReferenceValue = overlayGroup;
            so.FindProperty("chapterText").objectReferenceValue = chText;
            so.FindProperty("backgroundImage").objectReferenceValue = bgImg;
            so.FindProperty("rainParticles").objectReferenceValue = rain;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ═══════════════════════════════════════════
            // LƯU SCENE
            // ═══════════════════════════════════════════
            string scenePath = "Assets/_Project/Scenes/MainMenu/MainMenu.unity";
            if (!System.IO.Directory.Exists("Assets/_Project/Scenes/MainMenu"))
                System.IO.Directory.CreateDirectory("Assets/_Project/Scenes/MainMenu");

            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"[Gánh Hàng Rong] Đã tạo thành công {scenePath} — Parallax + Mưa + Cinematic Transition!");
        }

        private static void CreateMenuButton(Transform parent, string text, UI.MainMenuUI menuUI, string methodName)
        {
            GameObject btnObj = new GameObject(text + "_Button");
            btnObj.transform.SetParent(parent, false);

            // Nền nút giả lập màu gỗ (gradient đậm → nhạt)
            var bgImg = btnObj.AddComponent<Image>();
            bgImg.color = new Color(0.42f, 0.27f, 0.15f, 0.9f);

            // Viền giả lập bảng gỗ
            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.13f, 0.06f, 1f);
            outline.effectDistance = new Vector2(3, -3);

            var btn = btnObj.AddComponent<Button>();

            // Hover effect: sáng hơn khi di chuột
            var colors = btn.colors;
            colors.normalColor = new Color(0.42f, 0.27f, 0.15f, 0.9f);
            colors.highlightedColor = new Color(0.55f, 0.38f, 0.22f, 1f);
            colors.pressedColor = new Color(0.35f, 0.20f, 0.10f, 1f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;

            // Text của nút
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = text;
            textMesh.fontSize = 24;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = new Color(1f, 0.87f, 0.67f);
            textMesh.enableAutoSizing = true;
            textMesh.fontSizeMin = 14;
            textMesh.fontSizeMax = 24;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);

            // Đăng ký sự kiện
            var actionDelegate = System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), menuUI, methodName
            ) as UnityEngine.Events.UnityAction;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, actionDelegate);
        }
    }
}
