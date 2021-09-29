using System;
using System.Collections;
using UnityClient;
using static UnityClient.HazelNetworkManager;
using UnityEngine;

public class Client : MonoBehaviour
{
    //TODO replace with MonoBehaviour singleton
    
    public static Client instance;
    public string ip = "127.0.0.1";
    public string port = "30003";
    private HazelNetworkManager networkManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //this SHOULD keep the HazelNetworkManager as a single instance
            //TODO repalce with actual singleton implementation in HazelNetworkManager class
            networkManager = new HazelNetworkManager();
        }
        else if (instance != null)
        {
            Debug.Log("[TODO] why would you ever get here?  eliminate this");
            Destroy(this);
        }
    }

    public void ConnectToServer()
    {
        var coConnect = networkManager.CoConnect();
        StartCoroutine(coConnect);
    }
}
