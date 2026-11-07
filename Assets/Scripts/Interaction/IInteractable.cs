using UnityEngine;

namespace Rimbara.Interaction
{
    public readonly struct InteractionResult
    {
        public readonly bool Success;
        public readonly string Message;
        public InteractionResult(bool success, string message) { Success = success; Message = message; }
    }

    public interface IInteractable
    {
        Vector3 FocusPosition { get; }
        bool Available { get; }
        string GetPrompt(VolunteerActor actor);
        InteractionResult Interact(VolunteerActor actor);
        void SetFocused(bool focused);
    }
}
