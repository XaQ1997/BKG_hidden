using BKG.Math;
using UnityEngine;

//drzewa: na siatce komorek CellSize x CellSize kazda komorka moze miec jedno drzewo w losowym miejscu (losowanie z ziarna i wspolrzednych komorki)
//szansa zalezy od szumu lasu, wiec sa laki i gestsze lasy; drzewo rosnie tylko na trawie nad woda i pod granica lasu
//korona siega CanopyRadius od pnia, wiec kazdy chunk liczy tez drzewa z sasiednich chunkow i wpisuje tylko swoje kolumny (chunki zgadzaja sie na granicach)
public static class TreeGenerator
{
    private const int CellSize = 5;
    private const int CanopyRadius = 2;
    private const int MinHeight = 4;
    private const int HeightRange = 3;
    private const float BaseChance = 0.08f;
    private const float ForestChance = 0.75f;
    private const float ForestFrequency = 0.004f;
    private const float ForestMin = 0f;
    private const float ForestMax = 0.5f;

    //bez drzew w poblizu startu i powyzej tej wysokosci (wyzej jest niebianski kamien i skaly)
    private const int SpawnClear = 6;
    private const int TreeLine = 470;

    private const int RollSalt = 0x1f3d5b79;
    private const int XSalt = 0x2c9277b5;
    private const int ZSalt = 0x5851f42d;
    private const int HeightSalt = 0x4c957f2d;

    private static SimplexNoise forest;
    private static Vector2 offsetForest;
    private static int noiseSeed;
    private static bool isReady = false;

    //wpisuje do danych chunka (komorki, ktore sa jeszcze puste) pnie i korony drzew
    public static void Place(Vector2Int chunkPos, string[,,] blocks, int seed)
    {
        Prepare(seed);

        int minX = chunkPos.x * 16;
        int minZ = chunkPos.y * 16;
        int cellX0 = FloorDiv(minX - CanopyRadius, CellSize);
        int cellX1 = FloorDiv(minX + 15 + CanopyRadius, CellSize);
        int cellZ0 = FloorDiv(minZ - CanopyRadius, CellSize);
        int cellZ1 = FloorDiv(minZ + 15 + CanopyRadius, CellSize);

        for (int cellZ = cellZ0; cellZ <= cellZ1; ++cellZ)
        {
            for (int cellX = cellX0; cellX <= cellX1; ++cellX)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);
                int treeX = cellX * CellSize + MathOperations.PositiveModulo(MathOperations.ChunkSeed(seed ^ XSalt, cell), CellSize);
                int treeZ = cellZ * CellSize + MathOperations.PositiveModulo(MathOperations.ChunkSeed(seed ^ ZSalt, cell), CellSize);

                //drzewo wplywa na ten chunk tylko, gdy jego korona go dotyka
                if (treeX < minX - CanopyRadius || treeX > minX + 15 + CanopyRadius || treeZ < minZ - CanopyRadius || treeZ > minZ + 15 + CanopyRadius)
                    continue;

                float roll = MathOperations.PositiveModulo(MathOperations.ChunkSeed(seed ^ RollSalt, cell), 1000) / 1000f;

                if (roll >= Chance(treeX, treeZ) || Mathf.Abs(treeX) <= SpawnClear && Mathf.Abs(treeZ) <= SpawnClear)
                    continue;

                if (!ChunkGenerator.IsGrassColumn(treeX, treeZ, seed, out int surface) || surface <= WaterGenerator.SeaLevel || surface > TreeLine)
                    continue;

                int height = MinHeight + MathOperations.PositiveModulo(MathOperations.ChunkSeed(seed ^ HeightSalt, cell), HeightRange);

                Grow(blocks, minX, minZ, treeX, treeZ, surface, height);
            }
        }
    }

    private static void Grow(string[,,] blocks, int minX, int minZ, int treeX, int treeZ, int surface, int height)
    {
        int worldHeight = blocks.GetLength(1);
        int top = surface + height - 1;

        //pien tylko na nietknietej trawie (jaskinia mogla ja wyciac)
        if (treeX >= minX && treeX < minX + 16 && treeZ >= minZ && treeZ < minZ + 16 && blocks[treeX - minX, surface - 1, treeZ - minZ] != "30")
            return;

        for (int y = surface; y <= top + 1 && y < worldHeight; ++y)
        {
            for (int dx = -CanopyRadius; dx <= CanopyRadius; ++dx)
            {
                for (int dz = -CanopyRadius; dz <= CanopyRadius; ++dz)
                {
                    int lx = treeX + dx - minX;
                    int lz = treeZ + dz - minZ;

                    if (lx < 0 || lx > 15 || lz < 0 || lz > 15 || blocks[lx, y, lz] != null)
                        continue;

                    if (dx == 0 && dz == 0 && y <= top)
                        blocks[lx, y, lz] = BlockKind.Wood;
                    else if (IsCrown(dx, dz, y - top))
                        blocks[lx, y, lz] = BlockKind.Leaf;
                }
            }
        }
    }

    //korona: dwie dolne warstwy 5x5 bez naroznikow, potem 3x3 i krzyz na czubku; dy to wysokosc wzgledem czubka pnia
    private static bool IsCrown(int dx, int dz, int dy)
    {
        int ax = Mathf.Abs(dx);
        int az = Mathf.Abs(dz);

        if (dy == -2 || dy == -1)
            return !(ax == 2 && az == 2);

        if (dy == 0)
            return ax <= 1 && az <= 1;

        if (dy == 1)
            return ax + az <= 1;

        return false;
    }

    private static float Chance(int worldX, int worldZ)
    {
        float value = forest.Generate(new Vector3(worldX * ForestFrequency + offsetForest.x, 0f, worldZ * ForestFrequency + offsetForest.y));
        float t = Mathf.Clamp01(Mathf.InverseLerp(ForestMin, ForestMax, value));

        return BaseChance + ForestChance * t * t * (3f - 2f * t);
    }

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor;

        return value % divisor != 0 && value < 0 ? result - 1 : result;
    }

    private static void Prepare(int worldSeed)
    {
        if (isReady && noiseSeed == worldSeed)
            return;

        forest = new SimplexNoise(worldSeed + 61);
        offsetForest = new Vector2(Offset(worldSeed, 61), Offset(worldSeed, 62));

        noiseSeed = worldSeed;
        isReady = true;
    }

    private static float Offset(int worldSeed, int salt)
    {
        return (MathOperations.ChunkSeed(worldSeed, new Vector2Int(salt, salt * 31 + 7)) & 0xFFFF) / 65536f * 20000f;
    }
}
