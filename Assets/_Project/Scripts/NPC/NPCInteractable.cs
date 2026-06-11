using UnityEngine;
using GanhHangRong.Interaction;

namespace GanhHangRong.NPC
{
    public class NPCInteractable : Interactable
    {
        private NPCController controller;

        private void Awake()
        {
            controller = GetComponent<NPCController>();
            promptText = "Nhấn E để hỏi chuyện khách";
            canInteract = false;
        }

        public void SetInteractable(bool active)
        {
            canInteract = active;
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            // Tương tác bằng F không làm gì đối với NPC này lúc gọi món.
            // Có thể hiển thị một thông báo nhỏ "Nhấn E để nói chuyện"
        }

        protected override void OnInteractE(Player.PlayerController player)
        {
            if (controller != null && controller.CurrentState == Core.NPCState.Ordering)
            {
                controller.StartOrderingDialogue(player);
                SetInteractable(false); // Đã hỏi xong
            }
        }
    }
}
