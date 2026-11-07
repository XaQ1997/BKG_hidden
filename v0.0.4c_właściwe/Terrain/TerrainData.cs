using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainData
{
    //indeksy warstw kolumny; Bedrock..Stone ukladaja sie jedna na drugiej (kazda konczy sie na swoim topie), Sky..HighSky to pasma wysokosci
    //wewnatrz kamienia (LayerBorders to dol i gora pasma), a Dirt i Grass leza na kamieniu
    public const int Bedrock = 0;
    public const int HellLow = 1;
    public const int Void = 2;
    public const int HellHigh = 3;
    public const int Dark = 4;
    public const int Deep = 5;
    public const int Stone = 6;
    public const int Sky = 7;
    public const int Cloud = 8;
    public const int HighSky = 9;
    public const int Dirt = 10;
    public const int Grass = 11;
    public const int LayerCount = 12;

    private TerrainLayer[] layers = new TerrainLayer[LayerCount];

    public static TerrainData Default()
    {
        TerrainData data = new TerrainData();

        data.layers[0] = new TerrainLayer(0, 1, 4, 1, "00");
        data.layers[1] = new TerrainLayer(1, 2, 48, 32, "20v3");
        data.layers[2] = new TerrainLayer(2, 32, 64, 64, "20v6");
        data.layers[3] = new TerrainLayer(3, 48, 96, 80, "20v3");
        data.layers[4] = new TerrainLayer(4, 80, 192, 136, "20v2");
        data.layers[5] = new TerrainLayer(5, 128, 256, 192, "20v1");
        data.layers[6] = new TerrainLayer(6, 192, 512, 384, "20");
        data.layers[7] = new TerrainLayer(7, 512, 768, 512, "20v4");
        data.layers[8] = new TerrainLayer(8, 768, 800, 768, "20v5");
        data.layers[9] = new TerrainLayer(9, 800, 960, 800, "20v4");
        data.layers[10] = new TerrainLayer(10, 195, 963, 387, "31");
        data.layers[11] = new TerrainLayer(11, 196, 964, 388, "30");

        return data;
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
