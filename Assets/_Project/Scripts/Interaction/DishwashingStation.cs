using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Bồn rửa ly: người chơi mang ly đang cầm hoặc ly pha sai đến đây và nhấn Z để rửa.
    /// Sau khi rửa, ly sạch được trả về kho và đặt lại trên mặt bàn xe đẩy.
    /// </summary>
    public class DishwashingStation : Interactable
    {
        [Header("Bồn rửa ly")]
        [SerializeField] private float washDuration = 1.5f;

        private bool isWashing = false;

        private void Start()
        {
            promptText = "Nhấn Z để rửa ly";
            interactionCooldown = 1.0f;
        }

        private void Update()
        {
            bool needsWash = CartItem.HasCupToWash;

            if (needsWash)
            {
                canInteract = !isWashing;
                if (CartItem.IsHoldingCup)
                {
                    promptText = "Nhấn Z để rửa ly pha sai (+1 ly sạch)";
                }
                else if (CartItem.HasPreparedTea)
                {
                    promptText = "Nhấn Z để đổ bỏ ly pha sai (+1 ly sạch)";
                }
            }
            else
            {
                canInteract = false;
                promptText = string.Empty;
            }
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Mang ly tới bồn rửa rồi nhấn Z để rửa ly.");
        }

        protected override void OnInteractZ(Player.PlayerController player)
        {
            if (isWashing)
            {
                return;
            }

            if (!CartItem.HasCupToWash)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Chưa có ly nào cần rửa.");
                return;
            }

            StartCoroutine(WashCupRoutine(player));
        }

        private System.Collections.IEnumerator WashCupRoutine(Player.PlayerController player)
        {
            isWashing = true;
            canInteract = false;
            EventManager.TriggerInteractionPromptShow("Đang rửa ly...");

            yield return new WaitForSeconds(washDuration);

            CartItem.ResetCupState();
            CartItem.ReturnCleanCupToCart();

            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats != null)
            {
                stats.AddSupplies(0, 0, 1);
            }

            isWashing = false;
            canInteract = CartItem.HasCupToWash;
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã rửa sạch ly và đặt lại lên mặt bàn xe đẩy.");
        }
    }
}
