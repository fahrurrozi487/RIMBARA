using UnityEngine;
using UnityEngine.InputSystem;

namespace Rimbara.Player
{
    // Separate RIMBARA action map for interaction, water, pause and the test
    // driver. VolunteerRig feeds movement into the unmodified official motor;
    // the Starter Assets PlayerInput map is deactivated to avoid double input.
    [DefaultExecutionOrder(-100)]
    public sealed class VolunteerInput : MonoBehaviour
    {
        private InputActionMap actions;
        private InputAction move;
        private InputAction look;
        private InputAction sprint;
        private InputAction interact;
        private InputAction spray;
        private InputAction pause;
        private bool testMode;
        private Vector2 testMove;
        private Vector2 testLook;
        private bool testSprint;
        private bool testInteract;
        private bool testSpray;
        public bool GameplayEnabled { get; private set; }
        public Vector2 Move => GameplayEnabled ? testMode ? testMove : move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => GameplayEnabled ? testMode ? testLook : look.ReadValue<Vector2>() : Vector2.zero;
        public bool SprintHeld => GameplayEnabled && (testMode ? testSprint : sprint.IsPressed());
        public bool InteractPressed => GameplayEnabled && (testMode ? testInteract : interact.WasPressedThisFrame());
        public bool SprayHeld => GameplayEnabled && (testMode ? testSpray : spray.IsPressed());
        public bool PausePressed => !testMode && pause.WasPressedThisFrame();

        private void Awake()
        {
            actions = new InputActionMap("Rimbara");
            move = actions.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            look = actions.AddAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            sprint = actions.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprint.AddBinding("<Keyboard>/rightShift");
            interact = actions.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");
            spray = actions.AddAction("Spray", InputActionType.Button, "<Mouse>/leftButton");
            spray.AddBinding("<Gamepad>/rightTrigger");
            pause = actions.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");
        }

        private void OnEnable() { actions?.Enable(); }
        private void OnDisable() { actions?.Disable(); }

        public void SetGameplayEnabled(bool enabled)
        {
            GameplayEnabled = enabled;
            if (!enabled) SetTestInput(Vector2.zero, Vector2.zero, false, false, false);
        }

        public void EnableTestDriver(bool enabled) { testMode = enabled; }

        public void SetTestInput(Vector2 movement, Vector2 cameraLook, bool running, bool interacting, bool spraying)
        {
            testMove = Vector2.ClampMagnitude(movement, 1f);
            testLook = cameraLook;
            testSprint = running;
            testInteract = interacting;
            testSpray = spraying;
        }

        private void OnDestroy() { actions?.Dispose(); }
    }
}
