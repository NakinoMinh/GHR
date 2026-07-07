using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GanhHangRong.Editor.SceneBuilder
{
    public static class CoastalEmbankmentBuilder
    {
        private const string RootName = "Coastal_Concrete_Embankment";
        private const string MaterialFolder = "Assets/_Project/Materials/Environment";

        [MenuItem("Ganh Hang Rong/Scene/Tao bo ke ven bien", false, 152)]
        public static void Build()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            EnsureFolder(MaterialFolder);

            Material concrete = GetOrCreateMaterial("Coastal_Embankment_Concrete", new Color(0.52f, 0.51f, 0.47f));
            Material concreteAlt = GetOrCreateMaterial("Coastal_Embankment_Concrete_Alt", new Color(0.61f, 0.59f, 0.54f));
            Material groove = GetOrCreateMaterial("Coastal_Embankment_Groove", new Color(0.22f, 0.205f, 0.18f));

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create coastal embankment");

            float minX = -38f;
            float maxX = 92f;
            float topZ = 8.15f;
            float bottomZ = 14.4f;
            float topY = 0.08f;
            float bottomY = -1.65f;

            CreateSlope(root.transform, minX, maxX, topZ, bottomZ, topY, bottomY, concrete);
            CreateHexPaving(root.transform, minX, maxX, topZ, bottomZ, topY, bottomY, concrete, concreteAlt, groove);

            Selection.activeGameObject = root;
            EditorSceneManagerShim.MarkActiveSceneDirty();
            Debug.Log($"[{nameof(CoastalEmbankmentBuilder)}] Created {RootName} along the waterfront.");
        }

        private static void CreateSlope(Transform parent, float minX, float maxX, float topZ, float bottomZ, float topY, float bottomY, Material material)
        {
            GameObject slope = new GameObject("Sloped_Concrete_Base");
            slope.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = "Coastal_Embankment_SlopeMesh" };
            mesh.vertices = new[]
            {
                new Vector3(minX, topY, topZ),
                new Vector3(maxX, topY, topZ),
                new Vector3(minX, bottomY, bottomZ),
                new Vector3(maxX, bottomY, bottomZ)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(18f, 0f),
                new Vector2(0f, 4f),
                new Vector2(18f, 4f)
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = slope.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = slope.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            MeshCollider collider = slope.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        private static void CreateHexPaving(
            Transform parent,
            float minX,
            float maxX,
            float topZ,
            float bottomZ,
            float topY,
            float bottomY,
            Material concrete,
            Material concreteAlt,
            Material groove)
        {
            GameObject tiles = new GameObject("Hex_Concrete_Paving");
            tiles.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = "Coastal_Embankment_HexTiles" };
            List<Vector3> vertices = new List<Vector3>();
            List<int> concreteTriangles = new List<int>();
            List<int> alternateTriangles = new List<int>();
            List<int> grooveTriangles = new List<int>();
            List<Vector2> uv = new List<Vector2>();

            float radius = 0.48f;
            float xStep = radius * 1.72f;
            float zStep = radius * 1.48f;
            int row = 0;
            for (float z = topZ + 0.5f; z < bottomZ - 0.2f; z += zStep, row++)
            {
                float rowOffset = row % 2 == 0 ? 0f : xStep * 0.5f;
                int column = 0;
                for (float x = minX + 1.2f + rowOffset; x < maxX - 1.2f; x += xStep, column++)
                {
                    AddHexTile(vertices, uv, grooveTriangles, x, z, SurfaceY(z, topZ, bottomZ, topY, bottomY) + 0.018f, radius, topZ, bottomZ, topY, bottomY);
                    AddHexTile(vertices, uv, (row + column) % 3 == 0 ? alternateTriangles : concreteTriangles, x, z, SurfaceY(z, topZ, bottomZ, topY, bottomY) + 0.035f, radius * 0.94f, topZ, bottomZ, topY, bottomY);
                }
            }

            mesh.SetVertices(vertices);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(concreteTriangles, 0);
            mesh.SetTriangles(alternateTriangles, 1);
            mesh.SetTriangles(grooveTriangles, 2);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = tiles.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = tiles.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { concrete, concreteAlt, groove };
        }

        private static void AddHexTile(
            List<Vector3> vertices,
            List<Vector2> uv,
            List<int> triangles,
            float centerX,
            float centerZ,
            float centerY,
            float radius,
            float topZ,
            float bottomZ,
            float topY,
            float bottomY)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(centerX, centerY, centerZ));
            uv.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i + 30f);
                float x = centerX + Mathf.Cos(angle) * radius;
                float z = centerZ + Mathf.Sin(angle) * radius;
                float y = SurfaceY(z, topZ, bottomZ, topY, bottomY) + (centerY - SurfaceY(centerZ, topZ, bottomZ, topY, bottomY));
                vertices.Add(new Vector3(x, y, z));
                uv.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            for (int i = 1; i <= 6; i++)
            {
                triangles.Add(start);
                triangles.Add(start + (i == 6 ? 1 : i + 1));
                triangles.Add(start + i);
            }
        }

        private static void CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static float SurfaceY(float z, float topZ, float bottomZ, float topY, float bottomY)
        {
            return Mathf.Lerp(topY, bottomY, Mathf.InverseLerp(topZ, bottomZ, z));
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader) { name = name, color = color };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }

    internal static class EditorSceneManagerShim
    {
        public static void MarkActiveSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
