using GanhHangRong.Core;
using GanhHangRong.Player;
using UnityEngine;

namespace GanhHangRong.Interaction
{
    public class Chapter2FoodItem : MonoBehaviour
    {
        [SerializeField] private int orderId = ChapterOrderCatalog.BanhMiMuoiOt;
        [SerializeField] private string itemName = "Bánh mì nướng muối ớt";
        [SerializeField] private Color highlightColor = new Color(1f, 0.7f, 0.25f, 1f);

        private Renderer[] renderers;
        private Color[] originalColors;
        private bool isHighlighted;

        public int OrderId => orderId;
        public string ItemName => string.IsNullOrWhiteSpace(itemName)
            ? ChapterOrderCatalog.GetOrderName(orderId)
            : itemName;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    originalColors[i] = renderers[i].material.color;
                }
            }

            if (GetComponent<Collider>() == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                FitColliderToRenderers(box);
            }
        }

        public void Configure(int newOrderId, string newItemName)
        {
            orderId = newOrderId;
            itemName = newItemName;
            name = "FoodItem_" + ItemName;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted) return;
            isHighlighted = highlighted;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer current = renderers[i];
                if (current == null || current.material == null) continue;

                if (highlighted)
                {
                    current.material.color = Color.Lerp(originalColors[i], highlightColor, 0.45f);
                    current.material.EnableKeyword("_EMISSION");
                    current.material.SetColor("_EmissionColor", highlightColor * 0.25f);
                }
                else
                {
                    current.material.color = originalColors[i];
                    current.material.DisableKeyword("_EMISSION");
                    current.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        public void OnItemClicked(PlayerController player)
        {
            if (player == null) return;

            if (CartItem.HasPreparedTea)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Mình đang có món đã chuẩn bị. Phục vụ khách trước rồi làm món tiếp theo.");
                return;
            }

            CartItem.PrepareReadyOrder(orderId);
            EventManager.TriggerInteractionPromptShow(ChapterOrderCatalog.GetPrepareFeedback(orderId));
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã chuẩn bị {ItemName}. Nhấn Space để phục vụ khách đang gọi món.");
        }

        private void FitColliderToRenderers(BoxCollider box)
        {
            if (renderers == null || renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!hasBounds) return;

            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = transform.InverseTransformVector(bounds.size);
            box.center = localCenter;
            box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }
    }
}
