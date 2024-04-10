using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BKG.Math;

public class ChunkPregenerator : MonoBehaviour
{
    public Dictionary<Vector2Int, ChunkFrame> chunkFrames = new Dictionary<Vector2Int, ChunkFrame>();
    private ChunkManager manager = new ChunkManager();
    private MathOperations maths = new MathOperations();

    public void PregenerateFrame(int seed, Vector2Int vector)
    {
        int chunkSeed = manager.chunkSeed(vector, seed);
    }

    //pregeneracja ramki chunku startowego
    ChunkFrame PregeneratePrimaryChunk(int seed)
    {
        ChunkFrame result = new ChunkFrame();
        ChunkCorner[] corners = new ChunkCorner[4];
        ChunkEdge[] edges = new ChunkEdge[4];

        //"wie¿yczka" na pozycji lokalnej chunku (0, 0)
        corners[0].LayerData[0] = maths.PrimeNumberModularSeed(4, seed) % 4;
        corners[0].LayerData[1] = maths.CreatingSeed(seed + 1, 32, 16, 24);
        corners[0].LayerData[2] = maths.CreatingSeed(seed + 2, 56, 40, 48);
        corners[0].LayerData[3] = maths.CreatingSeed(seed + 3, 80, 64, 72);
        corners[0].LayerData[4] = maths.CreatingSeed(seed + 4, 140, 88, 96);

        //krawêdŸ na lokalnym x==0
        ChunkEdge edge = new ChunkEdge();

        edge.LayerData[0][0] = maths.PrimeNumberModularSeed(4, seed + 17) % 4;
        edge.LayerData[0][1] = maths.CalculatingOffset(seed + 17 + 1, -2, 16, 2, 32, 24, corners[0].LayerData[1]);
        edge.LayerData[0][2] = maths.CalculatingOffset(seed + 17 + 2, -2, 40, 2, 56, 48, corners[0].LayerData[2]);
        edge.LayerData[0][3] = maths.CalculatingOffset(seed + 17 + 3, -2, 64, 2, 80, 72, corners[0].LayerData[3]);
        edge.LayerData[0][4] = maths.CalculatingOffset(seed + 17 + 4, -4, 88, 4, 140, 96, corners[0].LayerData[4]);

        for(int i=1; i<14; ++i)
        {
            edge.LayerData[i][0] = maths.PrimeNumberModularSeed(4, seed + 17 * (i + 1)) % 4;
            edge.LayerData[i][1] = maths.CalculatingOffset(seed + 17 * (i + 1) + 1, -2, 16, 2, 32, 24, edge.LayerData[i - 1][1]);
            edge.LayerData[i][2] = maths.CalculatingOffset(seed + 17 * (i + 1) + 2, -2, 40, 2, 56, 48, edge.LayerData[i - 1][2]);
            edge.LayerData[i][3] = maths.CalculatingOffset(seed + 17 * (i + 1) + 3, -2, 64, 2, 80, 72, edge.LayerData[i - 1][3]);
            edge.LayerData[i][4] = maths.CalculatingOffset(seed + 17 * (i + 1) + 4, -4, 88, 4, 140, 96, edge.LayerData[i - 1][4]);
        }

        edges[0] = edge;

        //"wie¿yczka" na pozycji lokalnej chunku (0, 15)
        corners[1].LayerData[0] = maths.PrimeNumberModularSeed(4, seed + 17 * 15) % 4;
        corners[1].LayerData[1] = maths.CalculatingOffset(seed + 17 * 15 + 1, -2, 16, 2, 32, 24, edges[0].LayerData[13][1]);
        corners[1].LayerData[2] = maths.CalculatingOffset(seed + 17 * 15 + 2, -2, 40, 2, 56, 48, edges[0].LayerData[13][2]);
        corners[1].LayerData[3] = maths.CalculatingOffset(seed + 17 * 15 + 3, -2, 64, 2, 80, 72, edges[0].LayerData[13][3]);
        corners[1].LayerData[4] = maths.CalculatingOffset(seed + 17 * 15 + 4, -4, 88, 4, 140, 96, edges[0].LayerData[13][4]);

        //krawêdŸ na lokalnym z==0
        edge = new ChunkEdge();

        edge.LayerData[0][0] = maths.PrimeNumberModularSeed(4, seed + 257) % 4;
        edge.LayerData[0][1] = maths.CalculatingOffset(seed + 257 + 1, -2, 16, 2, 32, 24, corners[0].LayerData[1]);
        edge.LayerData[0][2] = maths.CalculatingOffset(seed + 257 + 2, -2, 40, 2, 56, 48, corners[0].LayerData[2]);
        edge.LayerData[0][3] = maths.CalculatingOffset(seed + 257 + 3, -2, 64, 2, 80, 72, corners[0].LayerData[3]);
        edge.LayerData[0][4] = maths.CalculatingOffset(seed + 257 + 4, -4, 88, 4, 140, 96, corners[0].LayerData[4]);

        for (int i = 1; i < 14; ++i)
        {
            edge.LayerData[i][0] = maths.PrimeNumberModularSeed(4, seed + 17 * (i + 1)) % 4;
            edge.LayerData[i][1] = maths.CalculatingOffset(seed + 257 * (i + 1) + 1, -2, 16, 2, 32, 24, edge.LayerData[i - 1][1]);
            edge.LayerData[i][2] = maths.CalculatingOffset(seed + 257 * (i + 1) + 2, -2, 40, 2, 56, 48, edge.LayerData[i - 1][2]);
            edge.LayerData[i][3] = maths.CalculatingOffset(seed + 257 * (i + 1) + 3, -2, 64, 2, 80, 72, edge.LayerData[i - 1][3]);
            edge.LayerData[i][4] = maths.CalculatingOffset(seed + 257 * (i + 1) + 4, -4, 88, 4, 140, 96, edge.LayerData[i - 1][4]);
        }

        edges[1] = edge;

        //"wie¿yczka" na pozycji lokalnej chunku (15, 0)
        corners[2].LayerData[0] = maths.PrimeNumberModularSeed(4, seed + 257 * 15) % 4;
        corners[2].LayerData[1] = maths.CalculatingOffset(seed + 257 * 15 + 1, -2, 16, 2, 32, 24, edges[1].LayerData[13][1]);
        corners[2].LayerData[2] = maths.CalculatingOffset(seed + 257 * 15 + 2, -2, 40, 2, 56, 48, edges[1].LayerData[13][2]);
        corners[2].LayerData[3] = maths.CalculatingOffset(seed + 257 * 15 + 3, -2, 64, 2, 80, 72, edges[1].LayerData[13][3]);
        corners[2].LayerData[4] = maths.CalculatingOffset(seed + 257 * 15 + 4, -4, 88, 4, 140, 96, edges[1].LayerData[13][4]);

        //krawêdŸ na lokalnym x==15
        edge = new ChunkEdge();

        edge.LayerData[0][0] = maths.PrimeNumberModularSeed(4, seed + 17) % 4;
        edge.LayerData[0][1] = maths.CalculatingOffset(seed + 17 + 257 * 15 + 1, -2, 16, 2, 32, 24, corners[2].LayerData[1]);
        edge.LayerData[0][2] = maths.CalculatingOffset(seed + 17 + 257 * 15 + 2, -2, 40, 2, 56, 48, corners[2].LayerData[2]);
        edge.LayerData[0][3] = maths.CalculatingOffset(seed + 17 + 257 * 15 + 3, -2, 64, 2, 80, 72, corners[2].LayerData[3]);
        edge.LayerData[0][4] = maths.CalculatingOffset(seed + 17 + 257 * 15 + 4, -4, 88, 4, 140, 96, corners[2].LayerData[4]);

        for (int i = 1; i < 14; ++i)
        {
            edge.LayerData[i][0] = maths.PrimeNumberModularSeed(4, seed + 17 * (i + 1) + 257 * 15) % 4;
            edge.LayerData[i][1] = maths.CalculatingOffset(seed + 17 * (i + 1) + 257 * 15 + 1, -2, 16, 2, 32, 24, edge.LayerData[i - 1][1]);
            edge.LayerData[i][2] = maths.CalculatingOffset(seed + 17 * (i + 1) + 257 * 15 + 2, -2, 40, 2, 56, 48, edge.LayerData[i - 1][2]);
            edge.LayerData[i][3] = maths.CalculatingOffset(seed + 17 * (i + 1) + 257 * 15 + 3, -2, 64, 2, 80, 72, edge.LayerData[i - 1][3]);
            edge.LayerData[i][4] = maths.CalculatingOffset(seed + 17 * (i + 1) + 257 * 15 + 4, -4, 88, 4, 140, 96, edge.LayerData[i - 1][4]);
        }

        edges[2] = edge;

        //krawêdŸ na lokalnym z==15
        edge = new ChunkEdge();

        edge.LayerData[0][0] = maths.PrimeNumberModularSeed(4, seed + 257 + 17 * 15) % 4;
        edge.LayerData[0][1] = maths.CalculatingOffset(seed + 257 + 17 * 15 + 1, -2, 16, 2, 32, 24, corners[1].LayerData[1]);
        edge.LayerData[0][2] = maths.CalculatingOffset(seed + 257 + 17 * 15 + 2, -2, 40, 2, 56, 48, corners[1].LayerData[2]);
        edge.LayerData[0][3] = maths.CalculatingOffset(seed + 257 + 17 * 15 + 3, -2, 64, 2, 80, 72, corners[1].LayerData[3]);
        edge.LayerData[0][4] = maths.CalculatingOffset(seed + 257 + 17 * 15 + 4, -4, 88, 4, 140, 96, corners[1].LayerData[4]);

        for (int i = 1; i < 14; ++i)
        {
            edge.LayerData[i][0] = maths.PrimeNumberModularSeed(4, seed + 17 * (i + 1) + 17 * 15) % 4;
            edge.LayerData[i][1] = maths.CalculatingOffset(seed + 257 * (i + 1) + 17 * 15 + 1, -2, 16, 2, 32, 24, edge.LayerData[i - 1][1]);
            edge.LayerData[i][2] = maths.CalculatingOffset(seed + 257 * (i + 1) + 17 * 15 + 2, -2, 40, 2, 56, 48, edge.LayerData[i - 1][2]);
            edge.LayerData[i][3] = maths.CalculatingOffset(seed + 257 * (i + 1) + 17 * 15 + 3, -2, 64, 2, 80, 72, edge.LayerData[i - 1][3]);
            edge.LayerData[i][4] = maths.CalculatingOffset(seed + 257 * (i + 1) + 17 * 15 + 4, -4, 88, 4, 140, 96, edge.LayerData[i - 1][4]);
        }

        edges[3] = edge;

        //"wie¿yczka" na pozycji lokalnej chunku (15, 15)

        return result;
    }
}
