using Rimbara.Player;
using UnityEngine;

namespace Rimbara.Interaction
{
    public sealed class WaterSprayer : MonoBehaviour
    {
        [SerializeField] private VolunteerInput input;
        [SerializeField] private VolunteerActor actor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform nozzle;
        [SerializeField] private ParticleSystem spray;
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private LayerMask obstructionLayers;
        public FirePoint AimedFire { get; private set; }
        public bool Spraying { get; private set; }
        public string AimHint { get; private set; }
        public Vector3 LastAimPoint { get; private set; }

        public void Configure(VolunteerInput controls, VolunteerActor volunteer, Camera camera,
            Transform sprayNozzle, ParticleSystem particles, LayerMask hits, LayerMask obstructions)
        {
            input = controls;
            actor = volunteer;
            viewCamera = camera;
            nozzle = sprayNozzle;
            spray = particles;
            hitLayers = hits;
            obstructionLayers = obstructions;
        }

        private void Update()
        {
            if (!actor.State.SessionRunning) { SetSpraying(false); AimedFire = null; AimHint = string.Empty; return; }
            UpdateAim();
            bool useTool = input.SprayHeld && actor.Missions.CanExtinguish;
            SetSpraying(useTool);
            if (useTool) ApplySpray(Time.deltaTime);
        }

        public void UpdateAim()
        {
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            bool didHit = Physics.Raycast(ray, out hit, 100f, hitLayers, QueryTriggerInteraction.Collide);
            LastAimPoint = didHit ? hit.point : ray.GetPoint(30f);
            if (nozzle != null) nozzle.rotation = Quaternion.LookRotation((LastAimPoint - nozzle.position).normalized);
            AimedFire = didHit ? hit.collider.GetComponentInParent<FirePoint>() : null;
            AimHint = string.Empty;
            if (AimedFire == null || AimedFire.Extinguished) return;
            if (!actor.Missions.CanExtinguish) { AimHint = "Finish Seeds of Hope before using the water tool"; return; }
            float distance = Vector3.Distance(actor.transform.position + Vector3.up, AimedFire.AimPosition);
            if (distance > actor.State.Profile.SprayRange) { AimHint = "Move closer to the fire  |  range " + actor.State.Profile.SprayRange.ToString("0") + " m"; return; }
            if (Physics.Linecast(nozzle.position, AimedFire.AimPosition, obstructionLayers, QueryTriggerInteraction.Ignore))
            { AimHint = "Water stream blocked  |  move around the obstacle"; return; }
            AimHint = "HOLD LEFT MOUSE  |  Extinguish fire";
        }

        public bool ApplySpray(float deltaTime)
        {
            if (!actor.State.SessionRunning || !actor.Missions.CanExtinguish || AimedFire == null || AimedFire.Extinguished) return false;
            if (Vector3.Distance(actor.transform.position + Vector3.up, AimedFire.AimPosition) > actor.State.Profile.SprayRange) return false;
            if (Physics.Linecast(nozzle.position, AimedFire.AimPosition, obstructionLayers, QueryTriggerInteraction.Ignore)) return false;
            bool extinguished = AimedFire.ReceiveWater(deltaTime);
            if (extinguished) actor.Report(new InteractionResult(true, "The flames are out. Arunika can breathe again."));
            return extinguished;
        }

        private void SetSpraying(bool value)
        {
            Spraying = value;
            if (spray == null) return;
            if (value && !spray.isPlaying) spray.Play();
            else if (!value && spray.isPlaying) spray.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnDisable() { SetSpraying(false); }
    }
}
