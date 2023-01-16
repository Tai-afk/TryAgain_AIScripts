using TryAgainFSM.Lookups;

namespace TryAgainFSM.Actions
{
    public class AIFall : AIMovementActionHandler<EmptyContext>
    {
        public AIFall(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        {
            return !IsActive() && !movement.grounded && !movement.acquiringGround;
        }

        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Fall; }

        public override bool IsActive()
        {  return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Fall; }
    }
}