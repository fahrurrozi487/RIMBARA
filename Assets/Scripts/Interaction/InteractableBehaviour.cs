using UnityEngine;

namespace Rimbara.Interaction
{
    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [SerializeField] protected string objectId;
        [SerializeField] private GameObject focusRing;
        public string ObjectId => objectId;
        public virtual Vector3 FocusPosition => transform.position + Vector3.up * 0.55f;
        public abstract bool Available { get; }
        public abstract string GetPrompt(VolunteerActor actor);
        public abstract InteractionResult Interact(VolunteerActor actor);

        public void SetIdentity(string id, GameObject ring)
        {
            objectId = id;
            focusRing = ring;
            if (focusRing != null) focusRing.SetActive(false);
        }

        public void SetFocused(bool focused)
        {
            if (focusRing != null) focusRing.SetActive(focused && Available);
        }

        protected bool InRange(VolunteerActor actor)
        {
            if (actor == null || actor.State == null || !actor.State.SessionRunning) return false;
            Vector3 difference = actor.transform.position - transform.position;
            difference.y = 0f;
            float range = actor.State.Profile.InteractionRange;
            return difference.sqrMagnitude <= range * range;
        }
    }
}
