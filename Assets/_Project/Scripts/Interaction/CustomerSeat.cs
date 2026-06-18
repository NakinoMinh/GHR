using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Ghế nhựa cho khách ngồi. 
    /// Nếu có khách thì Player không tương tác được. Nếu trống, Player có thể ngồi nghỉ.
    /// </summary>
    public class CustomerSeat : Interactable
    {
        [SerializeField] private bool isPlayerOnly = false;
        public bool IsPlayerOnly { get => isPlayerOnly; set => isPlayerOnly = value; }

        private bool isOccupied = false;
        public bool IsOccupied => isOccupied;

        private GameObject placedCupObj;
        public GameObject PlacedCupObj { get => placedCupObj; set => placedCupObj = value; }

        private void Update()
        {
            if (isOccupied)
            {
                if (CartItem.HasPreparedTea)
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
                // Ghế trống không cho phép người chơi tương tác nghỉ ngơi nữa
                canInteract = false;
                promptText = string.Empty;
            }
        }

        /// <summary>Đặt trước ghế khi NPC đang trên đường đi tới (chưa ngồi hẳn).</summary>
        public void ReserveSeat()
        {
            isOccupied = true; // Coi như đã chiếm để không spawn thêm NPC khác vào ghế này
        }

        public void OccupySeat()
        {
            isOccupied = true;
            canInteract = true; // Cho tương tác để phục vụ/đặt ly lên bàn
        }

        /// <summary>Gọi khi NPC thực sự ngồi xuống ghế.</summary>
        public void OccupySeat(NPC.NPCController npc)
        {
            isOccupied = true;
            canInteract = true;
        }

        public void FreeSeat()
        {
            isOccupied = false;
            canInteract = false;
            
            // Dọn ly nước tĩnh trên bàn khi khách đứng dậy rời đi
            if (placedCupObj != null)
            {
                Destroy(placedCupObj);
                placedCupObj = null;
            }
            promptText = string.Empty;
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            if (!isOccupied || !CartItem.HasPreparedTea) return;

            // Tìm NPC đang ngồi trên chiếc ghế này
            NPC.NPCController seatNPC = null;
            var npcs = FindObjectsByType<NPC.NPCController>(FindObjectsInactive.Exclude);
            foreach (var npc in npcs)
            {
                if (npc.TargetSeat == this)
                {
                    seatNPC = npc;
                    break;
                }
            }

            if (seatNPC == null) return;

            if (seatNPC.CurrentState != NPCState.Waiting)
            {
                EventManager.TriggerDialogueLine("Khách hàng", "Cảm ơn em, để lát nữa nhé.");
                return;
            }

            // 1. Kích hoạt trạng thái uống nước của khách
            seatNPC.ServeDrink();

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
}
