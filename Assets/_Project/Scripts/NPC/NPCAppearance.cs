using UnityEngine;

namespace GanhHangRong.NPC
{
    /// <summary>
    /// Thay đổi ngoại hình NPC dựa trên profile (tint màu, bật tắt phụ kiện...).
    /// </summary>
    public class NPCAppearance : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderersToTint;

        public void ApplyProfile(NPCProfile profile)
        {
            if (profile == null) return;

            // Đổi màu (Tint)
            if (profile.possibleColorTints != null && profile.possibleColorTints.Length > 0 && renderersToTint != null)
            {
                Color tint = profile.possibleColorTints[Random.Range(0, profile.possibleColorTints.Length)];
                foreach (var renderer in renderersToTint)
                {
                    if (renderer != null)
                    {
                        renderer.color = tint;
                    }
                }
            }
        }
    }
}
