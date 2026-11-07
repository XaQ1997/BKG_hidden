using BKG.Math;
using UnityEngine;

//gory: rzadkie, bardzo wysokie masywy ponad zwyklym terenem, siegajace pasm nieba (kamien od 512 w gore)
//maska szumu 2D o niskiej czestotliwosci wyznacza masywy, a grzbiety (1 - |szum|) rzezbia w nich granie i doliny; teren pod spodem zostaje bez zmian
//wspolrzedne swiata + szum 2D, wiec sasiednie chunki zgadzaja sie na granicach; okolice startu sa omijane
public static class MountainGenerator
{
    private const float MaskFrequency = 0.0009f;
    private const float MaskMin = 0.36f;
    private const float MaskRamp = 0.4f;
    private const float MaskPower = 1.5f;
    private const float RidgeFrequency = 0.005f;
    private const float DetailFrequency = 0.02f;
    private const float ValleyShare = 0.2f;
    private const float DetailStrength = 0.08f;
    private const float SpawnClear = 96f;
    private const float SpawnFade = 128f;

    private static SimplexNoise mask;
    private static SimplexNoise ridge;
    private static SimplexNoise detail;
    private static Vector2 offsetMask;
    private static Vector2 offsetRidge;
    private static Vector2 offsetDetail;
    private static int noiseSeed;
    private static bool isReady = false;

    //nowa gora kamienia kolumny: zwykla wysokosc, a w masywie podniesiona w strone peak (najwyzszy szczyt)
    public static int Raise(int stoneTop, int worldX, int worldZ, int worldSeed, int peak)
    {
        Prepare(worldSeed);

        float spawn = Smooth(SpawnClear, SpawnClear + SpawnFade, Mathf.Sqrt(worldX * worldX + worldZ * worldZ));

        if (spawn <= 0f || stoneTop >= peak)
            return stoneTop;

        float massif = Smooth(MaskMin, MaskMin + MaskRamp, mask.Generate(new Vector3(worldX * MaskFrequency + offsetMask.x, 0f, worldZ * MaskFrequency + offsetMask.y)));

        if (massif <= 0f)
            return stoneTop;

        massif = Mathf.Pow(massif, MaskPower);

        float crest = 1f - Mathf.Abs(ridge.Generate(new Vector3(worldX * RidgeFrequency + offsetRidge.x, 0f, worldZ * RidgeFrequency + offsetRidge.y)));
        float rough = (detail.Generate(new Vector3(worldX * DetailFrequency + offsetDetail.x, 0f, worldZ * DetailFrequency + offsetDetail.y)) + 1f) * 0.5f;
        float shape = Mathf.Min(1f, ValleyShare + (1f - ValleyShare) * crest * crest + DetailStrength * rough);

        return Mathf.Max(stoneTop, Mathf.FloorToInt(stoneTop + (peak - stoneTop) * massif * shape * spawn));
    }

    private static float Smooth(float from, float to, float value)
    {
        float t = Mathf.Clamp01(Mathf.InverseLerp(from, to, value));

        return t * t * (3f - 2f * t);
    }

    private static void Prepare(int worldSeed)
    {
        if (isReady && noiseSeed == worldSeed)
            return;

        mask = new SimplexNoise(worldSeed + 51);
        ridge = new SimplexNoise(worldSeed + 52);
        detail = new SimplexNoise(worldSeed + 53);

        offsetMask = new Vector2(Offset(worldSeed, 51), Offset(worldSeed, 52));
        offsetRidge = new Vector2(Offset(worldSeed, 53), Offset(worldSeed, 54));
        offsetDetail = new Vector2(Offset(worldSeed, 55), Offset(worldSeed, 56));

        noiseSeed = worldSeed;
        isReady = true;
    }

    private static float Offset(int worldSeed, int salt)
    {
        return (MathOperations.ChunkSeed(worldSeed, new Vector2Int(salt, salt * 31 + 7)) & 0xFFFF) / 65536f * 20000f;
    }
}
