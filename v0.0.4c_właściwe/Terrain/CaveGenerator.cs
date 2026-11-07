using BKG.Math;
using UnityEngine;

//maska chunka: komorki jaskin i zlozy zwiru, indeks [x, y - Bottom, z]
public class CaveMask
{
    public int Bottom;
    public bool[,,] Cells;
    public bool[,,] Gravel;
    public bool[,,] Water;

    public bool IsCave(int x, int y, int z)
    {
        int index = y - Bottom;

        return index >= 0 && index < Cells.GetLength(1) && Cells[x, index, z];
    }

    public bool IsWater(int x, int y, int z)
    {
        int index = y - Bottom;

        return index >= 0 && index < Water.GetLength(1) && Water[x, index, z];
    }
}

//jaskinie to tunele: miejsca, gdzie dwa pola szumu 3D jednoczesnie leza blisko zera
//zloza zwiru to bryly, gdzie trzecie pole szumu 3D jest duze (pod powierzchnia, poza jaskiniami)
//szum liczony jest na siatce co Step blokow (wspolrzedne swiata) i interpolowany, wiec sasiednie chunki zgadzaja sie na granicach
public static class CaveGenerator
{
    public const int Bottom = 8;

    //jaskinie koncza sie na tej wysokosci, takze wewnatrz bardzo wysokich gor (maska i szum rosly by z wysokoscia szczytu)
    public const int Ceiling = 480;
    public const string GravelId = "70";

    private const int Step = 4;
    private const float FrequencyXZ = 0.028f;
    private const float FrequencyY = 0.045f;
    private const float Width = 0.15f;
    private const float EntranceFrequency = 0.02f;
    private const float EntranceMin = 0.6f;
    private const int FadeStart = 3;
    private const int FadeEnd = 9;
    private const int SpawnRadius = 6;
    private const int SpawnDepth = 12;
    private const float GravelFrequency = 0.06f;
    private const float GravelThreshold = 0.62f;
    private const int GravelMinDepth = 8;
    private const int GravelMinY = 12;
    private const float PocketFrequency = 0.018f;
    private const float PocketLevelFrequency = 0.004f;
    private const float PocketMin = 0.45f;
    private const float PocketMax = 0.8f;
    private const int PocketBottom = 24;
    private const int PocketTop = 160;
    private const int PocketStep = 8;
    private const int PocketDepth = 5;
    private const int PocketHeight = 7;
    private const int PocketCover = 5;

    private static SimplexNoise tunnelA;
    private static SimplexNoise tunnelB;
    private static SimplexNoise entrance;
    private static SimplexNoise gravel;
    private static SimplexNoise pocket;
    private static SimplexNoise pocketLevel;
    private static Vector3 offsetA;
    private static Vector3 offsetB;
    private static Vector3 offsetGravel;
    private static Vector2 offsetEntrance;
    private static Vector2 offsetPocket;
    private static Vector2 offsetPocketLevel;
    private static int noiseSeed;
    private static bool isReady = false;

    //solidTop[x, z] to wysokosc, ponizej ktorej kolumna jest lita
    public static CaveMask Generate(Vector2Int chunkPos, int[,] solidTop, int worldSeed)
    {
        Prepare(worldSeed);

        int maxTop = 0;

        for (int x = 0; x < 16; ++x)
            for (int z = 0; z < 16; ++z)
                maxTop = Mathf.Max(maxTop, solidTop[x, z]);

        maxTop = Mathf.Min(maxTop, Ceiling);

        int height = maxTop - Bottom;

        if (height <= 0)
            return null;

        CaveMask mask = new CaveMask();

        mask.Bottom = Bottom;
        mask.Cells = new bool[16, height, 16];
        mask.Gravel = new bool[16, height, 16];
        mask.Water = new bool[16, height, 16];

        int baseX = chunkPos.x * 16;
        int baseZ = chunkPos.y * 16;
        int latticeHeight = height / Step + 2;
        int latticeWidth = 16 / Step + 1;

        float[,,] a = new float[latticeWidth, latticeHeight, latticeWidth];
        float[,,] b = new float[latticeWidth, latticeHeight, latticeWidth];
        float[,,] g = new float[latticeWidth, latticeHeight, latticeWidth];

        for (int lx = 0; lx < latticeWidth; ++lx)
        {
            for (int ly = 0; ly < latticeHeight; ++ly)
            {
                for (int lz = 0; lz < latticeWidth; ++lz)
                {
                    float wx = baseX + lx * Step;
                    float wy = Bottom + ly * Step;
                    float wz = baseZ + lz * Step;

                    a[lx, ly, lz] = tunnelA.Generate(new Vector3(wx * FrequencyXZ + offsetA.x, wy * FrequencyY + offsetA.y, wz * FrequencyXZ + offsetA.z));
                    b[lx, ly, lz] = tunnelB.Generate(new Vector3(wx * FrequencyXZ + offsetB.x, wy * FrequencyY + offsetB.y, wz * FrequencyXZ + offsetB.z));
                    g[lx, ly, lz] = gravel.Generate(new Vector3(wx * GravelFrequency + offsetGravel.x, wy * GravelFrequency + offsetGravel.y, wz * GravelFrequency + offsetGravel.z));
                }
            }
        }

        for (int x = 0; x < 16; ++x)
        {
            for (int z = 0; z < 16; ++z)
            {
                int top = solidTop[x, z];
                int worldX = baseX + x;
                int worldZ = baseZ + z;

                bool isEntrance = entrance.Generate(new Vector3(worldX * EntranceFrequency + offsetEntrance.x, 0f, worldZ * EntranceFrequency + offsetEntrance.y)) > EntranceMin;
                bool nearSpawn = worldX * worldX + worldZ * worldZ < SpawnRadius * SpawnRadius;

                bool submerged = top < WaterGenerator.SeaLevel;

                Pocket(worldX, worldZ, top, nearSpawn, out int pocketLow, out int pocketHigh, out int pocketWater);

                int lx = x / Step;
                int lz = z / Step;
                float fx = (x % Step) / (float)Step;
                float fz = (z % Step) / (float)Step;

                int caveTop = Mathf.Min(top, Ceiling);

                for (int y = Bottom; y < caveTop; ++y)
                {
                    int depth = top - y;
                    int ly = (y - Bottom) / Step;
                    float fy = ((y - Bottom) % Step) / (float)Step;

                    bool inPocket = y >= pocketLow && y < pocketHigh;

                    if (inPocket || IsTunnel(a, b, lx, ly, lz, fx, fy, fz, depth, isEntrance, nearSpawn))
                    {
                        mask.Cells[x, y - Bottom, z] = true;

                        //pod morzem tunele sa zalane, a w podziemnych jeziorach woda siega do poziomu jeziora
                        mask.Water[x, y - Bottom, z] = (inPocket && y < pocketWater) || (submerged && y < WaterGenerator.SeaLevel);

                        continue;
                    }

                    if (depth >= GravelMinDepth && y >= GravelMinY && Trilinear(g, lx, ly, lz, fx, fy, fz) > GravelThreshold)
                        mask.Gravel[x, y - Bottom, z] = true;
                }
            }
        }

        return mask;
    }

    //podziemne jezioro: soczewkowata komora o plaskim poziomie wody (zawsze otoczona skala, wiec woda nie ma gdzie uciec)
    //ksztalt i poziom zalezy od szumu 2D, poziom jest skwantowany co PocketStep, by sasiednie kolumny mialy te sama wode
    private static void Pocket(int worldX, int worldZ, int top, bool nearSpawn, out int low, out int high, out int water)
    {
        low = 0;
        high = 0;
        water = 0;

        if (nearSpawn)
            return;

        float n = pocket.Generate(new Vector3(worldX * PocketFrequency + offsetPocket.x, 0f, worldZ * PocketFrequency + offsetPocket.y));
        float p = Mathf.Clamp01(Mathf.InverseLerp(PocketMin, PocketMax, n));

        if (p <= 0f)
            return;

        p = p * p * (3f - 2f * p);

        float level = pocketLevel.Generate(new Vector3(worldX * PocketLevelFrequency + offsetPocketLevel.x, 0f, worldZ * PocketLevelFrequency + offsetPocketLevel.y));
        int steps = (PocketTop - PocketBottom) / PocketStep + 1;
        int baseY = PocketBottom + PocketStep * Mathf.Clamp(Mathf.FloorToInt((level + 1f) * 0.5f * steps), 0, steps - 1);

        int lowY = baseY - Mathf.RoundToInt(PocketDepth * p);
        int highY = baseY + Mathf.RoundToInt(PocketHeight * p);

        if (lowY < Bottom || highY + PocketCover > top)
            return;

        low = lowY;
        high = highY;
        water = baseY;
    }

    private static bool IsTunnel(float[,,] a, float[,,] b, int lx, int ly, int lz, float fx, float fy, float fz, int depth, bool isEntrance, bool nearSpawn)
    {
        float width = Width;

        if (!isEntrance)
        {
            if (depth < FadeStart)
                return false;

            if (depth < FadeEnd)
                width *= (depth - FadeStart) / (float)(FadeEnd - FadeStart);
        }

        if (nearSpawn && depth < SpawnDepth)
            return false;

        if (Mathf.Abs(Trilinear(a, lx, ly, lz, fx, fy, fz)) >= width)
            return false;

        return Mathf.Abs(Trilinear(b, lx, ly, lz, fx, fy, fz)) < width;
    }

    private static float Trilinear(float[,,] v, int lx, int ly, int lz, float fx, float fy, float fz)
    {
        float c00 = Mathf.Lerp(v[lx, ly, lz], v[lx + 1, ly, lz], fx);
        float c10 = Mathf.Lerp(v[lx, ly + 1, lz], v[lx + 1, ly + 1, lz], fx);
        float c01 = Mathf.Lerp(v[lx, ly, lz + 1], v[lx + 1, ly, lz + 1], fx);
        float c11 = Mathf.Lerp(v[lx, ly + 1, lz + 1], v[lx + 1, ly + 1, lz + 1], fx);

        return Mathf.Lerp(Mathf.Lerp(c00, c10, fy), Mathf.Lerp(c01, c11, fy), fz);
    }

    private static void Prepare(int worldSeed)
    {
        if (isReady && noiseSeed == worldSeed)
            return;

        tunnelA = new SimplexNoise(worldSeed + 11);
        tunnelB = new SimplexNoise(worldSeed + 12);
        entrance = new SimplexNoise(worldSeed + 13);
        gravel = new SimplexNoise(worldSeed + 14);
        pocket = new SimplexNoise(worldSeed + 15);
        pocketLevel = new SimplexNoise(worldSeed + 16);

        offsetA = new Vector3(Offset(worldSeed, 1), Offset(worldSeed, 2), Offset(worldSeed, 3));
        offsetB = new Vector3(Offset(worldSeed, 4), Offset(worldSeed, 5), Offset(worldSeed, 6));
        offsetEntrance = new Vector2(Offset(worldSeed, 7), Offset(worldSeed, 8));
        offsetGravel = new Vector3(Offset(worldSeed, 9), Offset(worldSeed, 10), Offset(worldSeed, 11));
        offsetPocket = new Vector2(Offset(worldSeed, 12), Offset(worldSeed, 13));
        offsetPocketLevel = new Vector2(Offset(worldSeed, 14), Offset(worldSeed, 15));

        noiseSeed = worldSeed;
        isReady = true;
    }

    private static float Offset(int worldSeed, int salt)
    {
        return (MathOperations.ChunkSeed(worldSeed, new Vector2Int(salt, salt * 31 + 7)) & 0xFFFF) / 65536f * 20000f;
    }
}
