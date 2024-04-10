using BKG.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int chunkSeed(Vector2Int currentChunk, int baseSeed)
    {
        // Oblicz "ziarno" na podstawie po�o�enia chunku wzgl�dem chunku startowego
        int seed = baseSeed + 257 * currentChunk.x + currentChunk.y;

        return seed;
    }
}