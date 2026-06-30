using UnityEngine;
using UnityEditor;

public class ScatterStones : EditorWindow
{
    [MenuItem("Tools/Scatter Stones Under Houses")]
    public static void Scatter()
    {
        string prefabPath = "Assets/_Project/Art/Environment/Simple city plain/Prefabs/Stone Floor prefab.prefab";
        GameObject stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (stonePrefab == null)
        {
            Debug.LogError("Could not find Stone Floor prefab at " + prefabPath);
            return;
        }

        GameObject[] allObjs = Object.FindObjectsOfType<GameObject>();
        int count = 0;
        
        Undo.RegisterCompleteObjectUndo(allObjs, "Scatter Stones");

        foreach (GameObject obj in allObjs)
        {
            // Lọc các nhà bằng tên
            if (obj.name.Contains("ShopFront") || obj.name.Contains("Building") || obj.name.Contains("Shophouse"))
            {
                bool hasStone = false;
                foreach (Transform child in obj.transform)
                {
                    if (child.name.Contains("Stone Floor prefab"))
                    {
                        hasStone = true;
                        break;
                    }
                }

                if (!hasStone)
                {
                    GameObject stoneInstance = (GameObject)PrefabUtility.InstantiatePrefab(stonePrefab);
                    stoneInstance.transform.position = obj.transform.position;
                    // Đặt làm con của nhà
                    stoneInstance.transform.SetParent(obj.transform);
                    
                    // Ghi Undo để có thể Ctrl+Z
                    Undo.RegisterCreatedObjectUndo(stoneInstance, "Add Stone");
                    count++;
                }
            }
        }

        Debug.Log("Đã rải thêm " + count + " bề mặt stone dưới các ngôi nhà!");
    }
}
