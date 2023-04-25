using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum GameMode
{
    Observator,
    Creative
}

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
public class Controller : MonoBehaviour
{
    private float horizontalSpeed =10f;
    private float verticalSpeed = 2f;
    private float moveSpeed = 2f;
    private bool gamePaused = false;
    private float scroll;
    private int nrSlot = 0;

    [SerializeField] private PlayerInput input;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameMode gm=GameMode.Creative;

    [SerializeField] private KeyCode pause = KeyCode.Escape;
    [SerializeField] private KeyCode destroy = KeyCode.Mouse0;
    [SerializeField] private KeyCode build = KeyCode.Mouse1;
    [SerializeField] private KeyCode kill = KeyCode.K;

    [SerializeField] private KeyCode[] slotKeys = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };
    [SerializeField] private KeyCode[] cameras = new KeyCode[] { KeyCode.F1, KeyCode.F2, KeyCode.F3 };

    [SerializeField] private GameObject firstPersonCamera;
    [SerializeField] private GameObject secondPersonCamera;
    [SerializeField] private GameObject thirdPersonCamera;

    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject[] slots = new GameObject[10];
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject controls;

    // Start is called before the first frame update
    void Awake()
    {
        Resume();

        firstPersonCamera.SetActive(true);
        secondPersonCamera.SetActive(false);
        thirdPersonCamera.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        var blockMap = this.gameObject.GetComponent<BlockMap>().blockMap;
        var blockController = this.gameObject.GetComponent<BlockController>();
        var gameSettings = this.gameObject.GetComponent<GameSettings>();
        scroll = Input.mouseScrollDelta.y;

        if (gamePaused == false)
        {
            if (gm == GameMode.Observator)
                this.gameObject.layer = LayerMask.NameToLayer("Observator");
            else
                this.gameObject.layer = LayerMask.NameToLayer("Default");

            float h = horizontalSpeed * Input.GetAxis("Mouse X");
            float v = verticalSpeed * Input.GetAxis("Mouse Y");

            transform.Rotate(0, h, 0, Space.World);
            transform.Rotate(-v, 0, 0, Space.Self);

            if (Input.GetKey(destroy))
                blockController.DestroyBlock();
            if (Input.GetKey(build))
            {
                if(slots[nrSlot])
                    blockController.Build(slots[nrSlot]);
            }

            if (Input.GetKey(kill))
                gameSettings.Spawn();

            if (scroll != 0)
            {
                if (scroll > 0)
                    nrSlot--;
                else
                    nrSlot++;

                if (nrSlot > 9)
                    nrSlot = 0;
                if (nrSlot < 0)
                    nrSlot = 9;
            }

            for (int i = 0; i < slotKeys.Length; ++i)
                if (Input.GetKeyDown(slotKeys[i]))
                    nrSlot = i;

            Vector3 pos = new Vector3(88 * nrSlot - 396, 0, 0);
            highlight.transform.localPosition = pos;
        }

        if (Input.GetKeyDown(pause))
        {
            if (gamePaused)
            {
                Resume();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Time.timeScale = 0;
                gamePaused = true;

                cursor.SetActive(false);
                panel.SetActive(true);
            }
        }

        if(Input.GetKeyDown(cameras[0]))
        {
            firstPersonCamera.SetActive(true);
            secondPersonCamera.SetActive(false);
            thirdPersonCamera.SetActive(false);
        }
        if(Input.GetKeyDown(cameras[1]))
        {
            firstPersonCamera.SetActive(false);
            secondPersonCamera.SetActive(true);
            thirdPersonCamera.SetActive(false);
        }
        if(Input.GetKeyDown(cameras[2]))
        {
            firstPersonCamera.SetActive(false);
            secondPersonCamera.SetActive(false);
            thirdPersonCamera.SetActive(true);
        }
    }

    public void Resume()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
        gamePaused = false;

        cursor.SetActive(true);
        controls.SetActive(false);
        panel.SetActive(false);
    }

    public void Reset()
    {
        var mapGenerator = this.gameObject.GetComponent<MapGenerator>();

        mapGenerator.CleanMap();
        mapGenerator.InitGame(this.gameObject.GetComponent<BlockMap>().blockMap);

        Resume();
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        rb.velocity = ctx.ReadValue<Vector3>() * moveSpeed;
        rb.velocity = Vector3.MoveTowards(rb.velocity, transform.forward, moveSpeed);
    }
}