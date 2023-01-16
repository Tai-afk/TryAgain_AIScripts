using TryAgainFSM.Lookups;

namespace TryAgainFSM.Actions
{
    public class AIChase : AIMovementActionHandler<EmptyContext>
    {
        public AIChase(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        { return !IsActive() && movement.seePlayer && (CharacterState)movement.currentState != CharacterState.Fall && (CharacterState)movement.currentState != CharacterState.Jump; }

        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Chase; }

        public override bool IsActive()
        { return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Chase; }
    }
}

