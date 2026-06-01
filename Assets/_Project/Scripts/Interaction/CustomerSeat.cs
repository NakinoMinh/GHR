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

        public void OccupySeat()
        {
            isOccupied = true;
            canInteract = false;
        }

        public void FreeSeat()
        {
            isOccupied = false;
            canInteract = true;
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            if (isOccupied) return;

            // Player ngồi nghỉ
            if (player.CurrentState != PlayerState.Sitting)
            {
                player.transform.position = transform.position;
                player.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                player.SetState(PlayerState.Sitting);
                promptText = "Nhấn E để đứng lên";
                // Cập nhật prompt ngay lập tức
                GanhHangRong.Core.EventManager.TriggerInteractionPromptShow(promptText);
            }
        }

        /// <summary>
        /// Được gọi từ PlayerController khi người chơi nhấn E để đứng dậy.
        /// </summary>
        public void OnPlayerStandUp(Player.PlayerController player)
        {
            player.SetState(PlayerState.Idle);
            promptText = "Ngồi nghỉ";
        }
    }
}
