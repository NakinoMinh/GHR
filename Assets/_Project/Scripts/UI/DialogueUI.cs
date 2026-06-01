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
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private string currentFullText = "";

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
                if ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || 
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
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
            }
        }

        private void ShowDialogue()
        {
            dialoguePanel.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void HideDialogue()
        {
            dialoguePanel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void DisplayLine(string speaker, string text)
        {
            if (speakerNameText != null) speakerNameText.text = speaker;
            
            currentFullText = text;
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(text));
        }

        private IEnumerator TypeSentence(string sentence)
        {
            isTyping = true;
            dialogueText.text = "";
            foreach (char letter in sentence.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(Constants.TYPEWRITER_SPEED);
            }
            isTyping = false;
        }
    }
}
