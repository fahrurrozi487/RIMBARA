using System;
using Rimbara.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rimbara.Mission
{
    public enum ScreenMode { Menu, Briefing, Controls, Playing, Paused, Victory }

    [DefaultExecutionOrder(-40)]
    public sealed class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameState state;
        [SerializeField] private MissionSystem missions;
        [SerializeField] private VolunteerRig rig;
        [SerializeField] private VolunteerInput input;
        private static ScreenMode nextLaunchMode = ScreenMode.Menu;
        public ScreenMode Mode { get; private set; } = ScreenMode.Menu;
        public float ElapsedSeconds { get; private set; }
        public bool DiagnosticMode => CommandLine.Value("--rimbara-smoke") != null;
        public event Action<ScreenMode> ModeChanged;

        public void Configure(GameState gameState, MissionSystem missionSystem, VolunteerRig player, VolunteerInput controls)
        {
            state = gameState;
            missions = missionSystem;
            rig = player;
            input = controls;
        }

        private void Awake() { missions.Completed += ShowVictory; }

        private void Start()
        {
            ScreenMode initial = nextLaunchMode;
            nextLaunchMode = ScreenMode.Menu;
            SetMode(initial);
        }

        private void Update()
        {
            if (Mode == ScreenMode.Playing) ElapsedSeconds += Time.deltaTime;
            if (input.PausePressed)
            {
                if (Mode == ScreenMode.Playing) Pause();
                else if (Mode == ScreenMode.Paused) Resume();
                else if (Mode == ScreenMode.Briefing || Mode == ScreenMode.Controls) ShowMenu();
            }
        }

        public void ShowMenu() { SetMode(ScreenMode.Menu); }
        public void ShowBriefing() { SetMode(ScreenMode.Briefing); }
        public void ShowControls() { SetMode(ScreenMode.Controls); }

        public void Begin()
        {
            if (Mode != ScreenMode.Briefing && Mode != ScreenMode.Menu) return;
            SetMode(ScreenMode.Playing);
        }

        public void Pause() { if (Mode == ScreenMode.Playing) SetMode(ScreenMode.Paused); }
        public void Resume() { if (Mode == ScreenMode.Paused) SetMode(ScreenMode.Playing); }
        private void ShowVictory() { SetMode(ScreenMode.Victory); }

        public void Replay()
        {
            nextLaunchMode = ScreenMode.Briefing;
            ReloadScene();
        }

        public void ReturnToMenu()
        {
            nextLaunchMode = ScreenMode.Menu;
            ReloadScene();
        }

        private static void ReloadScene()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Quit()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetMode(ScreenMode mode)
        {
            Mode = mode;
            bool playing = mode == ScreenMode.Playing;
            bool gameplayView = playing || mode == ScreenMode.Paused || mode == ScreenMode.Victory;
            Time.timeScale = mode == ScreenMode.Paused ? 0f : 1f;
            state.SetSessionRunning(playing);
            rig.SetControlsEnabled(playing, mode == ScreenMode.Paused);
            rig.SetGameplayView(gameplayView);
            Cursor.lockState = playing && !DiagnosticMode ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing || DiagnosticMode;
            ModeChanged?.Invoke(mode);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && Mode == ScreenMode.Playing && !DiagnosticMode) Pause();
        }

        private void OnDestroy()
        {
            if (missions != null) missions.Completed -= ShowVictory;
            Time.timeScale = 1f;
        }
    }
}
