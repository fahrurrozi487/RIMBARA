using System.Collections;
using UnityEngine;

namespace Rimbara.Interaction
{
    public sealed class PlantingPoint : InteractableBehaviour
    {
        [SerializeField] private GameObject treePrefab;
        [SerializeField] private GameObject emptyBed;
        [SerializeField] private int slot;
        public bool Planted { get; private set; }
        public GameObject PlantedTree { get; private set; }
        public int Slot => slot;
        public override bool Available => !Planted && gameObject.activeInHierarchy;
        public override Vector3 FocusPosition => transform.position + Vector3.up * 0.12f;

        public void Configure(string id, GameObject tree, GameObject bed, GameObject ring, int index)
        {
            SetIdentity(id, ring);
            treePrefab = tree;
            emptyBed = bed;
            slot = index;
        }

        public override string GetPrompt(VolunteerActor actor)
        {
            if (!actor.Missions.CanCollectOrPlant) return "Follow the active mission before planting";
            return actor.State.Snapshot.CarriedSeedlings > 0 ? "[E]  Plant native tree" : "Collect a seedling before planting";
        }

        public override InteractionResult Interact(VolunteerActor actor)
        {
            if (!Available || !InRange(actor)) return new InteractionResult(false, "Move closer to a planting bed.");
            if (!actor.Missions.CanCollectOrPlant) return new InteractionResult(false, "Planting belongs to Seeds of Hope.");
            if (actor.State.Snapshot.CarriedSeedlings <= 0)
                return new InteractionResult(false, "You are not carrying a seedling. Look for the small nursery pots.");
            if (treePrefab == null) return new InteractionResult(false, "The tree prefab is missing from this planting point.");
            if (!actor.State.PlantTree(objectId)) return new InteractionResult(false, "This planting bed is already restored.");
            Planted = true;
            SetFocused(false);
            if (emptyBed != null) emptyBed.SetActive(false);
            PlantedTree = Instantiate(treePrefab, transform.position, Quaternion.Euler(0f, slot * 73f, 0f), transform);
            PlantedTree.name = "Restored native tree " + (slot + 1);
            StartCoroutine(GrowTree(PlantedTree.transform));
            return new InteractionResult(true, "Roots take hold. One more tree returns to Arunika.");
        }

        private static IEnumerator GrowTree(Transform tree)
        {
            const float duration = 1.8f;
            Vector3 targetScale = tree.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t * (3f - 2f * t);
                tree.localScale = targetScale * Mathf.Lerp(0.12f, 1f, ease);
                yield return null;
            }
            tree.localScale = targetScale;
        }
    }
}
