using TryAgainFSM.Lookups;
using UnityEngine;
namespace TryAgainFSM.Actions
{
    public class AISlide : AIMovementActionHandler<EmptyContext>
    {
        public AISlide(AIStateControl movement) : base(movement)
        {
        }

        public override bool CanStartAction(AICharacterController controller)
        {
            return
                !IsActive() && movement.grounded && controller.canAction;
        }
        public override bool CanEndAction(AICharacterController controller)
        {
            bool isUnderObject = false; 
            if (Physics.Raycast(movement.transform.position, Vector3.up, movement.capsuleCollider.height))
            {
                isUnderObject = true;
            }
            return base.CanEndAction(controller) && !isUnderObject;
        }
        protected override void _StartAction(AICharacterController controller, EmptyContext context)
        { movement.currentState = CharacterState.Slide; }

        public override bool IsActive()
        { return movement.currentState != null && (CharacterState)movement.currentState == CharacterState.Slide; }
    }
}