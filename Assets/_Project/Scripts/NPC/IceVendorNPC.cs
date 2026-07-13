using UnityEngine;
using System.Collections;
using GanhHangRong.Core;
using GanhHangRong.Player;

namespace GanhHangRong.NPC
{
    /// <summary>
    /// NPC Ông Ba bán đá — đi tuần trên đường, được gọi tới để giao đá cho xe đẩy.
    /// </summary>
    public class IceVendorNPC : MonoBehaviour
    {
        public enum VendorState { Patrolling, DeliveringIce, Dumping, Leaving }

        [Header("Patrol")]
        [SerializeField] private Vector3[] patrolPoints = new Vector3[]
        {
            new Vector3(60f, 0f, -3f),
            new Vector3(10f, 0f, -3f)
        };
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float deliverySpeed = 4f;

        [Header("Delivery")]
        [SerializeField] private float iceDeliveryDuration = 2.5f; // thời gian "đổ đá"
        [SerializeField] private int iceCost = 5000;

        private VendorState state = VendorState.Patrolling;
        private int patrolIndex = 0;
        private Transform targetTransform;
        private bool patrolForward = true;

        public VendorState CurrentState => state;
        public bool IsAvailable => state == VendorState.Patrolling;

        private void Start()
        {
            // Spawn ở đầu patrol
            transform.position = patrolPoints[0] + Vector3.up * 0f;
        }

        private void Update()
        {
            switch (state)
            {
                case VendorState.Patrolling:
                    Patrol();
                    break;
                case VendorState.DeliveringIce:
                    MoveToTarget();
                    break;
                case VendorState.Leaving:
                    MoveToLeavePoint();
                    break;
            }
        }

        // ─── Patrol ────────────────────────────────────────────────────
        private void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            Vector3 dest = patrolPoints[patrolIndex];
            dest.y = transform.position.y;
            MoveToward(dest, patrolSpeed);

            if (Vector3.Distance(transform.position, dest) < 0.5f)
            {
                // Ping-pong
                if (patrolForward)
                {
                    patrolIndex++;
                    if (patrolIndex >= patrolPoints.Length)
                    {
                        patrolIndex = patrolPoints.Length - 2;
                        patrolForward = false;
                    }
                }
                else
                {
                    patrolIndex--;
                    if (patrolIndex < 0)
                    {
                        patrolIndex = 1;
                        patrolForward = true;
                    }
                }
            }
        }

        // ─── Delivery ──────────────────────────────────────────────────
        public void StartDelivery(Transform cartTransform)
        {
            if (state != VendorState.Patrolling) return;
            state = VendorState.DeliveringIce;
            targetTransform = cartTransform;
        }

        private void MoveToTarget()
        {
            if (targetTransform == null) { state = VendorState.Patrolling; return; }

            Vector3 dest = targetTransform.position;
            dest.y = transform.position.y;
            MoveToward(dest, deliverySpeed);

            if (Vector3.Distance(transform.position, dest) < 2.0f)
            {
                state = VendorState.Dumping;
                StartCoroutine(DumpIce());
            }
        }

        private IEnumerator DumpIce()
        {
            // Quay mặt về xe đẩy
            if (targetTransform != null)
            {
                Vector3 dir = (targetTransform.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            // Chờ "đổ đá"
            yield return new WaitForSeconds(iceDeliveryDuration);

            // Refill đá
            var stats = FindAnyObjectByType<PlayerStats>();
            if (stats != null)
                stats.RefillIce();

            // Rời đi
            state = VendorState.Leaving;
        }

        // ─── Leaving ───────────────────────────────────────────────────
        private void MoveToLeavePoint()
        {
            // Chạy thẳng ra đầu đường patrol xa nhất rồi disable
            Vector3 leavePoint = patrolPoints[patrolPoints.Length - 1];
            leavePoint.y = transform.position.y;
            MoveToward(leavePoint, deliverySpeed * 1.2f);

            if (Vector3.Distance(transform.position, leavePoint) < 1f)
            {
                // Ông Ba đi rồi — tắt đi, sẽ respawn ở đầu patrol sau một lúc
                state = VendorState.Patrolling;
                transform.position = patrolPoints[0];
                patrolIndex = 0;
                patrolForward = true;
            }
        }

        // ─── Util ──────────────────────────────────────────────────────
        private void MoveToward(Vector3 dest, float speed)
        {
            Vector3 dir = (dest - transform.position).normalized;
            dir.y = 0f;
            transform.position = Vector3.MoveTowards(transform.position, dest, speed * Time.deltaTime);
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
            }
        }
    }
}
