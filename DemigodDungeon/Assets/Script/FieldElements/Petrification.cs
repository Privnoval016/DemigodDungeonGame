using System.Collections;
using System.Collections.Generic;
using Tobii.Gaming;
using UnityEngine;

public abstract class Petrification : MonoBehaviour
{
    [SerializeField] protected Material[] petrifiedMaterials;
    
    [SerializeField] protected Material[] originalMaterials;
    
    public virtual void Petrify() { }
    
    public virtual void UnPetrify() { }
}
