using TryAgainFSM.Lookups;

namespace TryAgainFSM.Actions
{
    public class AIAttack : AIMovementActionHandler<EmptyContext>
    {
        public AIAttack(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        {
            return !IsActive() && movement.caughtPlayer;
        }

        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Attack; }

        public override bool IsActive()
        { return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Attack; }
    }
}
