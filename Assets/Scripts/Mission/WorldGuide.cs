using Rimbara.Interaction;
using UnityEngine;

namespace Rimbara.Mission
{
    public sealed class WorldGuide : MonoBehaviour
    {
        [SerializeField] private GameState state;
        [SerializeField] private MissionSystem missions;
        [SerializeField] private Transform player;
        [SerializeField] private SeedlingPickup[] seedlings;
        [SerializeField] private PlantingPoint[] plantingPoints;
        [SerializeField] private FirePoint[] fires;
        [SerializeField] private GameObject inspectionBeacon;
        [SerializeField] private GameObject returnBeacon;
        [SerializeField] private GameObject[] seedlingBeacons;
        [SerializeField] private GameObject[] plantingBeacons;
        [SerializeField] private GameObject[] fireBeacons;
        public Transform Destination { get; private set; }
        public string DestinationName { get; private set; }
        public float Distance => Destination == null ? 0f : Vector3.Distance(player.position, Destination.position);

        public void Configure(GameState gameState, MissionSystem system, Transform volunteer, SeedlingPickup[] seeds,
            PlantingPoint[] beds, FirePoint[] firePoints, GameObject inspection, GameObject post, GameObject[] seedMarkers, GameObject[] bedMarkers)
        {
            state = gameState;
            missions = system;
            player = volunteer;
            seedlings = seeds;
            plantingPoints = beds;
            fires = firePoints;
            inspectionBeacon = inspection;
            returnBeacon = post;
            seedlingBeacons = seedMarkers;
            plantingBeacons = bedMarkers;
        }

        public void ConfigureFireMarkers(GameObject[] markers) { fireBeacons = markers; }

        private void Start()
        {
            foreach (SeedlingPickup seedling in seedlings)
                if (seedling.Slot >= state.Profile.Seedlings) seedling.gameObject.SetActive(false);
            foreach (PlantingPoint bed in plantingPoints)
                if (bed.Slot >= state.Profile.Trees) bed.gameObject.SetActive(false);
        }

        private void Update()
        {
            MissionStage stage = missions.Stage;
            inspectionBeacon.SetActive(stage == MissionStage.FirstSteps);
            returnBeacon.SetActive(stage == MissionStage.ANewBeginning);
            for (int i = 0; i < seedlings.Length; i++)
                seedlingBeacons[i].SetActive(stage == MissionStage.SeedsOfHope && seedlings[i].Available);
            for (int i = 0; i < plantingPoints.Length; i++)
                plantingBeacons[i].SetActive(stage == MissionStage.SeedsOfHope && plantingPoints[i].Available);
            if (fireBeacons != null)
                for (int i = 0; i < fireBeacons.Length; i++)
                    fireBeacons[i].SetActive(stage == MissionStage.StopTheFire && fires[i].gameObject.activeInHierarchy && !fires[i].Extinguished);
            Destination = missions.ActiveDestination;
            DestinationName = stage == MissionStage.FirstSteps ? "Inspect the forest"
                : stage == MissionStage.ANewBeginning ? "Volunteer post" : missions.Title;
            float nearest = float.MaxValue;
            if (stage == MissionStage.SeedsOfHope)
            {
                bool carrying = state.Snapshot.CarriedSeedlings > 0 && state.Snapshot.Trees < state.Snapshot.TreeGoal;
                if (carrying)
                {
                    foreach (PlantingPoint bed in plantingPoints)
                        if (bed.Available) Consider(bed.transform, "Planting bed", ref nearest);
                }
                else
                {
                    foreach (SeedlingPickup seedling in seedlings)
                        if (seedling.Available) Consider(seedling.transform, "Native seedling", ref nearest);
                }
            }
            else if (stage == MissionStage.StopTheFire)
            {
                foreach (FirePoint fire in fires)
                    if (fire.gameObject.activeInHierarchy && !fire.Extinguished) Consider(fire.transform, "Remaining fire", ref nearest);
            }
        }

        private void Consider(Transform target, string label, ref float nearest)
        {
            float distance = (target.position - player.position).sqrMagnitude;
            if (distance >= nearest) return;
            nearest = distance;
            Destination = target;
            DestinationName = label;
        }
    }
}
