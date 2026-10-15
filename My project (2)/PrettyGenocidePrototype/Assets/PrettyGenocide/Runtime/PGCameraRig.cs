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
        public float recoilPitchDegrees = 2.2f;
        public float shotShakeDegrees = 0.8f;
        public float shotShakeSeconds = 0.12f;

        public bool IsAiming { get; private set; }
        public Quaternion FlatRotation => Quaternion.Euler(0f, yaw, 0f);
        public Camera ViewCamera => viewCamera;
        private float yaw;
        private float pitch;
        private float aimBlend;
        private float aimPunchLeft;
        private float shotShakeLeft;
        private float recoil;
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
            recoil = Mathf.Lerp(recoil, 0f, 1f - Mathf.Exp(-20f * Time.deltaTime));
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
            float width = Mathf.Max(1, Screen.width);
            float height = Mathf.Max(1, Screen.height);
            float tangent = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Vector3 local = new Vector3((pointer.x / width * 2f - 1f) * width / height * tangent,
                (pointer.y / height * 2f - 1f) * tangent, 1f);
            return new Ray(origin, rotation * local.normalized);
        }

        public Vector3 GetAimPoint()
        {
            Ray ray = GetAimRay();
            return Physics.Raycast(ray, out RaycastHit hit, aimRange, aimMask, QueryTriggerInteraction.Ignore)
                ? hit.point : ray.GetPoint(aimRange);
        }

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
            Quaternion effect = Quaternion.Euler(
                (-recoil + nx * shotShakeDegrees * shake) * motionScale,
                ny * shotShakeDegrees * shake * motionScale,
                (aimPunchDegrees * punch + nx * shotShakeDegrees * 0.45f * shake) * motionScale);
            Vector3 offset = rotation * new Vector3(punch * 0.14f, ny * shake * 0.018f, punch * 0.18f) * motionScale;
            Vector3 pivot = target.position + Vector3.up * focusHeight;
            viewCamera.transform.SetPositionAndRotation(ClampCamera(pivot, position + offset), rotation * effect);
            viewCamera.fieldOfView = fov;
        }
    }
}
