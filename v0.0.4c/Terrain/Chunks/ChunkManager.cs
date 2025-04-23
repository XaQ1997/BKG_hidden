using BKG.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private ChunkGenerator chunkGenerator;
    [SerializeField] private ChunkLoader chunkLoader;

    private ChunkCollector chunks;

    public void Awake()
    {
        Init();
    }

    private void Init()
    {
        chunks = new ChunkCollector();
        MathOperations math = new MathOperations();

        int distance = mapGenerator.GenerateDistance();

        int startX = -distance;
        int endX = distance;
        int startZ = -distance;
        int endZ = distance;

        for (int x = startX; x <= endX; ++x)
        {
            for (int z = startZ; z <= endZ; ++z)
            {
                Vector2Int chunkPos = new Vector2Int(x, z);

                if (math.IsShownChunk(Vector2Int.zero, chunkPos, distance))
                {
                    chunkGenerator.Generate(chunkPos);
                }
            }
        }

        distance = mapGenerator.RenderDistance();

        for (int x = startX; x <= endX; ++x)
        {
            for (int z = startZ; z <= endZ; ++z)
            {
                Vector2Int chunkPos = new Vector2Int(x, z);

                if (math.IsShownChunk(Vector2Int.zero, chunkPos, distance))
                {
                    chunkLoader.Load(chunkPos);
                }
            }
        }
    }

    public void ChangeChunk(Vector2Int pos)
    {
        foreach (var chunk in chunks.Chunks)
            if(chunk.Value.State==ChunkState.Loaded||chunk.Value.State==ChunkState.Reloaded)
                chunks.ChunkUnload(chunk.Key);

        MathOperations math = new MathOperations();

        int distance = mapGenerator.GenerateDistance();

        int startX = pos.x-distance;
        int endX = pos.x+distance;
        int startZ = pos.y-distance;
        int endZ = pos.y+distance;

        for (int x = startX; x <= endX; ++x)
        {
            for (int z = startZ; z <= endZ; ++z)
            {
                Vector2Int chunkPos = new Vector2Int(x, z);

                if (math.IsShownChunk(pos, chunkPos, distance))
                {
                    bool IsGenerated = chunks.Chunks.ContainsKey(chunkPos);

                    chunkGenerator.Generate(chunkPos);

                    if (IsGenerated)
                        chunks.ChunkRegenerate(pos, chunks.Chunks[chunkPos].Generator.TerrainLayers);
                }
            }
        }

        distance = mapGenerator.RenderDistance();

        for (int x = startX; x <= endX; ++x)
        {
            for (int z = startZ; z <= endZ; ++z)
            {
                Vector2Int chunkPos = new Vector2Int(x, z);

                if (math.IsShownChunk(pos, chunkPos, distance))
                {
                    bool IsLoaded = chunks.Chunks[chunkPos].State == ChunkState.Unloaded ? true : false;

                    chunkLoader.Load(chunkPos);

                    if (IsLoaded)
                        chunks.ChunkReload(chunkPos);
                }
            }
        }
    }

    public ChunkCollector Chunks()
    {
        return chunks;
    }
}