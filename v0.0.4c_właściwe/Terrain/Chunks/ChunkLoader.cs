using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    private const int MaxFluidSeeds = 512;

    [SerializeField] MapGenerator mapGenerator;
    [SerializeField] ChunkManager chunkManager;

    public void Load(Vector2Int pos)
    {
        ChunkCollector chunks = chunkManager.Chunks();
        Chunk chunk = chunks.Chunk(pos);

        int height = mapGenerator.MapSize().y;

        string[,,] blocks = new string[16, height, 16];

        for(int x=0;x<16;++x)
        {
            for(int z=0;z<16;++z)
            {
                TerrainData column = chunk.Generator.TerrainLayers[x, z];

                int dirtTop = column.GetLayer(TerrainData.Dirt).LayerBorders[1];
                int grassTop = column.GetLayer(TerrainData.Grass).LayerBorders[1];
                string dirt = column.GetLayer(TerrainData.Dirt).BlockId;
                string grass = column.GetLayer(TerrainData.Grass).BlockId;

                //nad trawa nic sie nie zapisuje (tablica jest pusta), wiec warstwy koncza sie najwyzej na jej wysokosci, a nie na wysokosci swiata
                int limit = Mathf.Min(height, grassTop);
                int bottom = 0;

                //warstwy od skaly macierzystej do kamienia leza jedna na drugiej; kazda konczy sie na swoim topie
                for (int i = TerrainData.Bedrock; i <= TerrainData.Stone; ++i)
                {
                    TerrainLayer layer = column.GetLayer(i);
                    int top = Mathf.Min(layer.LayerBorders[1], limit);

                    for (int y = bottom; y < top; ++y)
                        blocks[x, y, z] = i == TerrainData.Stone ? StoneBlock(column, y) : layer.BlockId;

                    bottom = Mathf.Max(bottom, top);
                }

                for (int y = bottom; y < Mathf.Min(dirtTop, limit); ++y)
                    blocks[x, y, z] = Soak(dirt, y);

                for (int y = Mathf.Max(bottom, dirtTop); y < limit; ++y)
                    blocks[x, y, z] = Soak(grass, y);

                //morza, jeziora i rzeki: komorki nad dnem do poziomu morza to woda (zrodla)
                for (int y = grassTop; y < WaterGenerator.SeaLevel && y < height; ++y)
                    blocks[x, y, z] = Fluid.Source;
            }
        }

        CaveMask caves = chunk.Generator.Caves;
        List<Vector3Int> fluidSeeds = new List<Vector3Int>();

        if (caves != null)
        {
            int caveTop = Mathf.Min(height, caves.Bottom + caves.Cells.GetLength(1));

            for (int x = 0; x < 16; ++x)
                for (int z = 0; z < 16; ++z)
                    for (int y = caves.Bottom; y < caveTop; ++y)
                    {
                        if (caves.Cells[x, y - caves.Bottom, z])
                            blocks[x, y, z] = caves.Water[x, y - caves.Bottom, z] ? Fluid.Source : null;
                        else if (caves.Gravel[x, y - caves.Bottom, z] && blocks[x, y, z] != null && blocks[x, y, z] != "00")
                            blocks[x, y, z] = CaveGenerator.GravelId;
                    }
        }

        if (caves != null)
            CollectFluidSeeds(pos, blocks, caves, fluidSeeds);

        //drzewa po jaskiniach (rosna tylko na nietknietej trawie) i przed zmianami gracza (te maja pierwszenstwo)
        TreeGenerator.Place(pos, blocks, mapGenerator.Seed());

        if (chunks.Edits.ContainsKey(pos))
            foreach (var edit in chunks.Edits[pos])
                blocks[edit.Key.x - pos.x * 16, edit.Key.y, edit.Key.z - pos.y * 16] = edit.Value;

        chunks.ChunkLoad(pos, blocks, fluidSeeds);
    }

    //kamien, a w pasmach nieba (Sky, Cloud, HighSky) ich kamienie
    private static string StoneBlock(TerrainData column, int y)
    {
        if (y >= column.GetLayer(TerrainData.Sky).LayerBorders[0])
        {
            for (int i = TerrainData.Sky; i <= TerrainData.HighSky; ++i)
            {
                TerrainLayer band = column.GetLayer(i);

                if (y >= band.LayerBorders[0] && y < band.LayerBorders[1])
                    return band.BlockId;
            }
        }

        return column.GetLayer(TerrainData.Stone).BlockId;
    }

    //piasek ponizej poziomu morza jest mokry (poziom wody gruntowej), powyzej suchy
    private static string Soak(string id, int y)
    {
        return id == BlockKind.DrySand && y < WaterGenerator.SeaLevel ? BlockKind.WetSand : id;
    }

    //zalane jaskinie i jeziora: woda stykajaca sie z powietrzem (wylot tunelu) ma od razu zaczac plynac
    private void CollectFluidSeeds(Vector2Int pos, string[,,] blocks, CaveMask caves, List<Vector3Int> seeds)
    {
        int caveTop = Mathf.Min(blocks.GetLength(1), caves.Bottom + caves.Cells.GetLength(1));

        for (int x = 0; x < 16; ++x)
            for (int z = 0; z < 16; ++z)
                for (int y = caves.Bottom; y < caveTop; ++y)
                {
                    if (!caves.Water[x, y - caves.Bottom, z] || seeds.Count >= MaxFluidSeeds)
                        continue;

                    if (IsOpen(blocks, x + 1, y, z) || IsOpen(blocks, x - 1, y, z) || IsOpen(blocks, x, y - 1, z) || IsOpen(blocks, x, y, z + 1) || IsOpen(blocks, x, y, z - 1))
                        seeds.Add(new Vector3Int(pos.x * 16 + x, y, pos.y * 16 + z));
                }
    }

    private static bool IsOpen(string[,,] blocks, int x, int y, int z)
    {
        return x >= 0 && x < 16 && z >= 0 && z < 16 && y >= 0 && blocks[x, y, z] == null;
    }
}
