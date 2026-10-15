using System.Collections.Generic;
using UnityEngine;

namespace PrettyGenocide
{
    [DefaultExecutionOrder(-100)]
    public sealed class PGRevolver : MonoBehaviour
    {
        public enum CyclePhase { Ready, RecallDelay, Recalling }
        [Header("References")]
        public PGInput input;
        public PGCameraRig cameraRig;
        public Transform weaponPivot;
        public Transform muzzle;
        public Transform recallSocket;
        public Light muzzleFlash;
        public Material outboundMaterial;
        public Material returnMaterial;
        public Material trailMaterial;
        public LayerMask outboundMask;
        public LayerMask worldMask;
        public LayerMask returnDamageMask;
        public int projectileLayer = 2;

        [Header("Revolver")]
        public float shotInterval = 0.18f;
        public float aimWarmup = 0.08f;
        public float autoRecallDelay = 0.25f;
        public float bulletSpeed = 55f;
        public float bulletRange = 45f;
        public float bulletRadius = 0.07f;
        public int outboundDamage = 20;
        public float returnSpeed = 44f;
        public float returnRadius = 0.22f;
        public float catchRadius = 0.16f;
        public int returnDamage = 35;

        public bool IsAiming { get; private set; }
        public CyclePhase Phase { get; private set; }
        public bool IsRecalling => Phase != CyclePhase.Ready;
        public int Ammo => ledger.Loaded;
        public int FieldBullets => ledger.Outstanding;
        public Vector3 RecallPosition => recallSocket != null ? recallSocket.position : transform.position + Vector3.up;
        private readonly PGBulletLedger ledger = new PGBulletLedger(6);
        private readonly Dictionary<int, PGRecallBullet> bullets = new Dictionary<int, PGRecallBullet>();
        private float nextShotTime;
        private float recallStartsAt;
        private float flashUntil;
        private bool shuttingDown;

        private void Start()
        {
            if (input == null || cameraRig == null || muzzle == null)
            {
                Debug.LogError("PGRevolver: assign input, cameraRig and muzzle, or generate the supplied prototype scene.", this);
                enabled = false;
            }
            if (muzzleFlash != null) muzzleFlash.enabled = false;
        }

        private void Update()
        {
            if (input == null || cameraRig == null || muzzle == null) return;
            if (muzzleFlash != null) muzzleFlash.enabled = Time.time < flashUntil;
            if (input.Active && input.SecondaryPressed)
            {
                SetAiming(false);
                RequestRecall(0f);
            }
            else if (input.Active && input.PrimaryPressed)
            {
                // First click exclusively enters aim; it can NEVER fall through to Fire().
                if (!IsAiming && !IsRecalling)
                {
                    SetAiming(true);
                    nextShotTime = Mathf.Max(nextShotTime, Time.time + aimWarmup);
                }
                else if (IsAiming && !IsRecalling && Time.time >= nextShotTime)
                    Fire();
            }
            UpdateWeaponPose();
            if (Phase == CyclePhase.RecallDelay && Time.time >= recallStartsAt) StartReturnWave();
            if (IsRecalling && ledger.Outstanding == 0) Phase = CyclePhase.Ready;
        }

        private void SetAiming(bool aiming)
        {
            IsAiming = aiming;
            if (cameraRig != null) cameraRig.SetAiming(aiming);
        }

        private void UpdateWeaponPose()
        {
            if (weaponPivot == null) return;
            if (IsAiming)
            {
                Vector3 delta = cameraRig.GetAimPoint() - weaponPivot.position;
                if (delta.sqrMagnitude > 0.001f) weaponPivot.rotation = Quaternion.LookRotation(delta);
            }
            else weaponPivot.localRotation = Quaternion.Euler(48f, 0f, 0f);
        }

        private void Fire()
        {
            UpdateWeaponPose();
            if (!ledger.TryIssue(out int id)) return;
            Vector3 origin = muzzle.position;
            // Never spawn beyond a wall protruding between the player's chest and the muzzle.
            Vector3 chest = RecallPosition;
            Vector3 toMuzzle = origin - chest;
            if (toMuzzle.sqrMagnitude > 0.0001f && Physics.SphereCast(chest, bulletRadius,
                    toMuzzle.normalized, out RaycastHit obstruction, toMuzzle.magnitude,
                    worldMask, QueryTriggerInteraction.Ignore))
                origin = chest + toMuzzle.normalized * Mathf.Max(0f, obstruction.distance - 0.01f);
            Ray aimRay = cameraRig.GetAimRay();
            Vector3 target = cameraRig.GetAimPoint();
            Vector3 direction = target - origin;
            if (direction.sqrMagnitude < 0.001f || Vector3.Dot(direction, aimRay.direction) <= 0f)
                direction = aimRay.direction;
            SpawnBullet(id, origin, direction.normalized);
            nextShotTime = Time.time + Mathf.Max(0.01f, shotInterval);
            flashUntil = Time.time + 0.04f;
            cameraRig.AddShotKick();
            if (Ammo == 0) RequestRecall(autoRecallDelay); // Keep aiming after an automatic full-cylinder return.
        }

        private PGRecallBullet SpawnBullet(int id, Vector3 origin, Vector3 direction)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "RevolverBullet_" + id;
            go.layer = projectileLayer;
            Collider collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider); // Swept queries, not a Rigidbody or trigger, own player-bullet collision.
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * bulletRadius * 2f;
            PGRecallBullet bullet = go.AddComponent<PGRecallBullet>();
            bullet.Initialize(this, id, direction);
            bullets[id] = bullet;
            return bullet;
        }

        public void RecallNow() => RequestRecall(0f);

        private void RequestRecall(float delay)
        {
            if (ledger.Outstanding == 0) return;
            if (Phase == CyclePhase.Recalling) return;
            Phase = CyclePhase.RecallDelay;
            recallStartsAt = Time.time + Mathf.Max(0f, delay);
            if (delay <= 0f) StartReturnWave();
        }

        private void StartReturnWave()
        {
            Phase = CyclePhase.Recalling;
            foreach (PGRecallBullet bullet in bullets.Values)
                if (bullet != null) bullet.BeginReturn();
        }

        public void OnBulletRecovered(int id)
        {
            if (!ledger.TryRecover(id)) return; // Duplicate callbacks never create extra ammunition.
            bullets.Remove(id);
            if (ledger.Outstanding == 0 && IsRecalling) Phase = CyclePhase.Ready;
        }

        public void OnBulletDestroyedUnexpectedly(int id)
        {
            if (shuttingDown || !isActiveAndEnabled || !bullets.ContainsKey(id)) return;
            // Preserve the same outstanding ID; respawn it at the hand and catch normally.
            Debug.LogWarning("An active revolver bullet was externally removed. Recovering the same chamber ID.", this);
            PGRecallBullet replacement = SpawnBullet(id, RecallPosition, Vector3.forward);
            replacement.BeginReturn();
        }

        private void OnApplicationQuit() { shuttingDown = true; }
        private void OnDisable() { shuttingDown = true; }
        private void OnEnable() { shuttingDown = false; }
        private void OnDestroy()
        {
            shuttingDown = true;
            foreach (PGRecallBullet bullet in bullets.Values)
                if (bullet != null) Destroy(bullet.gameObject);
            bullets.Clear();
        }
    }
}
