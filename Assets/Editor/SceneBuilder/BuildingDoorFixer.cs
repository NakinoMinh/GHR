using UnityEngine;
using UnityEditor;
using GanhHangRong.Interaction;

namespace GanhHangRong.Editor
{
    /// <summary>
    /// Tool để fix lỗi nhân vật bị kẹt trước cửa nhà.
    ///
    /// CÁCH SỬ DỤNG:
    ///   Menu Unity → Gánh Hàng Rong → Fix Cửa Nhà (Building Door)
    ///
    /// Tool này sẽ:
    ///   1. Tìm tất cả các building trong scene (Building_N_Foreground, Building_A1_Foreground...)
    ///   2. Với mỗi building: xóa các MeshCollider solid, thay bằng BoxCollider vừa đủ bao quanh
    ///   3. Tìm GameObject con có tên chứa "door" / "cua" / "gate" → gán DoorInteractable
    ///   4. Thêm Trigger collider vào cửa để detect tương tác
    /// </summary>
    public static class BuildingDoorFixer
    {
        [MenuItem("Gánh Hàng Rong/Fix Cửa Nhà (Building Door)", false, 20)]
        public static void FixBuildingDoors()
        {
            int buildingsFixed = 0;
            int doorsSetup = 0;

            // Tìm tất cả GameObject có tên chứa "Building" trong scene hiện tại
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var go in allObjects)
            {
                string nameLower = go.name.ToLower();
                bool isBuilding = nameLower.Contains("building") || nameLower.Contains("house") || nameLower.Contains("nha");
                if (!isBuilding) continue;

                Debug.Log($"[BuildingDoorFixer] Đang xử lý building: {go.name}");

                // --- Bước 1: Xóa tất cả MeshCollider solid trong building ---
                // Thay bằng BoxCollider bao quanh toàn bộ building để player không bị "stuck" vào mesh chi tiết
                var meshColliders = go.GetComponentsInChildren<MeshCollider>();
                Bounds buildingBounds = CalculateBounds(go);

                foreach (var mc in meshColliders)
                {
                    if (!mc.isTrigger)
                    {
                        Undo.DestroyObjectImmediate(mc);
                    }
                }

                // Thêm một BoxCollider duy nhất bao toàn bộ building (làm tường/sàn/trần)
                // Nhưng KHÔNG đặt ở vị trí cửa
                // → Cách đơn giản nhất: chỉ để BoxCollider gốc, player đi vào qua khoảng hở
                // Nếu building chưa có BoxCollider gốc thì thêm vào
                if (go.GetComponent<BoxCollider>() == null)
                {
                    var bc = Undo.AddComponent<BoxCollider>(go);
                    if (buildingBounds.size != Vector3.zero)
                    {
                        bc.center = go.transform.InverseTransformPoint(buildingBounds.center);
                        bc.size = buildingBounds.size;
                    }
                    Debug.Log($"[BuildingDoorFixer] Đã thêm BoxCollider tổng thể cho {go.name}");
                }

                buildingsFixed++;

                // --- Bước 2: Tìm và setup DoorInteractable cho cửa con ---
                Transform[] children = go.GetComponentsInChildren<Transform>();
                foreach (var child in children)
                {
                    if (child == go.transform) continue;

                    string childName = child.name.ToLower();
                    bool isDoor = childName.Contains("door") || childName.Contains("cua")
                                  || childName.Contains("gate") || childName.Contains("shutter")
                                  || childName.Contains("canh");

                    if (!isDoor) continue;

                    // Đã có DoorInteractable rồi thì bỏ qua
                    if (child.GetComponent<DoorInteractable>() != null) continue;

                    Debug.Log($"[BuildingDoorFixer] Tìm thấy cửa: {child.name} trong {go.name}");

                    // Xóa MeshCollider solid trên cánh cửa
                    var doorMeshColliders = child.GetComponents<MeshCollider>();
                    foreach (var mc in doorMeshColliders)
                    {
                        if (!mc.isTrigger) Undo.DestroyObjectImmediate(mc);
                    }

                    // Tính bounds của cánh cửa
                    Bounds doorBounds = CalculateBounds(child.gameObject);
                    Vector3 doorLocalCenter = child.InverseTransformPoint(doorBounds.center);
                    Vector3 doorSize = doorBounds.size;
                    if (doorSize == Vector3.zero) doorSize = new Vector3(1f, 2f, 0.1f);

                    // 1. BoxCollider NON-trigger: block vật lý khi cửa đóng
                    var physicsCollider = child.GetComponent<BoxCollider>();
                    if (physicsCollider == null)
                    {
                        physicsCollider = Undo.AddComponent<BoxCollider>(child.gameObject);
                        physicsCollider.center = doorLocalCenter;
                        physicsCollider.size = doorSize;
                        physicsCollider.isTrigger = false;
                    }

                    // 2. BoxCollider IS-trigger: để raycast/OverlapSphere detect
                    //    Tạo child GameObject riêng để chứa trigger collider
                    GameObject triggerGO = new GameObject("DoorTrigger");
                    Undo.RegisterCreatedObjectUndo(triggerGO, "Create DoorTrigger");
                    triggerGO.transform.SetParent(child);
                    triggerGO.transform.localPosition = Vector3.zero;
                    triggerGO.transform.localRotation = Quaternion.identity;
                    triggerGO.transform.localScale = Vector3.one;

                    var triggerCol = Undo.AddComponent<BoxCollider>(triggerGO);
                    triggerCol.center = doorLocalCenter;
                    triggerCol.size = doorSize * 1.2f; // Trigger lớn hơn một chút
                    triggerCol.isTrigger = true;

                    // 3. Gắn DoorInteractable vào cánh cửa
                    var door = Undo.AddComponent<DoorInteractable>(child.gameObject);

                    // Tự động phát hiện hướng mở: cửa quay theo Y
                    // SerializedObject để set private fields
                    var so = new SerializedObject(door);
                    so.FindProperty("openAngle").floatValue = 90f;
                    so.FindProperty("isOpen").boolValue = false;
                    so.FindProperty("canInteract").boolValue = true;
                    so.FindProperty("promptText").stringValue = "Nhấn F để mở cửa";
                    so.ApplyModifiedPropertiesWithoutUndo();

                    doorsSetup++;
                    Debug.Log($"[BuildingDoorFixer] ✅ Đã setup DoorInteractable cho: {child.name}");
                }
            }

            if (buildingsFixed == 0)
            {
                // Nếu không tìm thấy building, thử tìm theo tag hoặc hiển thị hướng dẫn thủ công
                Debug.LogWarning("[BuildingDoorFixer] Không tìm thấy building nào! " +
                    "Hãy đặt tên GameObject nhà có chứa từ 'Building', 'House', hoặc 'Nha'.");
                EditorUtility.DisplayDialog("Building Door Fixer",
                    "Không tìm thấy building nào trong scene!\n\n" +
                    "Đổi tên GameObject nhà thành có chứa 'Building' hoặc 'House',\n" +
                    "sau đó chạy lại tool này.",
                    "OK");
            }
            else
            {
                string msg = $"Đã fix {buildingsFixed} building(s).\nĐã setup {doorsSetup} cửa (DoorInteractable).\n\n";
                if (doorsSetup == 0)
                {
                    msg += "⚠️ Không tìm thấy cửa nào tự động!\n" +
                           "Hãy tự tay gán DoorInteractable:\n" +
                           "1. Chọn GameObject cánh cửa trong Hierarchy\n" +
                           "2. Add Component → Door Interactable\n" +
                           "3. Thêm BoxCollider (non-trigger) + BoxCollider (trigger)";
                }
                else
                {
                    msg += "✅ Nhấn Play để kiểm tra — nhân vật có thể đi vào nhà bằng phím F!";
                }

                EditorUtility.DisplayDialog("Building Door Fixer", msg, "OK");
                Debug.Log($"[BuildingDoorFixer] Hoàn tất! Buildings: {buildingsFixed}, Cửa: {doorsSetup}");
            }
        }

        /// <summary>
        /// Tool thủ công: Chọn GameObject cánh cửa trong Hierarchy rồi chạy.
        /// Sẽ setup DoorInteractable ngay cho đối tượng được chọn.
        /// </summary>
        [MenuItem("Gánh Hàng Rong/Setup DoorInteractable (Chọn cánh cửa trước)", false, 21)]
        public static void SetupSelectedDoor()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Setup Door", "Hãy chọn GameObject cánh cửa trong Hierarchy trước!", "OK");
                return;
            }

            // Xóa MeshCollider solid cũ
            foreach (var mc in selected.GetComponents<MeshCollider>())
            {
                if (!mc.isTrigger) Undo.DestroyObjectImmediate(mc);
            }

            // Tính bounds cánh cửa
            Bounds b = CalculateBounds(selected);
            Vector3 localCenter = selected.transform.InverseTransformPoint(b.center);
            Vector3 size = b.size;
            if (size == Vector3.zero) size = new Vector3(1f, 2f, 0.1f);

            // BoxCollider NON-trigger (block vật lý)
            BoxCollider physics = selected.GetComponent<BoxCollider>();
            if (physics == null) physics = Undo.AddComponent<BoxCollider>(selected);
            physics.isTrigger = false;
            physics.center = localCenter;
            physics.size = size;

            // BoxCollider IS-trigger (detect tương tác)
            // Kiểm tra xem đã có trigger child chưa
            Transform triggerChild = selected.transform.Find("DoorTrigger");
            if (triggerChild == null)
            {
                GameObject tgo = new GameObject("DoorTrigger");
                Undo.RegisterCreatedObjectUndo(tgo, "Create DoorTrigger");
                tgo.transform.SetParent(selected.transform);
                tgo.transform.localPosition = Vector3.zero;
                tgo.transform.localRotation = Quaternion.identity;
                tgo.transform.localScale = Vector3.one;
                var tc = Undo.AddComponent<BoxCollider>(tgo);
                tc.isTrigger = true;
                tc.center = localCenter;
                tc.size = size * 1.5f;
            }

            // DoorInteractable
            if (selected.GetComponent<DoorInteractable>() == null)
            {
                var door = Undo.AddComponent<DoorInteractable>(selected);
                var so = new SerializedObject(door);
                so.FindProperty("openAngle").floatValue = 90f;
                so.FindProperty("isOpen").boolValue = false;
                so.FindProperty("canInteract").boolValue = true;
                so.FindProperty("promptText").stringValue = "Nhấn F để mở cửa";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.DisplayDialog("Setup Door",
                $"✅ Đã setup DoorInteractable cho: {selected.name}\n\n" +
                "Kiểm tra trong Inspector:\n" +
                "• BoxCollider (non-trigger) → block vật lý\n" +
                "• DoorTrigger/BoxCollider (trigger) → detect F\n" +
                "• DoorInteractable → mở/đóng cửa\n\n" +
                "Điều chỉnh openAngle nếu cửa mở sai chiều!",
                "OK");

            Selection.activeGameObject = selected;
        }

        [MenuItem("Gánh Hàng Rong/Setup DoorInteractable (Chọn cánh cửa trước)", true)]
        private static bool SetupSelectedDoorValidate() => Selection.activeGameObject != null;

        private static Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
