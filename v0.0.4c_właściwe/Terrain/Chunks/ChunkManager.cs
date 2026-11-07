using BKG.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//chunki powstaja w kolejkach, od najblizszego graczowi, a nie wszystkie naraz przy zmianie chunka gracza
//chunk jest wczytywany dopiero, gdy wygenerowani sa on i jego czterej sasiedzi (widocznosc scian na granicach zalezy od sasiadow)
public class ChunkManager : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private ChunkGenerator chunkGenerator;
    [SerializeField] private ChunkLoader chunkLoader;

    private static readonly Vector2Int[] Neighbors = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    private ChunkCollector chunks;
    private readonly List<Vector2Int> removedChunks = new List<Vector2Int>();
    private readonly List<Vector2Int> generateQueue = new List<Vector2Int>();
    private readonly List<Vector2Int> loadQueue = new List<Vector2Int>();
    private readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
    private Vector2Int center = Vector2Int.zero;

    public void Init()
    {
        chunks = new ChunkCollector();
        center = Vector2Int.zero;

        Plan();

        //tylko chunk startowy (z sasiadami do generowania); reszta doplywa w kolejkach klatka po klatce
        Process(0f);
    }

    public void ChangeChunk(Vector2Int pos)
    {
        float generateDistance = mapGenerator.GenerateDistance();
        float renderDistance = mapGenerator.RenderDistance();

        center = pos;

        removedChunks.Clear();

        foreach (var chunk in chunks.Chunks)
        {
            bool isLoaded = chunk.Value.State == ChunkState.Loaded || chunk.Value.State == ChunkState.Reloaded;

            if (isLoaded && !MathOperations.IsShownChunk(pos, chunk.Key, renderDistance))
                chunks.ChunkUnload(chunk.Key);

            if (!MathOperations.IsShownChunk(pos, chunk.Key, generateDistance + 1))
                removedChunks.Add(chunk.Key);
        }

        foreach (var chunkPos in removedChunks)
            chunks.ChunkDelete(chunkPos);

        Plan();
    }

    //wykonuje zadania z kolejek, dopoki starcza budzetu czasu (co najmniej jedno na wywolanie); zwraca true, gdy wczytano jakis chunk
    //chunk gracza i generowanie jego sasiadow nie podlegaja budzetowi, zeby nie zabraklo terenu pod nogami
    public bool Process(float budgetMs)
    {
        bool loaded = false;
        int done = 0;

        watch.Restart();

        while (true)
        {
            int index = NextLoad();
            bool isLoad = index >= 0;
            Vector2Int pos;

            if (isLoad)
                pos = loadQueue[index];
            else if (generateQueue.Count > 0)
                pos = generateQueue[0];
            else
                break;

            bool urgent = isLoad ? pos == center : Mathf.Abs(pos.x - center.x) + Mathf.Abs(pos.y - center.y) <= 1;

            if (done > 0 && !urgent && watch.Elapsed.TotalMilliseconds >= budgetMs)
                break;

            if (isLoad)
            {
                loadQueue.RemoveAt(index);

                ChunkState state = chunks.Chunks[pos].State;

                chunkLoader.Load(pos);

                if (state == ChunkState.Unloaded)
                    chunks.ChunkReload(pos);

                loaded = true;
            }
            else
            {
                generateQueue.RemoveAt(0);

                if (!chunks.Chunks.ContainsKey(pos))
                    chunkGenerator.Generate(pos);
            }

            ++done;
        }

        return loaded;
    }

    //brakujace chunki w promieniu generateDistance i chunki do wczytania w promieniu renderDistance, najblizsze pierwsze
    //generateDistance jest zawsze o co najmniej 1 wiekszy od renderDistance (MapGenerator), wiec sasiedzi wczytywanych chunkow tez sa generowani
    private void Plan()
    {
        float generateDistance = mapGenerator.GenerateDistance();
        float renderDistance = mapGenerator.RenderDistance();
        int reach = Mathf.CeilToInt(generateDistance);

        generateQueue.Clear();
        loadQueue.Clear();

        for (int x = center.x - reach; x <= center.x + reach; ++x)
        {
            for (int z = center.y - reach; z <= center.y + reach; ++z)
            {
                Vector2Int chunkPos = new Vector2Int(x, z);

                if (!MathOperations.IsShownChunk(center, chunkPos, generateDistance))
                    continue;

                if (!chunks.Chunks.ContainsKey(chunkPos))
                    generateQueue.Add(chunkPos);

                if (MathOperations.IsShownChunk(center, chunkPos, renderDistance) && !IsLoaded(chunkPos))
                    loadQueue.Add(chunkPos);
            }
        }

        generateQueue.Sort((a, b) => Distance(a).CompareTo(Distance(b)));
        loadQueue.Sort((a, b) => Distance(a).CompareTo(Distance(b)));
    }

    //indeks najblizszego chunka z kolejki wczytywania, ktory ma juz wygenerowane otoczenie, albo -1
    private int NextLoad()
    {
        for (int i = 0; i < loadQueue.Count; ++i)
        {
            if (!IsGenerated(loadQueue[i]))
                continue;

            bool ready = true;

            foreach (Vector2Int neighbor in Neighbors)
                ready &= IsGenerated(loadQueue[i] + neighbor);

            if (ready)
                return i;
        }

        return -1;
    }

    private bool IsGenerated(Vector2Int pos)
    {
        return chunks.Chunks.TryGetValue(pos, out Chunk chunk) && chunk.Generator != null;
    }

    private bool IsLoaded(Vector2Int pos)
    {
        return chunks.Chunks.TryGetValue(pos, out Chunk chunk) && (chunk.State == ChunkState.Loaded || chunk.State == ChunkState.Reloaded);
    }

    private int Distance(Vector2Int pos)
    {
        Vector2Int delta = pos - center;

        return delta.x * delta.x + delta.y * delta.y;
    }

    //ile chunkow czeka na wygenerowanie i wczytanie (do okna debugowania)
    public int QueuedGenerate()
    {
        return generateQueue.Count;
    }

    public int QueuedLoad()
    {
        return loadQueue.Count;
    }

    public ChunkCollector Chunks()
    {
        return chunks;
    }
}
