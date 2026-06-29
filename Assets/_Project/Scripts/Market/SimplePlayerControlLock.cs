using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Player
{
    public class SimplePlayerControlLock : MonoBehaviour
    {
        [Header("Component movement cần bật/tắt")]
        [SerializeField] private List<Behaviour> movementComponents = new List<Behaviour>();

        [Header("Cursor")]
        [SerializeField] private bool unlockCursorWhenLocked = true;
        [SerializeField] private bool lockCursorWhenUnlocked = true;

        private bool isLocked;

        public bool IsLocked => isLocked;

        public void LockControls()
        {
            SetControlsLocked(true);
        }

        public void UnlockControls()
        {
            SetControlsLocked(false);
        }

        public void SetControlsLocked(bool locked)
        {
            isLocked = locked;

            foreach (Behaviour component in movementComponents)
            {
                if (component == null)
                {
                    continue;
                }

                component.enabled = !locked;
            }

            if (locked && unlockCursorWhenLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!locked && lockCursorWhenUnlocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void AddMovementComponent(Behaviour component)
        {
            if (component == null || movementComponents.Contains(component))
            {
                return;
            }

            movementComponents.Add(component);
        }
    }
}
