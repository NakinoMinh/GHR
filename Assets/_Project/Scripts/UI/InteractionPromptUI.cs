using UnityEngine;
using TMPro;
using GanhHangRong.Core;

namespace GanhHangRong.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Tooltip("Có theo dõi người chơi trên màn hình không, hay đứng cố định ở góc?")]
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);

        private Transform playerTransform;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            EventManager.OnInteractionPromptShow += ShowPrompt;
            EventManager.OnInteractionPromptHide += HidePrompt;
        }

        private void OnDisable()
        {
            EventManager.OnInteractionPromptShow -= ShowPrompt;
            EventManager.OnInteractionPromptHide -= HidePrompt;
        }

        private void Update()
        {
            if (canvasGroup.alpha > 0f)
            {
                // Kiểm tra xem camera có đang ở góc nhìn thứ nhất (pha chế) không
                var cam = Camera.main != null ? Camera.main.GetComponent<Player.CinematicCamera>() : null;
                bool isFirstPerson = cam != null && cam.IsCartFirstPersonMode;

                if (followPlayer && !isFirstPerson)
                {
                    if (playerTransform == null)
                    {
                        GameObject p = GameObject.FindGameObjectWithTag(Constants.TAG_PLAYER);
                        if (p != null) playerTransform = p.transform;
                    }

                    if (playerTransform != null && Camera.main != null)
                    {
                        // Di chuyển prompt UI theo người chơi trong chế độ đi lại bình thường
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(playerTransform.position + offset);
                        transform.position = screenPos;
                    }
                }
                else if (isFirstPerson)
                {
                    // Giữ vị trí cố định ở phần dưới màn hình (anchored ở center-bottom) để tránh bay nhảy khi xoay camera
                    var rect = GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = new Vector2(0f, 100f);
                    }
                }
            }
        }

        private void ShowPrompt(string text)
        {
            if (promptText != null) promptText.text = text;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void HidePrompt()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }
}
