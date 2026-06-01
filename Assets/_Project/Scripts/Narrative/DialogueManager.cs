using UnityEngine;
using GanhHangRong.Core;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace GanhHangRong.Narrative
{
    /// <summary>
    /// Quản lý việc hiển thị đối thoại.
    /// </summary>
    public class DialogueManager : Singleton<DialogueManager>
    {
        private Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
        private bool isDialogueActive = false;
        
        public bool IsDialogueActive => isDialogueActive;

        public void StartDialogue(DialogueData data)
        {
            if (data == null || data.lines == null || data.lines.Length == 0) return;

            linesQueue.Clear();
            foreach (var line in data.lines)
            {
                linesQueue.Enqueue(line);
            }

            isDialogueActive = true;
            EventManager.TriggerDialogueStarted();
            GameManager.Instance.SetGamePhase(GamePhase.Dialogue);

            DisplayNextLine();
        }

        public void DisplayNextLine()
        {
            if (linesQueue.Count == 0)
            {
                EndDialogue();
                return;
            }

            DialogueLine currentLine = linesQueue.Dequeue();
            EventManager.TriggerDialogueLine(currentLine.speakerName, currentLine.text);
        }

        private void EndDialogue()
        {
            isDialogueActive = false;
            EventManager.TriggerDialogueEnded();
            GameManager.Instance.SetGamePhase(GamePhase.Playing);
        }

        private void Update()
        {
            if (isDialogueActive)
            {
                if ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || 
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                {
                    // Truyền cho UI biết để skip typewriter hoặc qua câu mới
                    // Logic này sẽ được kết hợp trong DialogueUI
                }
            }
        }
    }
}
