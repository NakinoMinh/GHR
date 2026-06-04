using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Xe trà đá chính của người chơi.
    /// Phục vụ khách hàng, sửa chữa.
    /// Nhấn F gần xe → góc nhìn thứ 1 từ mặt bàn xe đẩy (First Person).
    /// </summary>
    public class TeaCart : Interactable
    {
        [SerializeField] private bool needsRepair = false;
        [SerializeField] private Transform cameraViewPoint;

        [Header("Cart First Person — Điểm căn cứ mặt bàn")]
        [Tooltip("Transform chính giữa mặt bàn xe đẩy, camera sẽ đứng nhìn từ đây")]
        [SerializeField] private Transform cartOrbitCenter;

        private bool isPlayerInteracting = false;

        /// <summary>
        /// Có đang ở chế độ tương tác xe đẩy không.
        /// </summary>
        public bool IsPlayerInteracting => isPlayerInteracting;

        private void Start()
        {
            // Nếu chưa gán cartOrbitCenter, dùng chính transform của xe đẩy
            if (cartOrbitCenter == null)
                cartOrbitCenter = transform;

            promptText = "Nhấn F để tương tác xe đẩy";
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            if (isPlayerInteracting)
            {
                ExitCartInteraction(player);
                return;
            }

            var dayNight = FindAnyObjectByType<Economy.DayNightCycle>();
            bool isNight = dayNight != null && dayNight.CurrentTimeOfDay == TimeOfDay.Night;

            if (needsRepair)
            {
                if (!isNight)
                {
                    RepairCart(player.GetComponent<Player.PlayerStats>());
                }
                else
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Trời tối rồi, giờ không thể sửa xe được.");
                }
                return;
            }

            EnterCartInteraction(player);
        }

        private void EnterCartInteraction(Player.PlayerController player)
        {
            isPlayerInteracting = true;
            player.SetState(PlayerState.Interacting);
            player.DisableMovement();

            // Tính toán hướng forward nằm ngang thực tế của xe đẩy (tránh bị nghiêng do mô hình import -90 độ X)
            Vector3 cartForward;
            if (cameraViewPoint != null)
            {
                cartForward = transform.position - cameraViewPoint.position;
            }
            else
            {
                cartForward = transform.up; // Trục Y cục bộ của xe đẩy tương đương với hướng dọc của xe trong thế giới thực
            }
            cartForward.y = 0f;
            cartForward.Normalize();

            // Đứng cố định ở phía sau xe đẩy
            Vector3 targetStandPos = transform.position - cartForward * 1.1f;
            targetStandPos.y = player.transform.position.y; // Giữ nhân vật trên mặt đất
            player.transform.position = targetStandPos;
            player.transform.rotation = Quaternion.LookRotation(cartForward, Vector3.up);

            // ═══ BẬT CAMERA GÓC NHÌN THỨ 1 TỪ MẶT BÀN XE ĐẨY ═══
            var cam = Camera.main != null ? Camera.main.GetComponent<Player.CinematicCamera>() : null;
            if (cam != null)
            {
                // Ưu tiên cameraViewPoint (được gán từ SceneBuilder là FirstPersonCameraPoint)
                Transform viewPoint = (cameraViewPoint != null) ? cameraViewPoint : cartOrbitCenter;
                cam.EnableCartFirstPerson(viewPoint);
            }

            var dayNight = FindAnyObjectByType<Economy.DayNightCycle>();
            bool isNight = dayNight != null && dayNight.CurrentTimeOfDay == TimeOfDay.Night;
            if (isNight)
            {
                promptText = "Nhìn từ mặt bàn | Click vật phẩm | Space phục vụ | F thoát";
            }
            else
            {
                promptText = "Nhìn từ mặt bàn | Click vật phẩm trên bàn | F thoát";
            }
            EventManager.TriggerInteractionPromptShow(promptText);
        }

        private void ExitCartInteraction(Player.PlayerController player)
        {
            isPlayerInteracting = false;
            player.SetState(PlayerState.Idle);
            player.EnableMovement();

            // ═══ TẮT CAMERA GÓC NHÌN THỨ 1 ═══
            var cam = Camera.main != null ? Camera.main.GetComponent<Player.CinematicCamera>() : null;
            if (cam != null)
            {
                cam.DisableCartFirstPerson();
            }

            promptText = "Nhấn F để tương tác xe đẩy";
            EventManager.TriggerInteractionPromptShow(promptText);
        }

        public void ServeFromFirstPerson(Player.PlayerController player)
        {
            var dayNight = FindAnyObjectByType<Economy.DayNightCycle>();
            bool isNight = dayNight != null && dayNight.CurrentTimeOfDay == TimeOfDay.Night;

            if (isNight)
            {
                ServeNearestCustomer(player);
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Trời chưa tối, chưa có khách hàng để phục vụ.");
            }
        }

        private void ServeNearestCustomer(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (!stats.HasSuppliesForTea())
            {
                if (CartItem.IsHoldingCup)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", $"Ly trà đá chưa pha xong! (Hiện có: Trà {CartItem.TeaInCup}g/50g, Nước {Mathf.RoundToInt(CartItem.WaterInCup * 1000f)}ml/200ml, Đá {CartItem.IceInCup}%/5%)");
                }
                else
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Chưa có ly trà nào được pha! Hãy nhấp vào Ly Nước trên bàn để lấy ly và bắt đầu pha chế.");
                }
                return;
            }

            // Tìm khách hàng đang Wait gần nhất
            var npcs = FindObjectsByType<NPC.NPCController>(FindObjectsInactive.Exclude);
            NPC.NPCController closestWaiting = null;
            float minDist = float.MaxValue;

            foreach (var npc in npcs)
            {
                if (npc.CurrentState == NPCState.Waiting)
                {
                    float dist = Vector3.Distance(transform.position, npc.transform.position);
                    if (dist < 3f && dist < minDist) // Khách hàng ở gần xe
                    {
                        minDist = dist;
                        closestWaiting = npc;
                    }
                }
            }

            if (closestWaiting != null)
            {
                // Play animation serving
                player.SetState(PlayerState.Serving);
                stats.UseTeaSupplies();
                closestWaiting.ServeDrink();

                // Đặt ly trà đá tĩnh lên bàn ảo trước mặt khách nếu ghế ngồi hợp lệ
                if (closestWaiting.TargetSeat != null)
                {
                    Vector3 tablePos = closestWaiting.TargetSeat.transform.position + closestWaiting.TargetSeat.transform.forward * 0.5f + Vector3.up * 0.35f;
                    GameObject placedCup = CartItem.CreateStaticTeaCupModel(tablePos);
                    closestWaiting.TargetSeat.PlacedCupObj = placedCup;
                }

                // Tháo mô hình ly trà đá khỏi tay nhân vật sau khi phục vụ
                CartItem.DetachTeaCup();
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã trao ly trà đá cho khách! Cảm ơn vì đã đến ủng hộ.");
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Chưa có ai gọi món cả.");
            }
        }

        private void RepairCart(Player.PlayerStats stats)
        {
            if (stats == null) return;

            if (stats.SpendMoney(Constants.CART_REPAIR_COST))
            {
                needsRepair = false;
                promptText = "Nhấn F để tương tác xe đẩy";
                EventManager.TriggerDialogueLine("Hoàng Hôn", "May quá, xe vẫn còn dùng được.");
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Không đủ tiền sửa xe rồi...");
            }
        }
    }
}
