using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Ghế nhựa cho khách ngồi. 
    /// Nếu có khách thì Player không tương tác được. Nếu trống, Player có thể ngồi nghỉ.
    /// Khi khách uống xong rời đi, ly sẽ để lại bàn — người chơi nhấn R để dọn ly dơ.
    /// </summary>
    public class CustomerSeat : Interactable
    {
        [SerializeField] private bool isPlayerOnly = false;
        public bool IsPlayerOnly { get => isPlayerOnly; set => isPlayerOnly = value; }

        private bool isOccupied = false;
        public bool IsOccupied => isOccupied;

        private GameObject placedCupObj;
        public GameObject PlacedCupObj { get => placedCupObj; set => placedCupObj = value; }

        // Ly dơ còn lại sau khi khách uống xong rời đi
        private bool hasDirtyCup = false;

        private NPC.NPCController occupyingNPC;

        private void Update()
        {
            if (isOccupied && occupyingNPC != null)
            {
                canInteract = true;
                if (occupyingNPC.CurrentState == Core.NPCState.Ordering)
                {
                    promptText = "Nhấn E để hỏi chuyện khách";
                }
                else if (occupyingNPC.CurrentState == Core.NPCState.Waiting)
                {
                    if (CartItem.HasPreparedTea)
                        promptText = "Nhấn Q để mang ly nước ra cho khách\nNhấn E để nói chuyện";
                    else
                        promptText = "Nhấn E để hỏi chuyện khách";
                }
                else
                {
                    promptText = "Nhấn E để nói chuyện";
                }
            }
            else if (!isOccupied && hasDirtyCup)
            {
                // Bàn trống nhưng còn ly dơ cần dọn
                canInteract = true;
                promptText = "Nhấn R để dọn ly dơ";
            }
            else
            {
                canInteract = false;
                promptText = string.Empty;
            }
        }

        public void ReserveSeat()
        {
            isOccupied = true;
            canInteract = false; // Chưa cho tương tác vì khách chưa ngồi
        }

        public void OccupySeat(NPC.NPCController npc)
        {
            isOccupied = true;
            canInteract = true;
            occupyingNPC = npc;
        }

        public void FreeSeat()
        {
            isOccupied = false;
            occupyingNPC = null;

            // Nếu có ly đã đặt (khách đã uống), giữ lại làm ly dơ thay vì xóa
            if (placedCupObj != null)
            {
                hasDirtyCup = true;
                // Đổi màu ly thành hơi ngả vàng để trông "dơ"
                foreach (var rend in placedCupObj.GetComponentsInChildren<Renderer>())
                {
                    if (rend.material != null)
                        rend.material.color = new Color(0.72f, 0.58f, 0.30f, 0.85f);
                }
                canInteract = true;
            }
            else
            {
                hasDirtyCup = false;
                canInteract = false;
                promptText = string.Empty;
            }
        }

        /// <summary>Người chơi nhấn R để cầm ly dơ trên bàn.</summary>
        protected override void OnInteractR(Player.PlayerController player)
        {
            if (!hasDirtyCup || placedCupObj == null) return;

            // Cầm ly dơ lên tay
            CartItem.PickUpDirtyCup(player);

            // Xóa ly dơ trên bàn
            Destroy(placedCupObj);
            placedCupObj = null;
            hasDirtyCup = false;
            canInteract = false;
            promptText = string.Empty;
        }

        protected override void OnInteractQ(Player.PlayerController player)
        {
            if (!isOccupied || occupyingNPC == null) return;

            // Nút Q: Chỉ dùng để mang nước ra cho khách nếu khách đang đợi và người chơi có nước
            if (occupyingNPC.CurrentState == Core.NPCState.Waiting && CartItem.HasPreparedTea)
            {
                // 1. Kích hoạt trạng thái uống nước của khách
                occupyingNPC.ServeDrink();

                // 2. Đặt ly trà đá tĩnh lên bàn ảo phía trước khách
                Vector3 tablePos = transform.position + transform.forward * 0.5f + Vector3.up * 0.35f;
                placedCupObj = CartItem.CreateStaticTeaCupModel(tablePos);

                // 3. Bỏ ly trà trên tay Hoàng Hôn
                CartItem.DetachTeaCup();
                CartItem.HasPreparedTea = false;

                // 4. Trừ tài nguyên ly trà đá trong PlayerStats
                var stats = player.GetComponent<Player.PlayerStats>();
                if (stats != null)
                {
                    stats.UseTeaSupplies();
                }

                EventManager.TriggerDialogueLine("Hoàng Hôn", "Trà đá của quý khách đây ạ. Chúc quý khách ngon miệng!");
            }
        }

        protected override void OnInteractE(Player.PlayerController player)
        {
            if (!isOccupied || occupyingNPC == null) return;

            // Nút E: Dùng để hỏi chuyện / đối thoại
            if (occupyingNPC.CurrentState == Core.NPCState.Ordering)
            {
                // Chuyển giao việc gọi món cho NPC
                occupyingNPC.StartOrderingDialogue(player);
                var npcInt = occupyingNPC.GetComponent<NPC.NPCInteractable>();
                if (npcInt != null) npcInt.SetInteractable(false);
            }
            else if (occupyingNPC.CurrentState == Core.NPCState.Waiting)
            {
                string[] complaints = {
                    "Đồ uống của tôi đâu em ơi, khát quá rồi!",
                    "Cho anh xin ly nước đi em trai!",
                    "Nước nôi lâu quá em ơi!",
                    "Làm lẹ giùm anh ly nước nhé!"
                };
                string complaint = complaints[Random.Range(0, complaints.Length)];
                Narrative.DialogueManager.Instance.StartSingleDialogue("Khách hàng", complaint);
            }
            else if (occupyingNPC.CurrentState == Core.NPCState.Drinking)
            {
                Narrative.DialogueManager.Instance.StartSingleDialogue("Khách hàng", "Nước mát lạnh, ngon quá!");
            }
            else
            {
                Narrative.DialogueManager.Instance.StartSingleDialogue("Khách hàng", "Cảm ơn em nhé.");
            }
        }
    }
}
