using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Freezable : MonoBehaviour
{
    public bool isFrozen = false;

    public Vector3 lockPosition;

    public Petrification petrification;

    public void TriggerFreeze()
    {
        isFrozen = true;
        ActivateFreeze();
    }
    protected virtual void ActivateFreeze() { }
    
    
    public void TriggerUnfreeze()
    {
        isFrozen = false;
        ActivateUnfreeze();
    }
    
    protected virtual void ActivateUnfreeze() { }
}
