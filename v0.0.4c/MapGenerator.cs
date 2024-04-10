using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class MapGenerator : MonoBehaviour
{
    private Dictionary<Vector2Int, Chunk> chunks;
    private Dictionary<Vector2Int, bool> chunkActive;

    private GameObject[,,] blocks;
    
    [SerializeField] private Vector3Int mapSize = new Vector3Int(256, 256, 256);
    [SerializeField] private Vector3Int mapOffset = new Vector3Int(128, 0, 128);

    [SerializeField] private int? seed;

    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private int renderDistance = 1;

    [SerializeField] private BlockMap blockMap;
    [SerializeField] private GameSettings gameSettings;

    // Start is called before the first frame update
    private void Awake()
    {
        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];

        chunks = new Dictionary<Vector2Int, Chunk>();
        chunkActive = new Dictionary<Vector2Int, bool>();

        InitGame();
    }

    public void InitGame()
    {
        if(seed==null)
        {
        seed = (int)DateTime.Now.Ticks;

        seed = new Random((uint)seed).NextInt(-(int)Mathf.Pow(2, 7), (int)Mathf.Pow(2, 7) + 1);
        }

        Vector2Int vector = new Vector2Int(0, 0);

        chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
        chunkActive[vector]=true;

        //Chunk chunk=chunkManager.GenerateChunk((int)seed, vector);

        //chunks[vector] = chunk;

        vector = new Vector2Int(-1, 0);

        chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
        chunkActive[vector] = true;

        //chunk = chunkManager.GenerateChunk((int)seed, vector);

        //chunks[vector] = chunk;

        vector = new Vector2Int(-1, -1);

        chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
        chunkActive[vector] = true;

        //chunk = chunkManager.GenerateChunk((int)seed, vector);

        //chunks[vector] = chunk;

        vector = new Vector2Int(0, -1);

        chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
        chunkActive[vector] = true;

        //chunk = chunkManager.GenerateChunk((int)seed, vector);

        //chunks[vector] = chunk;

        for (int x=0;x<renderDistance;++x)
            for(int z=0;z<renderDistance;++z)
            {
                if((x!=0||z!=0)&&(Mathf.Pow(x, 2)+Mathf.Pow(z, 2)<Mathf.Pow(renderDistance, 2)))
                {
                    vector = new Vector2Int(x, z);

                    chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
                    chunkActive[vector] = true;

                    //chunk=chunkManager.GenerateChunk((int)seed, vector);

                    //chunks[vector] = chunk;
                    if (x != 0)
                    {
                        vector = new Vector2Int(-x, z);

                        chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
                        chunkActive[vector] = true;

                        //chunk = chunkManager.GenerateChunk((int)seed, vector);

                        //chunks[vector] = chunk;
                    }

                    if (z != 0)
                    {
                        if (x != 0)
                        {
                            vector = new Vector2Int(-x, -z);

                            chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
                            chunkActive[vector] = true;

                            //chunk = chunkManager.GenerateChunk((int)seed, vector);
                            //chunks[vector] = chunk;
                        }

                        vector = new Vector2Int(x, -z);

                        chunks[vector] = new Chunk(vector, new string[16, 256, 16]);
                        chunkActive[vector] = true;

                        //chunk = chunkManager.GenerateChunk((int)seed, vector);

                        //chunks[vector] = chunk;
                    }
                }
            }

        //Load();

        int height = 96;

        for (int y = 0; y < mapSize.y; ++y)
            if (chunks[new Vector2Int(0, 0)].Blocks[0, y, 0] == "30")
            {
                height = y;

                break;
            }

        gameSettings.Spawn(height);

        //chunkManager.inChunk[new Vector2Int(0, 0)] = true;
    }

    /*public void Load()
    {
        foreach(var chunk in chunks.Keys)
        {
            if(chunkActive[chunk]==true&&chunkManager.chunkLoaded[chunk]==false)
            {
                chunkManager.chunkLoaded[chunk] = true;

                var chunkBlocks = chunks[chunk].Blocks;

                for (int y = 0; y < mapSize.y; ++y)
                    for (int x = 0; x < 16; ++x)
                        for (int z = 0; z < 16; ++z)
                            if (chunkBlocks[x, y, z] != null)
                                blocks[(int)chunk.x * 16 + mapOffset.x + x, y, (int)chunk.y * 16 + mapOffset.z + z] = Instantiate(blockMap.blockMap[(string)chunkBlocks[x, y, z]].BlockPrefab, new Vector3((int)chunk.x * 16 + x, y - mapOffset.y, (int)chunk.y * 16 + z), Quaternion.identity);
            }
        }
    }

    public void UnLoad()
    {
        foreach(var chunk in chunks.Keys)
        {
            if(chunkActive[chunk]==false&&chunkManager.chunkLoaded[chunk]==true)
            {
                chunkManager.chunkLoaded[chunk] = false;

                for (int y = 0; y < mapSize.y; ++y)
                    for (int x = 0; x < 16; ++x)
                        for (int z = 0; z < 16; ++z)
                            Destroy(blocks[(int)chunk.x * 16 + mapOffset.x + x, y, (int)chunk.y * 16 + mapOffset.z + z]);
            }
        }
    }*/

    // Update is called once per frame
    void Update()
    {
        /*Vector2Int currentChunkPos = new Vector2Int((int)transform.position.x, (int)transform.position.z);

       foreach(var chunk in chunks.Keys)
        {
            if (new Vector2Int(chunk.x*16, chunk.y*16) == currentChunkPos)
                chunkManager.inChunk[chunk] = true;
            else
                chunkManager.inChunk[chunk] = false;
        }

        if (!chunks.ContainsKey(new Vector2Int((int)(currentChunkPos.x/16), (int)(currentChunkPos.y/16))))
            chunks[new Vector2Int((int)(currentChunkPos.x / 16), (int)(currentChunkPos.y / 16))] =chunkManager.GenerateChunk((int)seed, new Vector2Int((int)(currentChunkPos.x / 16), (int)(currentChunkPos.y / 16)));

        if (!chunkManager.inChunk[new Vector2Int((int)(currentChunkPos.x / 16), (int)(currentChunkPos.y / 16))])
        {
            foreach (var chunkPos in chunkManager.chunkLoaded.Keys)
            {
                chunkActive[chunkPos] = false;
                chunkManager.chunkLoaded[chunkPos] = false;
                chunkManager.inChunk[chunkPos] = false;
            }

            for (int x = -renderDistance; x <= renderDistance-1; x++)
            {
                for (int z = -renderDistance; z <= renderDistance-1; z++)
                {
                    Vector2Int chunkPos = new Vector2Int((int)(currentChunkPos.x / 16), (int)(currentChunkPos.y / 16)) + new Vector2Int(x, z);

                    if (!chunks.ContainsKey(chunkPos))
                        chunks[chunkPos]=chunkManager.GenerateChunk((int)seed, chunkPos);

                    if (!chunkManager.chunkLoaded[chunkPos])
                    {
                        chunkActive[chunkPos] = false;
                        chunkManager.chunkLoaded[chunkPos] = false;
                    }
                    else
                    {
                        chunkActive[chunkPos] = true;
                        chunkManager.chunkLoaded[chunkPos] = true;
                    }
                }
            }

            Load();
            UnLoad();
        }*/
    }

    public void CleanMap()
    {
        chunks = new Dictionary<Vector2Int, Chunk>();

       // chunkManager.chunkLoaded = new Dictionary<Vector2Int, bool>();
        chunkActive = new Dictionary<Vector2Int, bool>();
        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];
    }

    public Dictionary<Vector2Int, Chunk> Chunks()
    {
        return chunks;
    }
    public void ChunkReload(Chunk chunk, Vector2Int vector)
    {
        chunks[vector] = chunk;
    }

    public GameObject[,,] Blocks()
    {
        return blocks;
    }

    public Vector3Int MapSize()
    {
        return mapSize;
    }

    public Vector3Int MapOffset()
    {
        return mapOffset;
    }
}
