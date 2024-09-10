using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//LMAO good luck reading this code
public class TileLayer : MonoBehaviour
{
    [SerializeField] public Camera cam;
    [SerializeField] List<GameObject> tileOptions = new List<GameObject>();
    [SerializeField] bool refresh = false;
    [SerializeField] private float xTile;
    [SerializeField] private float yTile;
    [SerializeField] private float xMargin;
    [SerializeField] private float yMargin;
    [SerializeField] private float xOffset;
    [SerializeField] private float yOffset;
    [SerializeField] private float depth = 50f;
    [SerializeField] private float instanceScale = 1f;
    [SerializeField] private int instanceLimit = 50;
    [SerializeField] private bool randomRotate = true;
    [SerializeField] private bool randomDepth = false;

    private List<GameObject> tiles = new List<GameObject>();
    private List<Vector2> localOffset = new List<Vector2>();
    private List<float> initialRotation = new List<float>();
    private List<float> localDepth = new List<float>();
    //private GameObject centerVis, bottomLeftVis, topRightVis;
    private float cubeScale = 5f;

    private Vector3 topRight;
    private Vector3 bottomLeft;
    private float width;
    private float height;

    // Start is called before the first frame update
    void Start()
    {
        if (cam == null) cam = Camera.main;
        /*
        centerVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerVis.transform.localScale = Vector3.one * cubeScale;
        centerVis.name = "center";
        bottomLeftVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomLeftVis.transform.localScale = Vector3.one * cubeScale;
        bottomLeftVis.name = "bottomLeft";
        topRightVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topRightVis.transform.localScale = Vector3.one * cubeScale;
        topRightVis.name = "topRight";
        */

        preLoadTiles();
    }
    void screenCalculations()
    {   //All of these numbers already account for margin
        topRight = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, depth)) + new Vector3(xMargin, yMargin, 0);
        bottomLeft = cam.ScreenToWorldPoint(new Vector3(0, 0, depth)) - new Vector3(xMargin, yMargin, 0);
        width = topRight.x - bottomLeft.x;
        height = topRight.y - bottomLeft.y;
    }
    void preLoadTiles()
    {
        screenCalculations();

        if (xTile == 0) xTile = width;
        if (yTile == 0) yTile = width;

        //adjust margin for seemless loop
        xMargin += 0.5f * (Mathf.Ceil(width / xTile) * xTile - width);
        yMargin += 0.5f * (Mathf.Ceil(height / yTile) * yTile - height);

        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            Destroy(tiles[i]);
        }
        tiles = new List<GameObject>();
        localOffset = new List<Vector2>();
        initialRotation = new List<float>();
        localDepth = new List<float>();

        for (int y = 0; y< Mathf.Clamp(Mathf.Ceil(height / yTile), 1, 50); y++) //vertical tiling
        {
            for (int x = 0; x < Mathf.Clamp(Mathf.Ceil(width / xTile), 1, 50); x++)
            {
                if (tiles.Count >= instanceLimit) break;
                tiles.Add(Instantiate(tileOptions[Random.Range(0, tileOptions.Count)], transform));
                localOffset.Add(new Vector2(x * xTile, y * yTile));
                initialRotation.Add(Random.Range(0, 3) * 90);
                localDepth.Add(Random.Range(0,20));
            }
            if (tiles.Count >= instanceLimit) break;
        }
    }
    void mathUpdateTransform()
    {   
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].transform.position = new Vector3(
                betterModulus(xOffset + localOffset[i].x - bottomLeft.x, width) + bottomLeft.x,
                betterModulus(yOffset + localOffset[i].y - bottomLeft.y, height) + bottomLeft.y,
                bottomLeft.z + (randomDepth?localDepth[i]:0) );
            tiles[i].transform.localScale = Vector3.one * instanceScale;
            if (randomRotate) tiles[i].transform.rotation = Quaternion.Euler(0, initialRotation[i] + 90*Mathf.Floor((xOffset + localOffset[i].x - bottomLeft.x)/width),0);
        }
    }
    float betterModulus(float a, float b)
    {   //this is an adapted modulus function that accounts for negative numbers
        if (a>0)
        {
            return a % b;
        } else
        {
            return b-(-a % b);
        }
    }
    // Update is called once per frame
    void Update()
    {
        screenCalculations();

        if (refresh)
        {
            preLoadTiles();
            mathUpdateTransform();
            refresh = false;
        }
        mathUpdateTransform();

        //centerVis.transform.position = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, depth));
        //topRightVis.transform.position = topRight;
        //bottomLeftVis.transform.position = bottomLeft;
    }
}
