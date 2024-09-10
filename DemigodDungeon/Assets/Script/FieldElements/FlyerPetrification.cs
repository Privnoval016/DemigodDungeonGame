using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FlyerPetrification : Petrification
{
    
    [SerializeField] private GameObject parentModel;
    
    private List<GameObject> submeshes = new List<GameObject>();
    
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in parentModel.transform)
        {
            if (child.TryGetComponent(out SkinnedMeshRenderer meshRenderer))
            {
                submeshes.Add(child.gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public override void Petrify()
    {
        GameObject petrifyParticle = Instantiate(GazeTracker.Instance.petrifyEffect, transform.position, Quaternion.identity);
        Destroy(petrifyParticle, 1.5f);
        
        foreach (GameObject submesh in submeshes)
        {
            submesh.GetComponent<SkinnedMeshRenderer>().materials = petrifiedMaterials;
        }
    }
    
    public override void UnPetrify()
    {
        GameObject petrifyParticle = Instantiate(GazeTracker.Instance.unpetrifyEffect, transform.position, Quaternion.identity);
        Destroy(petrifyParticle, 1.5f);
        
        foreach (GameObject submesh in submeshes)
        {
            submesh.GetComponent<SkinnedMeshRenderer>().materials = originalMaterials;
        }
    }
}
