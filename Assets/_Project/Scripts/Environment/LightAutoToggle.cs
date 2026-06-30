using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Environment
{
    public class LightAutoToggle : MonoBehaviour
    {
        [Header("Nguon thoi gian")]
        [SerializeField] private TimeOfDayManager timeManager;

        [Header("Gio bat/tat")]
        [SerializeField, Range(0, 23)] private int turnOnHour = 18;
        [SerializeField, Range(0, 59)] private int turnOnMinute;
        [SerializeField, Range(0, 23)] private int turnOffHour = 5;
        [SerializeField, Range(0, 59)] private int turnOffMinute = 30;

        [Header("Danh sach den")]
        [SerializeField] private List<Light> lights = new List<Light>();
        [SerializeField] private List<StreetLightGroup> lightGroups = new List<StreetLightGroup>();

        [Header("Cuong do")]
        [SerializeField, Min(0f)] private float dayIntensity;
        [SerializeField, Min(0f)] private float nightIntensity = 2f;

        private bool lastState;
        private bool warnedMissingTimeManager;

        private void Reset()
        {
            timeManager = FindAnyObjectByType<TimeOfDayManager>();
            CollectChildLights();
        }

        private void OnEnable()
        {
            if (timeManager == null)
            {
                timeManager = FindAnyObjectByType<TimeOfDayManager>();
            }

            if (timeManager != null)
            {
                timeManager.TimeChanged += HandleTimeChanged;
                ApplyForCurrentTime();
            }
            else if (!warnedMissingTimeManager)
            {
                Debug.LogWarning($"{nameof(LightAutoToggle)} chua co TimeOfDayManager.", this);
                warnedMissingTimeManager = true;
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
            {
                timeManager.TimeChanged -= HandleTimeChanged;
            }
        }

        private void HandleTimeChanged(int day, int hour, int minute)
        {
            ApplyForCurrentTime();
        }

        public void ApplyForCurrentTime()
        {
            if (timeManager == null)
            {
                return;
            }

            int currentMinute = timeManager.CurrentHour * 60 + timeManager.CurrentMinute;
            bool shouldTurnOn = IsInsideNightWindow(currentMinute);
            if (Application.isPlaying && shouldTurnOn == lastState)
            {
                return;
            }

            lastState = shouldTurnOn;
            SetLights(shouldTurnOn);
        }

        public void RegisterLight(Light light)
        {
            if (light != null && !lights.Contains(light))
            {
                lights.Add(light);
            }
        }

        public void RegisterGroup(StreetLightGroup group)
        {
            if (group != null && !lightGroups.Contains(group))
            {
                lightGroups.Add(group);
            }
        }

        [ContextMenu("Collect Child Lights")]
        public void CollectChildLights()
        {
            lights.Clear();
            lights.AddRange(GetComponentsInChildren<Light>(true));
        }

        private bool IsInsideNightWindow(int currentMinute)
        {
            int on = turnOnHour * 60 + turnOnMinute;
            int off = turnOffHour * 60 + turnOffMinute;
            return on < off ? currentMinute >= on && currentMinute < off : currentMinute >= on || currentMinute < off;
        }

        private void SetLights(bool isOn)
        {
            float targetIntensity = isOn ? nightIntensity : dayIntensity;

            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                // Ban ngay co the tat han neu dayIntensity = 0.
                lights[i].enabled = isOn || dayIntensity > 0f;
                lights[i].intensity = targetIntensity;
            }

            for (int i = 0; i < lightGroups.Count; i++)
            {
                if (lightGroups[i] == null)
                {
                    continue;
                }

                if (isOn)
                {
                    lightGroups[i].TurnOn();
                }
                else
                {
                    lightGroups[i].TurnOff();
                }

                lightGroups[i].SetIntensity(targetIntensity);
            }
        }
    }
}
