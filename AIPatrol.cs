using TryAgainFSM.Lookups;

namespace TryAgainFSM.Actions
{
    public class AIPatrol : AIMovementActionHandler<EmptyContext>
    {
        public AIPatrol(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        { return !IsActive() && (movement.waypoints.Count != 0); }

        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Patrol; }

        public override bool IsActive()
        { return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Patrol; }
    }
}

