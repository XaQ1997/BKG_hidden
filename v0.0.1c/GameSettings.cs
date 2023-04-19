using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private Quaternion spawnRotation;

    // Start is called before the first frame update
    void Awake()
    {
        if(Screen.fullScreen)
            Screen.fullScreen = !Screen.fullScreen;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Spawn()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
    }
}
