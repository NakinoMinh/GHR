using System;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using GanhHangRong.Core;
using GanhHangRong.UI;

namespace GanhHangRong.Economy
{
    [Serializable]
    public class ShopOpenEvent : UnityEvent<ShopData, ShopInteractable>
    {
    }

    public class ShopOpenRequest
    {
        public ShopData ShopData { get; }
        public ShopInteractable Source { get; }

        public ShopOpenRequest(ShopData shopData, ShopInteractable source)
        {
            ShopData = shopData;
            Source = source;
        }
    }

    [RequireComponent(typeof(Collider))]
    public class ShopInteractable : MonoBehaviour
    {
        public static event Action<ShopData, ShopInteractable> ShopOpenRequested;

        [Header("Dữ liệu quầy")]
        [SerializeField] private ShopData shopData;

        [Header("Prompt")]
        [SerializeField] private PlayerInteractionPrompt prompt;
        [SerializeField] private string promptMessage = "Nhấn F để mua hàng";

        [Header("Hook cho UI ở phần 2")]
        [SerializeField] private ShopOpenEvent onShopOpenRequested = new ShopOpenEvent();
        [SerializeField] private GameObject shopUIReceiver;
        [SerializeField] private string receiverMethodName = "OpenShop";

        private bool playerInside;
        private GameObject currentPlayer;

        public ShopData ShopData => shopData;
        public bool PlayerInside => playerInside;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (!playerInside)
            {
                return;
            }

            if (WasInteractPressed())
            {
                TryOpenShop();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInside = true;
            currentPlayer = other.gameObject;
            ShowPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (currentPlayer != null && other.gameObject != currentPlayer)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            playerInside = false;
            currentPlayer = null;
            HidePrompt();
        }

        public void TryOpenShop()
        {
            if (!playerInside)
            {
                Debug.Log("Không mở shop vì player chưa ở trong vùng trigger.", this);
                return;
            }

            if (shopData == null)
            {
                Debug.LogWarning("ShopInteractable thiếu ShopData.", this);
                return;
            }

            ShopOpenRequested?.Invoke(shopData, this);
            onShopOpenRequested?.Invoke(shopData, this);

            if (shopUIReceiver != null && !string.IsNullOrWhiteSpace(receiverMethodName))
            {
                shopUIReceiver.SendMessage(
                    receiverMethodName,
                    new ShopOpenRequest(shopData, this),
                    SendMessageOptions.DontRequireReceiver);
            }
        }

        private void ShowPrompt()
        {
            string shopName = shopData != null ? shopData.DisplayName : string.Empty;

            if (prompt != null)
            {
                if (string.IsNullOrWhiteSpace(shopName))
                {
                    prompt.ShowMessage(promptMessage);
                }
                else
                {
                    prompt.Show(shopName);
                }

                return;
            }

            EventManager.TriggerInteractionPromptShow(promptMessage);
        }

        private void HidePrompt()
        {
            if (prompt != null)
            {
                prompt.Hide();
                return;
            }

            EventManager.TriggerInteractionPromptHide();
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            try
            {
                return other.CompareTag(Constants.TAG_PLAYER);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"Tag '{Constants.TAG_PLAYER}' chưa tồn tại. Hãy tạo tag Player và gắn vào nhân vật.");
                return false;
            }
        }

        private static bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }
    }
}
