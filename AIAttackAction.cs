using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    Punch = 0,
    Kick = 1,
    Tackle = 2
}

[Serializable]
public struct AttackParams
{
    #region Attack
    [Header("Attack")] [Tooltip("Type of attack")]
    public AttackType type;

    public float startRange;
    public float landDistance;
    public Transform playerNeck;
    public Transform playerHead;
    #endregion
    
    #region Traces
    [Header("Traces")]
    [Tooltip("Maximum number of traces to perform for checking whether the character can vault")]
    public int maxTraces;

    [Tooltip("Radius of the sphere traces")]
    public float traceRadius;
    
    [Tooltip("Whether to draw the debug traces for the vault")]
    public bool drawTraces;
    #endregion
}

[Serializable]
public abstract class AttackAction 
{
    #region Attack
    public AttackParams attackParams;
    #endregion

    #region Animation
    [Tooltip("List of target matching parameters to use during the animation")]
    public List<TargetMatchParams> targetMatchingParams;
    #endregion
    
    #region Internal Variables
    protected Transform transform;
    protected LayerMask traceMask;
    #endregion
    
    public AttackAction(Transform transform, AttackParams attackParams, LayerMask layerMask)
    {
        this.transform = transform;
        this.attackParams = attackParams;
        traceMask = layerMask;

        targetMatchingParams = new List<TargetMatchParams>();
    }
    
    public abstract bool Check();
}
