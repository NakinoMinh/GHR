using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Bồn rửa chén — người chơi mang ly dơ hoặc ly pha sai đến đây nhấn F để rửa.
    /// Sau khi rửa, 1 ly sạch được trả về kho đồ.
    /// </summary>
    public class DishwashingStation : Interactable
    {
        [Header("Bồn Rửa Chén")]
        [SerializeField] private float washDuration = 1.5f; // Thời gian hiệu ứng rửa

        private void Start()
        {
            promptText = "Nhấn F để rửa ly";
            interactionCooldown = 1.0f;
        }

        private void Update()
        {
            // Hiện gợi ý chỉ khi người chơi đang cầm ly cần rửa
            bool needsWash = CartItem.IsHoldingCup || CartItem.IsHoldingDirtyCup
                             || CartItem.HasPreparedTea || CartItem.HasPreparedCoffee;

            if (needsWash)
            {
                canInteract = true;
                if (CartItem.IsHoldingDirtyCup)
                    promptText = "Nhấn F để rửa ly dơ (+1 ly sạch)";
                else if (CartItem.IsHoldingCup || CartItem.HasPreparedTea || CartItem.HasPreparedCoffee)
                    promptText = "Nhấn F để đổ bỏ ly pha sai (+1 ly sạch)";
            }
            else
            {
                canInteract = false;
                promptText = string.Empty;
            }
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            CartItem.WashCup(stats);
        }
    }
}
