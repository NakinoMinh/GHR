using UnityEditor;
using UnityEngine;
using GanhHangRong.Systems;
using System.Collections.Generic;

public class AutoTrafficSetup : EditorWindow
{
    [MenuItem("GHR Tools/Setup Traffic System")]
    public static void SetupTrafficSystem()
    {
        // Find or create TrafficSystem root
        GameObject trafficObj = GameObject.Find("CityTrafficSystem");
        if (trafficObj == null)
        {
            trafficObj = new GameObject("CityTrafficSystem");
        }
        
        CityTrafficManager manager = trafficObj.GetComponent<CityTrafficManager>();
        if (manager == null) manager = trafficObj.AddComponent<CityTrafficManager>();

        // Find prefabs
        manager.taxiPrefab = LoadPrefab("taxi");
        manager.otoPrefab = LoadPrefab("oto");
        manager.busPrefab = LoadPrefab("bus");
        manager.motorbikePrefab = LoadPrefab("xeganmay");

        // Clear existing children
        for (int i = trafficObj.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(trafficObj.transform.GetChild(i).gameObject);
        }
        manager.leftLaneSpawns.Clear();
        manager.rightLaneSpawns.Clear();

        float laneZOffset = 1.8f; // Distance from center
        float roadCenterZ = -5.4f; // From TaxiTraffic script
        
        float startX = -60f;
        float endX = 120f;
        float stepX = 20f; // node every 20m

        // Create Left Lane (moving towards positive X)
        List<TrafficWaypoint> leftNodes = new List<TrafficWaypoint>();
        for (float x = startX; x <= endX; x += stepX)
        {
            TrafficWaypoint node = CreateNode(trafficObj.transform, $"Node_Left_{x}", new Vector3(x, 0, roadCenterZ + laneZOffset));
            leftNodes.Add(node);
        }

        // Link Left Lane
        for (int i = 0; i < leftNodes.Count - 1; i++)
        {
            leftNodes[i].nextWaypoints.Add(leftNodes[i+1]);
        }
        manager.leftLaneSpawns.Add(leftNodes[0]);

        // Create Right Lane (moving towards negative X)
        List<TrafficWaypoint> rightNodes = new List<TrafficWaypoint>();
        for (float x = endX; x >= startX; x -= stepX)
        {
            TrafficWaypoint node = CreateNode(trafficObj.transform, $"Node_Right_{x}", new Vector3(x, 0, roadCenterZ - laneZOffset));
            rightNodes.Add(node);
        }

        // Link Right Lane
        for (int i = 0; i < rightNodes.Count - 1; i++)
        {
            rightNodes[i].nextWaypoints.Add(rightNodes[i+1]);
        }
        manager.rightLaneSpawns.Add(rightNodes[0]);

        // Branching (Intersection logic)
        // Let's create a branch at X = 20, 60
        CreateBranch(trafficObj.transform, leftNodes, rightNodes, 20f, roadCenterZ, laneZOffset);
        CreateBranch(trafficObj.transform, leftNodes, rightNodes, 60f, roadCenterZ, laneZOffset);

        // Find bus stop and mark nearest node
        GameObject busStop = GameObject.Find("BusStop_02");
        if (busStop != null)
        {
            TrafficWaypoint closestLeft = GetClosest(leftNodes, busStop.transform.position);
            if (closestLeft != null) closestLeft.isBusStop = true;
            
            TrafficWaypoint closestRight = GetClosest(rightNodes, busStop.transform.position);
            if (closestRight != null) closestRight.isBusStop = true;
        }

        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        Debug.Log("Traffic System Setup Complete.");
    }

    private static void CreateBranch(Transform parent, List<TrafficWaypoint> leftNodes, List<TrafficWaypoint> rightNodes, float branchX, float centerZ, float offset)
    {
        // Find closest nodes to branchX
        TrafficWaypoint leftNode = leftNodes.Find(n => Mathf.Abs(n.transform.position.x - branchX) < 5f);
        TrafficWaypoint rightNode = rightNodes.Find(n => Mathf.Abs(n.transform.position.x - branchX) < 5f);

        if (leftNode != null)
        {
            // Branch turning "North" (positive Z)
            TrafficWaypoint branchNorth1 = CreateNode(parent, $"Branch_N1_{branchX}", new Vector3(branchX, 0, centerZ + offset + 15f));
            TrafficWaypoint branchNorth2 = CreateNode(parent, $"Branch_N2_{branchX}", new Vector3(branchX, 0, centerZ + offset + 30f));
            leftNode.nextWaypoints.Add(branchNorth1);
            branchNorth1.nextWaypoints.Add(branchNorth2);
        }

        if (rightNode != null)
        {
            // Branch turning "South" (negative Z)
            TrafficWaypoint branchSouth1 = CreateNode(parent, $"Branch_S1_{branchX}", new Vector3(branchX, 0, centerZ - offset - 15f));
            TrafficWaypoint branchSouth2 = CreateNode(parent, $"Branch_S2_{branchX}", new Vector3(branchX, 0, centerZ - offset - 30f));
            rightNode.nextWaypoints.Add(branchSouth1);
            branchSouth1.nextWaypoints.Add(branchSouth2);
        }
    }

    public static GameObject LoadPrefab(string folderName)
    {
        // Try searching in the specific folder first
        string folderPath = "Assets/" + folderName;
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
            
            // Prioritize .prefab first
            foreach(var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().EndsWith(".prefab"))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            
            // Fallback to .fbx
            foreach(var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().EndsWith(".fbx"))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
        }

        // Fallback to searching everywhere for prefab
        string[] fallbackGuids = AssetDatabase.FindAssets(folderName + " t:Prefab");
        foreach(var guid in fallbackGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("assets/" + folderName) || path.ToLower().Contains(folderName + ".prefab"))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        return null;
    }

    private static TrafficWaypoint CreateNode(Transform parent, string name, Vector3 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        return obj.AddComponent<TrafficWaypoint>();
    }

    private static TrafficWaypoint GetClosest(List<TrafficWaypoint> nodes, Vector3 pos)
    {
        TrafficWaypoint closest = null;
        float minDist = float.MaxValue;
        foreach (var node in nodes)
        {
            float d = Vector3.Distance(node.transform.position, pos);
            if (d < minDist)
            {
                minDist = d;
                closest = node;
            }
        }
        return closest;
    }
}
