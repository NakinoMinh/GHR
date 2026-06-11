using UnityEngine;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Lớp cơ sở cho mọi vật thể có thể tương tác.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected string promptText = "Nhấn F để tương tác";
        [SerializeField] protected bool canInteract = true;
        [SerializeField] protected float interactionCooldown = 0.5f;

        protected float lastInteractTime;

        public string PromptText => promptText;
        
        public bool CanInteract 
        {
            get { return canInteract && (Time.time - lastInteractTime >= interactionCooldown); }
        }

        public void Interact(Player.PlayerController player)
        {
            if (CanInteract)
            {
                lastInteractTime = Time.time;
                OnInteract(player);
            }
        }

        public void InteractE(Player.PlayerController player)
        {
            if (CanInteract)
            {
                lastInteractTime = Time.time;
                OnInteractE(player);
            }
        }

        public void InteractQ(Player.PlayerController player)
        {
            if (CanInteract)
            {
                lastInteractTime = Time.time;
                OnInteractQ(player);
            }
        }

        public void InteractR(Player.PlayerController player)
        {
            if (CanInteract)
            {
                lastInteractTime = Time.time;
                OnInteractR(player);
            }
        }

        protected virtual void OnInteract(Player.PlayerController player) { }
        protected virtual void OnInteractE(Player.PlayerController player) { }
        protected virtual void OnInteractQ(Player.PlayerController player) { }
        protected virtual void OnInteractR(Player.PlayerController player) { }
    }
}
