using UnityEngine;

namespace Rimbara.Interaction
{
    public sealed class SeedlingPickup : InteractableBehaviour
    {
        [SerializeField] private GameObject seedlingVisual;
        [SerializeField] private int slot;
        public bool Collected { get; private set; }
        public int Slot => slot;
        public override bool Available => !Collected && gameObject.activeInHierarchy;

        public void Configure(string id, GameObject visual, GameObject ring, int index)
        {
            SetIdentity(id, ring);
            seedlingVisual = visual;
            slot = index;
        }

        public override string GetPrompt(VolunteerActor actor)
        {
            return actor.Missions.CanCollectOrPlant ? "[E]  Collect native seedling"
                : "Inspect the amber forest marker first";
        }

        public override InteractionResult Interact(VolunteerActor actor)
        {
            if (!Available || !InRange(actor)) return new InteractionResult(false, "Move closer to the seedling.");
            if (!actor.Missions.CanCollectOrPlant)
                return new InteractionResult(false, "Follow your current mission before collecting seedlings.");
            if (!actor.State.CollectSeedling(objectId)) return new InteractionResult(false, "This seedling is already collected.");
            Collected = true;
            if (seedlingVisual != null) seedlingVisual.SetActive(false);
            SetFocused(false);
            foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            Debug.Log("RIMBARA: seedling collected (" + objectId + ").");
            return new InteractionResult(true, "A native seedling collected. Carry it to a marked planting bed.");
        }
    }
}
