using UnityEngine;
using UnityEditor;
using GanhHangRong.Interaction;

namespace GanhHangRong.Editor
{
    /// <summary>
    /// Editor tool — Tự động gán CartItem component lên các FBX models trên xe đẩy.
    /// Menu: GanhHangRong > Setup Cart Items
    /// </summary>
    public class SetupCartItems : EditorWindow
    {
        [MenuItem("GanhHangRong/Setup Cart Items")]
        public static void ShowWindow()
        {
            GetWindow<SetupCartItems>("Setup Cart Items");
        }

        private void OnGUI()
        {
            GUILayout.Label("Setup Vật Phẩm Trên Xe Đẩy", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Công cụ này sẽ tìm các FBX model trên xe đẩy và tự động thêm component CartItem.\n\n" +
                "Cách sử dụng:\n" +
                "1. Đặt các model FBX lên mặt bàn xe đẩy trong Scene\n" +
                "2. Nhấn nút 'Auto Setup' bên dưới\n" +
                "3. Hoặc chọn từng GameObject và nhấn 'Setup Selected'\n\n" +
                "Các model cần setup:\n" +
                "• amdunnuoc.fbx → Ấm đun nước\n" +
                "• Meshy_AI_Red_Vintage_Tea_Tin → Bình trà\n" +
                "• Meshy_AI_Vietnam_Sugarcane_Jui → Gánh nước mía",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Auto Setup — Tìm và gán tự động", GUILayout.Height(35)))
            {
                AutoSetupCartItems();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Setup Selected — Gán cho đối tượng đang chọn", GUILayout.Height(30)))
            {
                SetupSelectedObjects();
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Sau khi setup, kiểm tra trong Inspector:\n" +
                "• CartItem component đã được thêm\n" +
                "• Item Type đúng loại\n" +
                "• Collider đã có để raycast phát hiện",
                MessageType.Warning);
        }

        private static void AutoSetupCartItems()
        {
            int setupCount = 0;

            // Tìm tất cả GameObject trong Scene
            var allObjects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);

            foreach (var obj in allObjects)
            {
                string name = obj.name.ToLower();

                if (name.Contains("amdunnuoc") || name.Contains("am_dun_nuoc"))
                {
                    SetupItem(obj.gameObject, CartItem.CartItemType.WaterKettle, "Ấm Đun Nước", "Ấm nước nóng để pha trà.");
                    setupCount++;
                }
                else if (name.Contains("red_vintage_tea_tin") || name.Contains("binhtra") || name.Contains("tea_tin"))
                {
                    SetupItem(obj.gameObject, CartItem.CartItemType.TeaTin, "Bình Trà", "Bình trà đỏ chứa lá trà.");
                    setupCount++;
                }
                else if (name.Contains("sugarcane") || name.Contains("ganhnuoc") || name.Contains("nuoc_mia"))
                {
                    SetupItem(obj.gameObject, CartItem.CartItemType.SugarcaneJuice, "Gánh Nước Mía", "Nước mía ngọt mát.");
                    setupCount++;
                }
            }

            if (setupCount > 0)
            {
                Debug.Log($"[SetupCartItems] Đã setup {setupCount} vật phẩm trên xe đẩy.");
                EditorUtility.DisplayDialog("Hoàn tất", $"Đã setup {setupCount} vật phẩm CartItem.", "OK");
            }
            else
            {
                Debug.LogWarning("[SetupCartItems] Không tìm thấy vật phẩm nào. Hãy đảm bảo các FBX model đã được đặt trong Scene.");
                EditorUtility.DisplayDialog("Không tìm thấy", "Không tìm thấy vật phẩm nào trong Scene.\nHãy đảm bảo các FBX model đã được đặt vào Scene.", "OK");
            }
        }

        private static void SetupSelectedObjects()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Chưa chọn đối tượng", "Hãy chọn một hoặc nhiều GameObject trong Hierarchy.", "OK");
                return;
            }

            int setupCount = 0;
            foreach (var obj in selected)
            {
                string name = obj.name.ToLower();
                CartItem.CartItemType type = CartItem.CartItemType.WaterKettle;
                string itemName = "Vật phẩm";
                string desc = "Mô tả";

                if (name.Contains("amdunnuoc") || name.Contains("am_dun_nuoc"))
                {
                    type = CartItem.CartItemType.WaterKettle;
                    itemName = "Ấm Đun Nước";
                    desc = "Ấm nước nóng để pha trà.";
                }
                else if (name.Contains("tea_tin") || name.Contains("binhtra"))
                {
                    type = CartItem.CartItemType.TeaTin;
                    itemName = "Bình Trà";
                    desc = "Bình trà đỏ chứa lá trà.";
                }
                else if (name.Contains("sugarcane") || name.Contains("ganhnuoc"))
                {
                    type = CartItem.CartItemType.SugarcaneJuice;
                    itemName = "Gánh Nước Mía";
                    desc = "Nước mía ngọt mát.";
                }

                SetupItem(obj, type, itemName, desc);
                setupCount++;
            }

            Debug.Log($"[SetupCartItems] Setup {setupCount} vật phẩm từ Selection.");
            EditorUtility.DisplayDialog("Hoàn tất", $"Đã setup {setupCount} vật phẩm.", "OK");
        }

        private static void SetupItem(GameObject obj, CartItem.CartItemType type, string itemName, string description)
        {
            // Thêm CartItem nếu chưa có
            var cartItem = obj.GetComponent<CartItem>();
            if (cartItem == null)
            {
                cartItem = Undo.AddComponent<CartItem>(obj);
            }

            // Set các thuộc tính qua SerializedObject
            var so = new SerializedObject(cartItem);
            so.FindProperty("itemType").enumValueIndex = (int)type;
            so.FindProperty("itemName").stringValue = itemName;
            so.FindProperty("itemDescription").stringValue = description;
            so.ApplyModifiedProperties();

            // Đảm bảo có collider
            if (obj.GetComponent<Collider>() == null)
            {
                var col = Undo.AddComponent<BoxCollider>(obj);
                var meshFilter = obj.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    col.center = meshFilter.sharedMesh.bounds.center;
                    col.size = meshFilter.sharedMesh.bounds.size;
                }
            }

            EditorUtility.SetDirty(obj);
            Debug.Log($"[SetupCartItems] Setup: {obj.name} → {itemName} ({type})");
        }
    }
}
