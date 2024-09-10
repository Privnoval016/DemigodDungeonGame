using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkerPetrification : Petrification
{
    [SerializeField] private GameObject parentModel;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public override void Petrify()
    {
        GameObject petrifyParticle = Instantiate(GazeTracker.Instance.petrifyEffect, transform.position, Quaternion.identity);
        Destroy(petrifyParticle, 1.5f);
        
        parentModel.GetComponent<SkinnedMeshRenderer>().materials = petrifiedMaterials;
    }
    
    public override void UnPetrify()
    {
        GameObject petrifyParticle = Instantiate(GazeTracker.Instance.unpetrifyEffect, transform.position, Quaternion.identity);
        Destroy(petrifyParticle, 1.5f);
        
        parentModel.GetComponent<SkinnedMeshRenderer>().materials = originalMaterials;
    }
}
