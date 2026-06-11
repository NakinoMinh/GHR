using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using GanhHangRong.UI;

namespace GanhHangRong.Editor
{
    public class DialogueUIBuilder
    {
        [MenuItem("GanhHangRong/Tạo Tự Động Giao Diện Hội Thoại (Dialogue UI)")]
        public static void BuildDialogueUI()
        {
            // 1. Tìm Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Lỗi: Không tìm thấy Canvas nào trong Scene!");
                return;
            }

            // Xóa cái cũ nếu có
            Transform existingPanel = canvas.transform.Find("DialoguePanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            // 2. Tạo DialoguePanel
            GameObject panelObj = new GameObject("DialoguePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panelObj.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();

            // Đặt neo ở phía dưới màn hình
            panelRect.anchorMin = new Vector2(0.5f, 0.05f);
            panelRect.anchorMax = new Vector2(0.5f, 0.05f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(1000, 250);
            panelRect.anchoredPosition = new Vector2(0, 50);

            // Chỉnh màu gỗ nâu tối (RGBA)
            panelObj.GetComponent<Image>().color = new Color(0.2f, 0.12f, 0.05f, 0.95f);

            // 3. Tạo Avatar
            GameObject avatarObj = new GameObject("AvatarImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            avatarObj.transform.SetParent(panelObj.transform, false);
            RectTransform avatarRect = avatarObj.GetComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.pivot = new Vector2(0f, 0.5f);
            avatarRect.sizeDelta = new Vector2(200, 200);
            avatarRect.anchoredPosition = new Vector2(30, 0);

            // 4. Tạo Speaker Name
            GameObject nameObj = new GameObject("SpeakerNameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(panelObj.transform, false);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(0, 1);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.sizeDelta = new Vector2(400, 60);
            nameRect.anchoredPosition = new Vector2(260, -20);
            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.text = "TÊN KHÁCH HÀNG";
            nameText.fontSize = 36;
            nameText.color = new Color(1f, 0.8f, 0.2f, 1f); // Màu vàng đồng
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;

            // 5. Tạo Dialogue Text
            GameObject diagObj = new GameObject("DialogueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            diagObj.transform.SetParent(panelObj.transform, false);
            RectTransform diagRect = diagObj.GetComponent<RectTransform>();
            diagRect.anchorMin = new Vector2(0, 1);
            diagRect.anchorMax = new Vector2(0, 1);
            diagRect.pivot = new Vector2(0f, 1f);
            diagRect.sizeDelta = new Vector2(650, 150);
            diagRect.anchoredPosition = new Vector2(260, -80);
            TextMeshProUGUI diagText = diagObj.GetComponent<TextMeshProUGUI>();
            diagText.text = "Nội dung hội thoại sẽ hiển thị ở đây...";
            diagText.fontSize = 30;
            diagText.color = Color.white;
            diagText.enableWordWrapping = true;
            diagText.alignment = TextAlignmentOptions.TopLeft;

            // 6. Gắn script DialogueUI
            DialogueUI uiScript = canvas.GetComponent<DialogueUI>();
            if (uiScript == null)
            {
                uiScript = canvas.gameObject.AddComponent<DialogueUI>();
            }

            // Gán bằng SerializedObject để tránh private field issue
            SerializedObject so = new SerializedObject(uiScript);
            so.FindProperty("dialoguePanel").objectReferenceValue = panelObj;
            so.FindProperty("avatarImage").objectReferenceValue = avatarObj.GetComponent<Image>();
            so.FindProperty("speakerNameText").objectReferenceValue = nameText;
            so.FindProperty("dialogueText").objectReferenceValue = diagText;
            so.FindProperty("canvasGroup").objectReferenceValue = panelObj.GetComponent<CanvasGroup>();
            so.ApplyModifiedProperties();

            // Đánh dấu Scene đã thay đổi để lưu lại
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            // Ẩn panel mặc định
            panelObj.SetActive(false);

            Debug.Log("Tạo Giao Diện DialogueUI Thành Công! Hãy Play game để kiểm tra.");
            
            // Focus vào game object mới tạo
            Selection.activeGameObject = panelObj;
        }
    }
}
