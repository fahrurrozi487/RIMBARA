using Rimbara.Mission;
using UnityEngine;

namespace Rimbara.Interaction
{
    public sealed class FirePoint : MonoBehaviour
    {
        [SerializeField] private string objectId;
        [SerializeField] private int slot;
        [SerializeField] private GameState state;
        [SerializeField] private MissionSystem missions;
        [SerializeField] private ParticleSystem[] fireAndSmoke;
        [SerializeField] private Light glow;
        [SerializeField] private GameObject steamEffect;
        [SerializeField] private AudioSource crackle;
        private FireSuppression suppression;
        private bool recorded;
        public int Slot => slot;
        public string ObjectId => objectId;
        public bool Extinguished => recorded;
        public float SuppressionFraction => suppression == null ? 0f : suppression.Fraction;
        public Vector3 AimPosition => transform.position + Vector3.up * 1.1f;

        public void Configure(string id, int index, GameState gameState, MissionSystem missionSystem,
            ParticleSystem[] particles, Light fireLight, GameObject steam)
        {
            objectId = id;
            slot = index;
            state = gameState;
            missions = missionSystem;
            fireAndSmoke = particles;
            glow = fireLight;
            steamEffect = steam;
        }

        private void Start()
        {
            suppression = new FireSuppression(state.Profile.ExtinguishSeconds);
            if (slot >= state.Profile.Fires) gameObject.SetActive(false);
        }

        public bool ReceiveWater(float deltaTime)
        {
            if (recorded || !gameObject.activeInHierarchy || !missions.CanExtinguish) return false;
            bool newlyOut = suppression.ApplyWater(deltaTime);
            if (glow != null) glow.intensity = Mathf.Lerp(1.8f, 0f, suppression.Fraction);
            if (!newlyOut) return false;
            // Commit the counter exactly once before removing the collision target.
            if (!state.ExtinguishFire(objectId)) return false;
            recorded = true;
            foreach (ParticleSystem particles in fireAndSmoke)
                if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (glow != null) glow.enabled = false;
            if (crackle != null) crackle.Stop();
            foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (steamEffect != null)
            {
                steamEffect.SetActive(true);
                foreach (ParticleSystem particles in steamEffect.GetComponentsInChildren<ParticleSystem>()) particles.Play();
            }
            return true;
        }

        public void ConfigureAudio(AudioSource source) { crackle = source; }
    }
}
