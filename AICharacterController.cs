using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TryAgainFSM.Actions;
using TryAgainFSM.Lookups;
using StarterAssets;

/// <summary>
/// RPGCharacterController is the main entry point for triggering animations and holds all the
/// state related to a character. It is the core component of this package-no other controller
/// will run without it.
/// </summary>
public class AICharacterController : MonoBehaviour
{

    /// <summary>
    /// Event called when actions are locked by an animation.
    /// </summary>
    public event System.Action OnLockActions = delegate { };

    /// <summary>
    /// Event called when actions are unlocked at the end of an animation.
    /// </summary>
    public event System.Action OnUnlockActions = delegate { };

    /// <summary>
    /// Event called when movement is locked by an animation.
    /// </summary>
    public event System.Action OnLockMovement = delegate { };

    /// <summary>
    /// Event called when movement is unlocked at the end of an animation.
    /// </summary>
    public event System.Action OnUnlockMovement = delegate { };

    /// <summary>
    /// Unity Animator component.
    /// </summary>
    [HideInInspector] public Animator animator;

    /// <summary>
    /// Returns whether the character can take actions.
    /// </summary>
    public bool canAction => _canAction && !isDead;
    private bool _canAction;

    /// <summary>
    /// Returns whether the character can move.
    /// </summary>
    public bool canMove => _canMove && !isDead;
    private bool _canMove;

    /// <summary>
    /// Returns whether the Death action is active.
    /// </summary>
    public bool isDead => TryGetHandlerActive(HandlerTypes.Death);

    /// <summary>
    /// Returns whether the Idle action is active. Idle is added by
    /// RPGCharacterMovementController.
    /// </summary>
    public bool isIdle => TryGetHandlerActive(HandlerTypes.Idle);

    /// <summary>
    /// Returns whether the Move action is active. Idle is added by
    /// RPGCharacterMovementController.
    /// </summary>
    public bool isMoving => TryGetHandlerActive(HandlerTypes.Move);

    /// <summary>
    /// Returns whether character is in patrol state
    /// </summary>
    [HideInInspector] public bool isPatrol = false;

    /// <summary>
    /// Returns whether character is in chase state
    /// </summary>
    [HideInInspector] public bool isChase = false;

    /// <summary>
    /// Returns whether character is in attacking state
    /// </summary>
    [HideInInspector] public bool isAttack = false;

    private Dictionary<string, AIActionHandler> actionHandlers = new Dictionary<string, AIActionHandler>();

    #region Initialization

    private void Awake()
    {
        // Unlock actions and movement.
        Unlock(true, true);

        // Get the animator component
        TryGetComponent(out animator);
    }

    #endregion

    #region Actions

    /// <summary>
    /// Set an action handler.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    /// <param name="handler">The handler associated with this action.</param>
    public void SetHandler(string action, AIActionHandler handler)
    { actionHandlers[action] = handler; }

    /// <summary>
    /// Get an action handler by name. If it doesn't exist, return the Null handler.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    public AIActionHandler GetHandler(string action)
    {
        if (HandlerExists(action)) { return actionHandlers[action]; }
        Debug.LogError("RPGCharacterController: No handler for action \"" + action + "\"");
        return actionHandlers[HandlerTypes.Null];
    }

    /// <summary>
    /// Check if a handler exists.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    /// <returns>Whether or not that action exists on this controller.</returns>
    public bool HandlerExists(string action)
    { return actionHandlers.ContainsKey(action); }

    public bool TryGetHandlerActive(string action)
    { return HandlerExists(action) && IsActive(action); }

    /// <summary>
    /// Check if an action is active.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    /// <returns>Whether the action is active. If the action does not exist, returns false.</returns>
    public bool IsActive(string action)
    { return GetHandler(action).IsActive(); }

    /// <summary>
    /// Check if an action can be started.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    /// <returns>Whether the action can be started. If the action does not exist, returns false.</returns>
    public bool CanStartAction(string action)
    { return GetHandler(action).CanStartAction(this); }

    public bool TryStartAction(string action, object context = null)
    {
        if (!CanStartAction(action)) { return false; }

        if (context == null) { StartAction(action); }
        else { StartAction(action, context); }

        return true;
    }

    public bool TryEndAction(string action)
    {
        if (!CanEndAction(action)) { return false; }
        EndAction(action);
        return true;
    }

    /// <summary>
    /// Check if an action can be ended.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    /// <returns>Whether the action can be ended. If the action does not exist, returns false.</returns>
    public bool CanEndAction(string action)
    { return GetHandler(action).CanEndAction(this); }

    /// <summary>
    /// Start the action with the specified context. If the action does not exist, there is no effect.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    /// <param name="context">Contextual object used by this action. Leave blank if none is required.</param>
    public void StartAction(string action, object context = null)
    { GetHandler(action).StartAction(this, context); }

    /// <summary>
    /// End the action. If the action does not exist, there is no effect.
    /// </summary>
    /// <param name="action">Name of the action.</param>
    public void EndAction(string action)
    { GetHandler(action).EndAction(this); }

    #endregion

    #region Updates

    private void LateUpdate()
    {

    }

    #endregion

    #region Misc

    /// <summary>
    /// Gets the object with the animator on it. Useful if that object is a child of this one.
    /// </summary>
    /// <returns>GameObject to which the animator is attached.</returns>
    public Animator GetAnimatorTarget()
    { return animator; }

    /// <summary>
    /// Returns the current animation length of the given animation layer.
    /// </summary>
    /// <param name="animationlayer">The animation layer being checked.</param>
    /// <returns>Float time of the currently played animation on animationlayer.</returns>
    private float CurrentAnimationLength(int animationlayer)
    { return animator.GetCurrentAnimatorClipInfo(animationlayer).Length; }

    /// <summary>
    /// Lock character movement and/or action, on a delay for a set time.
    /// </summary>
    /// <param name="lockMovement">If set to <c>true</c> lock movement.</param>
    /// <param name="lockAction">If set to <c>true</c> lock action.</param>
    /// <param name="timed">If set to <c>true</c> timed.</param>
    /// <param name="delayTime">Delay time.</param>
    /// <param name="lockTime">Lock time.</param>
    public void Lock(bool lockMovement, bool lockAction, bool timed, float delayTime, float lockTime)
    {
        StopCoroutine("_Lock");
        StartCoroutine(_Lock(lockMovement, lockAction, timed, delayTime, lockTime));
    }

    private IEnumerator _Lock(bool lockMovement, bool lockAction, bool timed, float delayTime, float lockTime)
    {
        if (delayTime > 0) { yield return new WaitForSeconds(delayTime); }

        if (lockMovement)
        {
            _canMove = false;
            OnLockMovement();
        }
        if (lockAction)
        {
            _canAction = false;
            OnLockActions();
        }
        if (timed)
        {
            if (lockTime > 0) { yield return new WaitForSeconds(lockTime); }
            Unlock(lockMovement, lockAction);
        }
    }

    /// <summary>
    /// Let character move and act again.
    /// </summary>
    /// <param name="movement">Unlock movement if true.</param>
    /// <param name="actions">Unlock actions if true.</param>
    public void Unlock(bool movement, bool actions)
    {
        if (movement)
        {
            _canMove = true;
            OnUnlockMovement();
        }

        if (!actions) { return; }

        _canAction = true;

        OnUnlockActions();
    }
    #endregion
}