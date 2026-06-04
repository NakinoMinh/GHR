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

        protected abstract void OnInteract(Player.PlayerController player);
    }
}
