using System;

namespace TryAgainFSM.Actions
{
    /// <summary>
    /// General action handler type. This is an interface so that implementations of action
    /// handlers can remain ignorant of the type of the action handler's context (here, it's
    /// just "object").
    /// </summary>
    public interface AIActionHandler
    {
        /// <summary>
        /// Checks the RPGCharacterController to see if this action handler can be started, based
        /// on the controller's current state.
        /// </summary>
        /// <param name="controller">RPGCharacterController instance.</param>
        /// <returns>Whether this action handler can be started.</returns>
        bool CanStartAction(AICharacterController controller);

        /// <summary>
        /// Actually start the action handler, updating the controller's state, calling any
        /// animation methods, and emitting an OnStart event.
        /// </summary>
        /// <param name="controller">RPGCharacterController instance.</param>
        /// <param name="context">Contextual information used by this action handler.</param>
        void StartAction(AICharacterController controller, object context);

        /// <summary>
        /// Add an event listener to be called immediately after an action starts.
        /// </summary>
        /// <param name="callback">Event listener.</param>
        void AddStartListener(Action callback);

        /// <summary>
        /// Remove an event listener from the start callbacks.
        /// </summary>
        /// <param name="callback"></param>
        void RemoveStartListener(Action callback);

        /// <summary>
        /// Checks to see if this action handler is active.
        /// </summary>
        /// <returns>Whether this action handler is currently active.</returns>
        bool IsActive();

        /// <summary>
        /// Checks the RPGCharacterController to see if this action handler can be ended, based on
        /// the controller's current state.
        /// </summary>
        /// <param name="controller">RPGCharacterController instance.</param>
        /// <returns></returns>
        bool CanEndAction(AICharacterController controller);

        /// <summary>
        /// Actually end the action handler, updating the controller's state, calling any animation
        /// methods, and emitting an OnEnd event.
        /// </summary>
        /// <param name="controller"></param>
        void EndAction(AICharacterController controller);

        /// <summary>
        /// Add an event listener to be called immediately after an action ends.
        /// </summary>
        /// <param name="callback">Event listener.</param>
        void AddEndListener(Action callback);

        /// <summary>
        /// Remove an event listener from the end callbacks.
        /// </summary>
        /// <param name="callback"></param>
        void RemoveEndListener(Action callback);
    }
}