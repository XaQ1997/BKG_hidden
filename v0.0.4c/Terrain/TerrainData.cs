using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainData
{
    private TerrainLayer[] layers = new TerrainLayer[8];

    public TerrainData()
    {
        layers[0] = new TerrainLayer(0, 0, 3, 0, "00");
        layers[1] = new TerrainLayer(1, 1, 31, 23, "20v3");
        layers[2] = new TerrainLayer(2, 16, 55, 47, "20v2");
        layers[3] = new TerrainLayer(3, 40, 79, 71, "20v1");
        layers[4] = new TerrainLayer(4, 64, 139, 95, "20");
        layers[5] = new TerrainLayer(5, 256, 511, 256, "20v4");
        layers[6] = new TerrainLayer(6, 81, 142, 98, "31");
        layers[7] = new TerrainLayer(7, 84, 143, 99, "30");
    }

    public void SetLayer(int id, TerrainLayer layer)
    {
        layers[id] = layer;
    }

    public TerrainLayer GetLayer(int id)
    {
        return layers[id];
    }
}