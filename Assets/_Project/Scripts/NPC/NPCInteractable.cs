using UnityEngine;
using GanhHangRong.Interaction;
using GanhHangRong.Core;

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

        private void Update()
        {
            if (controller == null) return;

            if (controller.CurrentState == Core.NPCState.SittingDown || controller.CurrentState == Core.NPCState.Ordering)
            {
                canInteract = true;
                promptText = "Nhấn E để hỏi chuyện khách";
            }
            else if (controller.CurrentState == Core.NPCState.Waiting)
            {
                if (Interaction.CartItem.HasPreparedTea)
                {
                    canInteract = true;
                    promptText = "Nhấn F để đặt ly trà đá xuống bàn";
                }
                else
                {
                    canInteract = false;
                    promptText = string.Empty;
                }
            }
            else
            {
                canInteract = false;
                promptText = string.Empty;
            }
        }

        public void SetInteractable(bool active)
        {
            canInteract = active;
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            if (controller != null && (controller.CurrentState == Core.NPCState.SittingDown || controller.CurrentState == Core.NPCState.Ordering))
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hãy trò chuyện hỏi món khách trước rồi mới phục vụ nước!");
            }
            else if (controller != null && controller.CurrentState == Core.NPCState.Waiting)
            {
                if (controller.TargetSeat != null)
                {
                    controller.TargetSeat.Interact(player);
                }
            }
        }

        protected override void OnInteractE(Player.PlayerController player)
        {
            if (controller != null && (controller.CurrentState == Core.NPCState.SittingDown || controller.CurrentState == Core.NPCState.Ordering))
            {
                controller.StartOrderingDialogue(player);
            }
            else if (controller != null && controller.CurrentState == Core.NPCState.Waiting)
            {
                EventManager.TriggerDialogueLine("Khách hàng", "Đang chờ nước nè, có nước chưa em?");
            }
        }
    }
}
