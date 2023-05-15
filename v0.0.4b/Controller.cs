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
    //basic arguments
    private float horizontalSpeed = 10f;
    private float verticalSpeed = 2f;
    private bool gamePaused = false;
    private float scroll;
    private int nrSlot = 0;
    private float v;
    private float h;

    //gravity arguments
    [SerializeField] private LayerMask layer;
    [SerializeField] private float gravity = 9.81f;
    private bool isGrounded;
    private bool isGravity;

    //controller arguments
    [SerializeField] private PlayerInput input;
    [SerializeField] private Rigidbody rb;

    //gamemode arguments
    [SerializeField] private GameMode gm = GameMode.Creative;

    //basic old input controller arguments
    [SerializeField] private KeyCode pause = KeyCode.Escape;
    [SerializeField] private KeyCode destroy = KeyCode.Mouse0;
    [SerializeField] private KeyCode build = KeyCode.Mouse1;
    [SerializeField] private KeyCode kill = KeyCode.K;

    //lists old input controller arguments
    [SerializeField] private KeyCode[] slotKeys = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };
    [SerializeField] private KeyCode[] cameras = new KeyCode[] { KeyCode.F1, KeyCode.F2, KeyCode.F3 };

    //camera arguments
    [SerializeField] private GameObject firstPersonCamera;
    [SerializeField] private GameObject secondPersonCamera;
    [SerializeField] private GameObject thirdPersonCamera;

    //UI arguments
    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject[] slots = new GameObject[10];
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject controls;

    //animation arguments
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject head;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private GameObject leftLeg;
    [SerializeField] private GameObject rightLeg;
    private bool isForward = true;

    // Start is called before the first frame update
    void Awake()
    {
        Resume();

        rb.detectCollisions = true;

        firstPersonCamera.SetActive(true);
        secondPersonCamera.SetActive(false);
        thirdPersonCamera.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        var blockController = this.gameObject.GetComponent<BlockController>();
        var gameSettings = this.gameObject.GetComponent<GameSettings>();
        scroll = Input.mouseScrollDelta.y;

        if (gamePaused == false)
        {
            Quaternion rotation = Quaternion.Euler(30, 0, 0), inverseRotation = Quaternion.Euler(-30, 0, 0);
            RaycastHit hit;

            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f, layer))
                isGrounded = true;
            else
            {
                isGrounded = false;

                if (isGravity == true)
                    transform.position += Vector3.down * Time.deltaTime * 22.5f / gravity;
            }

            h = horizontalSpeed * Input.GetAxis("Mouse X");
            v = verticalSpeed * Input.GetAxis("Mouse Y");

            head.transform.Rotate(0, h, 0, Space.World);
            head.transform.Rotate(-v, 0, 0, Space.Self);

            if (rb.velocity.x != 0 || rb.velocity.z != 0)
            {
                if (isForward == true)
                {
                    leftHand.transform.rotation = rotation;
                    rightLeg.transform.rotation = rotation;

                    leftLeg.transform.rotation = inverseRotation;
                    rightHand.transform.rotation = inverseRotation;

                    isForward = false;
                }
                else
                {
                    leftHand.transform.rotation = inverseRotation;
                    rightLeg.transform.rotation = inverseRotation;

                    leftLeg.transform.rotation = rotation;
                    rightHand.transform.rotation = rotation;

                    isForward = true;
                }
            }
            else
            {
                leftHand.transform.rotation = Quaternion.Euler(0, 0, 0);
                rightHand.transform.rotation = Quaternion.Euler(0, 0, 0);

                leftLeg.transform.rotation = Quaternion.Euler(0, 0, 0);
                rightLeg.transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            if (gm == GameMode.Observator)
            {
                rb.detectCollisions = false;

                cursor.SetActive(false);
                hud.SetActive(false);
            }
            else
            {
                rb.detectCollisions = true;

                if(firstPersonCamera.activeSelf==true)
                    cursor.SetActive(true);
                
                hud.SetActive(true);
            }

            if (Input.GetKey(destroy)&&gm!=GameMode.Observator)
                blockController.DestroyBlock();
            if (Input.GetKey(build)&&gm!=GameMode.Observator)
            {
                if(slots[nrSlot])
                    blockController.Build(slots[nrSlot]);
            }

            /*if (Input.GetKey(kill))
                gameSettings.Spawn(this.gameObject.GetComponent<MapGenerator>().Chunks());*/

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
                hud.SetActive(false);
                panel.SetActive(true);
            }
        }

        if(Input.GetKeyDown(cameras[0]))
        {
            firstPersonCamera.SetActive(true);
            secondPersonCamera.SetActive(false);
            thirdPersonCamera.SetActive(false);

            cursor.SetActive(true);
        }
        if(Input.GetKeyDown(cameras[1]))
        {
            firstPersonCamera.SetActive(false);
            secondPersonCamera.SetActive(true);
            thirdPersonCamera.SetActive(false);

            cursor.SetActive(false);
        }
        if(Input.GetKeyDown(cameras[2]))
        {
            firstPersonCamera.SetActive(false);
            secondPersonCamera.SetActive(false);
            thirdPersonCamera.SetActive(true);

            cursor.SetActive(false);
        }
    }

    public void Resume()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
        gamePaused = false;

        cursor.SetActive(true);
        hud.SetActive(true);
        controls.SetActive(false);
        panel.SetActive(false);
    }

    public void Reset()
    {
        var mapGenerator = this.gameObject.GetComponent<MapGenerator>();

        mapGenerator.CleanMap();
        mapGenerator.InitGame();

        Resume();
    }

    public void OnHorizontalMove(InputAction.CallbackContext ctx)
    {
        Vector3 move = new Vector3((head.transform.right * ctx.ReadValue<Vector2>().x + head.transform.forward * ctx.ReadValue<Vector2>().y).x, 0, (head.transform.right * ctx.ReadValue<Vector2>().x + head.transform.forward * ctx.ReadValue<Vector2>().y).z);

        rb.velocity = move;

        rb.useGravity = false;
    }

    public void OnVerticalMove(InputAction.CallbackContext ctx)
    {
        rb.velocity = new Vector3(0, ctx.ReadValue<float>() * transform.up.y, 0);

        rb.useGravity = false;

        isGravity = false;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (isGrounded == true)
            rb.AddForce(Vector3.up * 22.5f / gravity, ForceMode.Impulse);

        isGravity = true;
        rb.useGravity = true;
    }

    public void ChangeGM()
    {
        if (gm == GameMode.Creative)
            gm = GameMode.Observator;
        else
            gm = GameMode.Creative;
    }
}