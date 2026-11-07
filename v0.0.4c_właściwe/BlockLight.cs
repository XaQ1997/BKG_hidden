using UnityEngine;

//swiatlo sloneczne blokow: zalezy od glebokosci pod powierzchnia kolumny, a do siatki trafia jako kolor wierzcholkow (ChunkMesher)
public static class BlockLight
{
    //najciemniejsza jasnosc powierzchni
    public const float MinLight = 0.1f;

    //glebokosc pod powierzchnia, od ktorej swiatlo zaczyna slabnac, i na ktorej zanika
    public const int BrightDepth = 3;
    public const int DarkDepth = 40;

    //swiatlo komorki powietrza pod powierzchnia: pelne tuz pod nia, potem coraz ciemniej do DarkDepth blokow w dol
    public static float CaveLight(int depth)
    {
        float t = Mathf.Clamp01((depth - BrightDepth) / (float)(DarkDepth - BrightDepth));

        return 1f - t * t * (3f - 2f * t);
    }
}
