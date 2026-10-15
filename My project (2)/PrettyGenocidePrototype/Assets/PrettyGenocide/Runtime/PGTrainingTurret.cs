using UnityEngine;

namespace PrettyGenocide
{
    public sealed class PGTrainingTurret : MonoBehaviour
    {
        public Transform player;
        public Transform muzzle;
        public PGDamageable health;
        public Material projectileMaterial;
        public LayerMask projectileHitMask;
        public int projectileLayer;
        public bool attacksEnabled;
        public float interval = 1.5f;
        public float projectileSpeed = 8f;
        private float nextShot;

        private void Update()
        {
            if (!attacksEnabled || player == null || muzzle == null || health == null || !health.IsAlive) return;
            if (Time.time < nextShot) return;
            nextShot = Time.time + interval;
            Vector3 target = player.position + Vector3.up * 0.85f;
            Vector3 delta = target - muzzle.position;
            if (delta.sqrMagnitude < 0.1f) return;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EnemyProjectile";
            go.layer = projectileLayer;
            go.transform.position = muzzle.position;
            go.transform.localScale = Vector3.one * 0.34f;
            go.GetComponent<Renderer>().sharedMaterial = projectileMaterial;
            go.GetComponent<Collider>().isTrigger = true; // Return capsule queries include this trigger.
            go.AddComponent<PGEnemyProjectile>().Initialize(delta.normalized * projectileSpeed, projectileHitMask, 10);
        }
    }
}
