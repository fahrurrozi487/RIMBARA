using System;
using Rimbara.Mission;
using UnityEngine;

namespace Rimbara.Interaction
{
    public sealed class VolunteerActor : MonoBehaviour
    {
        [SerializeField] private GameState state;
        [SerializeField] private MissionSystem missions;
        public GameState State => state;
        public MissionSystem Missions => missions;
        public event Action<InteractionResult> ActionPerformed;

        public void Configure(GameState gameState, MissionSystem missionSystem)
        {
            state = gameState;
            missions = missionSystem;
        }

        public void Report(InteractionResult result) { ActionPerformed?.Invoke(result); }
    }
}
