using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private MeshRenderer[,,] blocks = new MeshRenderer[32, 64, 32];
    private Block[,] blockMap;

    [SerializeField] private GameObject airPrefab;
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject grassBlockPrefab;
    [SerializeField] private GameObject dirtPrefab;

    // Start is called before the first frame update
    private void Awake()
    {
        blockMap = new Block[16, 16];

        Block block = new Block { Name = "air", Prefab = airPrefab };
        blockMap[0, 2] = block;


        block = new Block { Name = "stone", Prefab = stonePrefab };
        blockMap[1, 0] = block;


        block = new Block { Name = "grass block", Prefab = grassBlockPrefab };
        blockMap[2, 0] = block;

        block = new Block { Name = "dirt", Prefab = dirtPrefab };
        blockMap[2, 1] = block;
        

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
                    var block = new GameObject();

                    if (y < 25)
                        block = Instantiate(blockMap[1, 0].Prefab, new Vector3(x, y, z), Quaternion.identity);
                    else if (y < 32)
                        block = Instantiate(blockMap[2, 1].Prefab, new Vector3(x, y, z), Quaternion.identity);
                    else if (y == 32)
                        block = Instantiate(blockMap[2, 0].Prefab, new Vector3(x, y, z), Quaternion.identity);
                    else
                        block = Instantiate(blockMap[0, 2].Prefab, new Vector3(x, y, z), Quaternion.identity);

                    blocks[x - axisX.x_min, y, z - axisZ.z_min] = block.GetComponentInChildren<MeshRenderer>();
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public MeshRenderer[,,] Blocks()
    {
        return blocks;
    }

    public Block block(int categoryId, int typeId)
    {
        return blockMap[categoryId, typeId];
    }

    [System.Serializable]
    public class Block
    {
        public string Name;
        public GameObject Prefab;
    }
}
