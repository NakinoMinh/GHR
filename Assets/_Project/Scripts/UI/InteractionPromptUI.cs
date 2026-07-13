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
            ConfigurePromptText();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void ConfigurePromptText()
        {
            if (promptText == null) return;

            promptText.fontSize = Mathf.Max(promptText.fontSize, 34f);
            promptText.color = Color.white;
            promptText.enableWordWrapping = true;
            promptText.textWrappingMode = TextWrappingModes.Normal;
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
                    // Khi đang ở chế độ góc nhìn thứ nhất của xe đẩy
                    if (UnityEngine.InputSystem.Mouse.current != null)
                    {
                        if (Cursor.lockState == CursorLockMode.Locked)
                        {
                            // Nếu đang giữ chuột (xoay camera), đặt prompt ở giữa màn hình như trước
                            var rect = GetComponent<RectTransform>();
                            if (rect != null)
                            {
                                rect.anchoredPosition = new Vector2(0f, 100f);
                            }
                        }
                        else
                        {
                            // Nếu thả chuột (chọn vật phẩm), prompt đi theo con trỏ chuột
                            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                            transform.position = new Vector3(mousePos.x, mousePos.y + 60f, 0f);
                        }
                    }
                }
            }
        }

        private void ShowPrompt(string text)
        {
            ConfigurePromptText();
            if (promptText != null) promptText.text = text;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void HidePrompt()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }
}
