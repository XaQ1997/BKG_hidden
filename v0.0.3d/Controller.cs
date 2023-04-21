using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    private float horizontalSpeed =10f;
    private float verticalSpeed = 2f;
    private float moveSpeed = 0.1f;
    private bool gamePaused = false;
    private int nrSlot = 0;

    [SerializeField] private InputActionReference MoveActionReference;
    [SerializeField] private InputActionReference BuildOrDestroyActionsReference;

    private InputBinding move;

    private InputBinding scroll;

    private InputBinding destroy;
    private InputBinding build;

    [SerializeField] private KeyCode pause = KeyCode.Escape;
    [SerializeField] private KeyCode kill = KeyCode.K;

    [SerializeField] private KeyCode[] slotKeys = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };

    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject[] slots = new GameObject[10];
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject controls;

    // Start is called before the first frame update
    void Awake()
    {
        Controls();
        Resume();
    }

    // Update is called once per frame
    void Update()
    {
        var blockMap = this.gameObject.GetComponent<BlockMap>().blockMap;
        var blockController = this.gameObject.GetComponent<BlockController>();
        var gameSettings = this.gameObject.GetComponent<GameSettings>();

        if (gamePaused == false)
        {
            float h = horizontalSpeed * Input.GetAxis("Mouse X");
            float v = verticalSpeed * Input.GetAxis("Mouse Y");

            transform.Rotate(0, h, 0, Space.World);
            transform.Rotate(-v, 0, 0, Space.Self);

            for (int i = 0; i < 10; ++i)
                if (Input.GetKey(slotKeys[i]))
                    nrSlot = i;

            /*if (Input.GetKey(forward))
                transform.position += new Vector3(transform.forward.x, 0, transform.forward.z) * moveSpeed;
            if (Input.GetKey(backward))
                transform.position -= new Vector3(transform.forward.x, 0, transform.forward.z) * moveSpeed;
            if (Input.GetKey(left))
                transform.position -= new Vector3(transform.right.x, 0, transform.right.z) * moveSpeed;
            if (Input.GetKey(right))
                transform.position += new Vector3(transform.right.x, 0, transform.right.z) * moveSpeed;
            if (Input.GetKey(up))
                transform.position += new Vector3(0, transform.up.y, 0) * moveSpeed;
            if (Input.GetKey(down))
                transform.position -= new Vector3(0, transform.up.y, 0) * moveSpeed;

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

                Vector3 pos = new Vector3(88 * nrSlot - 396, 0, 0);
                highlight.transform.localPosition = pos;
            }*/
            
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
    }

    public void Controls()
    {
        move=MoveActionReference.action.bindings[0];

        scroll = BuildOrDestroyActionsReference.action.bindings[2];

        destroy = BuildOrDestroyActionsReference.action.bindings[0];
        build = BuildOrDestroyActionsReference.action.bindings[1];
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
}