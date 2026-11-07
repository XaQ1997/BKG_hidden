using System.Collections.Generic;
using UnityEngine;

//wilgotnosc piasku: suchy piasek stykajacy sie z woda (6 sasiadow) po chwili robi sie mokry, a mokry bez wody w poblizu
//(odleglosc Manhattan do DryRadius) po dluzszej chwili wysycha; wysuszony piasek ma pelna grawitacje, wiec moze od razu spasc
//zdarzenia (woda pojawia sie lub znika, piasek jest stawiany) planuja sprawdzenia z losowym opoznieniem, a w chwili sprawdzenia stan jest czytany od nowa
public class MoistureSimulator
{
    private const float Interval = 0.5f;
    private const int MaxChecks = 128;
    private const float SoakDelayMin = 1f;
    private const float SoakDelayMax = 4f;
    private const float DryDelayMin = 8f;
    private const float DryDelayMax = 20f;
    private const float PlacedDelay = 0.5f;
    private const int DryRadius = 2;

    private static readonly Vector3Int[] Neighbors = { Vector3Int.up, Vector3Int.down, Vector3Int.right, Vector3Int.left, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };

    private readonly MapGenerator map;
    private readonly Dictionary<Vector3Int, float> due = new Dictionary<Vector3Int, float>();
    private readonly List<Vector3Int> ready = new List<Vector3Int>();
    private float clock;
    private float timer;

    public MoistureSimulator(MapGenerator generator)
    {
        map = generator;
    }

    public void Clear()
    {
        due.Clear();

        timer = 0f;
    }

    public void Update(float deltaTime)
    {
        clock += deltaTime;
        timer += deltaTime;

        if (timer < Interval || due.Count == 0)
            return;

        timer = 0f;

        ready.Clear();

        foreach (var entry in due)
        {
            if (entry.Value <= clock)
                ready.Add(entry.Key);

            if (ready.Count >= MaxChecks)
                break;
        }

        foreach (Vector3Int cell in ready)
        {
            due.Remove(cell);

            Evaluate(cell);
        }
    }

    //woda pojawila sie w komorce: suchy piasek obok zacznie nasiakac
    public void WaterAppeared(int x, int y, int z)
    {
        foreach (Vector3Int direction in Neighbors)
        {
            Vector3Int cell = new Vector3Int(x + direction.x, y + direction.y, z + direction.z);

            if (IsCell(cell, BlockKind.DrySand))
                Schedule(cell, Random.Range(SoakDelayMin, SoakDelayMax));
        }
    }

    //woda zniknela z komorki: mokry piasek w poblizu moze zaczac wysychac
    public void WaterGone(int x, int y, int z)
    {
        for (int dx = -DryRadius; dx <= DryRadius; ++dx)
        {
            for (int dy = -DryRadius; dy <= DryRadius; ++dy)
            {
                for (int dz = -DryRadius; dz <= DryRadius; ++dz)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz) > DryRadius)
                        continue;

                    Vector3Int cell = new Vector3Int(x + dx, y + dy, z + dz);

                    if (IsCell(cell, BlockKind.WetSand))
                        Schedule(cell, Random.Range(DryDelayMin, DryDelayMax));
                }
            }
        }
    }

    //piasek postawiony w swiecie: sprawdz od razu, czy ma byc suchy czy mokry
    public void SandPlaced(int x, int y, int z)
    {
        Schedule(new Vector3Int(x, y, z), PlacedDelay);
    }

    private void Schedule(Vector3Int cell, float delay)
    {
        if (!due.ContainsKey(cell))
            due[cell] = clock + delay;
    }

    private void Evaluate(Vector3Int cell)
    {
        if (!map.TryGetCell(cell.x, cell.y, cell.z, out string id))
            return;

        if (id == BlockKind.DrySand && TouchesWater(cell))
            map.SetCell(cell.x, cell.y, cell.z, BlockKind.WetSand);
        else if (id == BlockKind.WetSand && !WaterNear(cell))
            map.SetCell(cell.x, cell.y, cell.z, BlockKind.DrySand);
    }

    private bool IsCell(Vector3Int cell, string id)
    {
        return map.TryGetCell(cell.x, cell.y, cell.z, out string found) && found == id;
    }

    private bool TouchesWater(Vector3Int cell)
    {
        foreach (Vector3Int direction in Neighbors)
            if (map.TryGetCell(cell.x + direction.x, cell.y + direction.y, cell.z + direction.z, out string id) && Fluid.IsWater(id))
                return true;

        return false;
    }

    //brak danych (niewczytany chunk) liczy sie jak woda w poblizu, zeby piasek przy krawedzi wczytanego terenu nie wysychal bez powodu
    private bool WaterNear(Vector3Int cell)
    {
        for (int dx = -DryRadius; dx <= DryRadius; ++dx)
        {
            for (int dy = -DryRadius; dy <= DryRadius; ++dy)
            {
                for (int dz = -DryRadius; dz <= DryRadius; ++dz)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz) > DryRadius)
                        continue;

                    if (!map.TryGetCell(cell.x + dx, cell.y + dy, cell.z + dz, out string id) || Fluid.IsWater(id))
                        return true;
                }
            }
        }

        return false;
    }
}
