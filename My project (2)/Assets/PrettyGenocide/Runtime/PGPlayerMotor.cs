using UnityEngine;

namespace PrettyGenocide
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-200)]
    public sealed class PGPlayerMotor : MonoBehaviour
    {
        public PGInput input;
        public PGCameraRig cameraRig;
        public Transform visualPose;
        public LayerMask standingMask = 1;
        [Header("Movement")]
        [Min(0f)] public float walkSpeed = 5f;
        [Min(0f)] public float sprintSpeed = 8.5f;
        [Min(0f)] public float aimMoveSpeed = 3.8f;
        [Min(0.1f)] public float acceleration = 65f;
        [Min(0.1f)] public float deceleration = 85f;
        public float gravity = -26f;
        public float turnSharpness = 24f;
        [Header("Slide (direction captured on Space)")]
        [Min(0f)] public float slideSpeed = 15f;
        [Min(0.05f)] public float slideDuration = 0.35f;
        [Range(0f, 1f)] public float slideEndSpeedRatio = 0.6f;
        [Min(0f)] public float slideCooldown = 0.8f;
        [Min(0.6f)] public float slideHeight = 0.95f;
        [Tooltip("슬라이딩 중 시각 모델의 기울기. 물리 캡슐은 똑바로 유지됩니다.")]
        public Vector3 slideVisualTilt = new Vector3(-18f, 0f, -12f);
        [Min(0.1f)] public float poseSharpness = 25f;
        public int maxHealth = 100;

        public bool IsSliding => slideLeft > 0f;
        public bool IsCrouched => controller != null && controller.height < standingHeight - 0.01f;
        public float SlideCooldownRemaining => Mathf.Max(0f, nextSlideTime - Time.time);
        public int Health { get; private set; }
        public int HitsTaken { get; private set; }
        private CharacterController controller;
        private float standingHeight;
        private Vector3 standingCenter;
        private float verticalSpeed;
        private float slideLeft;
        private float nextSlideTime;
        private Vector3 slideDirection;
        private Vector3 spawnPosition;
        private Vector3 horizontalVelocity;
        private Vector3 initialVisualScale;
        private Quaternion initialVisualRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            // Preserve tiny per-frame moves and grounding even at very high frame rates.
            controller.minMoveDistance = 0f;
            standingHeight = controller.height;
            standingCenter = controller.center;
            spawnPosition = transform.position;
            Health = maxHealth;
            initialVisualScale = visualPose != null ? visualPose.localScale : Vector3.one;
            initialVisualRotation = visualPose != null ? visualPose.localRotation : Quaternion.identity;
        }

        private bool CanStand()
        {
            float radius = Mathf.Max(0.05f, controller.radius - 0.025f);
            Vector3 center = transform.position + standingCenter;
            float half = Mathf.Max(0f, standingHeight * 0.5f - controller.radius);
            Collider[] overlaps = Physics.OverlapCapsule(center + Vector3.up * half,
                center - Vector3.up * half, radius, standingMask, QueryTriggerInteraction.Ignore);
            foreach (Collider c in overlaps)
                if (!c.transform.IsChildOf(transform)) return false;
            return true;
        }

        private void Update()
        {
            if (input == null || cameraRig == null || !controller.enabled) return;
            float dt = Time.deltaTime;
            Vector3 look = cameraRig.GetPlanarLook(transform.position);
            if (input.Active && input.SlidePressed && controller.isGrounded && !IsSliding && Time.time >= nextSlideTime)
            {
                slideDirection = look; // Captured on press: not WASD direction, not steerable during the slide.
                slideLeft = Mathf.Max(0.05f, slideDuration);
                nextSlideTime = Time.time + Mathf.Max(slideDuration, slideCooldown);
            }
            bool slidingThisFrame = IsSliding;
            Vector3 horizontal;
            if (slidingThisFrame)
            {
                // Integrate the speed ramp only for the remaining slide time. A long frame
                // must not add an extra full frame of sliding distance.
                float duration = Mathf.Max(0.05f, slideDuration);
                float slideDt = Mathf.Min(slideLeft, dt);
                float t0 = 1f - slideLeft / duration;
                float t1 = 1f - (slideLeft - slideDt) / duration;
                float averageSpeed = slideSpeed * Mathf.Lerp(1f, slideEndSpeedRatio, (t0 + t1) * 0.5f);
                horizontalVelocity = slideDirection * slideSpeed * Mathf.Lerp(1f, slideEndSpeedRatio, t1);
                horizontal = slideDirection * averageSpeed * (slideDt / Mathf.Max(0.00001f, dt));
                slideLeft = Mathf.Max(0f, slideLeft - slideDt);
                look = slideDirection;
            }
            else
            {
                Vector2 move = input.Active ? input.Move : Vector2.zero;
                float speed = input.Sprint ? sprintSpeed : (cameraRig.IsAiming ? aimMoveSpeed : walkSpeed);
                Vector3 desired = cameraRig.FlatRotation * new Vector3(move.x, 0f, move.y) * speed;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired,
                    (move.sqrMagnitude > 0f ? acceleration : deceleration) * dt);
                horizontal = horizontalVelocity;
            }
            if (input.Active && look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look),
                    1f - Mathf.Exp(-turnSharpness * dt));

            bool stayLow = slidingThisFrame || (controller.height < standingHeight - 0.01f && !CanStand());
            float height = stayLow ? Mathf.Clamp(slideHeight, controller.radius * 2f, standingHeight) : standingHeight;
            controller.height = height;
            controller.center = standingCenter + Vector3.up * ((height - standingHeight) * 0.5f);
            if (visualPose != null)
            {
                float blend = 1f - Mathf.Exp(-poseSharpness * dt);
                visualPose.localScale = Vector3.Lerp(visualPose.localScale,
                    Vector3.Scale(initialVisualScale, new Vector3(1f, height / standingHeight, 1f)),
                    blend);
                Quaternion pose = initialVisualRotation * (slidingThisFrame
                    ? Quaternion.Euler(slideVisualTilt) : Quaternion.identity);
                visualPose.localRotation = Quaternion.Slerp(visualPose.localRotation, pose, blend);
            }

            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed = Mathf.Max(-55f, verticalSpeed + gravity * dt);
            CollisionFlags flags = controller.Move((horizontal + Vector3.up * verticalSpeed) * dt);
            if (slidingThisFrame && (flags & CollisionFlags.Sides) != 0)
            {
                slideLeft = 0f;
                horizontalVelocity = Vector3.zero;
            }
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if (transform.position.y < -15f) Respawn();
        }

        public void ReceiveDamage(int amount)
        {
            Health = Mathf.Max(0, Health - Mathf.Max(0, amount));
            HitsTaken++;
            if (Health == 0) Respawn();
        }

        private void Respawn()
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
            verticalSpeed = slideLeft = 0f;
            horizontalVelocity = Vector3.zero;
            Health = maxHealth;
        }
    }
}
