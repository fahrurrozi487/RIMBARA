using Rimbara.Player;
using UnityEngine;

namespace Rimbara.Interaction
{
    [RequireComponent(typeof(VolunteerActor))]
    public sealed class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private VolunteerInput input;
        [SerializeField] private LayerMask interactionLayers;
        [SerializeField] private LayerMask obstructionLayers;
        private readonly Collider[] hits = new Collider[40];
        private VolunteerActor actor;
        public InteractableBehaviour Focused { get; private set; }
        public string Prompt => Focused == null ? string.Empty : Focused.GetPrompt(actor);

        public void Configure(VolunteerInput controls, LayerMask interactions, LayerMask obstructions)
        {
            input = controls;
            interactionLayers = interactions;
            obstructionLayers = obstructions;
        }

        private void Awake() { actor = GetComponent<VolunteerActor>(); }

        private void Update()
        {
            if (!actor.State.SessionRunning) { SetFocus(null); return; }
            Scan();
            if (input.InteractPressed) InteractFocused();
        }

        public void Scan()
        {
            float range = actor.State.Profile.InteractionRange;
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            int count = Physics.OverlapSphereNonAlloc(origin, range, hits, interactionLayers, QueryTriggerInteraction.Collide);
            float bestDistance = float.MaxValue;
            InteractableBehaviour best = null;
            for (int i = 0; i < count; i++)
            {
                InteractableBehaviour candidate = hits[i].GetComponentInParent<InteractableBehaviour>();
                if (candidate == null || !candidate.Available) continue;
                Vector3 difference = candidate.transform.position - transform.position;
                difference.y = 0f;
                float distance = difference.sqrMagnitude;
                if (distance > range * range || distance >= bestDistance) continue;
                if (Physics.Linecast(origin, candidate.FocusPosition, obstructionLayers, QueryTriggerInteraction.Ignore)) continue;
                best = candidate;
                bestDistance = distance;
            }
            SetFocus(best);
        }

        public InteractionResult InteractFocused()
        {
            InteractionResult result = Focused == null
                ? new InteractionResult(false, "Move near a seedling, planting bed, or mission board.")
                : Focused.Interact(actor);
            actor.Report(result);
            if (Focused != null && !Focused.Available) SetFocus(null);
            return result;
        }

        private void SetFocus(InteractableBehaviour value)
        {
            if (Focused == value) return;
            if (Focused != null) Focused.SetFocused(false);
            Focused = value;
            if (Focused != null) Focused.SetFocused(true);
        }

        private void OnDisable() { SetFocus(null); }
    }
}
