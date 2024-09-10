using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VinePetrification : Petrification
{
    
    [SerializeField] private GameObject parentModel;
    
    private List<GameObject> submeshes = new List<GameObject>();
    
    // Start is called before the first frame update
    void Start()
    {
        RetrieveAllSubmeshes();
    }
    
    private void RetrieveAllSubmeshes()
    {
        foreach (Transform child in parentModel.transform)
        {
            if (child.TryGetComponent(out MeshRenderer meshRenderer))
            {
                submeshes.Add(child.gameObject);
            }
            
            if (child.childCount > 0)
            {
                foreach (Transform grandchild in child)
                {
                    if (grandchild.TryGetComponent(out MeshRenderer grandchildMeshRenderer))
                    {
                        submeshes.Add(grandchild.gameObject);
                    }
                }
            }
        }
    }

    public override void Petrify()
    {
        GameObject petrifyParticle = Instantiate(GazeTracker.Instance.petrifyEffect, transform.position, Quaternion.identity);
        Destroy(petrifyParticle, 1.5f);

        foreach (GameObject submesh in submeshes)
        {
            submesh.GetComponent<MeshRenderer>().materials = petrifiedMaterials;
        }
    }

    public override void UnPetrify()
    {
        GameObject petrifyParticle = Instantiate(GazeTracker.Instance.unpetrifyEffect, transform.position, Quaternion.identity);
        Destroy(petrifyParticle, 1.5f);
        
        foreach (GameObject submesh in submeshes)
        {
            submesh.GetComponent<MeshRenderer>().materials = originalMaterials;
        }
    }
}
