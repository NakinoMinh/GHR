using UnityEngine;
using GanhHangRong.Core;
using System.Collections.Generic;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Hệ thống quản lý toàn bộ các lớp parallax, đồng bộ hóa tốc độ cuộn với camera.
    /// Tạo hiệu ứng 2.5D có chiều sâu cinematic.
    /// </summary>
    public class ParallaxSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Settings")]
        [SerializeField] private bool autoScroll = false;
        [SerializeField] private float autoScrollSpeed = 1f;
        [SerializeField] private float smoothing = 1f;

        private Vector3 previousCameraPosition;
        private List<ParallaxLayer> layers = new List<ParallaxLayer>();

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform != null)
            {
                previousCameraPosition = cameraTransform.position;
            }

            // Tìm tất cả các lớp parallax là con của GameObject này
            layers.AddRange(GetComponentsInChildren<ParallaxLayer>());
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            float deltaX = cameraTransform.position.x - previousCameraPosition.x;
            float deltaY = cameraTransform.position.y - previousCameraPosition.y;

            if (autoScroll)
            {
                deltaX += autoScrollSpeed * Time.deltaTime;
            }

            foreach (var layer in layers)
            {
                layer.Move(deltaX, deltaY, smoothing);
            }

            previousCameraPosition = cameraTransform.position;
        }

        public void RegisterLayer(ParallaxLayer layer)
        {
            if (!layers.Contains(layer))
            {
                layers.Add(layer);
            }
        }

        public void UnregisterLayer(ParallaxLayer layer)
        {
            if (layers.Contains(layer))
            {
                layers.Remove(layer);
            }
        }
    }
}
