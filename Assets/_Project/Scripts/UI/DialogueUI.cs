using UnityEngine;
using TMPro;
using GanhHangRong.Core;
using System.Collections;
using UnityEngine.InputSystem;

namespace GanhHangRong.UI
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private UnityEngine.UI.Image avatarImage;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private string currentFullText = "";
        private int frameStarted = 0;

        private void OnEnable()
        {
            EventManager.OnDialogueStarted += ShowDialogue;
            EventManager.OnDialogueEnded += HideDialogue;
            EventManager.OnDialogueLine += DisplayLine;
        }

        private void OnDisable()
        {
            EventManager.OnDialogueStarted -= ShowDialogue;
            EventManager.OnDialogueEnded -= HideDialogue;
            EventManager.OnDialogueLine -= DisplayLine;
        }

        private void Start()
        {
            HideDialogue();
        }

        private void Update()
        {
            if (Narrative.DialogueManager.HasInstance && Narrative.DialogueManager.Instance.IsDialogueActive)
            {
                // ESC to close dialogue
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Narrative.DialogueManager.Instance.EndDialogueEarly();
                    return;
                }

                // Chờ ít nhất 1 frame sau khi bắt đầu thoại để tránh xử lý phím F của chính lệnh mở thoại
                if (Time.frameCount > frameStarted + 1)
                {
                    if ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || 
                        (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) || 
                        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                    {
                        OnNextPressed();
                    }
                }
            }
        }

        public void OnNextPressed()
        {
            if (isTyping)
            {
                // Skip đánh máy
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                dialogueText.text = currentFullText;
                isTyping = false;
            }
            else
            {
                // Câu tiếp theo
                Narrative.DialogueManager.Instance.DisplayNextLine();
            }
        }

        private void ShowDialogue()
        {
            frameStarted = Time.frameCount;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void HideDialogue()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void DisplayLine(string speaker, string text, Sprite avatar)
        {
            if (speakerNameText != null) speakerNameText.text = speaker;
            
            if (avatarImage != null)
            {
                if (avatar != null)
                {
                    avatarImage.sprite = avatar;
                    avatarImage.color = Color.white;
                }
                else
                {
                    avatarImage.sprite = null;
                    avatarImage.color = new Color(1, 1, 1, 0); // Hide if no avatar
                }
            }

            
            currentFullText = text;
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (gameObject.activeInHierarchy)
            {
                typingCoroutine = StartCoroutine(TypeSentence(text));
            }
            else
            {
                if (dialogueText != null) dialogueText.text = text;
            }
        }

        private IEnumerator TypeSentence(string sentence)
        {
            isTyping = true;
            if (dialogueText != null) dialogueText.text = "";
            foreach (char letter in sentence.ToCharArray())
            {
                if (dialogueText != null) dialogueText.text += letter;
                yield return new WaitForSeconds(Constants.TYPEWRITER_SPEED);
            }
            isTyping = false;
        }
    }
}
