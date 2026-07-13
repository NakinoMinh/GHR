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

        private NPC.NPCController currentNPC;
        public NPC.NPCController CurrentNPC => currentNPC;

        public NPC.NPCController GetOccupyingNPC()
        {
            if (currentNPC != null && currentNPC.TargetSeat == this) return currentNPC;
            var npcs = FindObjectsByType<NPC.NPCController>(FindObjectsInactive.Exclude);
            foreach (var npc in npcs)
            {
                if (npc.TargetSeat == this)
                {
                    currentNPC = npc;
                    break;
                }
            }
            return currentNPC;
        }

        private const float TargetChairHeight = 0.43f;
        private const float TargetTableHeight = 0.70f;
        private const float FallbackSeatSurfaceHeight = 0.44f;

        private void Awake()
        {
            ApplySceneSeatingLayoutFixOnce();
        }

        private void Start()
        {
            ApplySceneSeatingLayoutFixOnce();
        }

        private void Update()
        {
            if (isOccupied)
            {
                var npc = GetOccupyingNPC();
                if (npc != null && (npc.CurrentState == NPCState.SittingDown || npc.CurrentState == NPCState.Ordering))
                {
                    canInteract = true;
                    promptText = "Nhấn E để hỏi chuyện khách";
                }
                else if (npc != null && npc.CurrentState == NPCState.Waiting)
                {
                    canInteract = true;
                    if (CartItem.HasPreparedTea)
                    {
                        promptText = "Nhấn F để phục vụ nước | Nhấn E để trò chuyện";
                    }
                    else
                    {
                        promptText = "Nhấn E để trò chuyện với khách";
                    }
                }
                else
                {
                    canInteract = false;
                    promptText = string.Empty;
                }
            }
            else
            {
                if (placedCupObj != null)
                {
                    if (CartItem.IsHoldingCup || CartItem.HasPreparedTea)
                    {
                        canInteract = false;
                        promptText = string.Empty;
                    }
                    else
                    {
                        canInteract = true;
                        promptText = "Nhấn F để dọn ly dơ đi rửa";
                    }
                }
                else
                {
                    // Ghế trống không cho phép người chơi tương tác nghỉ ngơi nữa
                    canInteract = false;
                    promptText = string.Empty;
                }
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
            currentNPC = npc;
            canInteract = true;
        }

        public void FreeSeat()
        {
            isOccupied = false;
            currentNPC = null;
            canInteract = false;
            promptText = string.Empty;
        }

        public float GetSeatSurfaceY()
        {
            if (TryGetRendererBounds(transform, out Bounds bounds))
            {
                return bounds.max.y;
            }

            return transform.position.y + FallbackSeatSurfaceHeight;
        }

        public float GetTableSurfaceY()
        {
            Vector3 fwdPos = transform.position + transform.forward * 0.5f;
            var colliders = Physics.OverlapSphere(new Vector3(fwdPos.x, GetSeatSurfaceY(), fwdPos.z), 1.5f);
            float maxColY = -999f;
            foreach (var col in colliders)
            {
                if (col != null && col.name.Contains("Table") && col.bounds.max.y > maxColY)
                {
                    maxColY = col.bounds.max.y;
                }
            }
            if (maxColY > -990f)
            {
                return maxColY;
            }
            return GetSeatSurfaceY() + 0.485f;
        }

        public float GetSeatBaseY()
        {
            if (TryGetRendererBounds(transform, out Bounds bounds))
            {
                return bounds.min.y;
            }

            return transform.position.y;
        }

        private static void ApplySceneSeatingLayoutFixOnce()
        {
            CustomerSeat[] seats = FindObjectsByType<CustomerSeat>(FindObjectsInactive.Exclude);
            foreach (CustomerSeat seat in seats)
            {
                if (seat != null)
                {
                    NormalizeObjectHeight(seat.transform, TargetChairHeight);
                }
            }

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            foreach (Transform item in transforms)
            {
                if (item != null && item.name == "TeaTable")
                {
                    NormalizeObjectHeight(item, TargetTableHeight);
                }
            }
        }

        private static void NormalizeObjectHeight(Transform root, float targetHeight)
        {
            if (root == null || targetHeight <= 0f) return;
            if (!TryGetRendererBounds(root, out Bounds beforeBounds)) return;
            if (beforeBounds.size.y <= 0.001f) return;

            float bottomBefore = beforeBounds.min.y;
            float scaleFactor = targetHeight / beforeBounds.size.y;
            root.localScale *= scaleFactor;

            if (TryGetRendererBounds(root, out Bounds afterBounds))
            {
                float bottomDelta = bottomBefore - afterBounds.min.y;
                root.position += Vector3.up * bottomDelta;
            }
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            if (!isOccupied && placedCupObj != null)
            {
                if (CartItem.IsHoldingCup || CartItem.HasPreparedTea)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Tay đang cầm vật khác, hãy cất hoặc dùng hết trước khi dọn ly!");
                    return;
                }

                Destroy(placedCupObj);
                placedCupObj = null;
                canInteract = false;
                promptText = string.Empty;

                CartItem.PickUpDirtyCupFromTable(player);
                return;
            }

            if (!isOccupied) return;

            var seatNPC = GetOccupyingNPC();
            if (seatNPC == null) return;

            if (seatNPC.CurrentState == NPCState.SittingDown || seatNPC.CurrentState == NPCState.Ordering)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hãy trò chuyện hỏi món khách trước rồi mới phục vụ nước!");
                return;
            }

            if (seatNPC.CurrentState != NPCState.Waiting)
            {
                return;
            }

            if (!CartItem.HasPreparedTea)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Chưa có món nước trên tay! Hãy ra xe đẩy để lấy nước phục vụ khách.");
                return;
            }

            if (seatNPC.OrderedDrinkId != CartItem.PreparedDrinkId)
            {
                EventManager.TriggerDialogueLine("Khách hàng", "😐 Đây không phải món tôi yêu cầu!");
                seatNPC.ReactToWrongDrink();
                return;
            }

            // 1. Kích hoạt trạng thái uống nước của khách
            seatNPC.ServeDrink();

            // 2. Đặt ly trà đá tĩnh lên bàn ảo phía trước khách
            Vector3 tablePos = transform.position + transform.forward * 0.5f;
            tablePos.y = GetTableSurfaceY();
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

        protected override void OnInteractE(Player.PlayerController player)
        {
            if (!isOccupied) return;

            var seatNPC = GetOccupyingNPC();
            if (seatNPC == null) return;

            if (seatNPC.CurrentState == NPCState.SittingDown || seatNPC.CurrentState == NPCState.Ordering)
            {
                seatNPC.StartOrderingDialogue(player);
            }
            else if (seatNPC.CurrentState == NPCState.Waiting)
            {
                EventManager.TriggerDialogueLine("Khách hàng", "Đang chờ nước nè, có nước chưa em?");
            }
        }
    }
}
