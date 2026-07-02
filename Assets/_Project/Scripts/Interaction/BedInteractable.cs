using UnityEngine;
using GanhHangRong.Interaction;
using GanhHangRong.Economy;
using GanhHangRong.Player;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    public class BedInteractable : Interactable
    {
        private DayNightCycle timeManager;

        private void Start()
        {
            timeManager = FindAnyObjectByType<DayNightCycle>();
            promptText = "Đi ngủ tới 5h sáng";
        }

        private void Update()
        {
            if (timeManager != null)
            {
                canInteract = timeManager.CurrentTimeOfDay == TimeOfDay.Night || 
                              timeManager.CurrentTimeOfDay == TimeOfDay.LateNight;
            }
        }

        protected override void OnInteract(PlayerController player)
        {
            if (timeManager != null && canInteract)
            {
                // Skip to 5:00 AM
                timeManager.SkipToHour(5f);
                Debug.Log("Bạn đã đánh một giấc ngon lành tới sáng.");
            }
            else
            {
                Debug.Log("Chưa tới giờ ngủ!");
            }
        }
    }
}
