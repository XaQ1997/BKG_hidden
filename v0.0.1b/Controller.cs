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
    [SerializeField] private KeyCode backward=KeyCode.S;
    [SerializeField] private KeyCode left=KeyCode.A;
    [SerializeField] private KeyCode right=KeyCode.D;
    [SerializeField] private KeyCode up = KeyCode.E;
    [SerializeField] private KeyCode down = KeyCode.Q;
    [SerializeField] private KeyCode pause = KeyCode.Escape;
    [SerializeField] private KeyCode destroy = KeyCode.Mouse0;
    [SerializeField] private KeyCode build = KeyCode.Mouse1;

    [SerializeField] private GameObject prefab;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var mapGenerator = this.gameObject.GetComponent<MapGenerator>();

        if (gamePaused == false)
        {
            float h = horizontalSpeed * Input.GetAxis("Mouse X");
            float v = verticalSpeed * Input.GetAxis("Mouse Y");

            transform.Rotate(0, h, 0, Space.World);
            transform.Rotate(-v, 0, 0, Space.Self);

            if (Input.GetKey(forward))
                transform.position += transform.forward * moveSpeed;
            if (Input.GetKey(backward))
                transform.position -= transform.forward * moveSpeed;
            if (Input.GetKey(left))
                transform.position -= transform.right * moveSpeed;
            if (Input.GetKey(right))
                transform.position += transform.right * moveSpeed;
            if (Input.GetKey(up))
                transform.position += transform.up * moveSpeed;
            if (Input.GetKey(down))
                transform.position -= transform.up * moveSpeed;
            
            if(Input.GetKeyDown(destroy))
            {
                float length = 8f;
                float vector = 0f;
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