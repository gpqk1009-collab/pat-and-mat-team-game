using UnityEngine;

namespace PrettyGenocide
{
    [DefaultExecutionOrder(-300)]
    public sealed class PGCameraRig : MonoBehaviour
    {
        [Header("References")]
        public Transform target;
        public PGInput input;
        public Camera viewCamera;
        public LayerMask aimMask = ~0;
        public LayerMask obstructionMask = 1;

        [Header("Orbit")]
        public float sensitivity = 0.13f;
        public float focusHeight = 1.35f;
        public float initialYaw;
        public float initialPitch = 12f;
        public float minPitch = -35f;
        public float maxPitch = 65f;
        public Vector3 normalOffset = new Vector3(0.65f, 0.25f, -5.8f);
        public Vector3 aimOffset = new Vector3(0.85f, 0.08f, -2.25f);
        public float normalFov = 72f;
        public float aimFov = 44f;
        [Min(0.02f)] public float aimTransitionSeconds = 0.11f;
        public float aimRange = 90f;
        public float collisionRadius = 0.18f;

        [Header("Visual effects only; never modify the ballistic ray")]
        [Range(0f, 1f)] public float motionScale = 1f;
        public float aimPunchDegrees = 4.5f;
        public float aimPunchSeconds = 0.18f;
        public float recoilPitchDegrees = 5f;
        public float shotShakeDegrees = 1.2f;
        public float shotShakeSeconds = 0.12f;
        [Min(0.1f)] public float recoilRecovery = 20f;

        [Header("회수 중 지속 흔들림 · 시각 효과만 적용")]
        [Min(0f)] public float recallShakeDegrees = 0.2f;
        [Min(0f)] public float recallShakePosition = 0.005f;
        [Min(0.1f)] public float recallShakeFrequency = 20f;
        [Min(0.01f)] public float recallShakeAttack = 0.08f;
        [Min(0.01f)] public float recallShakeRelease = 0.18f;

        [Header("회수 탄환이 적에게 명중했을 때의 추가 충격")]
        [Min(0f)] public float recallHitShakeDegrees = 8f;
        [Min(0f)] public float recallHitShakePosition = 0.22f;
        [Min(0f)] public float recallHitKickDegrees = 5f;
        [Min(0.01f)] public float recallHitShakeSeconds = 0.24f;
        [Range(1f, 3f)] public float recallHitMaxStack = 1.6f;

        public bool IsAiming { get; private set; }
        public Quaternion FlatRotation => Quaternion.Euler(0f, yaw, 0f);
        public Camera ViewCamera => viewCamera;
        private float yaw;
        private float pitch;
        private float aimBlend;
        private float aimPunchLeft;
        private float shotShakeLeft;
        private float recoil;
        private bool recallPullActive;
        private float recallPullBlend;
        private float recallHitLeft;
        private float recallHitAmplitude;
        private bool initialized;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            yaw = initialYaw;
            pitch = initialPitch;
            initialized = true;
        }

        private void Update()
        {
            Initialize();
            if (input != null && input.Active)
            {
                yaw += input.LookDelta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - input.LookDelta.y * sensitivity, minPitch, maxPitch);
            }
            aimBlend = Mathf.MoveTowards(aimBlend, IsAiming ? 1f : 0f,
                Time.deltaTime / Mathf.Max(0.02f, aimTransitionSeconds));
            aimPunchLeft = Mathf.Max(0f, aimPunchLeft - Time.deltaTime);
            shotShakeLeft = Mathf.Max(0f, shotShakeLeft - Time.deltaTime);
            recoil = Mathf.Lerp(recoil, 0f, 1f - Mathf.Exp(-recoilRecovery * Time.deltaTime));
            float recallFade = recallPullActive ? recallShakeAttack : recallShakeRelease;
            recallPullBlend = Mathf.MoveTowards(recallPullBlend, recallPullActive ? 1f : 0f,
                Time.deltaTime / Mathf.Max(0.01f, recallFade));
            recallHitLeft = Mathf.Max(0f, recallHitLeft - Time.deltaTime);
        }

        public void SetAiming(bool value)
        {
            if (value && !IsAiming) aimPunchLeft = aimPunchSeconds;
            IsAiming = value;
        }

        public void AddShotKick()
        {
            recoil = Mathf.Min(recoil + recoilPitchDegrees, recoilPitchDegrees * 2f);
            shotShakeLeft = shotShakeSeconds;
        }

        public void SetRecallPull(bool active) => recallPullActive = active;

        public void AddRecallHitKick()
        {
            // Multiple pellets/targets may hit together; keep the combined impulse bounded.
            float remaining = Mathf.Clamp01(recallHitLeft / Mathf.Max(0.01f, recallHitShakeSeconds));
            recallHitAmplitude = Mathf.Min(recallHitAmplitude * remaining * remaining + 1f,
                Mathf.Max(1f, recallHitMaxStack));
            recallHitLeft = Mathf.Max(0.01f, recallHitShakeSeconds);
        }

        private void OnDisable()
        {
            // The weapon still owns the pull state while this camera is temporarily disabled.
            recallPullBlend = recallHitLeft = recallHitAmplitude = 0f;
        }

        private Vector3 ClampCamera(Vector3 pivot, Vector3 desired)
        {
            Vector3 delta = desired - pivot;
            float length = delta.magnitude;
            if (length > 0.001f && Physics.SphereCast(pivot, collisionRadius, delta / length,
                    out RaycastHit hit, length, obstructionMask, QueryTriggerInteraction.Ignore))
                return pivot + delta / length * Mathf.Max(0f, hit.distance - 0.04f);
            return desired;
        }

        private void GetLogicalPose(out Vector3 position, out Quaternion rotation, out float fov)
        {
            Initialize();
            Vector3 pivot = target != null ? target.position + Vector3.up * focusHeight : transform.position;
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = Vector3.Lerp(normalOffset, aimOffset, Mathf.SmoothStep(0f, 1f, aimBlend));
            position = ClampCamera(pivot, pivot + rotation * offset);
            fov = Mathf.Lerp(normalFov, aimFov, aimBlend);
        }

        public Ray GetAimRay()
        {
            GetLogicalPose(out Vector3 origin, out Quaternion rotation, out float fov);
            Vector2 pointer = input != null ? input.Pointer : new Vector2(Screen.width / 2f, Screen.height / 2f);
            // Perspective ray from the unshaken camera pose. Additive kick/shake never enters this math.
            Rect viewport = viewCamera != null ? viewCamera.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            if (input == null || !input.freeCursorAim) pointer = viewport.center;
            float width = Mathf.Max(1f, viewport.width);
            float height = Mathf.Max(1f, viewport.height);
            float tangent = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Vector3 local = new Vector3(((pointer.x - viewport.x) / width * 2f - 1f) * width / height * tangent,
                ((pointer.y - viewport.y) / height * 2f - 1f) * tangent, 1f);
            return new Ray(origin, rotation * local.normalized);
        }

        public Vector3 GetAimPoint()
        {
            Ray ray = GetAimRay();
            return TryGetAimHit(out RaycastHit hit)
                ? hit.point : ray.GetPoint(aimRange);
        }

        public bool TryGetAimHit(out RaycastHit hit) => Physics.Raycast(GetAimRay(), out hit,
            aimRange, aimMask, QueryTriggerInteraction.Ignore);

        public Vector3 GetPlanarLook(Vector3 playerPosition)
        {
            Vector3 direction = GetAimPoint() - playerPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = FlatRotation * Vector3.forward;
            return direction.normalized;
        }

        private void LateUpdate()
        {
            if (viewCamera == null || target == null) return;
            GetLogicalPose(out Vector3 position, out Quaternion rotation, out float fov);
            float punchT = aimPunchSeconds > 0f ? aimPunchLeft / aimPunchSeconds : 0f;
            float punch = Mathf.Sin((1f - punchT) * Mathf.PI) * punchT;
            float shake = shotShakeSeconds > 0f ? shotShakeLeft / shotShakeSeconds : 0f;
            float nx = Mathf.PerlinNoise(Time.time * 43f, 13f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(7f, Time.time * 51f) * 2f - 1f;
            float recallTime = Time.time * recallShakeFrequency;
            Vector3 pullNoise = new Vector3(
                Mathf.PerlinNoise(recallTime, 31f) * 2f - 1f,
                Mathf.PerlinNoise(19f, recallTime * 1.17f) * 2f - 1f,
                Mathf.PerlinNoise(recallTime * 0.91f, 61f) * 2f - 1f);
            float hitT = Mathf.Clamp01(recallHitLeft / Mathf.Max(0.01f, recallHitShakeSeconds));
            float hitEnvelope = recallHitAmplitude * hitT * hitT;
            Vector3 hitNoise = new Vector3(nx, ny, Mathf.Sin(Time.time * 73f));
            Vector3 recallAngles = pullNoise * (recallShakeDegrees * recallPullBlend)
                + hitNoise * (recallHitShakeDegrees * hitEnvelope)
                + Vector3.left * (recallHitKickDegrees * hitEnvelope);
            Quaternion effect = Quaternion.Euler(
                (-recoil + nx * shotShakeDegrees * shake + recallAngles.x) * motionScale,
                (ny * shotShakeDegrees * shake + recallAngles.y) * motionScale,
                (aimPunchDegrees * punch + nx * shotShakeDegrees * 0.45f * shake + recallAngles.z) * motionScale);
            Vector3 localOffset = new Vector3(punch * 0.14f, ny * shake * 0.018f, punch * 0.18f)
                + pullNoise * (recallShakePosition * recallPullBlend)
                + hitNoise * (recallHitShakePosition * hitEnvelope);
            Vector3 offset = rotation * localOffset * motionScale;
            Vector3 pivot = target.position + Vector3.up * focusHeight;
            viewCamera.transform.SetPositionAndRotation(ClampCamera(pivot, position + offset), rotation * effect);
            viewCamera.fieldOfView = fov;
        }
    }
}
