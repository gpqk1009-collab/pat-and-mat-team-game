using UnityEngine;

namespace PrettyGenocide
{
    [DefaultExecutionOrder(-50)]
    public sealed class PGEnemyProjectile : MonoBehaviour
    {
        private Vector3 direction;
        private LayerMask hitMask;
        private float speed;
        private float life = 7f;
        private int damage;
        private bool removed;

        public void Initialize(Vector3 velocity, LayerMask mask, int hitDamage)
        {
            speed = velocity.magnitude;
            direction = speed > 0.001f ? velocity / speed : Vector3.forward;
            hitMask = mask;
            damage = hitDamage;
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
            if (removed) return;
            float distance = speed * Time.deltaTime;
            if (Physics.SphereCast(transform.position, 0.16f, direction, out RaycastHit hit,
                    distance, hitMask, QueryTriggerInteraction.Ignore))
            {
                PGPlayerMotor player = hit.collider.GetComponentInParent<PGPlayerMotor>();
                if (player != null) player.ReceiveDamage(damage);
                Intercept();
                return;
            }
            transform.position += direction * distance;
            life -= Time.deltaTime;
            if (life <= 0f) Intercept();
        }
    }
}
