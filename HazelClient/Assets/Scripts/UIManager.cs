using System;
using System.Collections;
using System.Collections.Generic;
using UnityClient;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject menuCanvas;
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
        menuCanvas.SetActive(false);
        usernameInputField.interactable = false;
        HazelNetworkManager.instance.ConnectToServer(usernameInputField.text);
    }

    public void ConnectionLost()
    {
        Debug.Log("booted/connection lost :(");
        usernameInputField.interactable = true;
        //TODO why the hell doesn't this work (checking the box in the unity editor does...)
        menuCanvas.SetActive(true);
    }
}
