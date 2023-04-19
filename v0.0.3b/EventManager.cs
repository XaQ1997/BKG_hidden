using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [SerializeField] private GameObject previousMenu;
    [SerializeField] private GameObject nextMenu;

    public void ChangeMenu()
    {
        previousMenu.SetActive(false);
        nextMenu.SetActive(true);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
