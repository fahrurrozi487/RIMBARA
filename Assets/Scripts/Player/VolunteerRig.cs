using System;
using System.Reflection;
using StarterAssets;
using Rimbara.Mission;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rimbara.Player
{
    // Adapts RIMBARA's action map to the real Unity Starter Assets motor.
    // No replacement movement implementation is hidden in this component.
    [DefaultExecutionOrder(-60)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class VolunteerRig : MonoBehaviour
    {
        [SerializeField] private VolunteerInput controls;
        [SerializeField] private ThirdPersonController starterController;
        [SerializeField] private StarterAssetsInputs starterInputs;
        [SerializeField] private PlayerInput starterPlayerInput;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private CinemachineCamera gameplayCamera;
        [SerializeField] private CinemachineCamera overviewCamera;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0.1f, -21f);
        private CharacterController capsule;
        public bool ControlsEnabled { get; private set; }
        public Camera ViewCamera => viewCamera;
        public Transform CameraTarget => cameraTarget;
        public Animator Animator => animator;
        public ThirdPersonController OfficialController => starterController;
        public CharacterController Capsule => capsule;

        public void Configure(VolunteerInput input, ThirdPersonController motor, StarterAssetsInputs motorInput, PlayerInput playerInput,
            Animator characterAnimator, Transform target)
        {
            controls = input;
            starterController = motor;
            starterInputs = motorInput;
            starterPlayerInput = playerInput;
            animator = characterAnimator;
            cameraTarget = target;
        }

        public void ConfigureScene(Camera camera, CinemachineCamera gameplay, CinemachineCamera overview, Vector3 spawn)
        {
            viewCamera = camera;
            gameplayCamera = gameplay;
            overviewCamera = overview;
            spawnPosition = spawn;
        }

        private void Awake()
        {
            capsule = GetComponent<CharacterController>();
            if (starterController == null || starterInputs == null)
                throw new InvalidOperationException("The official Unity Starter Assets controller is required on the volunteer prefab.");
            // PlayerInput retains its KeyboardMouse control scheme, which the
            // official camera uses, but our action map owns the actual input.
            if (starterPlayerInput != null) starterPlayerInput.DeactivateInput();
            starterInputs.cursorInputForLook = false;
            starterInputs.lookFromTouch = false;
            SetControlsEnabled(false);
        }

        private void Update()
        {
            if (!ControlsEnabled) return;
            starterInputs.MoveInput(controls.Move);
            starterInputs.LookInput(controls.Look * 0.12f);
            starterInputs.SprintInput(controls.SprintHeld);
            starterInputs.JumpInput(false); // Jumping is intentionally outside this walking restoration MVP.
        }

        private void Start()
        {
            // PlayerInput activates its map in OnEnable, potentially after this
            // adapter's early Awake. All OnEnable calls precede Start, so disable
            // the vendor map again here while retaining its control scheme.
            if (starterPlayerInput != null) starterPlayerInput.DeactivateInput();
        }

        private void LateUpdate()
        {
            if (!ControlsEnabled) return;
            if (controls.SprayHeld && viewCamera != null)
                transform.rotation = Quaternion.Euler(0f, viewCamera.transform.eulerAngles.y, 0f);
            // Recovery, not a fail condition. Counters and mission state are untouched.
            if (transform.position.y < -8f || Mathf.Abs(transform.position.x) > 64f
                || transform.position.z < -42f || transform.position.z > 90f)
                Teleport(spawnPosition);
        }

        public void SetControlsEnabled(bool enabled, bool freezeAnimation = false)
        {
            ControlsEnabled = enabled;
            if (starterInputs != null)
            {
                starterInputs.MoveInput(Vector2.zero);
                starterInputs.LookInput(Vector2.zero);
                starterInputs.SprintInput(false);
                starterInputs.JumpInput(false);
                starterInputs.cursorLocked = enabled && CommandLine.Value("--rimbara-smoke") == null;
            }
            if (starterController != null) starterController.enabled = enabled;
            if (controls != null) controls.SetGameplayEnabled(enabled);
            if (animator != null)
            {
                animator.speed = freezeAnimation ? 0f : 1f;
                if (!enabled) animator.SetFloat("Speed", 0f);
            }
        }

        public void SetGameplayView(bool gameplay)
        {
            if (gameplayCamera != null) gameplayCamera.enabled = gameplay;
            if (overviewCamera != null) overviewCamera.enabled = !gameplay;
        }

        public void Teleport(Vector3 position)
        {
            bool wasEnabled = capsule.enabled;
            Vector3 delta = position - transform.position;
            capsule.enabled = false;
            transform.position = position;
            capsule.enabled = wasEnabled;
            if (gameplayCamera != null) gameplayCamera.OnTargetObjectWarped(cameraTarget, delta);
        }

        // Useful when placing a scene camera and for opt-in runtime verification.
        public void SetCameraOrbit(float yaw, float pitch)
        {
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
            Type type = starterController.GetType();
            type.GetField("_cinemachineTargetYaw", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(starterController, yaw);
            type.GetField("_cinemachineTargetPitch", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(starterController, pitch);
        }
    }
}
