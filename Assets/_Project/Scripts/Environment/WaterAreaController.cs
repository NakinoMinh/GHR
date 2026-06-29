using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Environment
{
    public class WaterAreaController : MonoBehaviour
    {
        [Header("Mat nuoc")]
        [SerializeField] private Renderer waterRenderer;
        [SerializeField] private Vector2 uvSpeed = new Vector2(0.025f, 0.012f);
        [SerializeField] private string textureProperty = "_BaseMap";

        [Header("Thuyen tren nuoc")]
        [SerializeField] private List<BoatFloatSimple> boats = new List<BoatFloatSimple>();
        [SerializeField] private bool autoFindChildBoats = true;

        private Material runtimeMaterial;
        private Vector2 currentOffset;

        private void Awake()
        {
            if (waterRenderer == null)
            {
                waterRenderer = GetComponentInChildren<Renderer>();
            }

            if (waterRenderer != null)
            {
                runtimeMaterial = waterRenderer.material;
            }
            else
            {
                Debug.LogWarning($"{nameof(WaterAreaController)} can Renderer cua mat nuoc.", this);
            }

            if (autoFindChildBoats)
            {
                boats.Clear();
                boats.AddRange(GetComponentsInChildren<BoatFloatSimple>(true));
            }
        }

        private void Update()
        {
            AnimateWater();
        }

        private void AnimateWater()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            currentOffset += uvSpeed * Time.deltaTime;
            if (runtimeMaterial.HasProperty(textureProperty))
            {
                runtimeMaterial.SetTextureOffset(textureProperty, currentOffset);
            }
            else if (runtimeMaterial.HasProperty("_MainTex"))
            {
                runtimeMaterial.SetTextureOffset("_MainTex", currentOffset);
            }
        }
    }
}
