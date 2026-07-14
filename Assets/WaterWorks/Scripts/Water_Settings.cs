using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Water_Settings : MonoBehaviour
{
    const string VolumeResourceName = "Water_Volume";
    const string VolumeBoundsProperty = "bounds";
    const string VolumePositionProperty = "pos";
    const string LegacyDisplacementProperty = "_Displacement_Amount";
    const float DefaultDisplacementAmount = 0.1f;

    Material waterVolume;
    Material waterMaterial;
    MeshRenderer meshRenderer;

    void Update()
    {
        if (waterVolume == null)
        {
            waterVolume = (Material)Resources.Load(VolumeResourceName);
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (waterMaterial == null && meshRenderer != null)
        {
            waterMaterial = meshRenderer.sharedMaterial;
        }

        if (waterVolume == null || waterMaterial == null)
        {
            return;
        }

        float displacementAmount = waterMaterial.HasProperty(LegacyDisplacementProperty)
            ? waterMaterial.GetFloat(LegacyDisplacementProperty)
            : DefaultDisplacementAmount;

        float waterY = (waterVolume.GetVector(VolumeBoundsProperty).y / -2f)
            + transform.position.y
            + (displacementAmount / 3f);

        waterVolume.SetVector(VolumePositionProperty, new Vector4(0f, waterY, 0f, 0f));
    }
}
