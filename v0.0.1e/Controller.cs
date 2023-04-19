using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private float horizontalSpeed =10f;
    private float verticalSpeed = 2f;
    private float moveSpeed = 0.1f;
    private bool gamePaused = false;

    [SerializeField] private KeyCode forward=KeyCode.W;
    [SerializeField] private KeyCode backward=KeyCode.X;
    [SerializeField] private KeyCode left=KeyCode.A;
    [SerializeField] private KeyCode right=KeyCode.D;
    [SerializeField] private KeyCode up = KeyCode.E;
    [SerializeField] private KeyCode down = KeyCode.C;
    [SerializeField] private KeyCode pause = KeyCode.Escape;
    [SerializeField] private KeyCode destroy = KeyCode.Mouse0;
    [SerializeField] private KeyCode build = KeyCode.Mouse1;
    [SerializeField] private KeyCode reset = KeyCode.R;
    [SerializeField] private KeyCode kill = KeyCode.K;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var mapGenerator = this.gameObject.GetComponent<MapGenerator>();
        var blockController = this.gameObject.GetComponent<BlockController>();
        var gameSettings = this.gameObject.GetComponent<GameSettings>();

        if (gamePaused == false)
        {
            float h = horizontalSpeed * Input.GetAxis("Mouse X");
            float v = verticalSpeed * Input.GetAxis("Mouse Y");

            transform.Rotate(0, h, 0, Space.World);
            transform.Rotate(-v, 0, 0, Space.Self);

            if (Input.GetKey(forward))
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
                blockController.Build();

            if (Input.GetKey(kill))
                gameSettings.Spawn();

            if (Input.GetKey(reset))
            {
                mapGenerator.CleanMap();
                mapGenerator.InitGame(this.gameObject.GetComponent<BlockMap>().blockMap);
            }
        }

        if (Input.GetKeyDown(pause))
        {
            if (gamePaused)
            {
                Time.timeScale = 1;
                gamePaused = false;
            }
            else
            {
                Time.timeScale = 0;
                gamePaused = true;
            }
        }
    }
}