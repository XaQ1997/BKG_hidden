using BKG.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    private static readonly int[] smallOffsets = new int[2] { -2, 2 };
    private static readonly int[] bigOffsets = new int[2] { -4, 4 };

    //tabela warstw: zakresy wysokosci, poziomy i bloki
    private static readonly TerrainData template = TerrainData.Default();

    //plaza: kolumna powyzej poziomu morza, ale w strefie wplywu wody (shore > BeachShore) i nie wyzej niz BeachRise nad morzem
    private const float BeachShore = 0.05f;
    private const int BeachRise = 3;

    //glina tylko tam, gdzie woda jest plytsza niz ClayDepth
    private const int ClayDepth = 10;

    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private ChunkManager chunkManager;

    //gora kamienia kolumny: szum, masywy gorskie, a na koniec wyciete morza, jeziora i rzeki; shore to wplyw zaglebienia (plaze)
    private static int StoneTop(Vector2Int worldPos, int localSeed, int seed, out float shore)
    {
        TerrainLayer stoneRange = template.GetLayer(TerrainData.Stone);
        int top = MathOperations.CreatingSeed(localSeed + TerrainData.Stone, stoneRange.LayerBorders[0], stoneRange.LayerBorders[1], stoneRange.DefaultLevel, bigOffsets, worldPos, seed);

        //masywy gorskie podnosza kamien az do najwyzszego pasma nieba
        top = MountainGenerator.Raise(top, worldPos.x, worldPos.y, seed, template.GetLayer(TerrainData.HighSky).LayerBorders[1]);

        return WaterGenerator.Carve(top + 4, worldPos.x, worldPos.y, seed, out shore) - 4;
    }

    //dno pod woda i pas brzegu to piasek
    private static bool IsSandy(int surface, float shore)
    {
        return surface < WaterGenerator.SeaLevel || (shore > BeachShore && surface <= WaterGenerator.SeaLevel + BeachRise);
    }

    //czy w kolumnie rosnie trawa (nie piasek, nie woda); surface to wysokosc pierwszej wolnej komorki nad nia
    //liczone od zera z wspolrzednych swiata, wiec dziala dla dowolnej kolumny, takze w niewygenerowanym chunku (drzewa na granicach chunkow)
    public static bool IsGrassColumn(int worldX, int worldZ, int seed, out int surface)
    {
        Vector2Int chunk = new Vector2Int(worldX >> 4, worldZ >> 4);
        int localSeed = MathOperations.LocalSeed(MathOperations.ChunkSeed(seed, chunk), new Vector2Int(worldX & 15, worldZ & 15));
        int top = StoneTop(new Vector2Int(worldX, worldZ), localSeed, seed, out float shore);

        surface = top + 4;

        return !IsSandy(surface, shore);
    }

    public void Generate(Vector2Int pos)
    {
        int seed = mapGenerator.Seed();
        int chunkSeed = MathOperations.ChunkSeed(seed, pos);

        TerrainData[,] layers = new TerrainData[16, 16];
        int[,] solidTop = new int[16, 16];
        int[] tops = new int[TerrainData.Stone + 1];

        for(int x=0;x<16;++x)
        {
            for(int z=0;z<16;++z)
            {
                layers[x, z] = new TerrainData();

                int localSeed = MathOperations.LocalSeed(chunkSeed, new Vector2Int(x, z));
                Vector2Int worldPos = new Vector2Int(pos.x * 16 + x, pos.y * 16 + z);

                //zakresy, poziomy i bloki warstw pochodza z tabeli TerrainData.Default(); gora kazdej warstwy jest losowana z jej zakresu
                //i nigdy nie lezy ponizej gory warstwy pod nia (zakresy sie nakladaja)
                TerrainLayer ground = template.GetLayer(TerrainData.Bedrock);

                tops[TerrainData.Bedrock] = ground.LayerBorders[0] + MathOperations.PositiveModulo(MathOperations.PrimeNumberModularSeed(4, localSeed), ground.LayerBorders[1] - ground.LayerBorders[0] + 1);

                for (int i = TerrainData.HellLow; i < TerrainData.Stone; ++i)
                {
                    TerrainLayer range = template.GetLayer(i);

                    tops[i] = Mathf.Max(tops[i - 1], MathOperations.CreatingSeed(localSeed + i, range.LayerBorders[0], range.LayerBorders[1], range.DefaultLevel, smallOffsets, worldPos, seed, 0.5f));
                }

                int top = StoneTop(worldPos, localSeed, seed, out float shore);

                //wyciete zaglebienie obcina tez warstwy pod powierzchnia
                tops[TerrainData.Stone] = top;

                for (int i = TerrainData.Bedrock; i < TerrainData.Stone; ++i)
                    tops[i] = Mathf.Min(tops[i], top);

                int bottom = 1;

                for (int i = TerrainData.Bedrock; i <= TerrainData.Stone; ++i)
                {
                    TerrainLayer range = template.GetLayer(i);

                    layers[x, z].SetLayer(i, new TerrainLayer(i, bottom, tops[i], range.DefaultLevel, range.BlockId));

                    bottom = tops[i];
                }

                //pasma nieba sa wspolne dla wszystkich kolumn (stale zakresy wysokosci)
                for (int i = TerrainData.Sky; i <= TerrainData.HighSky; ++i)
                    layers[x, z].SetLayer(i, template.GetLayer(i));

                //dno pod woda i pas brzegu to piasek (ChunkLoader robi z niego mokry ponizej poziomu morza), trawa rosnie tylko dalej od wody
                int surface = top + 4;
                bool sandy = IsSandy(surface, shore);

                //na dnie plytkiej wody sa platy gliny zamiast piasku
                bool clay = sandy && surface < WaterGenerator.SeaLevel && surface >= WaterGenerator.SeaLevel - ClayDepth && ClayGenerator.IsClay(worldPos.x, worldPos.y, seed);
                string soil = clay ? BlockKind.Clay : BlockKind.DrySand;

                TerrainLayer dirtRange = template.GetLayer(TerrainData.Dirt);
                TerrainLayer grassRange = template.GetLayer(TerrainData.Grass);

                layers[x, z].SetLayer(TerrainData.Dirt, new TerrainLayer(TerrainData.Dirt, top, top + 3, dirtRange.DefaultLevel, sandy ? soil : dirtRange.BlockId));
                layers[x, z].SetLayer(TerrainData.Grass, new TerrainLayer(TerrainData.Grass, top + 4, top + 4, grassRange.DefaultLevel, sandy ? soil : grassRange.BlockId));

                solidTop[x, z] = top + 4;
            }
        }

        chunkManager.Chunks().ChunkGenerate(pos, layers, CaveGenerator.Generate(pos, solidTop, seed));
    }
}
