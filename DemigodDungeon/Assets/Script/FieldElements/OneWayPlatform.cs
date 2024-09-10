using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OneWayPlatform : MonoBehaviour
{
    private BoxCollider collider;
    
    private GameObject player;
    
    private Vector3 bottomPlayer, topPlatform;
    
    public enum AllowDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }
    
    public AllowDirection direction;


    private void Start()
    {
        player = GameManager.Instance.player;
        
        collider = GetComponent<BoxCollider>();
        
        switch (direction)
        {
            case AllowDirection.LeftToRight:
                topPlatform = transform.position + Vector3.right * collider.size.x / 2;
                break;
            case AllowDirection.RightToLeft:
                topPlatform = transform.position - Vector3.right * collider.size.x / 2;
                break;
            case AllowDirection.TopToBottom:
                topPlatform = transform.position - Vector3.up * collider.size.y / 2;
                break;
            case AllowDirection.BottomToTop:
                topPlatform = transform.position + Vector3.up * collider.size.y / 2;
                break;
        }
    }

    void Update()
    {
        switch (direction)
        {
            case AllowDirection.LeftToRight:
                bottomPlayer = player.transform.position + Vector3.right * player.GetComponent<CapsuleCollider>().radius;
                collider.isTrigger = bottomPlayer.x > topPlatform.x;
                break;
            case AllowDirection.RightToLeft:
                bottomPlayer = player.transform.position - Vector3.right * player.GetComponent<CapsuleCollider>().radius;
                collider.isTrigger = bottomPlayer.x < topPlatform.x;
                break;
            case AllowDirection.TopToBottom:
                bottomPlayer = player.transform.position + Vector3.up * player.GetComponent<CapsuleCollider>().height / 2;
                collider.isTrigger = bottomPlayer.y > topPlatform.y;
                break;
            case AllowDirection.BottomToTop:
                bottomPlayer = player.transform.position - Vector3.up * player.GetComponent<CapsuleCollider>().height / 2;
                collider.isTrigger = bottomPlayer.y < topPlatform.y;
                break;
        }
        
    }
}
