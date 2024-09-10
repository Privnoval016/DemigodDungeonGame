using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleController : MonoBehaviour
{
    public int collectibleID;
    public Vector3 returnPosition;
    public SceneField returnScene;

    public GameObject collectParticle;

    private GameObject model;

    public float hoverSpeed = 0.5f;
    public float hoverHeight = 0.2f;
    
    // Start is called before the first frame update
    void Start()
    {
        model = transform.GetChild(0).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * 50);
        
        transform.position += new Vector3(0, Mathf.Sin(Time.time * hoverSpeed) * hoverHeight * Time.deltaTime, 0);
    }
}
