using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Xe trà đá chính của người chơi.
    /// Phục vụ khách hàng, sửa chữa.
    /// </summary>
    public class TeaCart : Interactable
    {
        [SerializeField] private bool needsRepair = false;

        protected override void OnInteract(Player.PlayerController player)
        {
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

            if (isNight)
            {
                // Serve nearest customer
                ServeNearestCustomer(player);
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Mình cần mua thêm đồ trước khi tối đến.");
            }
        }

        private void ServeNearestCustomer(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (!stats.HasSuppliesForTea())
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hết nguyên liệu rồi...");
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
                promptText = "Phục vụ trà đá";
                EventManager.TriggerDialogueLine("Hoàng Hôn", "May quá, xe vẫn còn dùng được.");
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Không đủ tiền sửa xe rồi...");
            }
        }
    }
}
