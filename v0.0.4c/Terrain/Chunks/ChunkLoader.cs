using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    [SerializeField] MapGenerator mapGenerator;
    [SerializeField] ChunkManager chunkManager;

    public void Load(Vector2Int pos)
    {
        ChunkCollector chunks = chunkManager.Chunks();
        Chunk chunk = chunks.Chunk(pos);

        string[,,] blocks = new string[16, mapGenerator.MapSize().y, 16];

        for(int y=0;y<mapGenerator.MapSize().y;++y)
        {
            for(int x=0;x<16;++x)
            {
                for(int z=0;z<16;++z)
                {
                    if (y < chunk.Generator.TerrainLayers[x, z].GetLayer(0).LayerBorders[1])
                        blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(0).BlockId;

                    else if (y < chunk.Generator.TerrainLayers[x, z].GetLayer(1).LayerBorders[1])
                        blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(1).BlockId;

                    else if (y < chunk.Generator.TerrainLayers[x, z].GetLayer(2).LayerBorders[1])
                        blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(2).BlockId;

                    else if (y < chunk.Generator.TerrainLayers[x, z].GetLayer(3).LayerBorders[1])
                        blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(3).BlockId;

                    else if (y < chunk.Generator.TerrainLayers[x, z].GetLayer(4).LayerBorders[1])
                    {
                        if (y >= 256)
                            blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(5).BlockId;
                        else
                            blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(4).BlockId;
                    }

                    else if (y < chunk.Generator.TerrainLayers[x, z].GetLayer(6).LayerBorders[1])
                        blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(6).BlockId;

                    else if (y < chunk.Generator.TerrainLayers[x, z].GetLayer(7).LayerBorders[1])
                        blocks[x, y, z] = chunk.Generator.TerrainLayers[x, z].GetLayer(7).BlockId;
                }
            }
        }

        chunks.ChunkLoad(pos, blocks);
    }
}