using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventWeaponShop : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private float detectionRange = 1f;
    private Camera _camera;
    [SerializeField] private GameObject uiShopGo;
    // Start is called before the first frame update
    void Start()
    {
        _camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerCommands>())
        {
            player = other.transform;
        }

        if (player != null)
        {
            if (Vector3.Distance(gameObject.transform.position, player.transform.position) <= detectionRange)
            {
              
                if (uiShopGo.activeSelf != true)
                {
                    uiShopGo.SetActive(true);
                  
                }
                uiShopGo.transform.LookAt(_camera.transform, Vector3.up);
            }
            else
            {
                uiShopGo.SetActive(false);
            }
        }
        
    }

 
}
