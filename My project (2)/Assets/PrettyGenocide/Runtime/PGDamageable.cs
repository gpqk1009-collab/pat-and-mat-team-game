using UnityEngine;

namespace PrettyGenocide
{
    public sealed class PGDamageable : MonoBehaviour
    {
        [Tooltip("오른쪽 조준 대상 패널에 표시할 이름. 비워 두면 HUD의 기본 이름을 사용합니다.")]
        public string displayName = "훈련 표적";
        public int maxHealth = 150;
        public float respawnSeconds = 3f;
        public Renderer[] renderers;
        public Collider[] colliders;
        public int Health { get; private set; }
        public bool IsAlive => Health > 0;
        public int OutboundHits { get; private set; }
        public int ReturnHits { get; private set; }
        private float respawnAt;
        private Vector3 initialScale;
        private float flashUntil;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
            if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>();
            Health = maxHealth;
            initialScale = transform.localScale;
        }

        public void TakeDamage(int amount, bool returning)
        {
            if (!IsAlive) return;
            Health = Mathf.Max(0, Health - Mathf.Max(0, amount));
            if (returning) ReturnHits++; else OutboundHits++;
            flashUntil = Time.time + 0.08f;
            if (Health == 0)
            {
                SetVisible(false);
                respawnAt = Time.time + Mathf.Max(0.1f, respawnSeconds);
            }
        }

        private void Update()
        {
            if (!IsAlive)
            {
                if (Time.time >= respawnAt)
                {
                    Health = maxHealth;
                    SetVisible(true);
                }
                return;
            }
            transform.localScale = initialScale * (Time.time < flashUntil ? 1.05f : 1f);
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer r in renderers) if (r != null) r.enabled = visible;
            foreach (Collider c in colliders) if (c != null) c.enabled = visible;
        }
    }
}
