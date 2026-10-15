using UnityEngine;

namespace PrettyGenocide
{
    [DefaultExecutionOrder(-50)]
    public sealed class PGEnemyProjectile : MonoBehaviour
    {
        [Min(0.01f)] public float collisionRadius = 0.16f;
        [Min(0.1f)] public float lifetime = 7f;
        private Vector3 direction;
        private LayerMask hitMask;
        private float speed;
        private float life;
        private int damage;
        private bool removed;

        public void Initialize(Vector3 velocity, LayerMask mask, int hitDamage)
        {
            speed = velocity.magnitude;
            direction = speed > 0.001f ? velocity / speed : Vector3.forward;
            hitMask = mask;
            damage = hitDamage;
            life = lifetime;
        }

        public void Intercept()
        {
            if (removed) return;
            removed = true;
            Collider c = GetComponent<Collider>();
            if (c != null) c.enabled = false;
            Destroy(gameObject);
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        private void Step(float dt)
        {
            if (removed) return;
            Physics.SyncTransforms();
            Vector3 start = transform.position;
            float distance = speed * dt;
            bool blocked = Physics.SphereCast(start, collisionRadius, direction, out RaycastHit hit,
                distance, hitMask, QueryTriggerInteraction.Ignore);
            float hitFraction = blocked && distance > 0f ? hit.distance / distance : 1f;
            if (PGRecallBullet.InterceptsSegment(start, start + direction * distance, collisionRadius, dt, hitFraction))
            {
                Intercept();
                return;
            }
            if (blocked)
            {
                PGPlayerMotor player = hit.collider.GetComponentInParent<PGPlayerMotor>();
                if (player != null) player.ReceiveDamage(damage);
                Intercept();
                return;
            }
            transform.position += direction * distance;
            life -= dt;
            if (life <= 0f) Intercept();
        }
    }
}
