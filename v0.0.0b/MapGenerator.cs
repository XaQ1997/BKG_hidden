using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject grassBlockPrefab;

    GameObject[,,] blocks = new GameObject[32, 64, 32];

    // Start is called before the first frame update
    private void Awake()
    {
        InitGame();
    }

    private void InitGame()
    {
        var axisX = (x_min: -15, x_max: 16);
        var axisY = (y_min: 0, y_max: 64);
        var axisZ = (z_min: -15, z_max: 16);

        for (int x = axisX.x_min; x < axisX.x_max; ++x)
        {
            for (int y = axisY.y_min; y < axisY.y_max; ++y)
            {
                for (int z = axisZ.z_min; z < axisZ.z_max; ++z)
                {
                    if (y < 3)
                        blocks[x - axisX.x_min, y, z - axisZ.z_min] = Instantiate(stonePrefab, new Vector3(x, y, z), Quaternion.identity);
                    if (y == 3)
                        blocks[x - axisX.x_min, y, z - axisZ.z_min] = Instantiate(grassBlockPrefab, new Vector3(x, y, z), Quaternion.identity);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
