using System.Collections.Generic;
using UnityEngine;

//rysowanie terenu: jedna siatka na pod-chunk 16x16x16 (klucz: chunk x, numer pod-chunka, chunk z) zamiast obiektu na blok
//pod-chunk jest budowany ponownie, gdy zmieni sie komorka w nim lub obok niego (sciany, AO, swiatlo); budowanie idzie od najblizszego graczowi w limicie czasu
public class ChunkRenderer
{
    private const int Edge = ChunkMesher.Edge;
    private const int Pad = ChunkMesher.Pad;

    //pod-chunki o srodku blizej niz tyle od gracza buduja sie zawsze, bez limitu czasu (nie moze zabraknac terenu pod nogami)
    private const float UrgentRadius = 16f;

    private readonly MapGenerator map;
    private readonly Transform root;
    private readonly BlockTemplates templates;
    private readonly ChunkMesher mesher = new ChunkMesher();
    private readonly int height;
    private readonly int offsetY;
    private readonly Dictionary<Vector3Int, SubChunkView> views = new Dictionary<Vector3Int, SubChunkView>();
    private readonly HashSet<Vector3Int> pending = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> immediate = new HashSet<Vector3Int>();
    private readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

    //height: wysokosc swiata w blokach, offsetY: wysokosc swiata komorki o y = 0
    public ChunkRenderer(MapGenerator generator, BlockMap blockMap, int worldHeight, int worldOffsetY)
    {
        map = generator;
        height = worldHeight;
        offsetY = worldOffsetY;
        templates = new BlockTemplates(blockMap);

        root = new GameObject("Chunks").transform;
        root.SetParent(generator.transform, false);
    }

    //liczba obiektow siatek pod-chunkow i oczekujacych na zbudowanie (do okna debugowania)
    public int ViewCount
    {
        get { return views.Count; }
    }

    public int PendingCount
    {
        get { return pending.Count; }
    }

    private int SubChunkCount()
    {
        return (height + Edge - 1) / Edge;
    }

    public void Clear()
    {
        foreach (SubChunkView view in views.Values)
            Object.Destroy(view.gameObject);

        views.Clear();
        pending.Clear();
        immediate.Clear();
    }

    //nowy chunk: wszystkie jego pod-chunki az do najwyzszego bloku czekaja na zbudowanie
    public void AddChunk(Vector2Int pos, int highest)
    {
        int count = Mathf.Min(SubChunkCount(), (highest + Edge - 1) / Edge);

        for (int sub = 0; sub < count; ++sub)
            pending.Add(new Vector3Int(pos.x, sub, pos.y));
    }

    public void RemoveChunk(Vector2Int pos)
    {
        for (int sub = 0; sub < SubChunkCount(); ++sub)
        {
            Vector3Int key = new Vector3Int(pos.x, sub, pos.y);

            pending.Remove(key);
            immediate.Remove(key);

            RemoveView(key);
        }
    }

    //zmiana komorki: przebudowa pod-chunka z komorka i tych, ktore ja otaczaja (widocznosc scian, AO i swiatlo siegaja o 1 komorke)
    public void MarkCell(int x, int y, int z, bool urgent)
    {
        for (int dx = -1; dx <= 1; ++dx)
            for (int dy = -1; dy <= 1; ++dy)
                for (int dz = -1; dz <= 1; ++dz)
                    Mark(x + dx, y + dy, z + dz, urgent);
    }

    //zmiana wysokosci powierzchni kolumny zmienia swiatlo w niej i w sasiednich kolumnach od y0 do y1
    public void MarkColumns(int x, int z, int y0, int y1, bool urgent)
    {
        y0 = Mathf.Max(0, y0);
        y1 = Mathf.Min(height - 1, y1);

        for (int dx = -1; dx <= 1; ++dx)
            for (int dz = -1; dz <= 1; ++dz)
                for (int sub = y0 / Edge; sub <= y1 / Edge; ++sub)
                    Mark(x + dx, sub * Edge, z + dz, urgent);
    }

    private void Mark(int x, int y, int z, bool urgent)
    {
        if (y < 0 || y >= height || !map.IsChunkLoaded(new Vector2Int(x >> 4, z >> 4)))
            return;

        Vector3Int key = new Vector3Int(x >> 4, y / Edge, z >> 4);

        pending.Add(key);

        if (urgent)
            immediate.Add(key);
    }

    //buduje od razu pod-chunki oznaczone jako pilne (zmiana bloku przez gracza)
    public void Flush()
    {
        foreach (Vector3Int key in immediate)
            if (pending.Remove(key))
                Rebuild(key);

        immediate.Clear();
    }

    //buduje oczekujace pod-chunki od najblizszego graczowi, dopoki starcza budzetu czasu (co najmniej jeden na wywolanie)
    public void Process(float budgetMs, Vector3 playerPosition)
    {
        if (pending.Count == 0)
            return;

        Vector3 player = playerPosition - new Vector3(0f, offsetY, 0f);
        int built = 0;

        watch.Restart();

        while (pending.Count > 0)
        {
            Vector3Int key = Nearest(player, out float distance);
            bool urgent = distance <= UrgentRadius * UrgentRadius;

            if (built > 0 && !urgent && watch.Elapsed.TotalMilliseconds >= budgetMs)
                return;

            pending.Remove(key);

            Rebuild(key);

            ++built;
        }
    }

    private Vector3Int Nearest(Vector3 player, out float distance)
    {
        Vector3Int nearest = default(Vector3Int);
        float nearestDistance = float.MaxValue;

        foreach (Vector3Int key in pending)
        {
            float dx = key.x * Edge + 7.5f - player.x;
            float dy = key.y * Edge + 7.5f - player.y;
            float dz = key.z * Edge + 7.5f - player.z;
            float d = dx * dx + dy * dy + dz * dz;

            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearest = key;
            }
        }

        distance = nearestDistance;

        return nearest;
    }

    private void Rebuild(Vector3Int key)
    {
        string[,,] blocks = map.GetChunkBlocks(new Vector2Int(key.x, key.z));

        if (blocks == null)
        {
            RemoveView(key);

            return;
        }

        Snapshot(key, blocks);

        mesher.Build(key.y * Edge);

        if (mesher.Vertices.Count == 0 && mesher.Boxes.Count == 0)
        {
            RemoveView(key);

            return;
        }

        SubChunkView view = GetView(key);

        view.SetMesh(mesher);
        view.SetColliders(mesher.Boxes);
    }

    //komorki pod-chunka z ramka 1 bloku; wnetrze czytane wprost z danych chunka, a ramka z mapy (sasiednie chunki albo ich oszacowanie z generatora)
    private void Snapshot(Vector3Int key, string[,,] blocks)
    {
        int baseX = key.x * Edge;
        int baseY = key.y * Edge;
        int baseZ = key.z * Edge;
        string lastId = null;
        BlockTemplate lastTemplate = null;

        for (int pz = 0; pz < Pad; ++pz)
        {
            int z = pz - 1;

            for (int px = 0; px < Pad; ++px)
            {
                int x = px - 1;
                bool inside = x >= 0 && x < Edge && z >= 0 && z < Edge;

                mesher.Tops[px + Pad * pz] = map.ColumnTop(baseX + x, baseZ + z);

                for (int py = 0; py < Pad; ++py)
                {
                    int y = baseY + py - 1;
                    //pod swiatem (y < 0) jest pustka tylko dla rysowania: dzieki temu widac spod spodu dolna warstwe skaly; fizyka i grawitacja nadal traktuja to jako lite (MapGenerator.CellId)
                    string id = y < 0 ? null : inside && y < height ? blocks[x, y, z] : map.CellId(baseX + x, y, baseZ + z);
                    int index = px + Pad * (py + Pad * pz);

                    mesher.Ids[index] = id;

                    if (id == null)
                    {
                        mesher.Cells[index] = null;

                        continue;
                    }

                    if (!ReferenceEquals(id, lastId))
                    {
                        lastId = id;
                        lastTemplate = templates.Get(id);
                    }

                    mesher.Cells[index] = lastTemplate;
                }
            }
        }
    }

    private SubChunkView GetView(Vector3Int key)
    {
        if (views.TryGetValue(key, out SubChunkView view))
            return view;

        GameObject chunk = new GameObject("SubChunk " + key.x + "," + key.y + "," + key.z, typeof(MeshFilter), typeof(MeshRenderer), typeof(SubChunkView));

        chunk.tag = templates.Tag;
        chunk.transform.SetParent(root, false);
        chunk.transform.position = new Vector3(key.x * Edge, key.y * Edge + offsetY, key.z * Edge);

        view = chunk.GetComponent<SubChunkView>();

        views[key] = view;

        return view;
    }

    private void RemoveView(Vector3Int key)
    {
        if (!views.TryGetValue(key, out SubChunkView view))
            return;

        Object.Destroy(view.gameObject);

        views.Remove(key);
    }
}
