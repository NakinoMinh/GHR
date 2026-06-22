using UnityEngine;

namespace GanhHangRong.Systems
{
    public class BoatOnSeaPatrol : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private Vector3 startPoint = new Vector3(22f, -0.62f, 122f);
        [SerializeField] private Vector3 endPoint = new Vector3(58f, -0.62f, 152f);
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float turnSpeed = 3f;
        [SerializeField] private float waitAtEnds = 1.5f;

        [Header("Water")]
        [SerializeField] private float waterLevel = -0.62f;
        [SerializeField] private float heightOffset = 0.35f;
        [SerializeField] private float manualBobAmplitude = 0.12f;
        [SerializeField] private float manualBobFrequency = 1.2f;
        [SerializeField] private bool useSuimonoHeight = false;

        [Header("Motion Feel")]
        [SerializeField] private float pitchAmount = 2.5f;
        [SerializeField] private float rollAmount = 4f;
        [SerializeField] private float rockFrequency = 1.4f;

        private Suimono.Core.SuimonoModule suimonoModule;
        private Vector3 currentTarget;
        private float waitTimer;
        private float motionSeed;

        private void Awake()
        {
            if (useSuimonoHeight)
            {
                suimonoModule = FindObjectOfType<Suimono.Core.SuimonoModule>();
            }

            currentTarget = endPoint;
            motionSeed = Random.Range(0f, 100f);
        }

        private void Start()
        {
            Vector3 position = startPoint;
            position.y = GetSurfaceHeight(position) + heightOffset;
            transform.position = position;
            FaceTarget(currentTarget, true);
        }

        private void Update()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                ApplySurfaceHeightAndRocking();
                return;
            }

            Vector3 position = transform.position;
            Vector3 flatTarget = new Vector3(currentTarget.x, position.y, currentTarget.z);
            Vector3 nextPosition = Vector3.MoveTowards(position, flatTarget, moveSpeed * Time.deltaTime);
            nextPosition.y = GetSurfaceHeight(nextPosition) + heightOffset;
            transform.position = nextPosition;

            FaceTarget(currentTarget, false);
            ApplyRockingRotation();

            Vector2 currentFlat = new Vector2(transform.position.x, transform.position.z);
            Vector2 targetFlat = new Vector2(currentTarget.x, currentTarget.z);
            if (Vector2.Distance(currentFlat, targetFlat) <= 0.2f)
            {
                currentTarget = ApproximatelySameTarget(currentTarget, endPoint) ? startPoint : endPoint;
                waitTimer = waitAtEnds;
            }
        }

        private float GetSurfaceHeight(Vector3 position)
        {
            if (useSuimonoHeight && suimonoModule != null)
            {
                try
                {
                    float[] heightValues = suimonoModule.SuimonoGetHeightAll(position);
                    if (heightValues != null && heightValues.Length > 0)
                    {
                        return heightValues[0];
                    }
                }
                catch (System.NullReferenceException)
                {
                    useSuimonoHeight = false;
                }
            }

            return waterLevel + Mathf.Sin((Time.time + motionSeed) * manualBobFrequency) * manualBobAmplitude;
        }

        private void ApplySurfaceHeightAndRocking()
        {
            Vector3 position = transform.position;
            position.y = GetSurfaceHeight(position) + heightOffset;
            transform.position = position;
            ApplyRockingRotation();
        }

        private void FaceTarget(Vector3 target, bool instant)
        {
            Vector3 direction = new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z);
            if (direction.sqrMagnitude <= 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = instant
                ? targetRotation
                : Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void ApplyRockingRotation()
        {
            Vector3 euler = transform.rotation.eulerAngles;
            float t = (Time.time + motionSeed) * rockFrequency;
            transform.rotation = Quaternion.Euler(
                Mathf.Sin(t) * pitchAmount,
                euler.y,
                Mathf.Cos(t * 0.8f) * rollAmount);
        }

        private static bool ApproximatelySameTarget(Vector3 a, Vector3 b)
        {
            return (new Vector2(a.x, a.z) - new Vector2(b.x, b.z)).sqrMagnitude < 0.01f;
        }
    }
}
