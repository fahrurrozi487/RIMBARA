using UnityEngine;

namespace Rimbara.World
{
    public sealed class BeaconPulse : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.14f;
        private Vector3 origin;
        private void Awake() { origin = transform.localPosition; }
        private void Update() { transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * 2.2f) * amplitude); }
    }
}
