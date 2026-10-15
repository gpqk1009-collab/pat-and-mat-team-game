using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PrettyGenocide
{
    [DefaultExecutionOrder(-400)]
    public sealed class PGInput : MonoBehaviour
    {
        [Tooltip("Free cursor: aim at the pointer; hold middle mouse to orbit. Tab toggles this at runtime.")]
        public bool freeCursorAim = true;
        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public Vector2 Pointer { get; private set; }
        public bool Sprint { get; private set; }
        public bool SlidePressed { get; private set; }
        public bool PrimaryPressed { get; private set; }
        public bool SecondaryPressed { get; private set; }
        public bool ToggleEnemiesPressed { get; private set; }
        public bool Active { get; private set; } = true;

        private void OnEnable()
        {
            Active = true;
            ApplyCursor();
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("Pretty Genocide requires the Input System package and Active Input Handling = Input System Package (New) or Both.", this);
#endif
        }

        private void Update()
        {
            Move = LookDelta = Vector2.zero;
            Sprint = SlidePressed = PrimaryPressed = SecondaryPressed = ToggleEnemiesPressed = false;
            Pointer = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (!Application.isFocused || keyboard == null || mouse == null) return;
            if (freeCursorAim) Pointer = mouse.position.ReadValue();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Active = false;
                ApplyCursor();
                return;
            }
            if (!Active)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Active = true;
                    ApplyCursor();
                }
                return; // Consume the focus-recapture click, not an aim/fire command.
            }
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                freeCursorAim = !freeCursorAim;
                ApplyCursor();
                return;
            }
            Move = Vector2.ClampMagnitude(new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0)), 1f);
            Sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            SlidePressed = keyboard.spaceKey.wasPressedThisFrame;
            PrimaryPressed = mouse.leftButton.wasPressedThisFrame;
            SecondaryPressed = mouse.rightButton.wasPressedThisFrame;
            ToggleEnemiesPressed = keyboard.f2Key.wasPressedThisFrame;
            if (!freeCursorAim || mouse.middleButton.isPressed)
                LookDelta = mouse.delta.ReadValue(); // Mouse delta is already per-frame; do not multiply by dt.
#endif
        }

        private void ApplyCursor()
        {
            Cursor.lockState = Active && !freeCursorAim ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !Active || freeCursorAim;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Active = false;
            ApplyCursor();
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
