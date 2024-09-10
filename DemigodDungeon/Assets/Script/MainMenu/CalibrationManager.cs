using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Gaming;

public class CalibrationManager : MonoBehaviour
{
    
    private GazePoint gazePoint;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gazePoint = TobiiAPI.GetGazePoint();
        if (gazePoint.IsRecent())
        {
            Debug.Log("Gaze point in screen space: " + gazePoint.Screen);
        }
    }
}
