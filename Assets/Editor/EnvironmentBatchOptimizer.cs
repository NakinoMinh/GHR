using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class EnvironmentBatchOptimizer
{
    private const string EnvironmentRootName = "Environment";
    private const string OutputRootName = "_OptimizedRenderMeshes";
    private const string GeneratedMeshFolder = "Assets/_Project/Art/Environment/Generated/Chapter1OptimizedMeshes";
    private const float ChunkSize = 32f;
    private const int MaxCombinersPerMesh = 220;

    [MenuItem("Tools/GHR/Optimize Chapter1 Environment Batches")]
    public static void OptimizeChapter1Environment()
    {
        GameObject environment = GameObject.Find(EnvironmentRootName);
        if (environment == null)
        {
            Debug.LogError($"[GHR] Cannot find root GameObject '{EnvironmentRootName}'. Open Chapter1 before optimizing.");
            return;
        }

        Transform existingOutput = environment.transform.Find(OutputRootName);
        if (existingOutput != null)
        {
            UnityEngine.Object.DestroyImmediate(existingOutput.gameObject);
        }

        EnsureCleanMeshFolder();

        GameObject outputRoot = new GameObject(OutputRootName);
        outputRoot.transform.SetParent(environment.transform, false);
        GameObjectUtility.SetStaticEditorFlags(outputRoot, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);

        MeshRenderer[] renderers = environment.GetComponentsInChildren<MeshRenderer>(true);
        Dictionary<GroupKey, List<CombineInstance>> groups = new Dictionary<GroupKey, List<CombineInstance>>();
        Dictionary<GroupKey, RendererSettings> rendererSettings = new Dictionary<GroupKey, RendererSettings>();
        int candidateRendererCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            if (!IsCandidateRenderer(renderer, environment.transform))
            {
                continue;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                continue;
            }

            candidateRendererCount++;
            int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
            Vector2Int chunk = GetChunk(renderer.bounds.center);

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                Material material = materials[subMeshIndex];
                if (material == null)
                {
                    continue;
                }

                GroupKey key = new GroupKey(material, chunk);
                if (!groups.TryGetValue(key, out List<CombineInstance> combineInstances))
                {
                    combineInstances = new List<CombineInstance>();
                    groups.Add(key, combineInstances);
                    rendererSettings.Add(key, RendererSettings.From(renderer));
                }

                combineInstances.Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = subMeshIndex,
                    transform = environment.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix
                });
            }
        }

        int combinedMeshCount = 0;
        int combinedInstanceCount = 0;
        foreach (KeyValuePair<GroupKey, List<CombineInstance>> group in groups)
        {
            List<CombineInstance> instances = group.Value;
            for (int start = 0; start < instances.Count; start += MaxCombinersPerMesh)
            {
                int count = Mathf.Min(MaxCombinersPerMesh, instances.Count - start);
                CombineInstance[] batch = instances.GetRange(start, count).ToArray();
                Mesh combinedMesh = new Mesh
                {
                    name = $"ENV_Combined_{Sanitize(group.Key.Material.name)}_{group.Key.Chunk.x}_{group.Key.Chunk.y}_{combinedMeshCount:000}",
                    indexFormat = IndexFormat.UInt32
                };
                combinedMesh.CombineMeshes(batch, true, true, false);
                combinedMesh.RecalculateBounds();
                combinedMesh.Optimize();

                string meshPath = $"{GeneratedMeshFolder}/{combinedMesh.name}.asset";
                AssetDatabase.CreateAsset(combinedMesh, meshPath);

                GameObject combinedObject = new GameObject(combinedMesh.name);
                combinedObject.transform.SetParent(outputRoot.transform, false);
                GameObjectUtility.SetStaticEditorFlags(combinedObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);

                MeshFilter combinedFilter = combinedObject.AddComponent<MeshFilter>();
                combinedFilter.sharedMesh = combinedMesh;

                MeshRenderer combinedRenderer = combinedObject.AddComponent<MeshRenderer>();
                combinedRenderer.sharedMaterial = group.Key.Material;
                rendererSettings[group.Key].ApplyTo(combinedRenderer);

                combinedMeshCount++;
                combinedInstanceCount += count;
            }
        }

        int disabledRendererCount = 0;
        foreach (MeshRenderer renderer in renderers)
        {
            if (IsCandidateRenderer(renderer, environment.transform) && renderer.enabled)
            {
                renderer.enabled = false;
                disabledRendererCount++;
                EditorUtility.SetDirty(renderer);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(environment);
        EditorSceneManager.MarkSceneDirty(environment.scene);
        EditorSceneManager.SaveScene(environment.scene);

        Debug.Log($"[GHR] Environment batch optimization complete. Candidates: {candidateRendererCount}, combined instances: {combinedInstanceCount}, combined meshes: {combinedMeshCount}, disabled source renderers: {disabledRendererCount}.");
    }

    [MenuItem("Tools/GHR/Restore Chapter1 Environment Source Renderers")]
    public static void RestoreChapter1EnvironmentSourceRenderers()
    {
        GameObject environment = GameObject.Find(EnvironmentRootName);
        if (environment == null)
        {
            Debug.LogError($"[GHR] Cannot find root GameObject '{EnvironmentRootName}'.");
            return;
        }

        Transform outputRoot = environment.transform.Find(OutputRootName);
        if (outputRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(outputRoot.gameObject);
        }

        int restoredCount = 0;
        MeshRenderer[] renderers = environment.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            if (IsCandidateRenderer(renderer, environment.transform) && !renderer.enabled)
            {
                renderer.enabled = true;
                restoredCount++;
                EditorUtility.SetDirty(renderer);
            }
        }

        EditorUtility.SetDirty(environment);
        EditorSceneManager.MarkSceneDirty(environment.scene);
        EditorSceneManager.SaveScene(environment.scene);
        Debug.Log($"[GHR] Restored {restoredCount} source Environment renderers.");
    }

    private static bool IsCandidateRenderer(MeshRenderer renderer, Transform environmentRoot)
    {
        if (renderer == null || renderer.transform == environmentRoot)
        {
            return false;
        }

        if (renderer.GetComponent<SkinnedMeshRenderer>() != null || renderer.GetComponent<LODGroup>() != null)
        {
            return false;
        }

        Transform current = renderer.transform;
        while (current != null && current != environmentRoot)
        {
            string name = current.name;
            if (name == OutputRootName || name.StartsWith(OutputRootName, StringComparison.Ordinal))
            {
                return false;
            }

            if (name.StartsWith("Sidewalk_", StringComparison.Ordinal) ||
                name.StartsWith("Street_", StringComparison.Ordinal) ||
                name.StartsWith("StoneFloor_", StringComparison.Ordinal) ||
                name.StartsWith("RiverBankStones", StringComparison.Ordinal))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Vector2Int GetChunk(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / ChunkSize),
            Mathf.FloorToInt(worldPosition.z / ChunkSize));
    }

    private static void EnsureCleanMeshFolder()
    {
        string absoluteFolder = Path.Combine(Application.dataPath, "_Project/Art/Environment/Generated/Chapter1OptimizedMeshes");
        if (!Directory.Exists(absoluteFolder))
        {
            Directory.CreateDirectory(absoluteFolder);
        }

        AssetDatabase.Refresh();
        string[] assets = AssetDatabase.FindAssets("t:Mesh", new[] { GeneratedMeshFolder });
        foreach (string guid in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(' ', '_');
    }

    private readonly struct GroupKey : IEquatable<GroupKey>
    {
        public GroupKey(Material material, Vector2Int chunk)
        {
            Material = material;
            Chunk = chunk;
        }

        public readonly Material Material;
        public readonly Vector2Int Chunk;

        public bool Equals(GroupKey other)
        {
            return Material == other.Material && Chunk.Equals(other.Chunk);
        }

        public override bool Equals(object obj)
        {
            return obj is GroupKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Material != null ? RuntimeHelpers.GetHashCode(Material) : 0) * 397) ^ Chunk.GetHashCode();
            }
        }
    }

    private struct RendererSettings
    {
        public ShadowCastingMode ShadowCastingMode;
        public bool ReceiveShadows;
        public LightProbeUsage LightProbeUsage;
        public ReflectionProbeUsage ReflectionProbeUsage;
        public MotionVectorGenerationMode MotionVectorGenerationMode;

        public static RendererSettings From(MeshRenderer renderer)
        {
            return new RendererSettings
            {
                ShadowCastingMode = renderer.shadowCastingMode,
                ReceiveShadows = renderer.receiveShadows,
                LightProbeUsage = renderer.lightProbeUsage,
                ReflectionProbeUsage = renderer.reflectionProbeUsage,
                MotionVectorGenerationMode = renderer.motionVectorGenerationMode
            };
        }

        public void ApplyTo(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode;
            renderer.receiveShadows = ReceiveShadows;
            renderer.lightProbeUsage = LightProbeUsage;
            renderer.reflectionProbeUsage = ReflectionProbeUsage;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode;
        }
    }
}
