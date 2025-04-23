using BKG.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private ChunkManager chunkManager;

    public void Generate(Vector2Int pos)
    {
        int seed = mapGenerator.Seed();
        MathOperations math = new MathOperations();

        TerrainData[,] layers = new TerrainData[16, 16];

        for(int x=0;x<16;++x)
        {
            for(int z=0;z<16;++z)
            {
                layers[x, z] = new TerrainData();

                int top = math.PrimeNumberModularSeed(4, math.LocalSeed(math.ChunkSeed(seed, pos), new Vector2Int(x, z))) % 4;

                TerrainLayer groundBlockLayer = new TerrainLayer(0, 0, top, 0, "00");
                layers[x, z].SetLayer(0, groundBlockLayer);

                int bottom = top;

                top = math.CreatingSeed(math.LocalSeed(math.ChunkSeed(seed, pos), new Vector2Int(x, z)) + 1, 16, 32, 23, new int[2] { -2, 2 }, new Vector2Int(pos.x * 16 + x, pos.y * 16 + z), 0.5f);

                TerrainLayer hellStoneLayer = new TerrainLayer(1, bottom, top, 23, "20v3");
                layers[x, z].SetLayer(1, hellStoneLayer);

                bottom = top;
                top = math.CreatingSeed(math.LocalSeed(math.ChunkSeed(seed, pos), new Vector2Int(x, z))+2, 40, 56, 47, new int[2] { -2, 2 }, new Vector2Int(pos.x * 16 + x, pos.y * 16 + z), 0.5f);

                TerrainLayer darkStoneLayer = new TerrainLayer(2, bottom, top, 47, "20v2");
                layers[x, z].SetLayer(2, darkStoneLayer);

                bottom = top;
                top = math.CreatingSeed(math.LocalSeed(math.ChunkSeed(seed, pos), new Vector2Int(x, z)) + 3, 64, 80, 71, new int[2] { -2, 2 }, new Vector2Int(pos.x * 16 + x, pos.y * 16 + z), 0.5f);

                TerrainLayer deepStoneLayer = new TerrainLayer(3, bottom, top, 71, "20v1");
                layers[x, z].SetLayer(3, deepStoneLayer);

                bottom = top;
                top = math.CreatingSeed(math.LocalSeed(math.ChunkSeed(seed, pos), new Vector2Int(x, z)) + 4, 80, 140, 95, new int[2] { -4, 4 }, new Vector2Int(pos.x * 16 + x, pos.y * 16 + z));

                TerrainLayer stoneLayer = new TerrainLayer(4, bottom, top, 95, "20");
                layers[x, z].SetLayer(4, stoneLayer);

                TerrainLayer skyStoneLayer = new TerrainLayer(5, 256, top, 256, "20v4");
                layers[x, z].SetLayer(5, skyStoneLayer);

                TerrainLayer dirtLayer = new TerrainLayer(6, top, top + 3, 98, "31");
                layers[x, z].SetLayer(6, dirtLayer);

                TerrainLayer grassBlockLayer = new TerrainLayer(7, top + 4, top + 4, 99, "30");
                layers[x, z].SetLayer(7, grassBlockLayer);
            }
        }

        chunkManager.Chunks().ChunkGenerate(pos, layers);
    }
}
