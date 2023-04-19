using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private BlockMap blockMap = new BlockMap();

    private MeshRenderer[,,] renderers = new MeshRenderer[32, 128, 32];

    [SerializeField] private GameObject airPrefab;
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject grassBlockPrefab;
    [SerializeField] private GameObject dirtPrefab;

    // Start is called before the first frame update
    private void Awake()
    {
        blockMap.Swap(new Block("air", airPrefab), 0, 2);

        blockMap.Swap(new Block("stone", stonePrefab), 2, 0);

        blockMap.Swap(new Block("grass block", grassBlockPrefab), 3, 0);
        blockMap.Swap(new Block("dirt", dirtPrefab), 3, 1);

        InitGame();
    }

    public void InitGame()
    {
        var gameSettings = this.gameObject.GetComponent<GameSettings>();

        gameSettings.Spawn();

        var axisX = (x_min: -15, x_max: 16);
        var axisY = (y_min: 0, y_max: 127);
        var axisZ = (z_min: -15, z_max: 16);

        for (int x = axisX.x_min; x < axisX.x_max; ++x)
        {
            for (int y = axisY.y_min; y < axisY.y_max; ++y)
            {
                for (int z = axisZ.z_min; z < axisZ.z_max; ++z)
                {
                    var block = new GameObject();

                    if (y < 60)
                        block = Instantiate(blockMap.Show(2, 0).Prefab(), new Vector3(x, y, z), Quaternion.identity);
                    else if (y < 64)
                        block = Instantiate(blockMap.Show(3, 1).Prefab(), new Vector3(x, y, z), Quaternion.identity);
                    else if (y == 64)
                        block = Instantiate(blockMap.Show(3, 0).Prefab(), new Vector3(x, y, z), Quaternion.identity);
                    else
                        block = Instantiate(blockMap.Show(0, 2).Prefab(), new Vector3(x, y, z), Quaternion.identity);
                    renderers[x - axisX.x_min, y, z - axisZ.z_min] = block.GetComponentInChildren<MeshRenderer>();
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public MeshRenderer[,,] Renderers()
    {
        return renderers;
    }

    public BlockMap BlockMap()
    {
        return blockMap;
    }    
}
