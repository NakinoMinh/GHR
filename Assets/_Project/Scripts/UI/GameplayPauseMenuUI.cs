using GanhHangRong.Audio;
using GanhHangRong.Core;
using GanhHangRong.Interaction;
using GanhHangRong.Narrative;
using GanhHangRong.Player;
using GanhHangRong.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GanhHangRong.UI
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class GameplayPauseMenuUI : MonoBehaviour
    {
        private static readonly Color OverlayColor = new Color(0.015f, 0.02f, 0.025f, 0.8f);
        private static readonly Color PanelColor = new Color(0.075f, 0.085f, 0.09f, 0.985f);
        private static readonly Color BorderColor = new Color(0.33f, 0.37f, 0.36f, 0.8f);
        private static readonly Color TextColor = new Color(0.94f, 0.95f, 0.92f, 1f);
        private static readonly Color MutedTextColor = new Color(0.65f, 0.69f, 0.67f, 1f);
        private static readonly Color TrackColor = new Color(0.18f, 0.2f, 0.2f, 1f);
        private static readonly Color AccentColor = new Color(0.2f, 0.67f, 0.45f, 1f);
        private static readonly Color SecondaryColor = new Color(0.68f, 0.5f, 0.24f, 1f);

        private static GameplayPauseMenuUI instance;

        private GameObject rootOverlay;
        private Slider sensitivitySlider;
        private Slider masterVolumeSlider;
        private Slider musicVolumeSlider;
        private Slider sfxVolumeSlider;
        private Slider ambientVolumeSlider;
        private TMP_Text sensitivityValueText;
        private TMP_Text masterVolumeValueText;
        private TMP_Text musicVolumeValueText;
        private TMP_Text sfxVolumeValueText;
        private TMP_Text ambientVolumeValueText;
        private Button defaultsButton;
        private Button continueButton;
        private Button closeButton;
        private CinematicCamera cinematicCamera;
        private CursorLockMode cursorLockBeforePause;
        private bool cursorVisibleBeforePause;
        private bool isOpen;

        public static bool IsOpen
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameplayPauseMenuUI>();
                }

                return instance != null && instance.isOpen;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForGameplayScene()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryCreate(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreate(scene);
        }

        private static void TryCreate(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene != SceneManager.GetActiveScene()) return;

            if (instance == null)
            {
                instance = FindAnyObjectByType<GameplayPauseMenuUI>();
            }

            if (instance != null || FindAnyObjectByType<TeaCart>(FindObjectsInactive.Include) == null) return;
            if (GameManager.Instance == null) return;

            GameObject host = new GameObject("GameplayPauseMenuUI_Auto");
            host.AddComponent<GameplayPauseMenuUI>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureEventSystem();
            BuildInterface();
            rootOverlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance != null && instance != this) return;

            GameManager gameManager = GameManager.HasInstance ? GameManager.Instance : null;
            if (isOpen && gameManager != null && gameManager.IsPaused)
            {
                gameManager.ResumeGame();
            }

            instance = null;
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (isOpen)
            {
                CloseMenu();
                return;
            }

            if (ShouldYieldEscape()) return;

            GameManager gameManager = GameManager.HasInstance ? GameManager.Instance : null;
            if (gameManager != null && gameManager.IsPlaying)
            {
                OpenMenu();
            }
        }

        public void OpenMenu()
        {
            GameManager gameManager = GameManager.Instance;
            if (isOpen || gameManager == null || !gameManager.IsPlaying) return;

            cursorLockBeforePause = Cursor.lockState;
            cursorVisibleBeforePause = Cursor.visible;
            ResolveCamera();
            BindControlEvents();
            RefreshSettings();

            isOpen = true;
            rootOverlay.SetActive(true);
            GameplaySfxManager.Play(GameplaySfxCue.MenuOpen);
            gameManager.PauseGame();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseMenu()
        {
            if (!isOpen) return;

            GameplaySfxManager.Play(GameplaySfxCue.MenuClose);
            isOpen = false;
            rootOverlay.SetActive(false);
            PlayerPrefs.Save();

            GameManager gameManager = GameManager.HasInstance ? GameManager.Instance : null;
            if (gameManager != null && gameManager.IsPaused)
            {
                gameManager.ResumeGame();
            }

            Cursor.lockState = cursorLockBeforePause;
            Cursor.visible = cursorVisibleBeforePause;
        }

        private bool ShouldYieldEscape()
        {
            if (TabMenuUI.IsAnyMenuOpen) return true;
            if (BusinessDayController.HasInstance && BusinessDayController.Instance.HasPendingConfirmation) return true;
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacementModeActive) return true;
            if (DialogueManager.HasInstance && DialogueManager.Instance.IsDialogueActive) return true;

            ShopUIController shop = FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
            return shop != null && shop.IsOpen;
        }

        private void ResolveCamera()
        {
            if (cinematicCamera != null) return;

            Camera mainCamera = Camera.main;
            cinematicCamera = mainCamera != null ? mainCamera.GetComponent<CinematicCamera>() : null;
            if (cinematicCamera == null)
            {
                cinematicCamera = FindAnyObjectByType<CinematicCamera>();
            }
        }

        private void RefreshSettings()
        {
            float sensitivity = cinematicCamera != null
                ? cinematicCamera.MouseSensitivity
                : PlayerPrefs.GetFloat(CinematicCamera.MouseSensitivityPreferenceKey, CinematicCamera.DefaultMouseSensitivity);

            AudioManager audioManager = AudioManager.Instance;
            SetSliderValue(sensitivitySlider, sensitivity, sensitivityValueText, false);
            SetSliderValue(masterVolumeSlider, audioManager.MasterVolume, masterVolumeValueText, true);
            SetSliderValue(musicVolumeSlider, audioManager.MusicVolume, musicVolumeValueText, true);
            SetSliderValue(sfxVolumeSlider, audioManager.SfxVolume, sfxVolumeValueText, true);
            SetSliderValue(ambientVolumeSlider, audioManager.AmbientVolume, ambientVolumeValueText, true);
        }

        private void HandleSensitivityChanged(float value)
        {
            ResolveCamera();
            if (cinematicCamera != null)
            {
                cinematicCamera.SetMouseSensitivity(value);
            }
            else
            {
                PlayerPrefs.SetFloat(CinematicCamera.MouseSensitivityPreferenceKey,
                    Mathf.Clamp(value, CinematicCamera.MinimumMouseSensitivity, CinematicCamera.MaximumMouseSensitivity));
            }

            UpdateValueText(sensitivityValueText, value, false);
        }

        private void HandleMasterVolumeChanged(float value)
        {
            AudioManager.Instance.SetMasterVolume(value);
            UpdateValueText(masterVolumeValueText, value, true);
        }

        private void HandleMusicVolumeChanged(float value)
        {
            AudioManager.Instance.SetMusicVolume(value);
            UpdateValueText(musicVolumeValueText, value, true);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            AudioManager.Instance.SetSfxVolume(value);
            UpdateValueText(sfxVolumeValueText, value, true);
        }

        private void HandleAmbientVolumeChanged(float value)
        {
            AudioManager.Instance.SetAmbientVolume(value);
            UpdateValueText(ambientVolumeValueText, value, true);
        }

        private void RestoreDefaults()
        {
            ResolveCamera();
            if (cinematicCamera != null)
            {
                cinematicCamera.SetMouseSensitivity(CinematicCamera.DefaultMouseSensitivity);
            }
            else
            {
                PlayerPrefs.SetFloat(CinematicCamera.MouseSensitivityPreferenceKey, CinematicCamera.DefaultMouseSensitivity);
            }

            AudioManager audioManager = AudioManager.Instance;
            audioManager.SetMasterVolume(1f);
            audioManager.SetMusicVolume(Constants.MUSIC_BASE_VOLUME);
            audioManager.SetSfxVolume(Constants.SFX_BASE_VOLUME);
            audioManager.SetAmbientVolume(Constants.AMBIENT_BASE_VOLUME);
            RefreshSettings();
            PlayerPrefs.Save();
        }

        private void BindControlEvents()
        {
            BindSlider(sensitivitySlider, HandleSensitivityChanged);
            BindSlider(masterVolumeSlider, HandleMasterVolumeChanged);
            BindSlider(musicVolumeSlider, HandleMusicVolumeChanged);
            BindSlider(sfxVolumeSlider, HandleSfxVolumeChanged);
            BindSlider(ambientVolumeSlider, HandleAmbientVolumeChanged);
            BindButton(defaultsButton, RestoreDefaults);
            BindButton(continueButton, CloseMenu);
            BindButton(closeButton, CloseMenu);
        }

        private static void BindSlider(Slider slider, UnityAction<float> listener)
        {
            if (slider == null) return;
            slider.onValueChanged.RemoveListener(listener);
            slider.onValueChanged.AddListener(listener);
        }

        private static void BindButton(Button button, UnityAction listener)
        {
            if (button == null) return;
            button.onClick.RemoveListener(listener);
            button.onClick.AddListener(listener);
        }

        private static void SetSliderValue(Slider slider, float value, TMP_Text valueText, bool percentage)
        {
            slider.SetValueWithoutNotify(value);
            UpdateValueText(valueText, value, percentage);
        }

        private static void UpdateValueText(TMP_Text valueText, float value, bool percentage)
        {
            if (valueText == null) return;
            valueText.text = percentage ? $"{Mathf.RoundToInt(value * 100f)}%" : value.ToString("0.00");
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;

            GameObject eventSystemObject = new GameObject("EventSystem_Auto");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("GameplayPauseCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            rootOverlay = CreateRect("PauseOverlay", canvasObject.transform).gameObject;
            Stretch(rootOverlay.GetComponent<RectTransform>());
            Image overlayImage = rootOverlay.AddComponent<Image>();
            overlayImage.color = OverlayColor;

            RectTransform panel = CreateRect("SettingsPanel", rootOverlay.transform);
            SetRect(panel, Vector2.one * 0.5f, Vector2.zero, new Vector2(720f, 680f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1f, -1f);

            TMP_Text title = CreateText("Title", panel, "TẠM DỪNG", 38f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
            SetRect(title.rectTransform, Vector2.one * 0.5f, new Vector2(0f, 292f), new Vector2(600f, 54f));

            TMP_Text subtitle = CreateText("Subtitle", panel, "CÀI ĐẶT TRÒ CHƠI", 17f, FontStyles.Normal,
                TextAlignmentOptions.Center, MutedTextColor);
            SetRect(subtitle.rectTransform, Vector2.one * 0.5f, new Vector2(0f, 250f), new Vector2(600f, 30f));

            Image divider = CreateRect("HeaderDivider", panel).gameObject.AddComponent<Image>();
            divider.color = BorderColor;
            SetRect(divider.rectTransform, Vector2.one * 0.5f, new Vector2(0f, 218f), new Vector2(620f, 1f));

            sensitivitySlider = CreateSliderRow(panel, "MouseSensitivity", "ĐỘ NHẠY CHUỘT", 150f,
                CinematicCamera.MinimumMouseSensitivity, CinematicCamera.MaximumMouseSensitivity,
                HandleSensitivityChanged, out sensitivityValueText);
            masterVolumeSlider = CreateSliderRow(panel, "MasterVolume", "ÂM LƯỢNG TỔNG", 78f, 0f, 1f,
                HandleMasterVolumeChanged, out masterVolumeValueText);
            musicVolumeSlider = CreateSliderRow(panel, "MusicVolume", "NHẠC NỀN", 6f, 0f, 1f,
                HandleMusicVolumeChanged, out musicVolumeValueText);
            sfxVolumeSlider = CreateSliderRow(panel, "SfxVolume", "HIỆU ỨNG", -66f, 0f, 1f,
                HandleSfxVolumeChanged, out sfxVolumeValueText);
            ambientVolumeSlider = CreateSliderRow(panel, "AmbientVolume", "MÔI TRƯỜNG", -138f, 0f, 1f,
                HandleAmbientVolumeChanged, out ambientVolumeValueText);

            Image footerDivider = CreateRect("FooterDivider", panel).gameObject.AddComponent<Image>();
            footerDivider.color = BorderColor;
            SetRect(footerDivider.rectTransform, Vector2.one * 0.5f, new Vector2(0f, -202f), new Vector2(620f, 1f));

            defaultsButton = CreateButton("DefaultsButton", panel, "MẶC ĐỊNH", SecondaryColor, RestoreDefaults);
            SetRect(defaultsButton.GetComponent<RectTransform>(), Vector2.one * 0.5f, new Vector2(-165f, -270f), new Vector2(290f, 58f));

            continueButton = CreateButton("ContinueButton", panel, "TIẾP TỤC", AccentColor, CloseMenu);
            SetRect(continueButton.GetComponent<RectTransform>(), Vector2.one * 0.5f, new Vector2(165f, -270f), new Vector2(290f, 58f));

            closeButton = CreateButton("CloseButton", panel, "X", TrackColor, CloseMenu);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(44f, 44f));
        }

        private Slider CreateSliderRow(Transform parent, string name, string labelText, float y, float minValue,
            float maxValue, UnityAction<float> onChanged, out TMP_Text valueText)
        {
            RectTransform row = CreateRect(name + "Row", parent);
            SetRect(row, Vector2.one * 0.5f, new Vector2(0f, y), new Vector2(620f, 58f));

            TMP_Text label = CreateText(name + "Label", row, labelText, 18f, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, TextColor);
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(98f, 0f), new Vector2(196f, 44f));

            RectTransform sliderRect = CreateRect(name + "Slider", row);
            SetRect(sliderRect, Vector2.one * 0.5f, new Vector2(52f, 0f), new Vector2(310f, 34f));
            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.direction = Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            Image background = CreateRect("Background", sliderRect).gameObject.AddComponent<Image>();
            background.color = TrackColor;
            background.raycastTarget = false;
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.38f);
            backgroundRect.anchorMax = new Vector2(1f, 0.62f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            RectTransform fillArea = CreateRect("FillArea", sliderRect);
            Stretch(fillArea, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            Image fill = CreateRect("Fill", fillArea).gameObject.AddComponent<Image>();
            fill.color = AccentColor;
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = new Vector2(0f, 0.38f);
            fill.rectTransform.anchorMax = new Vector2(1f, 0.62f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            RectTransform handleArea = CreateRect("HandleArea", sliderRect);
            Stretch(handleArea, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            Image handle = CreateRect("Handle", handleArea).gameObject.AddComponent<Image>();
            handle.color = TextColor;
            handle.rectTransform.sizeDelta = new Vector2(22f, 30f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.onValueChanged.AddListener(onChanged);

            valueText = CreateText(name + "Value", row, string.Empty, 18f, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight, TextColor);
            SetRect(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-42f, 0f), new Vector2(84f, 44f));
            return slider;
        }

        private static Button CreateButton(string name, Transform parent, string labelText, Color color, UnityAction onClick)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.72f, 0.75f, 0.73f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            TMP_Text label = CreateText("Label", rect, labelText, 18f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
            Stretch(label.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 13f;
            label.fontSizeMax = 18f;
            return button;
        }

        private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize,
            FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
