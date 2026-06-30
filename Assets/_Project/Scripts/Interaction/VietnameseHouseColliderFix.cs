using UnityEngine;
using GanhHangRong.Interaction;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Tự động tắt các collider vật lý cản đường của nhà MoHinhNhaVietNam_New
    /// Hoạt động hoàn toàn tự động khi vào game (không cần gán script vào object nào)
    /// Đồng thời tự động gắn script DoorInteractable vào các cánh cửa để có thể click mở/đóng.
    /// </summary>
    public static class VietnameseHouseColliderFixAuto
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoFixColliders()
        {
            var house = GameObject.Find("MoHinhNhaVietNam_New");
            if (house == null) return;

            var allColliders = house.GetComponentsInChildren<Collider>(true);
            int disableCount = 0;
            foreach (var c in allColliders)
            {
                if (c.isTrigger) continue; // Bỏ qua trigger

                string n = c.gameObject.name.ToLower();
                // Không tắt collider của door, để DoorInteractable tự quản lý
                if (n.StartsWith("cutout") || n.StartsWith("column") || n == "path")
                {
                    c.enabled = false;
                    disableCount++;
                }
            }
            Debug.Log($"[HouseFix] Đã tự động tắt {disableCount} colliders vật lý chặn lối đi (giữ lại triggers).");

            // Tự động setup DoorInteractable cho các cửa xanh
            int doorCount = 0;
            foreach (Transform t in house.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLower();
                if (n.StartsWith("door_l") || n.StartsWith("door_r"))
                {
                    if (t.GetComponent<DoorInteractable>() == null)
                    {
                        var doorScript = t.gameObject.AddComponent<DoorInteractable>();
                        
                        // Cấu hình góc mở tùy bên trái hay phải
                        doorScript.SetOpenAngle(n.StartsWith("door_l") ? 90f : -90f);

                        var col = t.GetComponent<BoxCollider>();
                        if (col == null)
                        {
                            col = t.gameObject.AddComponent<BoxCollider>();
                            var meshFilter = t.GetComponent<MeshFilter>();
                            if (meshFilter != null && meshFilter.sharedMesh != null)
                            {
                                col.center = meshFilter.sharedMesh.bounds.center;
                                col.size = meshFilter.sharedMesh.bounds.size;
                            }
                        }
                        col.isTrigger = true; // Set trigger để click trúng
                        col.enabled = true; // ĐẢM BẢO luôn bật để tương tác
                        doorCount++;
                    }
                }
            }
            if (doorCount > 0)
            {
                Debug.Log($"[HouseFix] Đã tự động gắn DoorInteractable cho {doorCount} cánh cửa.");
            }
        }
    }
}
