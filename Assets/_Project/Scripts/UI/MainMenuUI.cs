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
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeValueText;

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
            Debug.Log("[MainMenu] Continue clicked");
        }

        public void OnSettingsClicked()
        {
            SetSettingsVisible(!settingsVisible);
        }

        public void OnAchievementsClicked()
        {
            Debug.Log("[MainMenu] Achievements clicked");
        }

        public void OnAboutClicked()
        {
            Debug.Log("[MainMenu] About clicked");
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
                chapterText.text = "Chương 1: Xe Trà Đá Ven Bến Tàu";
                yield return StartCoroutine(FadeText(chapterText, 0f, 1f, 1.5f));
            }

            yield return new WaitForSeconds(2.5f);
            SceneManager.LoadScene("Chapter1");
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
            float musicVolume = manager != null ? manager.MusicVolume : Constants.MUSIC_BASE_VOLUME;

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.SetValueWithoutNotify(musicVolume);
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            UpdateMusicVolumeText(musicVolume);
            SetSettingsVisible(false);
        }

        private void UpdateMusicVolumeText(float value)
        {
            if (musicVolumeValueText != null)
            {
                musicVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
            }
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
            panelRect.sizeDelta = new Vector2(420f, 260f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.13f, 0.08f, 0.05f, 0.92f);

            settingsPanel = panelObject.AddComponent<CanvasGroup>();

            VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 22, 22);
            layout.spacing = 18f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateSettingsText(panelObject.transform, "CÀI ĐẶT ÂM THANH", 26, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;

            GameObject row = new GameObject("MusicVolumeRow");
            row.transform.SetParent(panelObject.transform, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 54f);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;

            TextMeshProUGUI label = CreateSettingsText(row.transform, "Nhạc", 22, FontStyles.Bold);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 82f;

            musicVolumeSlider = CreateMusicSlider(row.transform);
            LayoutElement sliderLayout = musicVolumeSlider.gameObject.AddComponent<LayoutElement>();
            sliderLayout.preferredWidth = 190f;
            sliderLayout.preferredHeight = 34f;

            musicVolumeValueText = CreateSettingsText(row.transform, "18%", 20, FontStyles.Bold);
            musicVolumeValueText.alignment = TextAlignmentOptions.Right;
            LayoutElement valueLayout = musicVolumeValueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 58f;

            Button closeButton = CreateSettingsButton(panelObject.transform, "ĐÓNG");
            closeButton.onClick.AddListener(delegate { SetSettingsVisible(false); });
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
