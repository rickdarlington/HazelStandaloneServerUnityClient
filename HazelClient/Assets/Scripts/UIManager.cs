using System;
using System.Collections;
using System.Collections.Generic;
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
        Debug.Log("Client clicked connect");
        startMenu.SetActive(false);
        usernameInputField.interactable = false;
        Client.instance.ConnectToServer();
    }
}
