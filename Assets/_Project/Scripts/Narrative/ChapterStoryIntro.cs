using System.Collections;
using GanhHangRong.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GanhHangRong.Narrative
{
    public class ChapterStoryIntro : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI continueText;

        private string[] lines;
        private bool advanceRequested;

        private static bool isSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            if (!isSubscribed)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                isSubscribed = true;
            }

            TryShowForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryShowForScene(scene);
        }

        private static void TryShowForScene(Scene scene)
        {
            // The legacy Chapter1 scene now hosts the continuous business-day game loop.
            if (scene.name == Constants.GAMEPLAY_SCENE_NAME) return;

            if (!TryGetIntro(scene.name, out string title, out string[] sceneLines)) return;
            if (FindAnyObjectByType<ChapterStoryIntro>() != null) return;

            GameObject introObject = new GameObject("ChapterStoryIntro_Runtime");
            ChapterStoryIntro intro = introObject.AddComponent<ChapterStoryIntro>();
            intro.Begin(title, sceneLines);
        }

        private static bool TryGetIntro(string sceneName, out string title, out string[] sceneLines)
        {
            switch (sceneName)
            {
                case Constants.GAMEPLAY_SCENE_NAME:
                    title = "XE TRÀ ĐÁ VEN BẾN TÀU";
                    sceneLines = new[]
                    {
                        "Rạch Giá, năm 2018. Hoàng Hôn thức dậy lúc 06:00 để bắt đầu ngày đầu tiên với chiếc xe trà đá cũ của gia đình.",
                        "Từ sáng sớm đến tối muộn, ngư dân, tài xế và khách đi đảo sẽ ghé qua tìm một ly nước mát.",
                        "Chuẩn bị nguyên liệu, chọn thực đơn và phục vụ từng vị khách là cách Hoàng Hôn giữ cho căn nhà nhỏ của mình còn hy vọng."
                    };
                    return true;

                case "Chapter2":
                    title = "Chương 2 - Chợ Đêm Ven Biển";
                    sceneLines = new[]
                    {
                        "Sau những đêm bán trà đá, Hoàng Hôn dành dụm được đủ vốn để đẩy xe ra chợ đêm ven biển.",
                        "Đèn màu, tiếng sóng, khách du lịch và mùi than nóng làm khu chợ đông hơn, nhưng nhịp phục vụ cũng gấp hơn.",
                        "Từ tối nay, xe hàng có thêm bánh mì nướng muối ớt, bánh tráng nướng và hải sản xiên que."
                    };
                    return true;

                case "Chapter3":
                    title = "Chương 3 - Mùa Biển Động";
                    sceneLines = new[]
                    {
                        "Biển trở gió. Những chuyến ghe thưa dần, khách quen cũng ít ghé hơn.",
                        "Bệnh của cha nặng hơn, còn Hoàng Hôn phải chọn từng đồng giữa việc mở rộng xe hàng và lo cho gia đình.",
                        "Đây không chỉ là một mùa mưa, mà là lúc lòng người bị thử thách nhiều nhất."
                    };
                    return true;

                case "Chapter4":
                    title = "Chương 4 - Gánh Hàng Rong";
                    sceneLines = new[]
                    {
                        "Qua nhiều ngày cực nhọc, chiếc xe nhỏ dần trở thành một góc quán sáng đèn cho người lao động ven biển.",
                        "Hoàng Hôn không còn chỉ bán để sống qua ngày, mà bắt đầu tạo ra một nơi người khác có thể nương nhờ.",
                        "Gánh hàng rong ấy là giấc mơ lớn lên từ những buổi tối rất bình thường."
                    };
                    return true;

                default:
                    title = string.Empty;
                    sceneLines = null;
                    return false;
            }
        }

        private void Begin(string title, string[] introLines)
        {
            lines = introLines;
            BuildUI(title);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGamePhase(GamePhase.Cutscene);
            }

            StartCoroutine(PlayIntroRoutine());
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.fKey.wasPressedThisFrame)
                {
                    advanceRequested = true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                advanceRequested = true;
            }
        }

        private void BuildUI(string title)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.03f, 0.025f, 0.96f);

            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            RectTransform content = CreateRect("Content", transform);
            content.anchorMin = new Vector2(0.14f, 0.16f);
            content.anchorMax = new Vector2(0.86f, 0.84f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            TextMeshProUGUI eyebrow = CreateText("Eyebrow", content, 22, FontStyles.Bold, TextAlignmentOptions.Left);
            eyebrow.text = "GÁNH HÀNG RONG";
            eyebrow.color = new Color(0.9f, 0.55f, 0.24f, 1f);
            eyebrow.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            eyebrow.rectTransform.anchorMax = new Vector2(1f, 1f);
            eyebrow.rectTransform.offsetMin = Vector2.zero;
            eyebrow.rectTransform.offsetMax = Vector2.zero;

            titleText = CreateText("Title", content, 54, FontStyles.Bold, TextAlignmentOptions.Left);
            titleText.text = title;
            titleText.color = new Color(1f, 0.88f, 0.62f, 1f);
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.rectTransform.anchorMin = new Vector2(0f, 0.61f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 0.86f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;

            bodyText = CreateText("Body", content, 30, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.color = new Color(0.9f, 0.88f, 0.8f, 1f);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.lineSpacing = 12f;
            bodyText.rectTransform.anchorMin = new Vector2(0f, 0.24f);
            bodyText.rectTransform.anchorMax = new Vector2(1f, 0.58f);
            bodyText.rectTransform.offsetMin = Vector2.zero;
            bodyText.rectTransform.offsetMax = Vector2.zero;

            Button continueButton = CreateButton("ContinueButton", content);
            continueButton.onClick.AddListener(delegate { advanceRequested = true; });
            RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.03f);
            buttonRect.anchorMax = new Vector2(0f, 0.03f);
            buttonRect.pivot = new Vector2(0f, 0f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(210f, 54f);

            continueText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            continueText.text = "Tiếp tục";
        }

        private IEnumerator PlayIntroRoutine()
        {
            yield return FadeCanvas(0f, 1f, 0.45f);

            for (int i = 0; i < lines.Length; i++)
            {
                yield return TypeLine(lines[i]);
                continueText.text = i == lines.Length - 1 ? "Vào game" : "Tiếp tục";

                while (!advanceRequested)
                {
                    yield return null;
                }

                advanceRequested = false;
            }

            yield return FadeCanvas(1f, 0f, 0.35f);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGamePhase(GamePhase.Playing);
            }

            Destroy(gameObject);
        }

        private IEnumerator TypeLine(string line)
        {
            advanceRequested = false;
            bodyText.text = string.Empty;

            for (int i = 0; i < line.Length; i++)
            {
                if (advanceRequested)
                {
                    bodyText.text = line;
                    advanceRequested = false;
                    break;
                }

                bodyText.text += line[i];
                yield return new WaitForSecondsRealtime(0.022f);
            }

        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.54f, 0.22f, 0.08f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI label = CreateText("Label", rect, 24, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = new Color(1f, 0.88f, 0.55f, 1f);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
