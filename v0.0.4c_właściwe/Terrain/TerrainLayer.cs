using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainLayer
{
    public int LayerId;
    public int[] LayerBorders;
    public int DefaultLevel;
    public string BlockId;

    public TerrainLayer(int id, int bottom, int top, int defaultLevel, string block)
    {
        LayerId = id;
        LayerBorders = new int[2] { bottom, top };
        DefaultLevel = defaultLevel;
        BlockId = block;
    }
}
