using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GanhHangRong.UI;

namespace GanhHangRong.EditorScripts
{
    public class TabMenuUISetup
    {
        [MenuItem("GHR/Create Tab Menu UI")]
        public static void CreateTabMenu()
        {
            // 1. Create Event System if not exists
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 2. Create Canvas
            GameObject canvasGO = new GameObject("TabMenuCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // 3. Create MenuRoot
            GameObject menuRootGO = new GameObject("MenuRoot");
            menuRootGO.transform.SetParent(canvasGO.transform, false);
            RectTransform menuRootRect = menuRootGO.AddComponent<RectTransform>();
            menuRootRect.anchorMin = Vector2.zero;
            menuRootRect.anchorMax = Vector2.one;
            menuRootRect.sizeDelta = Vector2.zero;
            menuRootGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f); // Background semi-transparent

            // 4. Create Tab Buttons Area
            GameObject tabAreaGO = new GameObject("TabButtonsArea");
            tabAreaGO.transform.SetParent(menuRootGO.transform, false);
            RectTransform tabAreaRect = tabAreaGO.AddComponent<RectTransform>();
            tabAreaRect.anchorMin = new Vector2(0.1f, 0.85f);
            tabAreaRect.anchorMax = new Vector2(0.9f, 0.95f);
            tabAreaRect.sizeDelta = Vector2.zero;
            HorizontalLayoutGroup hLayout = tabAreaGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Helper function to create buttons
            Button CreateButton(string name, string textStr)
            {
                GameObject btnGO = new GameObject(name);
                btnGO.transform.SetParent(tabAreaGO.transform, false);
                btnGO.AddComponent<Image>().color = Color.white;
                Button btn = btnGO.AddComponent<Button>();

                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(btnGO.transform, false);
                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = textStr;
                tmp.color = Color.black;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 24;

                return btn;
            }

            Button menuBtn = CreateButton("MenuBtn", "MENU");
            Button recipeBtn = CreateButton("RecipeBtn", "CÔNG THỨC");
            Button phoneBtn = CreateButton("PhoneBtn", "ĐIỆN THOẠI");

            // 5. Create Panels Area
            GameObject panelsAreaGO = new GameObject("PanelsArea");
            panelsAreaGO.transform.SetParent(menuRootGO.transform, false);
            RectTransform panelsAreaRect = panelsAreaGO.AddComponent<RectTransform>();
            panelsAreaRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelsAreaRect.anchorMax = new Vector2(0.9f, 0.8f);
            panelsAreaRect.sizeDelta = Vector2.zero;

            // Helper function to create panels
            GameObject CreatePanel(string name, string title, Color color)
            {
                GameObject panelGO = new GameObject(name);
                panelGO.transform.SetParent(panelsAreaGO.transform, false);
                RectTransform panelRect = panelGO.AddComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
                panelGO.AddComponent<Image>().color = color;

                // Title text
                GameObject titleGO = new GameObject("Title");
                titleGO.transform.SetParent(panelGO.transform, false);
                RectTransform titleRect = titleGO.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0, 0.8f);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI tmp = titleGO.AddComponent<TextMeshProUGUI>();
                tmp.text = title;
                tmp.color = Color.black;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 36;

                return panelGO;
            }

            GameObject menuPanel = CreatePanel("MenuPanel", "Đây là Menu", new Color(0.9f, 0.9f, 0.9f));
            GameObject recipePanel = CreatePanel("RecipePanel", "Đây là Công Thức", new Color(0.8f, 0.9f, 0.8f));
            GameObject phonePanel = CreatePanel("PhonePanel", "Đây là Điện Thoại", new Color(0.8f, 0.8f, 0.9f));

            // 6. Attach Script and Assign Variables
            TabMenuUI tabMenuUI = canvasGO.AddComponent<TabMenuUI>();
            
            // We use SerializedObject to assign private serialized fields easily
            SerializedObject so = new SerializedObject(tabMenuUI);
            so.FindProperty("menuRoot").objectReferenceValue = menuRootGO;
            so.FindProperty("menuTabButton").objectReferenceValue = menuBtn;
            so.FindProperty("recipeTabButton").objectReferenceValue = recipeBtn;
            so.FindProperty("phoneTabButton").objectReferenceValue = phoneBtn;
            so.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            so.FindProperty("recipePanel").objectReferenceValue = recipePanel;
            so.FindProperty("phonePanel").objectReferenceValue = phonePanel;
            so.ApplyModifiedProperties();

            // Select the created canvas in hierarchy
            Selection.activeGameObject = canvasGO;
            EditorGUIUtility.PingObject(canvasGO);

            Debug.Log("Tab Menu UI has been created successfully!");
        }
    }
}
