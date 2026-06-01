using UnityEngine;

namespace GanhHangRong.Narrative
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(2, 5)]
        public string text;
        public Core.DialogueEmotion emotion;
    }

    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "Gánh Hàng Rong/Narrative/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
