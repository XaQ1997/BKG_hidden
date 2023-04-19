using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private float horizontalSpeed = 1f;
    private float verticalSpeed = 1f;
    private float moveSpeed = 0.5f;

    [SerializeField] private KeyCode forward=KeyCode.W;
    [SerializeField] private KeyCode backward=KeyCode.S;
    [SerializeField] private KeyCode left=KeyCode.A;
    [SerializeField] private KeyCode right=KeyCode.D;
    [SerializeField] private KeyCode up = KeyCode.E;
    [SerializeField] private KeyCode down = KeyCode.Q;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float h = horizontalSpeed * Input.GetAxis("Mouse X");
        float v = verticalSpeed * Input.GetAxis("Mouse Y");

        transform.Rotate(-v, h, 0);

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
    }
}