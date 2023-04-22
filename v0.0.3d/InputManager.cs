using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public InputManager Instance { get { return _instance; } }
    private ControllerMap controller;

    void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;

        controller = new ControllerMap();
    }

    public Vector3 PlayerMovement()
    {
        return controller.Player.Movement.ReadValue<Vector3>();
    }

    private void OnEnable()
    {
        controller.Enable();
    }

    private void OnDisable()
    {
        controller.Disable();
    }
}
