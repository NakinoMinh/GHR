using TMPro;
using UnityEngine;

namespace GanhHangRong.UI
{
    public class PlayerInteractionPrompt : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private string defaultPrompt = "Nhấn F để mua hàng";

        private void Awake()
        {
            Hide();
        }

        public void Show(string shopName)
        {
            string text = string.IsNullOrWhiteSpace(shopName)
                ? defaultPrompt
                : $"{defaultPrompt}\n{shopName}";

            if (promptText != null)
            {
                promptText.text = text;
            }
            else
            {
                Debug.LogWarning("PlayerInteractionPrompt thiếu promptText.", this);
            }

            SetVisible(true);
        }

        public void ShowMessage(string message)
        {
            if (promptText != null)
            {
                promptText.text = string.IsNullOrWhiteSpace(message) ? defaultPrompt : message;
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                return;
            }

            gameObject.SetActive(visible);
        }
    }
}
