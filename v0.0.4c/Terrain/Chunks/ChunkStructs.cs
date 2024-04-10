using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Chunk
{
    public Vector2Int Position;
    public string[,,] Blocks;

    public Chunk(Vector2Int pos, string[,,] blocks)
    {
        Position = pos;
        Blocks = blocks;
    }
}

public struct ChunkFrame
{
    public Vector2Int Position;
    public ChunkCorner[] Corners;
    public ChunkEdge[] Edges;

    public ChunkFrame(Vector2Int pos, ChunkEdge[] edges, ChunkCorner[] corners)
    {
        Position = pos;
        Edges = edges;
        Corners = corners;
    }
}

public struct ChunkEdge
{
    public int[][] LayerData;

    public ChunkEdge(int[][] layers)
    {
        LayerData = layers;
    }
}

public struct ChunkCorner
{
    public int[] LayerData;

    public ChunkCorner(int[] layers)
    {
        LayerData = layers;
    }
}