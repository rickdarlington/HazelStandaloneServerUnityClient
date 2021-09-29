using System;
using System.Collections;
using System.Collections.Generic;
using UnityClient;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject startMenu;
    public InputField usernameInputField;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Debug.Log("[TODO] why would you ever get here?  eliminate this");
            Destroy(this);
        }
    }

    public void ConnectClicked()
    {
        startMenu.SetActive(false);
        usernameInputField.interactable = false;
        HazelNetworkManager.instance.ConnectToServer();
    }

    public void ConnectionLost()
    {
        //TODO recall the connect menu!
        Debug.Log("booted/connection lost :(");
        startMenu.SetActive(true);
        usernameInputField.interactable = true;
    }
}
