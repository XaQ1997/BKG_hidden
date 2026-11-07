using BKG.Math;
using UnityEngine;

//woda powierzchniowa: morza, jeziora i rzeki to zaglebienia terenu ponizej poziomu morza, ktore wypelnia woda
//zaglebienia powstaja przez obnizenie powierzchni (nigdy jej nie podnosza), slabna na wysokich gorach i omijaja okolice startu
//wspolrzedne swiata + szum 2D, wiec sasiednie chunki zgadzaja sie na granicach
public static class WaterGenerator
{
    //poziom morza: woda wypelnia komorki ponizej tej wysokosci; lezy ok. 35 blokow pod mediana powierzchni (ok. 355), wiec woda zajmuje kilkanascie procent terenu
    //okno zaniku (FadeStart/FadeEnd) przesuwa sie razem z nim: powyzej niego (wzgorza, gory) nic sie nie wycina
    public const int SeaLevel = 320;

    private const float SeaFrequency = 0.002f;
    private const float SeaMin = 0.35f;
    private const float SeaRamp = 0.4f;
    private const int SeaBed = 2;
    private const int SeaDepth = 18;
    private const float LakeFrequency = 0.006f;
    private const float LakeMin = 0.55f;
    private const float LakeRamp = 0.35f;
    private const int LakeBed = 2;
    private const int LakeDepth = 5;
    private const float RiverFrequency = 0.0022f;
    private const float RiverWarpFrequency = 0.004f;
    private const float RiverWarp = 40f;
    private const float RiverInner = 0.02f;
    private const float RiverOuter = 0.22f;
    private const float RiverMaskFrequency = 0.0015f;
    private const int RiverBed = 3;
    private const int FadeStart = SeaLevel + 54;
    private const int FadeEnd = SeaLevel + 84;
    private const float SpawnClear = 32f;
    private const float SpawnFade = 48f;

    private static SimplexNoise sea;
    private static SimplexNoise lake;
    private static SimplexNoise river;
    private static SimplexNoise riverMask;
    private static SimplexNoise riverWarp;
    private static Vector2 offsetSea;
    private static Vector2 offsetLake;
    private static Vector2 offsetRiver;
    private static Vector2 offsetMask;
    private static Vector2 offsetWarp;
    private static int noiseSeed;
    private static bool isReady = false;

    public static int Carve(int surface, int worldX, int worldZ, int worldSeed)
    {
        return Carve(surface, worldX, worldZ, worldSeed, out float influence);
    }

    //nowa wysokosc powierzchni kolumny (gorna sciana najwyzszego bloku) po wycieciu morz, jezior i rzek
    //influence (0..1) to najwiekszy udzial zaglebienia w tej kolumnie: ponad zerem kolumna lezy w strefie brzegu (plaze), nawet jesli sama nie zostala zalana
    public static int Carve(int surface, int worldX, int worldZ, int worldSeed, out float influence)
    {
        Prepare(worldSeed);

        influence = 0f;

        float limit = (1f - Smooth(FadeStart, FadeEnd, surface)) * Smooth(SpawnClear, SpawnClear + SpawnFade, Mathf.Sqrt(worldX * worldX + worldZ * worldZ));

        if (limit <= 0f)
            return surface;

        float result = surface;

        float seaValue = sea.Generate(new Vector3(worldX * SeaFrequency + offsetSea.x, 0f, worldZ * SeaFrequency + offsetSea.y));

        float seaInfluence = Smooth(SeaMin, SeaMin + SeaRamp, seaValue) * limit;

        result = Basin(result, seaInfluence, SeaLevel - SeaBed - SeaDepth * Smooth(SeaMin + SeaRamp * 0.5f, 1f, seaValue));

        float lakeValue = lake.Generate(new Vector3(worldX * LakeFrequency + offsetLake.x, 0f, worldZ * LakeFrequency + offsetLake.y));

        float lakeInfluence = Smooth(LakeMin, LakeMin + LakeRamp, lakeValue) * limit;

        result = Basin(result, lakeInfluence, SeaLevel - LakeBed - LakeDepth * Smooth(LakeMin + LakeRamp * 0.5f, 1f, lakeValue));

        //rzeka to linia, na ktorej szum przechodzi przez zero; osie lekko zniekszalcone, wystepuje tylko w czesci swiata
        float warpX = riverWarp.Generate(new Vector3(worldX * RiverWarpFrequency + offsetWarp.x, 0f, worldZ * RiverWarpFrequency + offsetWarp.y)) * RiverWarp;
        float warpZ = riverWarp.Generate(new Vector3(worldX * RiverWarpFrequency + offsetWarp.x + 173.3f, 0f, worldZ * RiverWarpFrequency + offsetWarp.y + 91.7f)) * RiverWarp;
        float riverValue = Mathf.Abs(river.Generate(new Vector3((worldX + warpX) * RiverFrequency + offsetRiver.x, 0f, (worldZ + warpZ) * RiverFrequency + offsetRiver.y)));
        float present = Smooth(-0.2f, 0.2f, riverMask.Generate(new Vector3(worldX * RiverMaskFrequency + offsetMask.x, 0f, worldZ * RiverMaskFrequency + offsetMask.y)));
        float banks = 1f - Smooth(RiverInner, RiverOuter, riverValue);

        float riverInfluence = banks * present * limit;

        result = Basin(result, riverInfluence, SeaLevel - RiverBed);

        influence = Mathf.Max(seaInfluence, Mathf.Max(lakeInfluence, riverInfluence));

        return Mathf.FloorToInt(result);
    }

    //obniza wysokosc w strone dna proporcjonalnie do wplywu; nigdy nie podnosi terenu
    private static float Basin(float height, float influence, float bed)
    {
        return bed < height ? Mathf.Lerp(height, bed, influence) : height;
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

        sea = new SimplexNoise(worldSeed + 21);
        lake = new SimplexNoise(worldSeed + 22);
        river = new SimplexNoise(worldSeed + 23);
        riverMask = new SimplexNoise(worldSeed + 24);
        riverWarp = new SimplexNoise(worldSeed + 25);

        offsetSea = new Vector2(Offset(worldSeed, 21), Offset(worldSeed, 22));
        offsetLake = new Vector2(Offset(worldSeed, 23), Offset(worldSeed, 24));
        offsetRiver = new Vector2(Offset(worldSeed, 25), Offset(worldSeed, 26));
        offsetMask = new Vector2(Offset(worldSeed, 27), Offset(worldSeed, 28));
        offsetWarp = new Vector2(Offset(worldSeed, 29), Offset(worldSeed, 30));

        noiseSeed = worldSeed;
        isReady = true;
    }

    private static float Offset(int worldSeed, int salt)
    {
        return (MathOperations.ChunkSeed(worldSeed, new Vector2Int(salt, salt * 31 + 7)) & 0xFFFF) / 65536f * 20000f;
    }
}
