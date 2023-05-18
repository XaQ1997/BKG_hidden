using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private Dictionary<Vector2Int, Chunk> chunks;
    private Dictionary<Vector2Int, bool> chunkActive;

    private GameObject[,,] blocks;

    [SerializeField] private Vector3Int mapSize = new Vector3Int(256, 256, 256);
    [SerializeField] private Vector3Int mapOffset = new Vector3Int(128, 0, 128);

    private uint seed;

    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private int renderDistance = 1;

    [SerializeField] private BlockMap blockMap;

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
        var gameSettings = this.gameObject.GetComponent<GameSettings>();

        seed = (uint)DateTime.Now.Ticks;

        Vector2Int vector = new Vector2Int(0, 0);

        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
        chunkActive[vector]=true;

        Chunk chunk=chunkManager.GenerateChunk(seed, vector, null, null);

        chunks[vector] = chunk;

        vector = new Vector2Int(-1, 0);

        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
        chunkActive[vector] = true;

        chunk = chunkManager.GenerateChunk(seed, vector, chunks[new Vector2Int(0, 0)].verticalOffsets[1], 1);

        chunks[vector] = chunk;

        vector = new Vector2Int(-1, -1);

        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
        chunkActive[vector] = true;

        chunk = chunkManager.GenerateChunk(seed, vector, chunks[new Vector2Int(0, 0)].verticalOffsets[2], 2);

        chunks[vector] = chunk;

        vector = new Vector2Int(0, -1);

        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
        chunkActive[vector] = true;

        chunk = chunkManager.GenerateChunk(seed, vector, chunks[new Vector2Int(0, 0)].verticalOffsets[3], 3);

        chunks[vector] = chunk;

        for (int x=0;x<renderDistance;++x)
            for(int z=0;z<renderDistance;++z)
            {
                if((x!=0||z!=0)&&(Mathf.Pow(x, 2)+Mathf.Pow(z, 2)<Mathf.Pow(renderDistance, 2)))
                {
                    int verticalOffset = 0;

                    vector = new Vector2Int(x, z);

                    chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                    chunkActive[vector] = true;

                    if (x <= z)
                        verticalOffset = chunks[new Vector2Int(x, z - 1)].verticalOffsets[0];
                    else
                        verticalOffset = chunks[new Vector2Int(x - 1, z)].verticalOffsets[0];

                    chunk=chunkManager.GenerateChunk(seed, vector, verticalOffset, 0);

                    chunks[vector] = chunk;
                    if (x != 0)
                    {
                        vector = new Vector2Int(-x, z);

                        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                        chunkActive[vector] = true;

                        if (x <= z)
                            verticalOffset = chunks[new Vector2Int(-x, z - 1)].verticalOffsets[1];
                        else
                            verticalOffset = chunks[new Vector2Int(1 - x, z)].verticalOffsets[1];

                        chunk = chunkManager.GenerateChunk(seed, vector, verticalOffset, 1);

                        chunks[vector] = chunk;
                    }

                    if (z != 0)
                    {
                        if (x != 0)
                        {
                            vector = new Vector2Int(-x, -z);

                            chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                            chunkActive[vector] = true;

                            if (x <= z)
                                verticalOffset = chunks[new Vector2Int(-x, 1 - z)].verticalOffsets[2];
                            else
                                verticalOffset = chunks[new Vector2Int(1 - x, -z)].verticalOffsets[2];

                            chunk = chunkManager.GenerateChunk(seed, vector, verticalOffset, 2);
                            chunks[vector] = chunk;
                        }

                        vector = new Vector2Int(x, -z);

                        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                        chunkActive[vector] = true;

                        if (x <= z)
                            verticalOffset = chunks[new Vector2Int(x, 1 - z)].verticalOffsets[3];
                        else
                            verticalOffset = chunks[new Vector2Int(x - 1, -z)].verticalOffsets[3];

                        chunk = chunkManager.GenerateChunk(seed, vector, verticalOffset, 3);

                        chunks[vector] = chunk;
                    }
                }
            }

        Load();

        int height = 0;

        for (int y = 0; y < mapSize.y; ++y)
            if (chunks[new Vector2Int(0, 0)].Blocks[0, y, 0] == blockMap.blockMap[0x30].BlockId)
            {
                height = y;

                break;
            }

        gameSettings.Spawn(height);

        chunkManager.inChunk[new Vector2Int(0, 0)] = true;
    }

    public void Load()
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
                                blocks[(int)chunk.x * 16 + mapOffset.x + x, y, (int)chunk.y * 16 + mapOffset.z + z] = Instantiate(blockMap.blockMap[(int)chunkBlocks[x, y, z]].BlockPrefab, new Vector3((int)chunk.x * 16 + x, y - mapOffset.y, (int)chunk.y * 16 + z), Quaternion.identity);
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
    }

    // Update is called once per frame
    void Update()
    {
        if (chunkManager.inChunk[new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16)] ==false)
        {
            foreach (var ch in chunks.Keys)
            {
                chunkActive[ch] = false;
                chunkManager.inChunk[ch] = false;
            }

            Chunk chunk = new Chunk();
            Vector2Int vector = new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16);

            if (chunks.ContainsKey(vector) == false)
            {
                chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                chunkActive[vector] = true;

                chunk = chunkManager.GenerateChunk(seed, vector, null, null);

                chunks[vector] = chunk;
            }
            else
                chunkActive[vector] = true;

            vector = new Vector2Int((int)transform.position.x / 16-1, (int)transform.position.z / 16);

            if (chunks.ContainsKey(vector) == false)
            {
                chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                chunkActive[vector] = true;

                chunk = chunkManager.GenerateChunk(seed, vector, chunks[new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16)].verticalOffsets[1], 1);

                chunks[vector] = chunk;
            }
            else
                chunkActive[vector] = true;

            vector = new Vector2Int((int)transform.position.x / 16 - 1, (int)transform.position.z / 16 - 1);

            if (chunks.ContainsKey(vector) == false)
            {
                chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                chunkActive[vector] = true;

                chunk = chunkManager.GenerateChunk(seed, vector, chunks[new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16)].verticalOffsets[2], 2);

                chunks[vector] = chunk;
            }
            else
                chunkActive[vector] = true;

            vector = new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16 - 1);

            if (chunks.ContainsKey(vector) == false)
            {
                chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                chunkActive[vector] = true;

                chunk = chunkManager.GenerateChunk(seed, vector, chunks[new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16)].verticalOffsets[3], 3);

                chunks[vector] = chunk;
            }
            else
                chunkActive[vector] = true;

            for (int x = 0; x < renderDistance; ++x)
                for (int z = 0; z < renderDistance; ++z)
                    if (Mathf.Pow(x, 2) + Mathf.Pow(z, 2) < Mathf.Pow(renderDistance, 2) && (x != 0 || z != 0) && (Mathf.Abs(transform.position.x + 16 * x) < mapSize.x - mapOffset.x) && (Mathf.Abs(transform.position.z + 16 * z) < mapSize.z - mapOffset.z))
                    {
                        Vector3Int pos = Vector3Int.FloorToInt(transform.position);

                        vector = new Vector2Int(pos.x / 16 + x, pos.z / 16 + z);

                        if (chunks.ContainsKey(vector) == false)
                        {
                            int verticalOffset = 0;

                            chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                            chunkActive[vector] = true;

                            if (x <= z)
                                verticalOffset = chunks[new Vector2Int(pos.x/16+x, pos.z/16+z - 1)].verticalOffsets[0];
                            else
                                verticalOffset = chunks[new Vector2Int(pos.x / 16 + x-1, pos.z / 16 + z)].verticalOffsets[0];

                            chunk = chunkManager.GenerateChunk(seed, vector, verticalOffset, 0);

                            chunks[vector] = chunk;
                        }
                        else
                            chunkActive[vector] = true;

                        vector = new Vector2Int(pos.x / 16 - x, pos.z / 16 + z);

                        if (x != 0)
                        {
                            if (chunks.ContainsKey(vector) == false)
                            {
                                int verticalOffset = 0;

                                chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                                chunkActive[vector] = true;

                                if (x <= z)
                                    verticalOffset = chunks[new Vector2Int(pos.x / 16 - x, pos.z / 16 + z + 1)].verticalOffsets[1];
                                else
                                    verticalOffset = chunks[new Vector2Int(pos.x / 16 + 1 - x, pos.z / 16 + z)].verticalOffsets[1];

                                chunk = chunkManager.GenerateChunk(seed, vector, verticalOffset, 1);

                                chunks[vector] = chunk;
                            }
                            else
                                chunkActive[vector] = true;
                        }

                        vector = new Vector2Int(pos.x / 16 - x, pos.z / 16 - z);

                        if (z != 0)
                        {
                            if (chunks.ContainsKey(vector) == false)
                            {
                                int verticalOffset = 0;

                                chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                                chunkActive[vector] = true;

                                if (x <= z)
                                    verticalOffset = chunks[new Vector2Int(pos.x / 16 - x, pos.z / 16 + 1 - z)].verticalOffsets[2];
                                else
                                    verticalOffset = chunks[new Vector2Int(pos.x / 16 + 1 - x, pos.z / 16 - z)].verticalOffsets[2];

                                chunk = chunkManager.GenerateChunk(seed, vector, verticalOffset, 2);

                                chunks[vector] = chunk;
                            }
                            else
                                chunkActive[vector] = true;
                        }

                        vector = new Vector2Int(pos.x / 16 + x, pos.z / 16 - z);

                        if (x != 0)
                        {
                            if (chunks.ContainsKey(vector) == false)
                            {
                                int verticalOffset = 0;

                                chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                                chunkActive[vector] = true;

                                if (x <= z)
                                    verticalOffset = chunks[new Vector2Int(pos.x / 16 + x, pos.z / 16 + 1 - z)].verticalOffsets[3];
                                else
                                    verticalOffset = chunks[new Vector2Int(pos.x / 16 + x, pos.z / 16 - z)].verticalOffsets[3];

                                chunk = chunkManager.GenerateChunk(seed, vector, verticalOffset, 3);

                                chunks[vector] = chunk;
                            }
                            else
                                chunkActive[vector] = true;
                        }
                    }

            chunkManager.inChunk[new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16)] = true;

            Load();
            UnLoad();
        }
    }

    public void CleanMap()
    {
        chunks = new Dictionary<Vector2Int, Chunk>();

        chunkManager.chunkLoaded = new Dictionary<Vector2Int, bool>();
        chunkActive = new Dictionary<Vector2Int, bool>();
        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];
    }

    public Dictionary<Vector2Int, Chunk> Chunks()
    {
        return chunks;
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
