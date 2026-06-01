using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using GanhHangRong.Core;

namespace GanhHangRong.UI
{
    /// <summary>
    /// Main Menu UI — Bao gồm hiệu ứng Parallax, Mưa, và Cinematic Transition.
    /// Lấy cảm hứng từ bản HTML gốc.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Tham chiếu UI")]
        [SerializeField] private RectTransform backgroundRect;   // Ảnh nền
        [SerializeField] private CanvasGroup uiLayerGroup;       // Nhóm UI (Title + Buttons)
        [SerializeField] private CanvasGroup transitionOverlay;  // Lớp màn đen chuyển cảnh
        [SerializeField] private TextMeshProUGUI chapterText;    // Text "Chương 1: Tiếng Rao Đêm"
        [SerializeField] private Image backgroundImage;          // Image component để tối dần
        [SerializeField] private ParticleSystem rainParticles;   // Hệ thống mưa

        [Header("Parallax Settings")]
        [SerializeField] private float parallaxIntensity = 30f;  // Biên độ di chuyển (px)
        [SerializeField] private float parallaxSmooth = 5f;      // Độ mượt

        private bool isTransitioning = false;
        private Vector2 parallaxTarget;
        private Vector2 parallaxCurrent;
        private Vector2 bgOriginalSize;

        private void Start()
        {
            // Khởi tạo Parallax
            if (backgroundRect != null)
            {
                // Làm ảnh nền lớn hơn 4% mỗi chiều để có không gian Parallax
                bgOriginalSize = backgroundRect.sizeDelta;
                backgroundRect.sizeDelta = new Vector2(
                    bgOriginalSize.x + parallaxIntensity * 2,
                    bgOriginalSize.y + parallaxIntensity * 2
                );
            }

            // Khởi tạo Transition
            if (transitionOverlay != null)
            {
                transitionOverlay.alpha = 0f;
                transitionOverlay.gameObject.SetActive(true);
            }

            if (chapterText != null)
            {
                chapterText.alpha = 0f;
            }

            // Fade in khi mở menu
            if (uiLayerGroup != null)
            {
                uiLayerGroup.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(uiLayerGroup, 0f, 1f, 1.5f));
            }
        }

        private void Update()
        {
            if (isTransitioning) return;

            // --- PARALLAX: Di chuyển ảnh nền ngược hướng chuột ---
            if (backgroundRect != null)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current != null 
                    ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() 
                    : Vector2.zero;

                float normalizedX = (mousePos.x / Screen.width - 0.5f);
                float normalizedY = (mousePos.y / Screen.height - 0.5f);

                // Di chuyển ngược hướng chuột (giống HTML)
                parallaxTarget = new Vector2(
                    -normalizedX * parallaxIntensity,
                    -normalizedY * parallaxIntensity
                );

                // Lerp mượt mà
                parallaxCurrent = Vector2.Lerp(parallaxCurrent, parallaxTarget, Time.deltaTime * parallaxSmooth);
                backgroundRect.anchoredPosition = parallaxCurrent;
            }
        }

        // ═══════════════════════════════════════════
        // SỰ KIỆN NÚT
        // ═══════════════════════════════════════════

        public void OnPlayClicked()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            StartCoroutine(CinematicTransition());
        }

        public void OnContinueClicked()
        {
            Debug.Log("[MainMenu] Continue clicked");
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked");
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

        // ═══════════════════════════════════════════
        // CINEMATIC TRANSITION (từ code HTML gốc)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Chuỗi hiệu ứng khi bấm "Bắt Đầu Chơi":
        /// 1. Ẩn UI ngay lập tức (1s)
        /// 2. Zoom chậm vào ảnh nền + làm tối dần (3s)
        /// 3. Chuyển màn hình đen (2.5s)
        /// 4. Hiện text "Chương 1: Tiếng Rao Đêm" (1.5s)
        /// 5. Giữ text 2s rồi chuyển Scene
        /// </summary>
        private IEnumerator CinematicTransition()
        {
            // Bước 1: Ẩn UI (fade out nhanh)
            if (uiLayerGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(uiLayerGroup, 1f, 0f, 0.8f));
            }

            // Dừng mưa dần dần
            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                emission.rateOverTime = 0;
            }

            // Bước 2: Zoom chậm vào ảnh nền + làm tối
            float zoomDuration = 3f;
            float elapsed = 0f;

            Vector2 startSize = backgroundRect != null ? backgroundRect.sizeDelta : Vector2.zero;
            Vector2 targetSize = startSize * 1.15f; // Zoom 15%
            Vector2 startPos = backgroundRect != null ? backgroundRect.anchoredPosition : Vector2.zero;
            Vector2 targetPos = new Vector2(20f, -30f); // Zoom vào góc có nhân vật (giống HTML)

            Color startColor = Color.white;
            Color targetColor = new Color(0.3f, 0.3f, 0.3f, 1f); // brightness(0.3)

            while (elapsed < zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / zoomDuration);

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

            // Bước 3: Màn hình đen (fade overlay)
            if (transitionOverlay != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(transitionOverlay, 0f, 1f, 1.5f));
            }

            // Bước 4: Hiện text Chương
            if (chapterText != null)
            {
                chapterText.text = "Chương 1: Tiếng Rao Đêm";
                yield return StartCoroutine(FadeText(chapterText, 0f, 1f, 1.5f));
            }

            // Giữ text 2.5 giây
            yield return new WaitForSeconds(2.5f);

            // Bước 5: Chuyển Scene
            SceneManager.LoadScene("Chapter1");
        }

        // ═══════════════════════════════════════════
        // TIỆN ÍCH FADE
        // ═══════════════════════════════════════════

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
