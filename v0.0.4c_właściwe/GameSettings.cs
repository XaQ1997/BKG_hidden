using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [SerializeField] private Quaternion spawnRotation;

    // Start is called before the first frame update
    void Awake()
    {
        if(Screen.fullScreen)
            Screen.fullScreen = !Screen.fullScreen;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Spawn(int height)
    {
        transform.position = new Vector3(0, height+1.8f, 0);
        transform.rotation = spawnRotation;
    }
}
