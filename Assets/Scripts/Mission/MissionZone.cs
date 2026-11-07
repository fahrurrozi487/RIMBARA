using UnityEngine;

namespace Rimbara.Mission
{
    [RequireComponent(typeof(Collider))]
    public sealed class MissionZone : MonoBehaviour
    {
        public enum ZonePurpose { Inspection, VolunteerPost }
        [SerializeField] private MissionSystem missions;
        [SerializeField] private ZonePurpose purpose;

        public void Configure(MissionSystem system, ZonePurpose zonePurpose)
        {
            missions = system;
            purpose = zonePurpose;
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) { Visit(other); }
        private void OnTriggerStay(Collider other) { Visit(other); }

        private void Visit(Collider other)
        {
            if (!other.CompareTag("Player") || missions == null) return;
            if (purpose == ZonePurpose.Inspection) missions.ReachInspectionMarker();
            else missions.ReturnToPost();
        }
    }
}
