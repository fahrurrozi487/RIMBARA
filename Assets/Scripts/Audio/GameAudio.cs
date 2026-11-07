using Rimbara.Interaction;
using Rimbara.Mission;
using UnityEngine;

namespace Rimbara.Audio
{
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private GameState state;
        [SerializeField] private MissionSystem missions;
        [SerializeField] private WaterSprayer sprayer;
        [SerializeField] private AudioSource wind;
        [SerializeField] private AudioSource birds;
        [SerializeField] private AudioSource water;
        [SerializeField] private AudioSource feedback;
        [SerializeField] private AudioClip collect;
        [SerializeField] private AudioClip plant;
        [SerializeField] private AudioClip extinguish;
        [SerializeField] private AudioClip complete;
        private float birdTarget;
        public float Volume { get; private set; } = 0.65f;

        public void Configure(GameState gameState, MissionSystem system, WaterSprayer tool, AudioSource windSource,
            AudioSource birdSource, AudioSource waterSource, AudioSource feedbackSource,
            AudioClip collected, AudioClip planted, AudioClip outSound, AudioClip completion)
        {
            state = gameState;
            missions = system;
            sprayer = tool;
            wind = windSource;
            birds = birdSource;
            water = waterSource;
            feedback = feedbackSource;
            collect = collected;
            plant = planted;
            extinguish = outSound;
            complete = completion;
        }

        private void Awake()
        {
            Volume = PlayerPrefs.GetFloat("Rimbara.MasterVolume", 0.65f);
            AudioListener.volume = Volume;
            state.SeedlingCollected += PlayCollect;
            state.TreePlanted += PlayPlant;
            state.FireExtinguished += PlayOut;
            state.ProgressChanged += RefreshBirds;
            missions.Completed += PlayComplete;
        }

        private void Start()
        {
            wind.Play();
            birds.volume = 0.04f;
            birds.Play();
            RefreshBirds(state.Snapshot);
        }

        private void Update()
        {
            if (sprayer.Spraying && !water.isPlaying) water.Play();
            else if (!sprayer.Spraying && water.isPlaying) water.Stop();
            birds.volume = Mathf.MoveTowards(birds.volume, birdTarget, Time.unscaledDeltaTime * 0.05f);
        }

        public void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value);
            AudioListener.volume = Volume;
            PlayerPrefs.SetFloat("Rimbara.MasterVolume", Volume);
        }

        private void RefreshBirds(ProgressSnapshot progress) { birdTarget = Mathf.Lerp(0.04f, 0.28f, progress.Restoration); }
        private void PlayCollect(string id) { feedback.PlayOneShot(collect, 0.5f); }
        private void PlayPlant(string id) { feedback.PlayOneShot(plant, 0.6f); }
        private void PlayOut(string id) { feedback.PlayOneShot(extinguish, 0.55f); }
        private void PlayComplete() { feedback.PlayOneShot(complete, 0.75f); }

        private void OnDestroy()
        {
            if (state != null)
            {
                state.SeedlingCollected -= PlayCollect;
                state.TreePlanted -= PlayPlant;
                state.FireExtinguished -= PlayOut;
                state.ProgressChanged -= RefreshBirds;
            }
            if (missions != null) missions.Completed -= PlayComplete;
        }
    }
}
