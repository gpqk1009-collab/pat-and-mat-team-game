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
        public float walkSpeed = 5f;
        public float sprintSpeed = 8.5f;
        public float aimMoveSpeed = 3.8f;
        public float gravity = -26f;
        public float turnSharpness = 24f;
        public float slideSpeed = 15f;
        public float slideDuration = 0.35f;
        public float slideCooldown = 0.8f;
        public float slideHeight = 0.95f;
        public int maxHealth = 100;

        public bool IsSliding => slideLeft > 0f;
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

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standingHeight = controller.height;
            standingCenter = controller.center;
            spawnPosition = transform.position;
            Health = maxHealth;
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
                horizontal = slideDirection * slideSpeed;
                slideLeft = Mathf.Max(0f, slideLeft - dt);
                look = slideDirection;
            }
            else
            {
                Vector2 move = input.Active ? input.Move : Vector2.zero;
                float speed = input.Sprint ? sprintSpeed : (cameraRig.IsAiming ? aimMoveSpeed : walkSpeed);
                horizontal = cameraRig.FlatRotation * new Vector3(move.x, 0f, move.y) * speed;
            }
            if (input.Active && look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look),
                    1f - Mathf.Exp(-turnSharpness * dt));

            bool stayLow = slidingThisFrame || (controller.height < standingHeight - 0.01f && !CanStand());
            float height = stayLow ? slideHeight : standingHeight;
            controller.height = height;
            controller.center = standingCenter + Vector3.up * ((height - standingHeight) * 0.5f);
            if (visualPose != null)
                visualPose.localScale = Vector3.Lerp(visualPose.localScale,
                    new Vector3(1f, stayLow ? 0.56f : 1f, 1f), 1f - Mathf.Exp(-25f * dt));

            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed = Mathf.Max(-55f, verticalSpeed + gravity * dt);
            CollisionFlags flags = controller.Move((horizontal + Vector3.up * verticalSpeed) * dt);
            if (slidingThisFrame && (flags & CollisionFlags.Sides) != 0) slideLeft = 0f;
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
            Health = maxHealth;
        }
    }
}
