using System;
using System.Linq;
using Rimbara.Interaction;
using Rimbara.Player;
using StarterAssets;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rimbara.Editor
{
    internal static class CharacterFactory
    {
        public const string PlayerPath = "Assets/Prefabs/Player/Volunteer.prefab";
        private const string CharacterPath = "Assets/Art/Characters/Mixamo/YBot.fbx";
        private const string AnimatorPath = "Assets/Art/Characters/Mixamo/Volunteer.controller";

        public static GameObject CreateVolunteer()
        {
            ConfigureHumanoidImports();
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Mixamo Y Bot must have a valid Humanoid avatar.");
            AnimatorController animation = CreateAnimator();
            var root = new GameObject("Volunteer - Mixamo Y Bot + official Starter Assets");
            root.tag = "Player";
            root.layer = 9;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath));
            model.name = "Mixamo Y Bot";
            model.transform.SetParent(root.transform, false);
            foreach (Animator duplicate in model.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(duplicate);
            Bounds bounds = AssetCatalog.ModelBounds(model);
            float scale = 1.8f / Mathf.Max(0.01f, bounds.size.y);
            model.transform.localScale *= scale;
            bounds = AssetCatalog.ModelBounds(model);
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            AssetCatalog.ConvertMaterials(model);
            AssetCatalog.SetLayer(model, 9);
            Animator animator = root.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = animation;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            CharacterController capsule = root.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = 0.28f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.stepOffset = 0.3f;
            capsule.slopeLimit = 50f;
            capsule.skinWidth = 0.035f;
            var playerInput = root.AddComponent<PlayerInput>();
            playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions");
            if (playerInput.actions == null) throw new InvalidOperationException("Import the official Starter Assets input asset first.");
            playerInput.defaultActionMap = "Player";
            playerInput.defaultControlScheme = "KeyboardMouse";
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;
            var starterInput = root.AddComponent<StarterAssetsInputs>();
            starterInput.cursorLocked = false;
            starterInput.cursorInputForLook = false;
            var motor = root.AddComponent<ThirdPersonController>();
            motor.MoveSpeed = 2.6f;
            motor.SprintSpeed = 5.3f;
            motor.RotationSmoothTime = 0.10f;
            motor.SpeedChangeRate = 10f;
            motor.Gravity = -15f;
            motor.GroundedOffset = -0.14f;
            motor.GroundedRadius = 0.28f;
            motor.GroundLayers = 1 << 8;
            motor.CameraAngleOverride = 12f;
            motor.BottomClamp = -32f;
            motor.TopClamp = 60f;
            motor.FootstepAudioClips = Array.Empty<AudioClip>();
            var target = new GameObject("Camera target");
            target.transform.SetParent(root.transform, false);
            target.transform.localPosition = Vector3.up * 1.45f;
            motor.CinemachineCameraTarget = target;
            VolunteerInput controls = root.AddComponent<VolunteerInput>();
            var actor = root.AddComponent<VolunteerActor>();
            var detector = root.AddComponent<InteractionDetector>();
            detector.Configure(controls, 1 << 10, 1 << 8);
            var water = root.AddComponent<WaterSprayer>();
            Transform nozzle = CreateEquipment(root.transform);
            GameObject sprayObject = (GameObject)PrefabUtility.InstantiatePrefab(ParticleFactory.WaterPrefab());
            sprayObject.transform.SetParent(nozzle, false);
            ParticleSystem spray = sprayObject.GetComponent<ParticleSystem>();
            water.Configure(controls, actor, null, nozzle, spray, (1 << 8) | (1 << 11), 1 << 8);
            var rig = root.AddComponent<VolunteerRig>();
            rig.Configure(controls, motor, starterInput, playerInput, animator, target.transform);
            var grip = new GameObject("Water tool grip");
            grip.transform.SetParent(nozzle, false);
            grip.transform.localPosition = new Vector3(0f, -0.05f, -0.2f);
            root.AddComponent<EquipmentPose>().Configure(grip.transform, water);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureHumanoidImports()
        {
            var character = AssetImporter.GetAtPath(CharacterPath) as ModelImporter;
            if (character == null) throw new InvalidOperationException("Download Mixamo Y Bot before generating Main.");
            if (character.animationType != ModelImporterAnimationType.Human || character.importAnimation)
            {
                character.animationType = ModelImporterAnimationType.Human;
                character.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                character.importAnimation = false;
                character.SaveAndReimport();
            }
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterPath).OfType<Avatar>().FirstOrDefault();
            foreach (string name in new[] { "Idle", "Walk", "Run" })
            {
                string path = "Assets/Art/Characters/Mixamo/" + name + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Missing Mixamo animation: " + name);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                ModelImporterClipAnimation clip = importer.defaultClipAnimations.FirstOrDefault();
                if (clip == null) throw new InvalidOperationException("Mixamo export has no animation: " + name);
                clip.name = name;
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                importer.clipAnimations = new[] { clip };
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController CreateAnimator()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
            if (existing != null) return existing;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
            controller.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);
            AnimatorControllerLayer[] layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;
            AnimatorState state = controller.layers[0].stateMachine.AddState("Idle - walk - run");
            controller.layers[0].stateMachine.defaultState = state;
            var blend = new BlendTree { name = "Mixamo locomotion", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(Clip("Idle"), 0f);
            blend.AddChild(Clip("Walk"), 2.6f);
            blend.AddChild(Clip("Run"), 5.3f);
            state.motion = blend;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip Clip(string name)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/Mixamo/" + name + ".fbx")
                .OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview", StringComparison.Ordinal));
            if (clip == null) throw new InvalidOperationException("The humanoid clip was not imported: " + name);
            return clip;
        }

        private static Transform CreateEquipment(Transform player)
        {
            Material green = AssetCatalog.Material("Volunteer equipment green", new Color(0.15f, 0.36f, 0.22f));
            Material brass = AssetCatalog.Material("Water nozzle brass", new Color(0.76f, 0.64f, 0.29f));
            Material dark = AssetCatalog.Material("Equipment straps", new Color(0.12f, 0.16f, 0.13f));
            GameObject pack = EnvironmentFactory.Shape(PrimitiveType.Capsule, "Reusable water pack", player,
                new Vector3(0f, 1.17f, -0.31f), new Vector3(0.34f, 0.30f, 0.22f), green, false, 9);
            EnvironmentFactory.Shape(PrimitiveType.Cube, "Volunteer pack band", pack.transform, Vector3.zero,
                new Vector3(1.04f, 0.25f, 1.04f), brass, false, 9);
            var nozzle = new GameObject("Water nozzle");
            nozzle.layer = 9;
            nozzle.transform.SetParent(player, false);
            nozzle.transform.localPosition = new Vector3(0.43f, 1.20f, 0.37f);
            GameObject barrel = EnvironmentFactory.Shape(PrimitiveType.Cylinder, "Hose nozzle barrel", nozzle.transform,
                new Vector3(0f, 0f, -0.12f), new Vector3(0.07f, 0.15f, 0.07f), brass, false, 9);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            EnvironmentFactory.Shape(PrimitiveType.Cube, "Nozzle hand grip", nozzle.transform,
                new Vector3(0f, -0.08f, -0.18f), new Vector3(0.08f, 0.16f, 0.08f), dark, false, 9);
            return nozzle.transform;
        }
    }
}
