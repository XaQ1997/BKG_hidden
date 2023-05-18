using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ChunkManager : MonoBehaviour
{
    [HideInInspector] public Dictionary<Vector2Int, bool> chunkLoaded = new Dictionary<Vector2Int, bool>();
    [HideInInspector] public Dictionary<Vector2Int, bool> inChunk = new Dictionary<Vector2Int, bool>();

    public Chunk GenerateChunk(uint seed, Vector2Int vector, int? vO = null, int? dir = null)
    {
        int direction = new Random(seed + (uint)(257 * vector.x + vector.y)).NextInt(0, 4);

        int?[,,] blocks = new int?[16, 256, 16];
        int[,] grounds = new int[16, 16];
        int[,] verticalOffsets = new int[16, 16];

        for (int x = 0; x < 16; ++x)
            for (int z = 0; z < 16; ++z)
            {
                grounds[x, z] = (new Random(257 * seed + (uint)(257 * (vector.x + x) + vector.y + z)).NextInt(0, 3) + new Random(509 * seed + (uint)(509 * (vector.y + z) + Mathf.Pow(-1, x) * (vector.x + x))).NextInt(0, 3)) / 2;
                verticalOffsets[x, z] = new Random(257 * seed + (uint)(257 * (vector.x + x) + vector.y + z)).NextInt(-1, 1) + new Random(509 * seed + (uint)(509 * (vector.y + z) + Mathf.Pow(-1, x) * (vector.x + x))).NextInt(-1, 1) / 2;
            }

        for (int y = 0; y < 256; ++y)
            for (int x = 0; x < 16; ++x)
            {
                int offset = 0;

                if (x != 0)
                    offset = verticalOffsets[x - 1, 0];
                else if (vO != null)
                    offset = (int)vO;

                for (int z = 0; z < 16; ++z)
                {
                    if (direction == 0)
                    {
                        offset += verticalOffsets[x, z];

                        if (y < grounds[x, z])
                            blocks[x, y, z] = 0x00;
                        else if (y < grounds[x, z] + offset + 90)
                            blocks[x, y, z] = 0x20;
                        else if (y < grounds[x, z] + offset + 93)
                            blocks[x, y, z] = 0x31;
                        else if (y == grounds[x, z] + offset + 93)
                            blocks[x, y, z] = 0x30;
                    }
                    if (direction == 1)
                    {
                        offset += verticalOffsets[15 - x, z];

                        if (y < grounds[15 - x, z])
                            blocks[x, y, z] = 0x00;
                        else if (y < grounds[15 - x, z] + offset + 90)
                            blocks[x, y, z] = 0x20;
                        else if (y < grounds[15 - x, z] + offset + 93)
                            blocks[x, y, z] = 0x31;
                        else if (y == grounds[15 - x, z] + offset + 93)
                            blocks[x, y, z] = 0x30;
                    }
                    if (direction == 2)
                    {
                        offset += verticalOffsets[15 - x, 15 - z];

                        if (y < grounds[15 - x, 15 - z])
                            blocks[x, y, z] = 0x00;
                        else if (y < grounds[15 - x, 15 - z] + offset + 90)
                            blocks[x, y, z] = 0x20;
                        else if (y < grounds[15 - x, 15 - z] + offset + 93)
                            blocks[x, y, z] = 0x31;
                        else if (y == grounds[15 - x, 15 - z] + offset + 93)
                            blocks[x, y, z] = 0x30;
                    }
                    if (direction == 3)
                    {
                        offset += verticalOffsets[x, 15 - z];

                        if (y < grounds[x, 15 - z])
                            blocks[x, y, z] = 0x00;
                        else if (y < grounds[x, 15 - z] + offset + 90)
                            blocks[x, y, z] = 0x20;
                        else if (y < grounds[x, 15 - z] + offset + 93)
                            blocks[x, y, z] = 0x31;
                        else if (y == grounds[x, 15 - z] + offset + 93)
                            blocks[x, y, z] = 0x30;
                    }
                }
            }

        int[] offsets = new int[4];

        if (vO != null)
            offsets[0] = (int)vO + verticalOffsets[0, 0];
        else
            offsets[0] = verticalOffsets[0, 0];

        offsets[1] = offsets[0];

        for (int i = 1; i < 16; ++i)
            offsets[1] += verticalOffsets[0, i];

        offsets[2] = offsets[0];

        for (int i = 1; i < 16; ++i)
            offsets[2] += verticalOffsets[i, 0];

        offsets[3] = offsets[1];

        for (int i = 1; i < 16; ++i)
            offsets[3] += verticalOffsets[i, 15];

        int?[,,] tmp = blocks;

        if (dir == 1)
            for (int x = 0; x < 16; ++x)
                for (int y = 0; y < 256; ++y)
                    for (int z = 0; z < 16; ++z)
                        tmp[x, y, z] = blocks[15 - x, y, z];
        if (dir == 2)
            for (int x = 0; x < 16; ++x)
                for (int y = 0; y < 256; ++y)
                    for (int z = 0; z < 16; ++z)
                        tmp[x, y, z] = blocks[15 - x, y, 15 - z];
        if (dir == 3)
            for (int x = 0; x < 16; ++x)
                for (int y = 0; y < 256; ++y)
                    for (int z = 0; z < 16; ++z)
                        tmp[x, y, z] = blocks[x, y, 15 - z];

        chunkLoaded[vector] = false;

        Chunk result = new Chunk(vector, tmp);

        inChunk[vector] = false;

        return result;
    }
}

public struct Chunk
{
    public Vector2Int Position;
    public int?[,,] Blocks;
    public int[] verticalOffsets;

    public Chunk(Vector2Int pos, int?[,,] blocks)
    {
        Position = pos;
        Blocks = blocks;
        verticalOffsets = new int[4];
    }
}
