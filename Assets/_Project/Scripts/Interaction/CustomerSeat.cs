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
                canInteract = true;
                if (CartItem.HasPreparedTea)
                {
                    promptText = "Nhấn F để đặt ly trà đá xuống bàn";
                }
                else
                {
                    promptText = "Nhấn F để hỏi chuyện khách";
                }
            }
            else
            {
                // Ghế trống không cho phép người chơi tương tác nghỉ ngơi nữa
                canInteract = false;
                promptText = string.Empty;
            }
        }

        public void OccupySeat()
        {
            isOccupied = true;
            canInteract = true; // Cho tương tác để phục vụ/đặt ly lên bàn
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
            if (!isOccupied) return;

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

            if (seatNPC != null)
            {
                if (CartItem.HasPreparedTea)
                {
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
                else
                {
                    if (seatNPC.CurrentState == NPCState.Waiting)
                    {
                        string[] complaints = {
                            "Trà đá của tôi đâu em ơi, khát quá rồi!",
                            "Cho anh xin ly trà đá đi em trai!",
                            "Nước nôi lâu quá em ơi!",
                            "Pha giùm anh ly trà đá nhiều đá nhé!"
                        };
                        string complaint = complaints[Random.Range(0, complaints.Length)];
                        EventManager.TriggerDialogueLine("Khách hàng", complaint);
                    }
                    else if (seatNPC.CurrentState == NPCState.Drinking)
                    {
                        EventManager.TriggerDialogueLine("Khách hàng", "Trà đá mát lạnh, ngon quá!");
                    }
                    else
                    {
                        EventManager.TriggerDialogueLine("Khách hàng", "Cảm ơn em nhé.");
                    }
                }
            }
        }
    }
}
