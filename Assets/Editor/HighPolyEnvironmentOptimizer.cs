using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;

public static class HighPolyEnvironmentOptimizer
{
    private const string OutputFolder = "Assets/_Project/Art/Environment/Generated/PerformanceMeshes";
    private const string ManifestPath = OutputFolder + "/EnvironmentMeshOptimizationManifest.asset";
    private const long MinimumTriangleCount = 250000;

    [MenuItem("Tools/GHR/Optimize High Poly Background Meshes")]
    public static void Optimize()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[GHR] Exit Play Mode before optimizing environment meshes.");
            return;
        }

        RestoreInternal(false);
        EnsureOutputFolder();

        EnvironmentMeshOptimizationManifest manifest = ScriptableObject.CreateInstance<EnvironmentMeshOptimizationManifest>();
        Dictionary<string, Mesh> simplifiedMeshCache = new Dictionary<string, Mesh>();
        MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        long sourceTriangles = 0;
        long optimizedTriangles = 0;
        int optimizedRendererCount = 0;

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            Mesh sourceMesh = filter != null ? filter.sharedMesh : null;
            if (renderer == null || sourceMesh == null ||
                !TryGetQuality(filter.transform, sourceMesh, out float quality, out bool preserveOpenEdges))
            {
                continue;
            }

            string cacheKey = sourceMesh.GetInstanceID() + "_" + quality.ToString("F3") + "_" + preserveOpenEdges;
            if (!simplifiedMeshCache.TryGetValue(cacheKey, out Mesh optimizedMesh))
            {
                optimizedMesh = Simplify(sourceMesh, quality, preserveOpenEdges);
                optimizedMesh.name = sourceMesh.name + "_GHR_Q" + Mathf.RoundToInt(quality * 100f);
                string meshPath = AssetDatabase.GenerateUniqueAssetPath(OutputFolder + "/" + Sanitize(optimizedMesh.name) + ".asset");
                AssetDatabase.CreateAsset(optimizedMesh, meshPath);
                simplifiedMeshCache.Add(cacheKey, optimizedMesh);
            }

            manifest.entries.Add(new EnvironmentMeshOptimizationManifest.Entry
            {
                meshFilterGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(filter).ToString(),
                originalMesh = sourceMesh,
                optimizedMesh = optimizedMesh,
                originalShadowCastingMode = renderer.shadowCastingMode,
                originalMotionVectorMode = renderer.motionVectorGenerationMode
            });

            sourceTriangles += GetTriangleCount(sourceMesh);
            optimizedTriangles += GetTriangleCount(optimizedMesh);
            optimizedRendererCount++;

            filter.sharedMesh = optimizedMesh;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(renderer);
        }

        AssetDatabase.CreateAsset(manifest, ManifestPath);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        float reduction = sourceTriangles > 0 ? 1f - optimizedTriangles / (float)sourceTriangles : 0f;
        Debug.Log($"[GHR] High-poly environment optimized. Renderers: {optimizedRendererCount}, " +
                  $"triangles: {sourceTriangles:N0} -> {optimizedTriangles:N0} ({reduction:P1} reduction). " +
                  "Source meshes and colliders were preserved.");
    }

    [MenuItem("Tools/GHR/Restore High Poly Background Meshes")]
    public static void Restore()
    {
        RestoreInternal(true);
    }

    private static Mesh Simplify(Mesh sourceMesh, float quality, bool preserveOpenEdges)
    {
        MeshSimplifier simplifier = new MeshSimplifier(sourceMesh);
        SimplificationOptions options = SimplificationOptions.Default;
        options.PreserveBorderEdges = preserveOpenEdges;
        options.PreserveUVSeamEdges = preserveOpenEdges;
        options.PreserveUVFoldoverEdges = preserveOpenEdges;
        options.PreserveSurfaceCurvature = false;
        options.MaxIterationCount = 100;
        simplifier.SimplificationOptions = options;
        simplifier.SimplifyMesh(quality);

        Mesh result = simplifier.ToMesh();
        result.RecalculateBounds();
        return result;
    }

    private static bool TryGetQuality(Transform transform, Mesh mesh, out float quality, out bool preserveOpenEdges)
    {
        quality = 1f;
        preserveOpenEdges = true;
        if (GetTriangleCount(mesh) < MinimumTriangleCount) return false;

        Transform root = transform;
        while (root.parent != null) root = root.parent;
        string rootName = root.name;

        if (rootName == "SidewalkTrees")
        {
            // Foliage is mostly open cards. Preserving every border prevents meaningful simplification.
            quality = 0.20f;
            preserveOpenEdges = false;
            return true;
        }

        if (rootName == "NewCoastal_Environment_Replacements" ||
            rootName == "NhaCua2" ||
            rootName.StartsWith("New_TicketOffice", StringComparison.Ordinal))
        {
            quality = 0.22f;
            return true;
        }

        if (rootName.StartsWith("ThuyenDanhCa_", StringComparison.Ordinal) ||
            rootName.StartsWith("TauMauDo_", StringComparison.Ordinal) ||
            rootName.StartsWith("TauGhe_", StringComparison.Ordinal) ||
            rootName.StartsWith("tau_tuan_tra", StringComparison.OrdinalIgnoreCase))
        {
            quality = 0.20f;
            return true;
        }

        if (rootName.StartsWith("BusStop_", StringComparison.Ordinal))
        {
            quality = 0.25f;
            return true;
        }

        return false;
    }

    private static void RestoreInternal(bool logResult)
    {
        EnvironmentMeshOptimizationManifest manifest = AssetDatabase.LoadAssetAtPath<EnvironmentMeshOptimizationManifest>(ManifestPath);
        if (manifest == null)
        {
            if (logResult) Debug.Log("[GHR] No high-poly environment optimization manifest found.");
            return;
        }

        Dictionary<Mesh, EnvironmentMeshOptimizationManifest.Entry> entriesByOptimizedMesh =
            new Dictionary<Mesh, EnvironmentMeshOptimizationManifest.Entry>();
        for (int i = 0; i < manifest.entries.Count; i++)
        {
            EnvironmentMeshOptimizationManifest.Entry entry = manifest.entries[i];
            if (entry.optimizedMesh != null && !entriesByOptimizedMesh.ContainsKey(entry.optimizedMesh))
            {
                entriesByOptimizedMesh.Add(entry.optimizedMesh, entry);
            }
        }

        int restoredCount = 0;
        for (int i = 0; i < manifest.entries.Count; i++)
        {
            EnvironmentMeshOptimizationManifest.Entry entry = manifest.entries[i];
            if (!GlobalObjectId.TryParse(entry.meshFilterGlobalId, out GlobalObjectId globalId)) continue;

            MeshFilter filter = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as MeshFilter;
            if (filter == null) continue;
            RestoreFilter(filter, entry);
            restoredCount++;
        }

        // Prefab instance component IDs are not stable in every scene configuration.
        // The assigned optimized mesh is a reliable fallback and keeps restore non-destructive.
        if (entriesByOptimizedMesh.Count > 0)
        {
            MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.sharedMesh == null ||
                    !entriesByOptimizedMesh.TryGetValue(filter.sharedMesh, out EnvironmentMeshOptimizationManifest.Entry entry))
                {
                    continue;
                }

                RestoreFilter(filter, entry);
                restoredCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        if (!AssetDatabase.DeleteAsset(OutputFolder))
        {
            Debug.LogError($"[GHR] Could not delete generated mesh folder: {OutputFolder}");
        }
        AssetDatabase.Refresh();

        if (logResult) Debug.Log($"[GHR] Restored {restoredCount} original high-poly render meshes.");
    }

    private static void RestoreFilter(MeshFilter filter, EnvironmentMeshOptimizationManifest.Entry entry)
    {
        MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
        filter.sharedMesh = entry.originalMesh;
        if (renderer != null)
        {
            renderer.shadowCastingMode = entry.originalShadowCastingMode;
            renderer.motionVectorGenerationMode = entry.originalMotionVectorMode;
            EditorUtility.SetDirty(renderer);
        }
        EditorUtility.SetDirty(filter);
    }

    private static void EnsureOutputFolder()
    {
        string[] segments = OutputFolder.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }
            current = next;
        }
    }

    private static long GetTriangleCount(Mesh mesh)
    {
        if (mesh == null) return 0;

        long triangleCount = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            triangleCount += (long)mesh.GetIndexCount(i) / 3L;
        }
        return triangleCount;
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }
        return value.Replace(' ', '_');
    }
}

public sealed class EnvironmentMeshOptimizationManifest : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string meshFilterGlobalId;
        public Mesh originalMesh;
        public Mesh optimizedMesh;
        public ShadowCastingMode originalShadowCastingMode;
        public MotionVectorGenerationMode originalMotionVectorMode;
    }

    public List<Entry> entries = new List<Entry>();
}
