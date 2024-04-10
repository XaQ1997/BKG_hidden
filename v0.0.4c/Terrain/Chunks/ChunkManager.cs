using BKG.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int chunkSeed(Vector2Int currentChunk, int baseSeed)
    {
        // Oblicz "ziarno" na podstawie po³o¿enia chunku wzglêdem chunku startowego
        int seed = baseSeed + 257 * currentChunk.x + currentChunk.y;

        return seed;
    }
}