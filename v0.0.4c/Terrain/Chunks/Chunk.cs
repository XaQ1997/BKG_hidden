using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChunkState
{
    Generated,
    Regenerated,
    Loaded,
    Reloaded,
    Unloaded,
    Updated,
    Deleted
}

public class ChunkGen
{
    public Vector2Int Position;
    public TerrainData[,] TerrainLayers;

    public ChunkGen(Vector2Int pos, TerrainData[,] layers)
    {
        Position = pos;
        TerrainLayers = layers;
    }
}

public class ChunkLoad
{
    public Vector2Int Position;
    public string[,,] Blocks;

    public ChunkLoad(Vector2Int pos, string[,,] blocks)
    {
        Position = pos;
        Blocks = blocks;
    }
}

public class Chunk
{
    public Vector2Int Position;
    public ChunkState State;
    public ChunkGen Generator;
    public ChunkLoad Loader;

    public Chunk(Vector2Int pos, ChunkState state)
    {
        Position = pos;
        State = state;
    }

    public void GenerateChunk(TerrainData[,] layers)
    {
        Generator = new ChunkGen(Position, layers);
        State = ChunkState.Generated;
    }

    public void LoadChunk(string[,,] blocks)
    {
        Loader = new ChunkLoad(Position, blocks);
        State = ChunkState.Loaded;
    }
}

public class ChunkCollector
{
    public Dictionary<Vector2Int, Chunk> Chunks;

    public ChunkCollector()
    {
        Chunks = new Dictionary<Vector2Int, Chunk>();
    }

    public void ChunkGenerate(Vector2Int pos, TerrainData[,] layers)
    {
        Chunks[pos] = new Chunk(pos, ChunkState.Generated);
        Chunks[pos].GenerateChunk(layers);
    }

    public void ChunkRegenerate(Vector2Int pos, TerrainData[,] layers)
    {
        Chunks[pos].GenerateChunk(layers);
        Chunks[pos].State = ChunkState.Regenerated;
    }

    public void ChunkLoad(Vector2Int pos, string[,,] blocks)
    {
        Chunks[pos].LoadChunk(blocks);
    }

    public void ChunkReload(Vector2Int pos)
    {
        Chunks[pos].State = ChunkState.Reloaded;
    }

    public void ChunkUnload(Vector2Int pos)
    {
        Chunks[pos].State = ChunkState.Unloaded;
    }

    public void ChunkUpdate(Vector2Int pos, string[,,] blocks)
    {
        Chunks[pos].LoadChunk(blocks);
        Chunks[pos].State = ChunkState.Updated;
    }

    public void ChunkDelete(Vector2Int pos)
    {
        Chunks[pos].State = ChunkState.Deleted;
        Chunks.Remove(pos);
    }

    public Chunk Chunk(Vector2Int pos)
    {
        return Chunks[pos];
    }

    public bool IsExist(Vector2Int pos)
    {
        return Chunks.ContainsKey(pos);
    }
}