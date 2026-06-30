using UnityEngine;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Gắn script này vào GameObject nhà (Building_N_Foreground, Building_A1_Foreground...).
    /// Script tự động chạy khi Start() → xóa MeshCollider kín của building,
    /// thay bằng BoxColliders riêng cho từng tường, chừa lại khoảng trống tại cửa
    /// để nhân vật có thể đi vào bên trong.
    ///
    /// CÁCH DÙNG:
    ///   Không cần làm gì thủ công. Script này được tự động gắn vào building
    ///   qua Chapter1SceneBuilder hoặc Chapter2RuntimeBootstrap.
    ///   Nếu muốn gắn tay: Add Component → Building Walkthrough Collider
    /// </summary>
    [AddComponentMenu("GanhHangRong/Building Walkthrough Collider")]
    public class BuildingWalkthroughCollider : MonoBehaviour
    {
        [Header("Kích thước nhà (tự động tính nếu để 0)")]
        [Tooltip("Chiều rộng nhà theo trục X local (0 = tự tính từ MeshRenderer)")]
        public float buildingWidth = 0f;
        [Tooltip("Chiều sâu nhà theo trục Z local (0 = tự tính từ MeshRenderer)")]
        public float buildingDepth = 0f;
        [Tooltip("Chiều cao nhà (0 = tự tính từ MeshRenderer)")]
        public float buildingHeight = 0f;

        [Header("Cửa ra vào")]
        [Tooltip("Chiều rộng khoảng hở cửa (m)")]
        public float doorWidth = 1.8f;
        [Tooltip("Chiều cao cửa (m)")]
        public float doorHeight = 2.4f;
        [Tooltip("Tâm X của cửa tính từ tâm nhà (local space). 0 = giữa mặt tiền.")]
        public float doorOffsetX = 0f;
        [Tooltip("Tường nào là mặt tiền có cửa: Front=mặt tiền (Z-), Back=Z+, Left=X-, Right=X+")]
        public FacadeDirection doorFacade = FacadeDirection.Front;

        [Header("Debug")]
        public bool showGizmos = true;

        public enum FacadeDirection { Front, Back, Left, Right }

        private void Start()
        {
            SetupColliders();
        }

        public void SetupColliders()
        {
            // 1. Tính bounds thực tế của nhà
            Bounds bounds = CalculateBuildingBounds();

            float w = buildingWidth  > 0 ? buildingWidth  : bounds.size.x;
            float d = buildingDepth  > 0 ? buildingDepth  : bounds.size.z;
            float h = buildingHeight > 0 ? buildingHeight : bounds.size.y;

            // Center của nhà trong local space
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);

            float halfW = w * 0.5f;
            float halfD = d * 0.5f;
            float wallThick = 0.35f; // Độ dày tường (m)

            // 2. Xóa MeshCollider solid cũ của building (không xóa trigger)
            RemoveSolidMeshColliders();

            // 3. Tạo 4 tường + trần + sàn bằng BoxCollider
            //    Mỗi tường là 1 GameObject con riêng để dễ debug

            // --- Sàn ---
            AddWallBox("Wall_Floor", localCenter + Vector3.down * (h * 0.5f - wallThick * 0.5f),
                new Vector3(w, wallThick, d));

            // --- Trần ---
            AddWallBox("Wall_Ceiling", localCenter + Vector3.up * (h * 0.5f + wallThick * 0.5f),
                new Vector3(w + wallThick * 2f, wallThick, d + wallThick * 2f));

            // --- 4 tường xung quanh, chừa cửa ở mặt tiền ---
            switch (doorFacade)
            {
                case FacadeDirection.Front: // Cửa ở mặt Z-
                    BuildFrontWallWithDoor(localCenter, w, h, d, halfW, halfD, wallThick, isFrontZ: true);
                    // Tường hậu Z+
                    AddWallBox("Wall_Back",
                        localCenter + Vector3.forward * (halfD + wallThick * 0.5f),
                        new Vector3(w + wallThick * 2f, h, wallThick));
                    // Tường trái X-
                    AddWallBox("Wall_Left",
                        localCenter + Vector3.left * (halfW + wallThick * 0.5f),
                        new Vector3(wallThick, h, d));
                    // Tường phải X+
                    AddWallBox("Wall_Right",
                        localCenter + Vector3.right * (halfW + wallThick * 0.5f),
                        new Vector3(wallThick, h, d));
                    break;

                case FacadeDirection.Back: // Cửa ở mặt Z+
                    AddWallBox("Wall_Front",
                        localCenter + Vector3.back * (halfD + wallThick * 0.5f),
                        new Vector3(w + wallThick * 2f, h, wallThick));
                    BuildFrontWallWithDoor(localCenter, w, h, d, halfW, halfD, wallThick, isFrontZ: false);
                    AddWallBox("Wall_Left",
                        localCenter + Vector3.left * (halfW + wallThick * 0.5f),
                        new Vector3(wallThick, h, d));
                    AddWallBox("Wall_Right",
                        localCenter + Vector3.right * (halfW + wallThick * 0.5f),
                        new Vector3(wallThick, h, d));
                    break;

                case FacadeDirection.Left: // Cửa ở mặt X-
                    AddWallBox("Wall_Right",
                        localCenter + Vector3.right * (halfW + wallThick * 0.5f),
                        new Vector3(wallThick, h, d));
                    BuildSideWallWithDoor(localCenter, w, h, d, halfW, halfD, wallThick, isNegX: true);
                    AddWallBox("Wall_Front",
                        localCenter + Vector3.back * (halfD + wallThick * 0.5f),
                        new Vector3(w, h, wallThick));
                    AddWallBox("Wall_Back",
                        localCenter + Vector3.forward * (halfD + wallThick * 0.5f),
                        new Vector3(w, h, wallThick));
                    break;

                case FacadeDirection.Right: // Cửa ở mặt X+
                    AddWallBox("Wall_Left",
                        localCenter + Vector3.left * (halfW + wallThick * 0.5f),
                        new Vector3(wallThick, h, d));
                    BuildSideWallWithDoor(localCenter, w, h, d, halfW, halfD, wallThick, isNegX: false);
                    AddWallBox("Wall_Front",
                        localCenter + Vector3.back * (halfD + wallThick * 0.5f),
                        new Vector3(w, h, wallThick));
                    AddWallBox("Wall_Back",
                        localCenter + Vector3.forward * (halfD + wallThick * 0.5f),
                        new Vector3(w, h, wallThick));
                    break;
            }
        }

        // Tạo mặt tiền có lỗ cửa: chia thành 3 mảnh (trái cửa, trên cửa, phải cửa)
        private void BuildFrontWallWithDoor(Vector3 center, float w, float h, float d,
            float halfW, float halfD, float wt, bool isFrontZ)
        {
            float zSign = isFrontZ ? -1f : 1f;
            Vector3 wallCenter = center + Vector3.forward * zSign * (halfD + wt * 0.5f);

            float halfDoor = doorWidth * 0.5f;
            float leftWidth  = (halfW + doorOffsetX) - halfDoor;  // phần tường bên trái cửa
            float rightWidth = (halfW - doorOffsetX) - halfDoor;  // phần tường bên phải cửa
            float aboveHeight = h - doorHeight;                    // phần tường trên cửa

            // Trái cửa
            if (leftWidth > 0.05f)
                AddWallBox("Wall_Front_Left",
                    wallCenter + Vector3.left * (halfW - leftWidth * 0.5f + doorOffsetX),
                    new Vector3(leftWidth, h, wt));

            // Phải cửa
            if (rightWidth > 0.05f)
                AddWallBox("Wall_Front_Right",
                    wallCenter + Vector3.right * (halfW - rightWidth * 0.5f - doorOffsetX),
                    new Vector3(rightWidth, h, wt));

            // Trên cửa (lintel)
            if (aboveHeight > 0.05f)
                AddWallBox("Wall_Front_Above",
                    wallCenter + Vector3.up * (h * 0.5f - aboveHeight * 0.5f)
                               + Vector3.right * doorOffsetX,
                    new Vector3(doorWidth, aboveHeight, wt));
        }

        private void BuildSideWallWithDoor(Vector3 center, float w, float h, float d,
            float halfW, float halfD, float wt, bool isNegX)
        {
            float xSign = isNegX ? -1f : 1f;
            Vector3 wallCenter = center + Vector3.right * xSign * (halfW + wt * 0.5f);

            float halfDoor = doorWidth * 0.5f;
            float frontWidth  = (halfD + doorOffsetX) - halfDoor;
            float backWidth   = (halfD - doorOffsetX) - halfDoor;
            float aboveHeight = h - doorHeight;

            if (frontWidth > 0.05f)
                AddWallBox("Wall_Side_Front",
                    wallCenter + Vector3.back * (halfD - frontWidth * 0.5f + doorOffsetX),
                    new Vector3(wt, h, frontWidth));

            if (backWidth > 0.05f)
                AddWallBox("Wall_Side_Back",
                    wallCenter + Vector3.forward * (halfD - backWidth * 0.5f - doorOffsetX),
                    new Vector3(wt, h, backWidth));

            if (aboveHeight > 0.05f)
                AddWallBox("Wall_Side_Above",
                    wallCenter + Vector3.up * (h * 0.5f - aboveHeight * 0.5f)
                               + Vector3.forward * doorOffsetX,
                    new Vector3(wt, aboveHeight, doorWidth));
        }

        private void AddWallBox(string wallName, Vector3 localPos, Vector3 size)
        {
            // Tìm existing child có tên này để tránh duplicate khi gọi lại
            Transform existing = transform.Find("__Walls__/" + wallName);
            if (existing != null) Destroy(existing.gameObject);

            // Container cha cho tất cả tường
            Transform wallsContainer = transform.Find("__Walls__");
            if (wallsContainer == null)
            {
                var go = new GameObject("__Walls__");
                go.transform.SetParent(transform, false);
                wallsContainer = go.transform;
            }

            var wallGO = new GameObject(wallName);
            wallGO.transform.SetParent(wallsContainer, false);
            wallGO.transform.localPosition = localPos;
            wallGO.transform.localRotation = Quaternion.identity;

            var bc = wallGO.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = false;
        }

        private void RemoveSolidMeshColliders()
        {
            // Xóa tất cả MeshCollider NON-trigger trên toàn bộ building hierarchy
            foreach (var mc in GetComponentsInChildren<MeshCollider>())
            {
                if (!mc.isTrigger)
                    Destroy(mc);
            }
            // Xóa luôn BoxCollider solid gốc nếu có (do SceneBuilder thêm vào)
            foreach (var bc in GetComponents<BoxCollider>())
            {
                if (!bc.isTrigger)
                    Destroy(bc);
            }
        }

        private Bounds CalculateBuildingBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(transform.position, new Vector3(8f, 6f, 8f));

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // Vẽ khoảng hở cửa màu xanh lá để dễ debug
            Bounds b = CalculateBuildingBounds();
            float h = buildingHeight > 0 ? buildingHeight : b.size.y;
            float halfD = (buildingDepth > 0 ? buildingDepth : b.size.z) * 0.5f;

            Vector3 doorPos = transform.position;
            switch (doorFacade)
            {
                case FacadeDirection.Front:
                    doorPos += transform.forward * -halfD + transform.right * doorOffsetX
                             + transform.up * doorHeight * 0.5f;
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(doorPos, new Vector3(doorWidth, doorHeight, 0.5f));
                    break;
                case FacadeDirection.Back:
                    doorPos += transform.forward * halfD + transform.right * doorOffsetX
                             + transform.up * doorHeight * 0.5f;
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(doorPos, new Vector3(doorWidth, doorHeight, 0.5f));
                    break;
            }

            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}
