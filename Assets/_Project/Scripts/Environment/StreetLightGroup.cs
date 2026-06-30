using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Environment
{
    public class StreetLightGroup : MonoBehaviour
    {
        [Header("Danh sach den")]
        [SerializeField] private List<Light> lights = new List<Light>();
        [SerializeField] private List<Renderer> emissiveRenderers = new List<Renderer>();

        [Header("Cau hinh anh sang")]
        [SerializeField, Min(0f)] private float onIntensity = 2.2f;
        [SerializeField, Min(0f)] private float offIntensity;
        [SerializeField] private Color emissionOn = new Color(1f, 0.68f, 0.36f);
        [SerializeField] private Color emissionOff = Color.black;

        public void TurnOn()
        {
            SetEnabledState(true);
        }

        public void TurnOff()
        {
            SetEnabledState(false);
        }

        public void SetIntensity(float intensity)
        {
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].intensity = Mathf.Max(0f, intensity);
                }
            }
        }

        public void RegisterLight(Light light)
        {
            if (light != null && !lights.Contains(light))
            {
                lights.Add(light);
            }
        }

        public void RegisterEmissiveRenderer(Renderer renderer)
        {
            if (renderer != null && !emissiveRenderers.Contains(renderer))
            {
                emissiveRenderers.Add(renderer);
            }
        }

        public void CollectChildLights()
        {
            lights.Clear();
            lights.AddRange(GetComponentsInChildren<Light>(true));
        }

        private void SetEnabledState(bool isOn)
        {
            float targetIntensity = isOn ? onIntensity : offIntensity;
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                lights[i].enabled = isOn || offIntensity > 0f;
                lights[i].intensity = targetIntensity;
            }

            Color emission = isOn ? emissionOn : emissionOff;
            for (int i = 0; i < emissiveRenderers.Count; i++)
            {
                if (emissiveRenderers[i] == null)
                {
                    continue;
                }

                Material material = emissiveRenderers[i].material;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
        }
    }
}
