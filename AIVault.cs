using Animancer.Examples.StateMachines;
using CharacterState = TryAgainFSM.Lookups.CharacterState;

namespace TryAgainFSM.Actions
{
    public class AIVault : AIMovementActionHandler<EmptyContext>
    {
        public AIVault(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        {
            return !IsActive() && movement.grounded && ((CharacterState)movement.currentState == CharacterState.Chase)&& movement.vaultState.VaultCheck(); //this is where we can start action
        }

        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Vault; }

        public override bool IsActive()
        { return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Vault; }
    }
}