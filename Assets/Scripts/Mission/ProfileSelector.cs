using System;
using UnityEngine;

namespace Rimbara.Mission
{
    [DefaultExecutionOrder(-500)]
    public sealed class ProfileSelector : MonoBehaviour
    {
        [SerializeField] private GameState state;
        [SerializeField] private MissionProfile fullProfile;
        [SerializeField] private MissionProfile weekOneProfile;

        public void Configure(GameState gameState, MissionProfile full, MissionProfile weekOne)
        {
            state = gameState;
            fullProfile = full;
            weekOneProfile = weekOne;
        }

        private void Awake()
        {
            string profile = CommandLine.Value("--rimbara-profile");
            if (string.Equals(profile, "week1", StringComparison.OrdinalIgnoreCase)) state.Configure(weekOneProfile);
            else if (string.Equals(profile, "full", StringComparison.OrdinalIgnoreCase)) state.Configure(fullProfile);
            // Without a CLI override, keep the profile assigned in the Inspector.
        }
    }

    public static class CommandLine
    {
        public static string Value(string option)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument == option) return "true";
                if (argument.StartsWith(option + "=", StringComparison.Ordinal)) return argument.Substring(option.Length + 1);
            }
            return null;
        }
    }
}
