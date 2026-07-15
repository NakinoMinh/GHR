using GanhHangRong.Core;
using GanhHangRong.Systems;
using TMPro;
using UnityEngine;

namespace GanhHangRong.Interaction
{
    [DisallowMultipleComponent]
    public class ShopClosingPoint : Interactable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Color inactiveColor = new Color(0.28f, 0.16f, 0.08f, 1f);
        [SerializeField] private Color openColor = new Color(0.16f, 0.48f, 0.3f, 1f);
        [SerializeField] private Color closedLabelColor = new Color(1f, 0.84f, 0.56f, 1f);
        [SerializeField] private Color openLabelColor = new Color(0.9f, 1f, 0.88f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private bool wasAvailable;
        private bool wasOpen;

        private void Awake()
        {
            promptText = "Nhấn F để đóng quán";
            if (indicatorRenderer == null)
            {
                indicatorRenderer = GetComponent<Renderer>();
            }

            if (statusLabel == null)
            {
                statusLabel = GetComponentInChildren<TMP_Text>(true);
            }

            propertyBlock = new MaterialPropertyBlock();
            RefreshState(true);
        }

        private void OnEnable()
        {
            EventManager.OnBusinessDayPhaseChanged += HandleBusinessDayPhaseChanged;
            RefreshState(true);
        }

        private void OnDisable()
        {
            EventManager.OnBusinessDayPhaseChanged -= HandleBusinessDayPhaseChanged;
        }

        private void Update()
        {
            RefreshState(false);
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            if (!BusinessDayController.HasInstance) return;
            BusinessDayController.Instance.TryCloseShopAtClosingPoint();
        }

        private void RefreshState(bool force)
        {
            bool isOpen = BusinessDayController.HasInstance &&
                BusinessDayController.Instance.IsManagingGameLoop &&
                BusinessDayController.Instance.CurrentPhase == BusinessDayPhase.Trading;
            bool available = BusinessDayController.HasInstance &&
                BusinessDayController.Instance.CanCloseAtClosingPoint;
            canInteract = available;

            if (!force && available == wasAvailable && isOpen == wasOpen) return;
            wasAvailable = available;
            wasOpen = isOpen;

            SetIndicatorColor(isOpen ? openColor : inactiveColor);
            if (statusLabel != null)
            {
                statusLabel.text = isOpen ? "MỞ QUÁN" : "ĐÓNG QUÁN";
                statusLabel.color = isOpen ? openLabelColor : closedLabelColor;
            }
        }

        private void HandleBusinessDayPhaseChanged(BusinessDayPhase phase)
        {
            RefreshState(true);
        }

        private void SetIndicatorColor(Color color)
        {
            if (indicatorRenderer == null) return;

            indicatorRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            indicatorRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
