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
    public CaveMask Caves;

    public ChunkGen(Vector2Int pos, TerrainData[,] layers, CaveMask caves = null)
    {
        Position = pos;
        TerrainLayers = layers;
        Caves = caves;
    }
}

public class ChunkLoad
{
    public Vector2Int Position;
    public string[,,] Blocks;
    public List<Vector3Int> FluidSeeds;

    //Tops[x, z]: wysokosc nad najwyzszym nieprzezroczystym blokiem kolumny (do tego poziomu siega swiatlo slonca)
    public int[,] Tops;

    //wysokosc nad najwyzszym blokiem chunka: pod-chunki powyzej sa puste
    public int Highest;

    public ChunkLoad(Vector2Int pos, string[,,] blocks, List<Vector3Int> fluidSeeds = null)
    {
        Position = pos;
        Blocks = blocks;
        FluidSeeds = fluidSeeds;

        ComputeTops();
    }

    public void SetBlock(int x, int y, int z, string id)
    {
        Blocks[x, y, z] = id;

        if (BlockKind.IsOpaque(id))
        {
            Tops[x, z] = Mathf.Max(Tops[x, z], y + 1);
            Highest = Mathf.Max(Highest, y + 1);
        }
        else if (Tops[x, z] == y + 1)
            Tops[x, z] = ScanOpaque(x, y, z);
    }

    private void ComputeTops()
    {
        int height = Blocks.GetLength(1);

        Tops = new int[16, 16];
        Highest = 0;

        for (int x = 0; x < 16; ++x)
        {
            for (int z = 0; z < 16; ++z)
            {
                int y = height - 1;

                while (y >= 0 && Blocks[x, y, z] == null)
                    --y;

                Highest = Mathf.Max(Highest, y + 1);
                Tops[x, z] = ScanOpaque(x, y, z);
            }
        }
    }

    //wysokosc nad najwyzszym nieprzezroczystym blokiem, szukanym w dol od y wlacznie
    private int ScanOpaque(int x, int y, int z)
    {
        while (y >= 0 && !BlockKind.IsOpaque(Blocks[x, y, z]))
            --y;

        return y + 1;
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

    public void GenerateChunk(TerrainData[,] layers, CaveMask caves = null)
    {
        Generator = new ChunkGen(Position, layers, caves);
        State = ChunkState.Generated;
    }

    public void LoadChunk(string[,,] blocks, List<Vector3Int> fluidSeeds = null)
    {
        Loader = new ChunkLoad(Position, blocks, fluidSeeds);
        State = ChunkState.Loaded;
    }
}

public class ChunkCollector
{
    public Dictionary<Vector2Int, Chunk> Chunks;
    public Dictionary<Vector2Int, Dictionary<Vector3Int, string>> Edits;

    public ChunkCollector()
    {
        Chunks = new Dictionary<Vector2Int, Chunk>();
        Edits = new Dictionary<Vector2Int, Dictionary<Vector3Int, string>>();
    }

    public void SetEdit(Vector3Int pos, string id)
    {
        Vector2Int chunkPos = new Vector2Int(Mathf.FloorToInt(pos.x / 16f), Mathf.FloorToInt(pos.z / 16f));

        if (!Edits.ContainsKey(chunkPos))
            Edits[chunkPos] = new Dictionary<Vector3Int, string>();

        Edits[chunkPos][pos] = id;

        if (Chunks.ContainsKey(chunkPos) && Chunks[chunkPos].Loader != null)
            Chunks[chunkPos].Loader.SetBlock(pos.x - chunkPos.x * 16, pos.y, pos.z - chunkPos.y * 16, id);
    }

    public void ChunkGenerate(Vector2Int pos, TerrainData[,] layers, CaveMask caves = null)
    {
        Chunks[pos] = new Chunk(pos, ChunkState.Generated);
        Chunks[pos].GenerateChunk(layers, caves);
    }

    public void ChunkRegenerate(Vector2Int pos, TerrainData[,] layers)
    {
        Chunks[pos].GenerateChunk(layers, Chunks[pos].Generator != null ? Chunks[pos].Generator.Caves : null);
        Chunks[pos].State = ChunkState.Regenerated;
    }

    public void ChunkLoad(Vector2Int pos, string[,,] blocks, List<Vector3Int> fluidSeeds = null)
    {
        Chunks[pos].LoadChunk(blocks, fluidSeeds);
    }

    public void ChunkReload(Vector2Int pos)
    {
        Chunks[pos].State = ChunkState.Reloaded;
    }

    public void ChunkUnload(Vector2Int pos)
    {
        Chunks[pos].State = ChunkState.Unloaded;
        Chunks[pos].Loader = null;
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