using BKG.Math;
using UnityEngine;

//platy gliny na dnie plytkiej wody: szum 2D o niskiej czestotliwosci, wspolrzedne swiata, wiec sasiednie chunki zgadzaja sie na granicach
public static class ClayGenerator
{
    private const float Frequency = 0.07f;
    private const float Threshold = 0.35f;

    private static SimplexNoise clay;
    private static Vector2 offset;
    private static int noiseSeed;
    private static bool isReady = false;

    public static bool IsClay(int worldX, int worldZ, int worldSeed)
    {
        Prepare(worldSeed);

        return clay.Generate(new Vector3(worldX * Frequency + offset.x, 0f, worldZ * Frequency + offset.y)) > Threshold;
    }

    private static void Prepare(int worldSeed)
    {
        if (isReady && noiseSeed == worldSeed)
            return;

        clay = new SimplexNoise(worldSeed + 31);
        offset = new Vector2(Offset(worldSeed, 41), Offset(worldSeed, 42));

        noiseSeed = worldSeed;
        isReady = true;
    }

    private static float Offset(int worldSeed, int salt)
    {
        return (MathOperations.ChunkSeed(worldSeed, new Vector2Int(salt, salt * 31 + 7)) & 0xFFFF) / 65536f * 20000f;
    }
}
