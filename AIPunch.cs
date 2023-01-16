using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class AIPunch : MonoBehaviour
{
    [SerializeField] private FMODUnity.EventReference punchSFX;

    public void Punch()
    {
        if (!punchSFX.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(punchSFX, gameObject);
        }
        FadeToBlack.Instance.GoBlack();
    }
}
