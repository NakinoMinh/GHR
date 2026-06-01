using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace GanhHangRong.UI
{
    public class TransitionUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private Image fadeImage;
        [SerializeField] private TMPro.TextMeshProUGUI chapterTitleText;

        private void Start()
        {
            // Bắt đầu game sẽ fade in
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
                fadeCanvasGroup.gameObject.SetActive(true);
                StartCoroutine(FadeIn(1.5f));
            }
        }

        public IEnumerator FadeIn(float duration)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        public IEnumerator FadeOut(float duration)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        public IEnumerator ShowChapterTitle(string title, float duration)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 1f;
            if (fadeImage != null) fadeImage.color = Color.black;
            if (chapterTitleText != null) 
            {
                chapterTitleText.text = title;
                chapterTitleText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(duration);

            if (chapterTitleText != null) chapterTitleText.gameObject.SetActive(false);
            yield return StartCoroutine(FadeIn(1.5f));
        }
    }
}
