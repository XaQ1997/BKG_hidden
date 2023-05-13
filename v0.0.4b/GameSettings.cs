using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    private Vector3 spawnPosition;
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

    public void Spawn(Chunk[,] chunk)
    {
        var mapGenerator = this.gameObject.GetComponent<MapGenerator>();

        for (int y = 256; y > 0; --y)
            if (chunk[0, 0].Blocks[0, y-1, 0] != null)
            {
                spawnPosition = new Vector3(0.0f, y + 0.8f, 0.0f);
                break;
            }

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
    }
}
