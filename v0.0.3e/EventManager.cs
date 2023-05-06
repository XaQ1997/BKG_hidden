using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EventManager : MonoBehaviour
{
    [SerializeField] private GameObject sourcePanel;
    [SerializeField] private GameObject destinationPanel;

    public void Exit()
    {
        Application.Quit();
    }

    public void ChangeMenu()
    {
        destinationPanel.SetActive(true);
        sourcePanel.SetActive(false);
    }
}
