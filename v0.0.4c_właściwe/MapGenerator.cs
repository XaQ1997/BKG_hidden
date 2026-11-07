using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class MapGenerator : MonoBehaviour
{
    private readonly HashSet<Vector2Int> builtChunks = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> chunkBuffer = new List<Vector2Int>();
    private ChunkRenderer chunkRenderer;
    private FluidSimulator fluids;
    private MoistureSimulator moisture;

    private const string SolidId = "00";

    //przy jednej zmianie bloku sprawdzamy co najwyzej tyle komorek pod katem spadania, a obszar lity wiekszy niz limit uznajemy za powiazany z podlozem
    private const int GravityChecksPerEdit = 4096;
    private const int GroundSearchLimit = 2048;

    //kolejnosc: ostatni kierunek (w dol) jest sprawdzany pierwszy, bo podloze jest najczesciej pod blokiem
    private static readonly Vector3Int[] SearchDirections = { Vector3Int.up, Vector3Int.right, Vector3Int.left, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1), Vector3Int.down };

    private readonly Queue<Vector3Int> gravityQueue = new Queue<Vector3Int>();
    private readonly HashSet<Vector3Int> searchVisited = new HashSet<Vector3Int>();
    private readonly Stack<Vector3Int> searchStack = new Stack<Vector3Int>();
    private readonly List<Vector3Int> searchComponent = new List<Vector3Int>();
    private bool isCheckingGravity;

    [SerializeField] private Vector3Int mapSize = new Vector3Int(1024, 1024, 1024);
    [SerializeField] private Vector3Int mapOffset = new Vector3Int(512, 0, 512);

    [SerializeField] private int seed=0;
    [SerializeField] private bool useFixedSeed = false;
    [SerializeField] private int fixedSeed = 0;

    [SerializeField] private ChunkManager chunkManager;
    //zasieg renderowania w chunkach (krok 0.5), ustawiany w menu pauzy i zapamietywany; generowanie siega o GenerateMargin dalej
    public const float MinRenderDistance = 1f;
    public const float MaxRenderDistance = 32f;
    private const float GenerateMargin = 1f;
    private const string RenderDistanceKey = "RenderDistance";

    [SerializeField] private float renderDistance = 4f;
    [SerializeField] private float buildBudgetMs = 4f;
    [SerializeField] private float generateBudgetMs = 4f;
    private Vector2Int playerChunk = Vector2Int.zero;

    //obiekt, wokol ktorego ladowane sa chunki; generator stoi na osobnym, nieruchomym obiekcie, wiec bez tego chunki nigdy by sie nie zmienialy
    [SerializeField] private Transform player;

    [SerializeField] private BlockMap blockMap;
    [SerializeField] private GameSettings gameSettings;

    private ChunkCollector Chunks
    {
        get { return chunkManager.Chunks(); }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (player == null)
        {
            Controller controller = FindObjectOfType<Controller>();

            player = controller != null ? controller.transform : transform;
        }

        fluids = new FluidSimulator(this);
        moisture = new MoistureSimulator(this);
        chunkRenderer = new ChunkRenderer(this, blockMap, mapSize.y, mapOffset.y);

        InitGame();
    }

    private void Update()
    {
        Vector2Int pos = new Vector2Int(Mathf.FloorToInt(player.position.x / 16f), Mathf.FloorToInt(player.position.z / 16f));

        if (pos != playerChunk)
        {
            playerChunk = pos;

            chunkManager.ChangeChunk(pos);

            UpdateTerrain();
        }

        if (chunkManager.Process(generateBudgetMs))
            UpdateTerrain();

        chunkRenderer.Process(buildBudgetMs, player.position);

        fluids.Update(Time.deltaTime);
        moisture.Update(Time.deltaTime);
    }

    //nowo wczytane chunki trafiaja do kolejki rysowania, a wyladowane znikaja ze sceny
    private void UpdateTerrain()
    {
        chunkBuffer.Clear();

        foreach (var built in builtChunks)
            if (!IsChunkLoaded(built))
                chunkBuffer.Add(built);

        foreach (var removed in chunkBuffer)
        {
            chunkRenderer.RemoveChunk(removed);
            builtChunks.Remove(removed);
        }

        foreach (var chunk in Chunks.Chunks)
        {
            if (IsChunkLoaded(chunk.Key) && !builtChunks.Contains(chunk.Key))
            {
                builtChunks.Add(chunk.Key);

                chunkRenderer.AddChunk(chunk.Key, chunk.Value.Loader.Highest);
                fluids.ScheduleAll(chunk.Value.Loader.FluidSeeds);
            }
        }
    }

    //liczniki do okna debugowania: wygenerowane i wczytane chunki, obiekty siatek i kolejka budowania
    public int GeneratedChunkCount()
    {
        return Chunks.Chunks.Count;
    }

    public int LoadedChunkCount()
    {
        int count = 0;

        foreach (var chunk in Chunks.Chunks)
            if (IsChunkLoaded(chunk.Key))
                ++count;

        return count;
    }

    public int MeshCount()
    {
        return chunkRenderer.ViewCount;
    }

    public int PendingMeshCount()
    {
        return chunkRenderer.PendingCount;
    }

    //swiatlo sloneczne komorki (0..1), tak jak liczy je mesher: pelne nad powierzchnia kolumny, pod nia slabnie z glebokoscia
    public float SkyLight(int x, int y, int z)
    {
        int top = ColumnTop(x, z);

        return y >= top ? 1f : BlockLight.CaveLight(top - y);
    }

    public bool IsChunkLoaded(Vector2Int pos)
    {
        if (!Chunks.Chunks.TryGetValue(pos, out Chunk chunk))
            return false;

        return (chunk.State == ChunkState.Loaded || chunk.State == ChunkState.Reloaded) && chunk.Loader != null;
    }

    //dane wczytanego chunka (null, gdy chunk nie jest wczytany)
    public string[,,] GetChunkBlocks(Vector2Int pos)
    {
        if (!Chunks.Chunks.TryGetValue(pos, out Chunk chunk) || chunk.Loader == null)
            return null;

        return chunk.Loader.Blocks;
    }

    //y to indeks w tablicy swiata (y bloku + mapOffset.y); swiat jest nieskonczony w poziomie, ograniczony tylko wysokoscia
    private bool InBounds(int x, int y, int z)
    {
        return y >= 0 && y < mapSize.y;
    }

    private string BlockId(int x, int y, int z)
    {
        Vector2Int chunkPos = new Vector2Int(Mathf.FloorToInt(x / 16f), Mathf.FloorToInt(z / 16f));

        if (!Chunks.Chunks.TryGetValue(chunkPos, out Chunk chunk) || chunk.Loader == null)
            return null;

        return chunk.Loader.Blocks[x - chunkPos.x * 16, y, z - chunkPos.y * 16];
    }

    //id bloku w komorce: null = powietrze; dla niezaladowanego terenu z generatora zwraca oszacowanie (lity, woda albo powietrze)
    public string CellId(int x, int y, int z)
    {
        if (y < 0)
            return SolidId;

        if (y >= mapSize.y)
            return null;

        Vector2Int chunkPos = new Vector2Int(x >> 4, z >> 4);

        if (!Chunks.Chunks.TryGetValue(chunkPos, out Chunk chunk))
            return null;

        int localX = x & 15;
        int localZ = z & 15;

        if (chunk.Loader != null)
            return chunk.Loader.Blocks[localX, y, localZ];

        //chunk wygenerowany, ale niezaladowany: zmiany gracza, a potem to, co ChunkLoader zrobilby z warstw, jaskin i wody
        if (Chunks.Edits.TryGetValue(chunkPos, out Dictionary<Vector3Int, string> edits) && edits.TryGetValue(new Vector3Int(x, y, z), out string edited))
            return edited;

        if (chunk.Generator == null)
            return null;

        int top = chunk.Generator.TerrainLayers[localX, localZ].GetLayer(TerrainData.Grass).LayerBorders[1];

        if (y >= top)
            return y < WaterGenerator.SeaLevel ? Fluid.Source : null;

        CaveMask caves = chunk.Generator.Caves;

        if (caves != null && caves.IsCave(localX, y, localZ))
            return caves.IsWater(localX, y, localZ) ? Fluid.Source : null;

        return SolidId;
    }

    //lite w sensie fizycznym (podpiera zwir, blokuje ruch): wszystko poza powietrzem i woda
    private bool IsSolid(int x, int y, int z)
    {
        string id = CellId(x, y, z);

        return id != null && !Fluid.IsWater(id);
    }

    //wysokosc nad najwyzszym nieprzezroczystym blokiem kolumny (swiatlo slonca siega do tego poziomu); bez danych kolumna jest otwarta
    public int ColumnTop(int x, int z)
    {
        Vector2Int chunkPos = new Vector2Int(x >> 4, z >> 4);

        if (!Chunks.Chunks.TryGetValue(chunkPos, out Chunk chunk))
            return 0;

        if (chunk.Loader != null)
            return chunk.Loader.Tops[x & 15, z & 15];

        if (chunk.Generator == null)
            return 0;

        return chunk.Generator.TerrainLayers[x & 15, z & 15].GetLayer(TerrainData.Grass).LayerBorders[1];
    }

    //komorka z danych zaladowanego chunka: false = brak danych (chunk niezaladowany lub poza swiatem), id == null to powietrze
    public bool TryGetCell(int x, int y, int z, out string id)
    {
        id = null;

        if (y < 0 || y >= mapSize.y || Chunks == null)
            return false;

        Vector2Int chunkPos = new Vector2Int(x >> 4, z >> 4);

        if (!Chunks.Chunks.TryGetValue(chunkPos, out Chunk chunk) || chunk.Loader == null)
            return false;

        id = chunk.Loader.Blocks[x & 15, y, z & 15];

        return true;
    }

    //to samo we wspolrzednych swiata (y bloku z mapOffset.y), jak w SetBlock
    public bool TryGetBlockAt(Vector3Int pos, out string id)
    {
        return TryGetCell(pos.x, pos.y - mapOffset.y, pos.z, out id);
    }

    //ustawia komorke plynu (null = powietrze): dane, przebudowa siatki (w limicie czasu klatki) i planowanie dalszego przeplywu
    public void SetFluidCell(int x, int y, int z, string id)
    {
        if (y < 0 || y >= mapSize.y)
            return;

        TryGetCell(x, y, z, out string previous);

        Chunks.SetEdit(new Vector3Int(x, y, z), id);

        chunkRenderer.MarkCell(x, y, z, false);

        NotifyMoisture(x, y, z, previous, id);

        fluids.NotifyChange(x, y, z);
    }

    //pojawienie sie albo znikniecie wody zmienia wilgotnosc piasku obok, a postawiony piasek jest od razu sprawdzany
    private void NotifyMoisture(int x, int y, int z, string previous, string id)
    {
        bool wasWater = Fluid.IsWater(previous);
        bool isWater = Fluid.IsWater(id);

        if (isWater && !wasWater)
            moisture.WaterAppeared(x, y, z);
        else if (wasWater && !isWater)
            moisture.WaterGone(x, y, z);

        if (BlockKind.IsSand(id))
            moisture.SandPlaced(x, y, z);
    }

    //zmiana komorki we wspolrzednych tablicy swiata (y bez mapOffset.y), jak SetBlock
    public void SetCell(int x, int y, int z, string id)
    {
        SetBlock(new Vector3Int(x, y + mapOffset.y, z), id);
    }

    public bool IsLiquidAt(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x);
        int y = Mathf.RoundToInt(worldPosition.y) - mapOffset.y;
        int z = Mathf.RoundToInt(worldPosition.z);

        return TryGetCell(x, y, z, out string id) && Fluid.IsWater(id);
    }

    public void InitGame()
    {
        if (useFixedSeed)
            seed = fixedSeed;
        else
        {
            uint ticks = (uint)DateTime.Now.Ticks;
            Random random = new Random(ticks == 0 ? 1u : ticks);

            random.NextUInt();
            random.NextUInt();

            seed = random.NextInt();
        }

        Debug.Log("Seed: " + seed);

        playerChunk = Vector2Int.zero;

        chunkManager.Init();

        builtChunks.Clear();
        chunkRenderer.Clear();

        UpdateTerrain();

        //chunk startowy buduje sie w calosci od razu, reszta w kolejce
        chunkRenderer.Process(float.PositiveInfinity, new Vector3(0f, mapSize.y, 0f));

        if (TrySpawnHeight(out int height))
            gameSettings.Spawn(height);
    }

    public bool TrySpawnHeight(out int height)
    {
        for (int y = mapSize.y - 1; y >= 0; --y)
        {
            if (Block(0, y, 0, "30"))
            {
                height = y + mapOffset.y;

                return true;
            }
        }

        height = 0;

        return false;
    }

    public void CleanMap()
    {
        fluids.Clear();
        moisture.Clear();
        chunkRenderer.Clear();
        builtChunks.Clear();
    }

    public Vector3Int MapSize()
    {
        return mapSize;
    }

    public Vector3Int MapOffset()
    {
        return mapOffset;
    }

    public int Seed()
    {
        return seed;
    }

    public BlockMap GetBlockMap()
    {
        return blockMap;
    }

    //generowanie siega zawsze o chunk dalej niz renderowanie: wczytywany chunk potrzebuje wygenerowanych sasiadow
    public float GenerateDistance()
    {
        return renderDistance + GenerateMargin;
    }

    public float RenderDistance()
    {
        return renderDistance;
    }

    //zmienia zasieg renderowania w locie (krok 0.5, od MinRenderDistance do MaxRenderDistance): chunki poza nowym zasiegiem znikaja, brakujace dochodza w kolejkach
    public void SetRenderDistance(float distance)
    {
        renderDistance = Mathf.Clamp(Mathf.Round(distance * 2f) / 2f, MinRenderDistance, MaxRenderDistance);

        PlayerPrefs.SetFloat(RenderDistanceKey, renderDistance);
        PlayerPrefs.Save();

        if (chunkManager != null && chunkManager.Chunks() != null)
        {
            chunkManager.ChangeChunk(playerChunk);

            UpdateTerrain();
        }
    }

    //zapamietany zasieg nadpisuje wartosc ze sceny, zanim ktorykolwiek Start zacznie generowac chunki
    private void Awake()
    {
        if (PlayerPrefs.HasKey(RenderDistanceKey))
            renderDistance = Mathf.Clamp(Mathf.Round(PlayerPrefs.GetFloat(RenderDistanceKey) * 2f) / 2f, MinRenderDistance, MaxRenderDistance);
    }

    //pos to pozycja w swiecie (y bloku z mapOffset.y); id == null usuwa blok
    public void SetBlock(Vector3Int pos, string id)
    {
        int x = pos.x;
        int y = pos.y - mapOffset.y;
        int z = pos.z;

        if (!InBounds(x, y, z))
            throw new IndexOutOfRangeException("Block poza zakresem swiata: " + pos);

        int oldTop = ColumnTop(x, z);

        TryGetCell(x, y, z, out string previous);

        Chunks.SetEdit(new Vector3Int(x, y, z), id);

        int newTop = ColumnTop(x, z);

        chunkRenderer.MarkCell(x, y, z, true);

        //zmiana powierzchni kolumny zmienia swiatlo pod nia (do DarkDepth blokow w dol) i w sasiednich kolumnach
        if (newTop != oldTop)
            chunkRenderer.MarkColumns(x, z, Mathf.Min(oldTop, newTop) - BlockLight.DarkDepth, Mathf.Max(oldTop, newTop), true);

        chunkRenderer.Flush();

        NotifyMoisture(x, y, z, previous, id);

        fluids.NotifyChange(x, y, z);

        gravityQueue.Enqueue(new Vector3Int(x, y, z));

        DrainGravity();
    }

    //spadajace bloki same zmieniaja swiat (SetBlock), wiec sprawdzenia ida przez kolejke zamiast rekurencji
    private void DrainGravity()
    {
        if (isCheckingGravity)
            return;

        isCheckingGravity = true;

        try
        {
            for (int i = 0; i < GravityChecksPerEdit && gravityQueue.Count > 0; ++i)
                CheckGravity(gravityQueue.Dequeue());
        }
        finally
        {
            gravityQueue.Clear();

            isCheckingGravity = false;
        }
    }

    //po zmianie komorki: spada ona sama (jesli jest blokiem z grawitacja), blok nad nia, a po usunieciu bloku takze bloki z polgrawitacja w odcietych od podloza obszarach obok
    private void CheckGravity(Vector3Int cell)
    {
        TryFall(cell, false);
        TryFall(cell + Vector3Int.up, false);

        if (BlockId(cell.x, cell.y, cell.z) != null)
            return;

        foreach (Vector3Int direction in SearchDirections)
            CheckHanging(cell + direction);
    }

    //pelna grawitacja (zwir, suchy piasek): spada, gdy nic nie jest pod nim; polgrawitacja (mokry piasek): spada, gdy nic nie jest pod nim
    //i caly polaczony z nim obszar litych blokow (z dowolnej strony, takze posrednio) nie dotyka podloza swiata
    //hanging == true oznacza, ze obszar wiadomo juz, ze jest odciety
    private void TryFall(Vector3Int cell, bool hanging)
    {
        if (cell.y <= 0 || cell.y >= mapSize.y)
            return;

        string id = BlockId(cell.x, cell.y, cell.z);

        if (id == null || !blockMap.blockMap.TryGetValue(id, out Block block) || !block.HasHalfGravity || block.BlockPrefab == null || IsSolid(cell.x, cell.y - 1, cell.z))
            return;

        if (!block.HasFullGravity && !hanging && TouchesGround(cell, searchComponent))
            return;

        Fall(cell, id, block);
    }

    //obszar lity zawierajacy start odciety od podloza: kazdy blok z grawitacja w nim, ktory moze spasc, odpada
    private void CheckHanging(Vector3Int start)
    {
        if (!IsSolid(start.x, start.y, start.z) || TouchesGround(start, searchComponent))
            return;

        foreach (Vector3Int cell in searchComponent.ToArray())
            TryFall(cell, true);
    }

    private void Fall(Vector3Int cell, string id, Block block)
    {
        SetBlock(new Vector3Int(cell.x, cell.y + mapOffset.y, cell.z), null);

        GameObject falling = Instantiate(block.BlockPrefab, new Vector3(cell.x, cell.y + mapOffset.y, cell.z), Quaternion.identity);

        foreach (Collider collider in falling.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        falling.AddComponent<FallingBlock>().Setup(this, id, cell.x, cell.y, cell.z);
    }

    //czy lity obszar polaczony ze startem (sasiedztwo scian, 6 kierunkow) siega podloza swiata (y = 0); component dostaje odwiedzone komorki
    //nieznany teren (niewygenerowany chunk) i obszar wiekszy niz GroundSearchLimit uznajemy za podloze, zeby nie spadaly bloki na krawedzi swiata ani w ogromnych brylach
    private bool TouchesGround(Vector3Int start, List<Vector3Int> component)
    {
        searchVisited.Clear();
        searchStack.Clear();
        component.Clear();

        searchVisited.Add(start);
        searchStack.Push(start);

        while (searchStack.Count > 0)
        {
            Vector3Int cell = searchStack.Pop();

            component.Add(cell);

            if (cell.y <= 0 || component.Count >= GroundSearchLimit)
                return true;

            foreach (Vector3Int direction in SearchDirections)
            {
                Vector3Int next = cell + direction;

                if (searchVisited.Contains(next))
                    continue;

                if (!Chunks.Chunks.ContainsKey(new Vector2Int(next.x >> 4, next.z >> 4)))
                    return true;

                if (!IsSolid(next.x, next.y, next.z))
                    continue;

                searchVisited.Add(next);
                searchStack.Push(next);
            }
        }

        return false;
    }

    //najnizsza wolna komorka, na ktorej blok z gory oprze sie o lite podloze
    public int FindLanding(int x, int fromY, int z)
    {
        int y = fromY;

        while (y > 0 && !IsSolid(x, y - 1, z))
            --y;

        return y;
    }

    public void LandFallingBlock(int x, int y, int z, string id)
    {
        while (y < mapSize.y && IsSolid(x, y, z))
            ++y;

        if (y >= mapSize.y)
            return;

        if (builtChunks.Contains(new Vector2Int(x >> 4, z >> 4)))
            SetBlock(new Vector3Int(x, y + mapOffset.y, z), id);
        else
            Chunks.SetEdit(new Vector3Int(x, y, z), id);
    }

    public bool Block(int x, int y, int z, string id)
    {
        if (y < 0 || y >= mapSize.y)
            return false;

        Vector2Int chunkPos = new Vector2Int(Mathf.FloorToInt(x / 16f), Mathf.FloorToInt(z / 16f));

        if (!Chunks.Chunks.TryGetValue(chunkPos, out Chunk chunk) || chunk.Loader == null)
            return false;

        return chunk.Loader.Blocks[x - chunkPos.x * 16, y, z - chunkPos.y * 16] == id;
    }
}
