using GanhHangRong.Systems;
using UnityEngine;

namespace GanhHangRong.Interaction
{
    [DisallowMultipleComponent]
    public class ShopClosingPoint : Interactable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.28f, 0.16f, 0.08f, 1f);
        [SerializeField] private Color availableColor = new Color(0.2f, 0.65f, 0.3f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private bool wasAvailable;

        private void Awake()
        {
            promptText = "Nhấn F để đóng quán";
            if (indicatorRenderer == null)
            {
                indicatorRenderer = GetComponentInChildren<Renderer>();
            }

            propertyBlock = new MaterialPropertyBlock();
            RefreshState(true);
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
            bool available = BusinessDayController.HasInstance &&
                BusinessDayController.Instance.CanCloseAtClosingPoint;
            canInteract = available;

            if (!force && available == wasAvailable) return;
            wasAvailable = available;
            SetIndicatorColor(available ? availableColor : inactiveColor);
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
