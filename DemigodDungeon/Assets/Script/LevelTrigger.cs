using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    public SceneField nextLevel;
    public SceneField lastLevel;

    private float thresholdCoord;

    private float deloadBuffer;
    
    private bool touchingPlayer = false;

    private bool allowedToTryUnload = false;
    
    public enum Direction
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }
    
    public Direction direction;

    private GameObject player;
    
    
    // Start is called before the first frame update
    void Start()
    {
        deloadBuffer = LevelManager.Instance.deloadBuffer;
        
        player = GameManager.Instance.player;
        
        switch (direction)
        {
            case Direction.LeftToRight:
                thresholdCoord = transform.position.x;
                break;
            case Direction.RightToLeft:
                thresholdCoord = transform.position.x;
                break;
            case Direction.TopToBottom:
                thresholdCoord = transform.position.y;
                break;
            case Direction.BottomToTop:
                thresholdCoord = transform.position.y;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (direction)
        {
            case Direction.LeftToRight:
                if (player.transform.position.x > thresholdCoord - deloadBuffer)
                {
                    if (touchingPlayer)
                    {
                        LevelManager.Instance.LoadNextScene(nextLevel);
                        allowedToTryUnload = true;
                    }
                }

                break;
            case Direction.RightToLeft:
                if (player.transform.position.x < thresholdCoord + deloadBuffer)
                {
                    if (touchingPlayer)
                    {
                        LevelManager.Instance.LoadNextScene(nextLevel);
                        allowedToTryUnload = true;
                    }

                }

                break;
            case Direction.TopToBottom:
                if (player.transform.position.y < thresholdCoord + deloadBuffer)
                {
                    if (touchingPlayer)
                    {
                        LevelManager.Instance.LoadNextScene(nextLevel);
                        allowedToTryUnload = true;
                    }
                }

                break;
            case Direction.BottomToTop:
                if (player.transform.position.y > thresholdCoord - deloadBuffer)
                {
                    if (touchingPlayer)
                    {
                        LevelManager.Instance.LoadNextScene(nextLevel);
                        allowedToTryUnload = true;
                    }
                }

                break;
        }


        if (allowedToTryUnload)
        {
            switch (direction)
            {
                case Direction.LeftToRight:
                    if (player.transform.position.x > thresholdCoord + deloadBuffer)
                    {
                        LevelManager.Instance.UnloadLastScene(lastLevel);
                    }

                    break;
                case Direction.RightToLeft:
                    if (player.transform.position.x < thresholdCoord - deloadBuffer)
                    {
                        LevelManager.Instance.UnloadLastScene(lastLevel);
                    }
                    break;
                case Direction.TopToBottom:
                    if (player.transform.position.y < thresholdCoord - deloadBuffer)
                    {
                        LevelManager.Instance.UnloadLastScene(lastLevel);
                    }

                    break;
                case Direction.BottomToTop:
                    if (player.transform.position.y > thresholdCoord + deloadBuffer)
                    {
                        LevelManager.Instance.UnloadLastScene(lastLevel);
                    }
                    break;

            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            touchingPlayer = true;
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject == player)
        {
            touchingPlayer = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            touchingPlayer = false;
        }
    }
}
