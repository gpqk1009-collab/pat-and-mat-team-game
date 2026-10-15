using System.Collections.Generic;
using UnityEngine;

namespace PrettyGenocide
{
    public sealed class PGRecallBullet : MonoBehaviour
    {
        public enum FlightState { Outbound, Falling, Anchored, Returning }
        public FlightState State { get; private set; }
        public int Id { get; private set; }
        private PGRevolver owner;
        private Vector3 direction;
        private float distanceLeft;
        private float fallingSpeed;
        private float age;
        private bool delivered;
        private readonly HashSet<int> returnVictims = new HashSet<int>();
        private Renderer bulletRenderer;
        private TrailRenderer trail;

        public void Initialize(PGRevolver weapon, int id, Vector3 travelDirection)
        {
            owner = weapon;
            Id = id;
            direction = travelDirection.normalized;
            distanceLeft = owner.bulletRange;
            State = FlightState.Outbound;
            bulletRenderer = GetComponent<Renderer>();
            if (bulletRenderer != null) bulletRenderer.sharedMaterial = owner.outboundMaterial;
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = owner.trailMaterial;
            trail.time = 0.16f;
            trail.startWidth = 0.065f;
            trail.endWidth = 0.01f;
            trail.minVertexDistance = 0.04f;
            trail.startColor = new Color(1f, 0.78f, 0.25f);
            trail.endColor = new Color(1f, 0.78f, 0.25f, 0f);
        }

        public void BeginReturn()
        {
            if (delivered || State == FlightState.Returning || owner == null) return;
            State = FlightState.Returning;
            returnVictims.Clear();
            if (bulletRenderer != null && owner.returnMaterial != null)
                bulletRenderer.sharedMaterial = owner.returnMaterial;
            if (trail != null)
            {
                trail.time = 0.25f;
                trail.startWidth = 0.11f;
                trail.startColor = new Color(1f, 0.22f, 0.72f);
                trail.endColor = new Color(1f, 0.22f, 0.72f, 0f);
            }
        }

        private void Update()
        {
            if (owner == null) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            float dt = Time.deltaTime;
            switch (State)
            {
                case FlightState.Outbound: StepOutbound(dt); break;
                case FlightState.Falling: StepFalling(dt); break;
                case FlightState.Returning: StepReturning(dt); break;
            }
            // A bullet fired out of the training map is physically recalled, not silently reloaded.
            if ((transform.position.y < -20f || age > 35f) && State != FlightState.Returning)
                owner.RecallNow();
        }

        private void StepOutbound(float dt)
        {
            Vector3 start = transform.position;
            float step = Mathf.Min(owner.bulletSpeed * dt, distanceLeft);
            // SphereCast alone does not cover an initial overlap (e.g. a muzzle touching a wall).
            Collider[] initial = Physics.OverlapSphere(start, owner.bulletRadius, owner.outboundMask,
                QueryTriggerInteraction.Ignore);
            if (initial.Length > 0)
            {
                HitOutbound(initial[0]);
                return;
            }
            if (step > 0f && Physics.SphereCast(start, owner.bulletRadius, direction,
                    out RaycastHit hit, step, owner.outboundMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = start + direction * Mathf.Max(0f, hit.distance - 0.005f);
                HitOutbound(hit.collider);
                return;
            }
            transform.position = start + direction * step;
            distanceLeft -= step;
            if (distanceLeft <= 0.001f) State = FlightState.Falling;
        }

        private void HitOutbound(Collider collider)
        {
            PGDamageable victim = collider.GetComponentInParent<PGDamageable>();
            if (victim != null) victim.TakeDamage(owner.outboundDamage, false);
            State = FlightState.Falling;
            fallingSpeed = 0f;
        }

        private void StepFalling(float dt)
        {
            fallingSpeed = Mathf.Min(35f, fallingSpeed + 22f * dt);
            float distance = fallingSpeed * dt;
            if (Physics.SphereCast(transform.position, owner.bulletRadius, Vector3.down,
                    out RaycastHit hit, distance + 0.012f, owner.worldMask, QueryTriggerInteraction.Ignore))
            {
                transform.position += Vector3.down * Mathf.Max(0f, hit.distance - 0.004f);
                State = FlightState.Anchored;
                return;
            }
            transform.position += Vector3.down * distance;
        }

        private void StepReturning(float dt)
        {
            Physics.SyncTransforms(); // At most six return bullets in this prototype.
            Vector3 start = transform.position;
            Vector3 target = owner.RecallPosition;
            Vector3 end = Vector3.MoveTowards(start, target, owner.returnSpeed * dt);
            // A swept capsule reports EVERY overlap, including targets hidden behind walls.
            // World colliders are intentionally not used to block the return path.
            Collider[] overlaps = Physics.OverlapCapsule(start, end, owner.returnRadius,
                owner.returnDamageMask, QueryTriggerInteraction.Collide);
            foreach (Collider hit in overlaps)
            {
                PGDamageable victim = hit.GetComponentInParent<PGDamageable>();
                if (victim != null)
                {
                    int key = victim.GetInstanceID();
                    if (returnVictims.Add(key)) victim.TakeDamage(owner.returnDamage, true);
                    continue;
                }
                PGEnemyProjectile enemyBullet = hit.GetComponentInParent<PGEnemyProjectile>();
                if (enemyBullet != null) enemyBullet.Intercept();
            }
            transform.position = end;
            if ((end - target).sqrMagnitude <= owner.catchRadius * owner.catchRadius)
            {
                delivered = true;
                owner.OnBulletRecovered(Id);
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Avoid a permanently lost chamber if an external script removes a projectile.
            if (!delivered && owner != null) owner.OnBulletDestroyedUnexpectedly(Id);
        }
    }
}
