using UnityEngine;

namespace GanhHangRong.Environment
{
    public class BoatFloatSimple : MonoBehaviour
    {
        [Header("Lac nhe tren mat nuoc")]
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 0.9f;
        [SerializeField] private float rollAmplitude = 2.5f;
        [SerializeField] private float pitchAmplitude = 1.4f;
        [SerializeField] private bool randomizePhase = true;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private float phase;

        private void Awake()
        {
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
            phase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        }

        private void Update()
        {
            float t = Time.time * bobSpeed + phase;
            float y = Mathf.Sin(t) * bobAmplitude;
            float roll = Mathf.Sin(t * 0.83f) * rollAmplitude;
            float pitch = Mathf.Cos(t * 0.67f) * pitchAmplitude;

            transform.localPosition = startPosition + Vector3.up * y;
            transform.localRotation = startRotation * Quaternion.Euler(pitch, 0f, roll);
        }
    }
}
