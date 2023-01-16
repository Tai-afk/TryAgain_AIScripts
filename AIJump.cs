using TryAgainFSM.Lookups;

namespace TryAgainFSM.Actions
{
    public class AIJump : AIMovementActionHandler<EmptyContext>
    {
        public AIJump(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        {
            return
                !IsActive() &&
                movement.grounded; // && controller.canAction; //&& (movement.jumpState.rpgCharacterInputSystemController.jumpedThisFrame || movement.jumpState.rpgCharacterInputSystemController.canDoDelayedJumpThisFrame) && movement.jumpState.canJump;
        }

        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Jump; }

        public override bool IsActive()
        { return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Jump; }
    }
}