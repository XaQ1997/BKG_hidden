using System.Collections.Generic;
using UnityEngine;

//stany wody jak w Minecrafcie: zrodlo (60), plynaca woda o poziomach 1-7 (61-67, wyzszy numer = mniej wody) i spadajaca woda (68)
public static class Fluid
{
    public const string Source = "60";
    public const string Falling = "68";

    public static bool IsWater(string id)
    {
        return id != null && id.Length == 2 && id[0] == '6' && id[1] >= '0' && id[1] <= '8';
    }

    public static bool IsSource(string id)
    {
        return id == Source;
    }

    //ilosc wody w komorce: zrodlo i spadajaca woda maja 8, plynaca od 7 (61) do 1 (67)
    public static int Amount(string id)
    {
        char level = id[1];

        return level == '0' || level == '8' ? 8 : 8 - (level - '0');
    }

    public static string FlowId(int amount)
    {
        return "6" + (8 - amount);
    }
}

//symulacja plynow wg zasad Minecrafta: woda spada w dol, a oparta o podloze rozchodzi sie na boki do 7 blokow,
//zawsze w strone najblizszego spadku (do 4 blokow); dwa zrodla obok siebie tworza trzecie; bez podpory woda cofa sie
//zmiana bloku planuje aktualizacje sasiednich komorek wody, wykonywane co 0,25 s (5 tickow), z limitem na krok
public class FluidSimulator
{
    private const float Interval = 0.25f;
    private const int MaxUpdates = 600;
    private const int SlopeSearch = 4;

    private enum Cell
    {
        Air,
        Water,
        Solid
    }

    private static readonly Vector3Int[] Directions = { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };

    private readonly MapGenerator map;
    private readonly HashSet<Vector3Int> scheduled = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> batch = new List<Vector3Int>();
    private float timer;

    public FluidSimulator(MapGenerator generator)
    {
        map = generator;
    }

    public void Clear()
    {
        scheduled.Clear();

        timer = 0f;
    }

    public void NotifyChange(int x, int y, int z)
    {
        Schedule(x, y, z);
        Schedule(x + 1, y, z);
        Schedule(x - 1, y, z);
        Schedule(x, y + 1, z);
        Schedule(x, y - 1, z);
        Schedule(x, y, z + 1);
        Schedule(x, y, z - 1);
    }

    public void Update(float deltaTime)
    {
        if (scheduled.Count == 0)
        {
            timer = 0f;

            return;
        }

        timer += deltaTime;

        if (timer < Interval)
            return;

        timer = 0f;

        batch.Clear();

        foreach (Vector3Int position in scheduled)
        {
            batch.Add(position);

            if (batch.Count >= MaxUpdates)
                break;
        }

        foreach (Vector3Int position in batch)
            scheduled.Remove(position);

        foreach (Vector3Int position in batch)
            Tick(position.x, position.y, position.z);
    }

    //komorki wody tuz po wczytaniu chunka (np. wylot zalanego tunelu)
    public void ScheduleAll(List<Vector3Int> positions)
    {
        if (positions == null)
            return;

        foreach (Vector3Int position in positions)
            Schedule(position.x, position.y, position.z);
    }

    private void Schedule(int x, int y, int z)
    {
        if (Kind(x, y, z, out string id) == Cell.Water)
            scheduled.Add(new Vector3Int(x, y, z));
    }

    //komorki w niezaladowanych chunkach i poza swiatem traktujemy jak lite
    private Cell Kind(int x, int y, int z, out string id)
    {
        if (!map.TryGetCell(x, y, z, out id))
        {
            id = null;

            return Cell.Solid;
        }

        if (id == null)
            return Cell.Air;

        return Fluid.IsWater(id) ? Cell.Water : Cell.Solid;
    }

    private void Tick(int x, int y, int z)
    {
        if (Kind(x, y, z, out string id) != Cell.Water)
            return;

        if (!Fluid.IsSource(id))
        {
            string next = NewLiquid(x, y, z);

            if (next == null)
            {
                map.SetFluidCell(x, y, z, null);

                return;
            }

            if (next != id)
            {
                map.SetFluidCell(x, y, z, next);

                id = next;
            }
        }

        Spread(x, y, z, id);
    }

    //jaka woda powinna byc w tej komorce: zrodlo (dwa zrodla obok i podloze), spadajaca (woda nad), albo plynaca o jeden poziom slabsza od najsilniejszego sasiada
    private string NewLiquid(int x, int y, int z)
    {
        int highest = 0;
        int sources = 0;

        foreach (Vector3Int direction in Directions)
        {
            if (Kind(x + direction.x, y, z + direction.z, out string id) == Cell.Water)
            {
                if (Fluid.IsSource(id))
                    ++sources;

                highest = Mathf.Max(highest, Fluid.Amount(id));
            }
        }

        if (sources >= 2)
        {
            Cell below = Kind(x, y - 1, z, out string belowId);

            if (below == Cell.Solid || (below == Cell.Water && Fluid.IsSource(belowId)))
                return Fluid.Source;
        }

        if (Kind(x, y + 1, z, out string aboveId) == Cell.Water)
            return Fluid.Falling;

        int amount = highest - 1;

        return amount <= 0 ? null : Fluid.FlowId(amount);
    }

    private void Spread(int x, int y, int z, string id)
    {
        Cell below = Kind(x, y - 1, z, out string belowId);

        if (below == Cell.Air)
        {
            string flowing = NewLiquid(x, y - 1, z);

            if (flowing != null)
                map.SetFluidCell(x, y - 1, z, flowing);

            if (CountHorizontalSources(x, y, z) >= 3)
                SpreadToSides(x, y, z, id);
        }
        else if (Fluid.IsSource(id) || below == Cell.Solid)
            SpreadToSides(x, y, z, id);
    }

    private int CountHorizontalSources(int x, int y, int z)
    {
        int count = 0;

        foreach (Vector3Int direction in Directions)
            if (Kind(x + direction.x, y, z + direction.z, out string id) == Cell.Water && Fluid.IsSource(id))
                ++count;

        return count;
    }

    //komorka ponizej, do ktorej woda moze splynac (powietrze lub woda)
    private bool IsHole(int x, int y, int z)
    {
        return Kind(x, y - 1, z, out string id) != Cell.Solid;
    }

    private void SpreadToSides(int x, int y, int z, string id)
    {
        int amount = id == Fluid.Falling ? 7 : Fluid.Amount(id) - 1;

        if (amount <= 0)
            return;

        int best = 1000;
        string[] results = new string[Directions.Length];

        for (int d = 0; d < Directions.Length; ++d)
        {
            int nx = x + Directions[d].x;
            int nz = z + Directions[d].z;
            Cell kind = Kind(nx, y, nz, out string neighborId);

            if (kind == Cell.Solid || (kind == Cell.Water && Fluid.IsSource(neighborId)))
                continue;

            string next = NewLiquid(nx, y, nz);

            if (next == null)
                continue;

            int distance = IsHole(nx, y, nz) ? 0 : SlopeDistance(nx, y, nz, 1, d ^ 1);

            if (distance < best)
            {
                System.Array.Clear(results, 0, results.Length);

                best = distance;
            }

            if (distance <= best)
                results[d] = next;
        }

        for (int d = 0; d < Directions.Length; ++d)
        {
            if (results[d] == null)
                continue;

            int nx = x + Directions[d].x;
            int nz = z + Directions[d].z;

            if (Kind(nx, y, nz, out string existing) == Cell.Air)
                map.SetFluidCell(nx, y, nz, results[d]);
        }
    }

    //odleglosc (w blokach) do najblizszego spadku, szukana do SlopeSearch blokow
    private int SlopeDistance(int x, int y, int z, int depth, int fromDirection)
    {
        int best = 1000;

        for (int d = 0; d < Directions.Length; ++d)
        {
            if (d == fromDirection)
                continue;

            int nx = x + Directions[d].x;
            int nz = z + Directions[d].z;
            Cell kind = Kind(nx, y, nz, out string id);

            if (kind == Cell.Solid || (kind == Cell.Water && Fluid.IsSource(id)))
                continue;

            if (IsHole(nx, y, nz))
                return depth;

            if (depth < SlopeSearch)
                best = Mathf.Min(best, SlopeDistance(nx, y, nz, depth + 1, d ^ 1));
        }

        return best;
    }
}
