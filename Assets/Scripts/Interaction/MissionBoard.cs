namespace Rimbara.Interaction
{
    public sealed class MissionBoard : InteractableBehaviour
    {
        public override bool Available => true;
        public override string GetPrompt(VolunteerActor actor) => "[E]  Read volunteer briefing";

        public override InteractionResult Interact(VolunteerActor actor)
        {
            if (!InRange(actor)) return new InteractionResult(false, "Move closer to the mission board.");
            return new InteractionResult(true, actor.Missions.Title + ": " + actor.Missions.Instruction);
        }
    }
}
