using TryAgainFSM.Lookups;

namespace TryAgainFSM.Actions
{
    public class AIClimb : AIMovementActionHandler<EmptyContext>
    {
        public AIClimb(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        { return !IsActive(); }

        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Climb; }

        public override bool IsActive()
        { return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Climb; }
    }
}