using UnityEngine;

namespace GHR.Player
{
    public class WaterRipple : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Prefab của hạt nước / gợn sóng sinh ra khi di chuyển dưới nước")]
        public GameObject rippleParticlePrefab;
        [Tooltip("Khoảng cách tối thiểu giữa 2 bước chân để sinh ra gợn sóng")]
        public float rippleSpawnDistance = 0.5f;
        [Tooltip("Vị trí chân nhân vật")]
        public Transform feetPosition;

        private Vector3 lastRipplePosition;
        private bool isInWater = false;

        private void Start()
        {
            if (feetPosition == null)
            {
                feetPosition = transform;
            }
        }

        private void Update()
        {
            if (isInWater)
            {
                if (Vector3.Distance(feetPosition.position, lastRipplePosition) > rippleSpawnDistance)
                {
                    SpawnRipple();
                }
            }
        }

        private void SpawnRipple()
        {
            lastRipplePosition = feetPosition.position;

            if (rippleParticlePrefab != null)
            {
                // Spawn ripple at water surface level (Y might need adjustment depending on water height)
                Vector3 spawnPos = feetPosition.position;
                Instantiate(rippleParticlePrefab, spawnPos, Quaternion.Euler(-90, 0, 0));
            }
            else
            {
                // Fallback nếu chưa gán particle
                Debug.Log("💦 [WaterRipple] Splash! Bạn đang lội nước.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.name.Contains("Water") || other.CompareTag("Water"))
            {
                isInWater = true;
                lastRipplePosition = feetPosition.position;
                SpawnRipple();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.name.Contains("Water") || other.CompareTag("Water"))
            {
                isInWater = false;
            }
        }
    }
}
