using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using GanhHangRong.Audio;
using GanhHangRong.Core;

namespace GanhHangRong.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Tham chieu UI")]
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private CanvasGroup uiLayerGroup;
        [SerializeField] private CanvasGroup transitionOverlay;
        [SerializeField] private TextMeshProUGUI chapterText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private ParticleSystem rainParticles;

        [Header("Audio Settings")]
        [SerializeField] private CanvasGroup settingsPanel;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider ambientVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeValueText;
        [SerializeField] private TextMeshProUGUI musicVolumeValueText;
        [SerializeField] private TextMeshProUGUI sfxVolumeValueText;
        [SerializeField] private TextMeshProUGUI ambientVolumeValueText;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button qualityButton;
        [SerializeField] private TextMeshProUGUI qualityValueText;
        private CanvasGroup infoPanel;
        private TextMeshProUGUI infoTitle;
        private TextMeshProUGUI infoBody;

        [Header("Parallax Settings")]
        [SerializeField] private float parallaxIntensity = 30f;
        [SerializeField] private float parallaxSmooth = 5f;

        private const string MenuMusicResourcePath = "Music/Menu_AnhDenVenBien";

        private bool isTransitioning = false;
        private bool settingsVisible = false;
        private Vector2 parallaxTarget;
        private Vector2 parallaxCurrent;
        private Vector2 bgOriginalSize;

        private void Start()
        {
            PlayMenuMusic();
            BuildSettingsPanelIfNeeded();
            InitializeSettingsPanel();

            if (backgroundRect != null)
            {
                bgOriginalSize = backgroundRect.sizeDelta;
                backgroundRect.sizeDelta = new Vector2(
                    bgOriginalSize.x + parallaxIntensity * 2f,
                    bgOriginalSize.y + parallaxIntensity * 2f
                );
            }

            if (transitionOverlay != null)
            {
                transitionOverlay.alpha = 0f;
                transitionOverlay.gameObject.SetActive(true);
            }

            if (chapterText != null)
            {
                chapterText.alpha = 0f;
            }

            if (uiLayerGroup != null)
            {
                uiLayerGroup.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(uiLayerGroup, 0f, 1f, 1.5f));
            }
        }

        private void Update()
        {
            if (isTransitioning) return;

            if (backgroundRect != null)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current != null
                    ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
                    : Vector2.zero;

                float normalizedX = mousePos.x / Screen.width - 0.5f;
                float normalizedY = mousePos.y / Screen.height - 0.5f;

                parallaxTarget = new Vector2(
                    -normalizedX * parallaxIntensity,
                    -normalizedY * parallaxIntensity
                );

                parallaxCurrent = Vector2.Lerp(parallaxCurrent, parallaxTarget, Time.deltaTime * parallaxSmooth);
                backgroundRect.anchoredPosition = parallaxCurrent;
            }
        }

        public void OnPlayClicked()
        {
            if (isTransitioning) return;
            SetSettingsVisible(false);
            isTransitioning = true;
            StartCoroutine(CinematicTransition());
        }

        public void OnContinueClicked()
        {
            if (isTransitioning) return;
            StartCoroutine(ContinueGame());
        }

        public void OnSettingsClicked()
        {
            SetSettingsVisible(!settingsVisible);
        }

        public void OnAboutClicked()
        {
            ShowInfo("GÁNH HÀNG RONG", "Một câu chuyện đời thường bên bến tàu Rạch Giá.\n\nDi chuyển, pha chế và phục vụ khách để duy trì gánh hàng.");
        }

        public void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void OnMusicVolumeChanged(float value)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null)
            {
                manager.SetMusicVolume(value);
            }

            UpdateMusicVolumeText(value);
        }

        public void OnMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
            UpdateVolumeText(masterVolumeValueText, value);
        }

        public void OnSfxVolumeChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value);
            UpdateVolumeText(sfxVolumeValueText, value);
        }

        public void OnAmbientVolumeChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetAmbientVolume(value);
            UpdateVolumeText(ambientVolumeValueText, value);
        }

        public void OnFullscreenChanged(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt("GHR_Fullscreen", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void OnQualityChanged(int value)
        {
            if (QualitySettings.names.Length == 0) return;
            int level = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt("GHR_Quality", level);
            PlayerPrefs.Save();
            if (qualityValueText != null) qualityValueText.text = "Chất lượng: " + QualitySettings.names[level];
        }

        public void OnQualityClicked()
        {
            if (QualitySettings.names.Length == 0) return;
            OnQualityChanged((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
        }

        private IEnumerator ContinueGame()
        {
            isTransitioning = true;
            AsyncOperation load = SceneManager.LoadSceneAsync(Constants.GAMEPLAY_SCENE_NAME);
            while (!load.isDone) yield return null;
            yield return null;
            if (GanhHangRong.Systems.SaveManager.Instance != null)
            {
                GanhHangRong.Systems.SaveManager.Instance.LoadGame();
            }
        }

        private IEnumerator CinematicTransition()
        {
            if (uiLayerGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(uiLayerGroup, 1f, 0f, 0.8f));
            }

            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                emission.rateOverTime = 0;
            }

            float zoomDuration = 3f;
            float elapsed = 0f;

            Vector2 startSize = backgroundRect != null ? backgroundRect.sizeDelta : Vector2.zero;
            Vector2 targetSize = startSize * 1.15f;
            Vector2 startPos = backgroundRect != null ? backgroundRect.anchoredPosition : Vector2.zero;
            Vector2 targetPos = new Vector2(20f, -30f);

            Color startColor = Color.white;
            Color targetColor = new Color(0.3f, 0.3f, 0.3f, 1f);

            while (elapsed < zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);

                if (backgroundRect != null)
                {
                    backgroundRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
                    backgroundRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                }

                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                }

                yield return null;
            }

            if (transitionOverlay != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(transitionOverlay, 0f, 1f, 1.5f));
            }

            if (chapterText != null)
            {
                chapterText.text = "XE TRÀ ĐÁ VEN BẾN TÀU";
                yield return StartCoroutine(FadeText(chapterText, 0f, 1f, 1.5f));
            }

            yield return new WaitForSeconds(2.5f);
            SceneManager.LoadScene(Constants.GAMEPLAY_SCENE_NAME);
        }

        private void PlayMenuMusic()
        {
            AudioClip clip = Resources.Load<AudioClip>(MenuMusicResourcePath);
            if (clip == null)
            {
                Debug.LogWarning("[MainMenu] Missing menu music at Resources/" + MenuMusicResourcePath);
                return;
            }

            AudioManager manager = AudioManager.Instance;
            if (manager != null)
            {
                manager.CrossfadeMusic(clip, 1.2f, true);
            }
        }

        private void InitializeSettingsPanel()
        {
            AudioManager manager = AudioManager.Instance;
            float masterVolume = manager != null ? manager.MasterVolume : 1f;
            float musicVolume = manager != null ? manager.MusicVolume : Constants.MUSIC_BASE_VOLUME;
            float sfxVolume = manager != null ? manager.SfxVolume : Constants.SFX_BASE_VOLUME;
            float ambientVolume = manager != null ? manager.AmbientVolume : Constants.AMBIENT_BASE_VOLUME;

            SetupSlider(masterVolumeSlider, masterVolume, OnMasterVolumeChanged);
            SetupSlider(musicVolumeSlider, musicVolume, OnMusicVolumeChanged);
            SetupSlider(sfxVolumeSlider, sfxVolume, OnSfxVolumeChanged);
            SetupSlider(ambientVolumeSlider, ambientVolume, OnAmbientVolumeChanged);

            UpdateVolumeText(masterVolumeValueText, masterVolume);
            UpdateMusicVolumeText(musicVolume);
            UpdateVolumeText(sfxVolumeValueText, sfxVolume);
            UpdateVolumeText(ambientVolumeValueText, ambientVolume);

            bool fullscreen = PlayerPrefs.GetInt("GHR_Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (qualityButton != null)
            {
                int quality = Mathf.Clamp(PlayerPrefs.GetInt("GHR_Quality", QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                OnQualityChanged(quality);
                qualityButton.onClick.RemoveListener(OnQualityClicked);
                qualityButton.onClick.AddListener(OnQualityClicked);
            }
            SetSettingsVisible(false);
        }

        private static void SetupSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveListener(callback);
            slider.onValueChanged.AddListener(callback);
        }

        private void UpdateMusicVolumeText(float value)
        {
            UpdateVolumeText(musicVolumeValueText, value);
        }

        private static void UpdateVolumeText(TextMeshProUGUI target, float value)
        {
            if (target != null) target.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void SetSettingsVisible(bool visible)
        {
            settingsVisible = visible;
            if (settingsPanel == null) return;

            settingsPanel.alpha = visible ? 1f : 0f;
            settingsPanel.interactable = visible;
            settingsPanel.blocksRaycasts = visible;
        }

        private void BuildSettingsPanelIfNeeded()
        {
            if (settingsPanel != null && musicVolumeSlider != null) return;

            Canvas canvas = GetComponent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            GameObject panelObject = new GameObject("SettingsPanel");
            panelObject.transform.SetParent(parent, false);

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-72f, 0f);
            panelRect.sizeDelta = new Vector2(500f, 610f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.13f, 0.08f, 0.05f, 0.92f);

            settingsPanel = panelObject.AddComponent<CanvasGroup>();

            VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 22, 22);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateSettingsText(panelObject.transform, "CÀI ĐẶT", 28, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;

            CreateVolumeRow(panelObject.transform, "Âm lượng", out masterVolumeSlider, out masterVolumeValueText);
            CreateVolumeRow(panelObject.transform, "Nhạc", out musicVolumeSlider, out musicVolumeValueText);
            CreateVolumeRow(panelObject.transform, "Hiệu ứng", out sfxVolumeSlider, out sfxVolumeValueText);
            CreateVolumeRow(panelObject.transform, "Môi trường", out ambientVolumeSlider, out ambientVolumeValueText);
            CreateDisplayRow(panelObject.transform);

            Button closeButton = CreateSettingsButton(panelObject.transform, "ÁP DỤNG & ĐÓNG");
            closeButton.onClick.AddListener(delegate { PlayerPrefs.Save(); SetSettingsVisible(false); });
        }

        private void CreateVolumeRow(Transform parent, string rowLabel, out Slider slider, out TextMeshProUGUI valueText)
        {
            GameObject row = new GameObject(rowLabel + "Row");
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 54f);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;

            TextMeshProUGUI label = CreateSettingsText(row.transform, rowLabel, 19, FontStyles.Bold);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 112f;

            slider = CreateMusicSlider(row.transform);
            LayoutElement sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
            sliderLayout.preferredWidth = 190f;
            sliderLayout.preferredHeight = 34f;

            valueText = CreateSettingsText(row.transform, "100%", 18, FontStyles.Bold);
            valueText.alignment = TextAlignmentOptions.Right;
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 58f;
        }

        private void CreateDisplayRow(Transform parent)
        {
            GameObject row = new GameObject("DisplaySettings");
            row.transform.SetParent(parent, false);
            RectTransform rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 92f);
            VerticalLayoutGroup vertical = row.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 8f;

            fullscreenToggle = CreateToggle(row.transform, "Toàn màn hình");

            qualityButton = CreateSettingsButton(row.transform, "CHẤT LƯỢNG");
            qualityValueText = qualityButton.GetComponentInChildren<TextMeshProUGUI>();
            LayoutElement layout = qualityButton.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 40f;
        }

        private Toggle CreateToggle(Transform parent, string labelText)
        {
            GameObject toggleObject = new GameObject(labelText);
            toggleObject.transform.SetParent(parent, false);
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            Image background = toggleObject.AddComponent<Image>();
            background.color = new Color(0.25f, 0.15f, 0.08f, 1f);
            toggle.targetGraphic = background;
            TextMeshProUGUI label = CreateSettingsText(toggleObject.transform, labelText, 18, FontStyles.Bold);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(14f, 0f);
            label.rectTransform.offsetMax = new Vector2(-14f, 0f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            LayoutElement layout = toggleObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 40f;
            return toggle;
        }

        private void ShowInfo(string titleText, string bodyText)
        {
            SetSettingsVisible(false);
            if (infoPanel == null)
            {
                Canvas canvas = GetComponent<Canvas>();
                Transform parent = canvas != null ? canvas.transform : transform;
                GameObject panel = new GameObject("InfoPanel");
                panel.transform.SetParent(parent, false);
                RectTransform rect = panel.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(540f, 330f);
                Image background = panel.AddComponent<Image>();
                background.color = new Color(0.09f, 0.055f, 0.035f, 0.97f);
                infoPanel = panel.AddComponent<CanvasGroup>();

                VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(34, 34, 28, 28);
                layout.spacing = 18f;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;

                infoTitle = CreateSettingsText(panel.transform, titleText, 30, FontStyles.Bold);
                infoTitle.alignment = TextAlignmentOptions.Center;
                infoTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

                infoBody = CreateSettingsText(panel.transform, bodyText, 20, FontStyles.Normal);
                infoBody.alignment = TextAlignmentOptions.Center;
                infoBody.textWrappingMode = TextWrappingModes.Normal;
                infoBody.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

                Button close = CreateSettingsButton(panel.transform, "ĐÓNG");
                close.onClick.AddListener(delegate
                {
                    infoPanel.alpha = 0f;
                    infoPanel.interactable = false;
                    infoPanel.blocksRaycasts = false;
                });
            }

            infoTitle.text = titleText;
            infoBody.text = bodyText;
            infoPanel.alpha = 1f;
            infoPanel.interactable = true;
            infoPanel.blocksRaycasts = true;
        }

        private TextMeshProUGUI CreateSettingsText(Transform parent, string text, float fontSize, FontStyles style)
        {
            GameObject textObject = new GameObject(text);
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = new Color(1f, 0.82f, 0.52f, 1f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private Slider CreateMusicSlider(Transform parent)
        {
            GameObject sliderObject = new GameObject("MusicVolumeSlider");
            sliderObject.transform.SetParent(parent, false);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(190f, 34f);

            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObject.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.26f, 0.17f, 0.1f, 1f);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.35f);
            bgRect.anchorMax = new Vector2(1f, 0.65f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.95f, 0.55f, 0.16f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.35f);
            fillRect.anchorMax = new Vector2(1f, 0.65f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.86f, 0.45f, 1f);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 22f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private Button CreateSettingsButton(Transform parent, string text)
        {
            GameObject buttonObject = new GameObject(text + "Button");
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.53f, 0.22f, 0.08f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 46f);

            TextMeshProUGUI label = CreateSettingsText(buttonObject.transform, text, 22, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            group.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            group.alpha = to;
        }

        private IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
        {
            float elapsed = 0f;
            text.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                text.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            text.alpha = to;
        }
    }
}
