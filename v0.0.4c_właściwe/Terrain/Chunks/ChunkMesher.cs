using System.Collections.Generic;
using UnityEngine;

//buduje siatke i collidery jednego pod-chunka 16x16x16 z migawki komorek z ramka 1 bloku: sasiedzi decyduja o widocznosci scian, AO i swietle
//komorka (x, y, z) ma indeks x + Pad * (y + Pad * z), wspolrzedne z ramka to 0..17, a wnetrze to 1..16
//do siatki trafiaja tylko sciany, ktorych nic nie zaslania; swiatlo i AO sa kolorem wierzcholkow (gladkie, usrednione z 4 komorek przy wierzcholku)
//collidery to kilka duzych pudel scalonych z lity komorek (zamiast jednego collidera na blok)
public class ChunkMesher
{
    public const int Edge = 16;
    public const int Pad = Edge + 2;

    public struct Box
    {
        public Vector3 Center;
        public Vector3 Extent;
        public bool Trigger;
    }

    //kolejnosc scian jak w CubeFaces: +y, -y, +z, -z, +x, -x
    private static readonly int[] FaceOffsets = { Pad, -Pad, Pad * Pad, -Pad * Pad, 1, -1 };
    private static readonly Vector3[] FaceNormals = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

    //osie (0 = x, 1 = y, 2 = z) styczne do sciany i ich kroki w indeksie komorek
    private static readonly int[,] TangentAxes = { { 0, 2 }, { 0, 2 }, { 0, 1 }, { 0, 1 }, { 1, 2 }, { 1, 2 } };
    private static readonly int[] AxisSteps = { 1, Pad, Pad * Pad };

    //jasnosc wierzcholka wg liczby zaslonietych sasiadow (0 = naroze zamkniete z obu stron)
    private static readonly float[] AoFactors = { 0.5f, 0.7f, 0.85f, 1f };

    //wejscie (wypelnia ChunkRenderer)
    public readonly BlockTemplate[] Cells = new BlockTemplate[Pad * Pad * Pad];
    public readonly string[] Ids = new string[Pad * Pad * Pad];

    //Tops[x + Pad * z]: wysokosc nad najwyzszym nieprzezroczystym blokiem kolumny (swiatlo slonca siega do tego poziomu)
    public readonly int[] Tops = new int[Pad * Pad];

    //wyjscie
    public readonly List<Vector3> Vertices = new List<Vector3>();
    public readonly List<Vector3> Normals = new List<Vector3>();
    public readonly List<Vector2> Uvs = new List<Vector2>();
    public readonly List<Color32> Colors = new List<Color32>();
    public readonly List<Material> Materials = new List<Material>();
    public readonly List<List<int>> Triangles = new List<List<int>>();
    public readonly List<Box> Boxes = new List<Box>();

    private readonly float[] lights = new float[Pad * Pad * Pad];
    private readonly bool[] taken = new bool[Edge * Edge * Edge];
    private readonly int[] ao = new int[4];
    private int baseY;

    //cellBaseY to wysokosc swiata pierwszej warstwy pod-chunka
    public void Build(int cellBaseY)
    {
        baseY = cellBaseY;

        Vertices.Clear();
        Normals.Clear();
        Uvs.Clear();
        Colors.Clear();
        Materials.Clear();
        Boxes.Clear();

        foreach (List<int> triangles in Triangles)
            triangles.Clear();

        for (int i = 0; i < lights.Length; ++i)
            lights[i] = -1f;

        bool exposedSolid = false;

        for (int z = 1; z <= Edge; ++z)
        {
            for (int y = 1; y <= Edge; ++y)
            {
                for (int x = 1; x <= Edge; ++x)
                {
                    int index = x + Pad * (y + Pad * z);
                    BlockTemplate template = Cells[index];

                    if (template == null)
                        continue;

                    int faces = AddFaces(index, x, y, z, template);

                    if (template.Passable && faces > 0)
                        Boxes.Add(new Box { Center = new Vector3(x - 1, y - 1, z - 1), Extent = Vector3.one, Trigger = true });

                    if (template.Solid && !exposedSolid && IsExposed(index))
                        exposedSolid = true;
                }
            }
        }

        //pod-chunk w calosci ukryty w skale nie potrzebuje collidera
        if (exposedSolid)
            AddSolidBoxes();
    }

    private int AddFaces(int index, int x, int y, int z, BlockTemplate template)
    {
        int added = 0;

        for (int face = 0; face < CubeFaces.Count; ++face)
        {
            FaceTemplate shape = template.Faces[face];

            if (shape == null || !IsFaceVisible(index, face, template))
                continue;

            AddFace(index, x, y, z, face, template, shape);

            ++added;
        }

        return added;
    }

    //sciana lita jest widoczna przy powietrzu i przezroczystym sasiedzie; przezroczysta (szklo, woda) tez przy innym przezroczystym,
    //ale woda przy wodzie tylko z boku i tylko ponad nizsza sasiadka
    private bool IsFaceVisible(int index, int face, BlockTemplate template)
    {
        int neighbor = index + FaceOffsets[face];
        BlockTemplate other = Cells[neighbor];

        if (other == null)
            return true;

        if (!template.Transparent)
            return other.Transparent;

        if (!other.Transparent)
            return false;

        if (template.Water != other.Water)
            return true;

        if (!template.Water)
            return Ids[neighbor] != Ids[index];

        if (face < 2)
            return false;

        return WaterHeight(neighbor) < WaterHeight(index);
    }

    //wysokosc wody w komorce: pelna, gdy spada albo ma wode nad soba; inaczej proporcjonalna do ilosci wody
    private float WaterHeight(int index)
    {
        string id = Ids[index];

        if (id == Fluid.Falling || Fluid.IsWater(Ids[index + Pad]))
            return 1f;

        return Mathf.Min(1f, Fluid.Amount(id) / 9f);
    }

    private void AddFace(int index, int x, int y, int z, int face, BlockTemplate template, FaceTemplate shape)
    {
        int submesh = Submesh(template.Material);
        List<int> triangles = Triangles[submesh];
        int start = Vertices.Count;
        Vector3 origin = new Vector3(x - 1, y - 1, z - 1);
        float height = template.Water ? WaterHeight(index) : 1f;

        for (int i = 0; i < shape.Positions.Length; ++i)
        {
            Vector3 corner = shape.Positions[i];
            Vector3 position = corner;

            //woda nizsza niz pelny blok: dol zostaje na miejscu, gora opada
            if (template.Water)
                position.y = (position.y + 0.5f) * height - 0.5f;

            Colors.Add(VertexColor(index, face, corner, i));
            Vertices.Add(origin + position);
            Normals.Add(FaceNormals[face]);
            Uvs.Add(shape.Uvs[i]);
        }

        //przekatna czworokata idzie przez jasniejsza pare wierzcholkow, zeby cien AO nie przeciekal przez sciane
        if (shape.Quad && ao[0] + ao[2] < ao[1] + ao[3])
        {
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 1);
            triangles.Add(start + 3);
            triangles.Add(start + 2);
            triangles.Add(start + 1);

            return;
        }

        foreach (int vertex in shape.Triangles)
            triangles.Add(start + vertex);
    }

    private int Submesh(Material material)
    {
        int submesh = Materials.IndexOf(material);

        if (submesh >= 0)
            return submesh;

        Materials.Add(material);

        if (Materials.Count > Triangles.Count)
            Triangles.Add(new List<int>());

        return Materials.Count - 1;
    }

    //jasnosc wierzcholka: srednie swiatlo 4 komorek przy narozu (przed sciana, dwie boczne i narozna) razy AO z ich zaslonienia
    private Color32 VertexColor(int index, int face, Vector3 corner, int vertex)
    {
        int front = index + FaceOffsets[face];
        int axisB = TangentAxes[face, 0];
        int axisC = TangentAxes[face, 1];
        int stepB = (corner[axisB] > 0f ? 1 : -1) * AxisSteps[axisB];
        int stepC = (corner[axisC] > 0f ? 1 : -1) * AxisSteps[axisC];

        bool blockedB = IsOpaque(front + stepB);
        bool blockedC = IsOpaque(front + stepC);
        bool blockedD = IsOpaque(front + stepB + stepC);

        float center = Light(front);
        float lightB = blockedB ? center : Light(front + stepB);
        float lightC = blockedC ? center : Light(front + stepC);
        float lightD = (blockedB && blockedC) || blockedD ? center : Light(front + stepB + stepC);

        int level = blockedB && blockedC ? 0 : 3 - (blockedB ? 1 : 0) - (blockedC ? 1 : 0) - (blockedD ? 1 : 0);

        if (vertex < ao.Length)
            ao[vertex] = level;

        float value = Mathf.Lerp(BlockLight.MinLight, 1f, (center + lightB + lightC + lightD) * 0.25f) * AoFactors[level];
        byte shade = (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);

        return new Color32(shade, shade, shade, 255);
    }

    private bool IsOpaque(int index)
    {
        BlockTemplate template = Cells[index];

        return template != null && !template.Transparent;
    }

    private float Light(int index)
    {
        float value = lights[index];

        if (value >= 0f)
            return value;

        int x = index % Pad;
        int rest = index / Pad;
        int y = rest % Pad;
        int z = rest / Pad;
        int worldY = baseY + y - 1;
        int top = Tops[x + Pad * z];

        value = worldY >= top ? 1f : BlockLight.CaveLight(top - worldY);

        lights[index] = value;

        return value;
    }

    private bool IsExposed(int index)
    {
        for (int face = 0; face < CubeFaces.Count; ++face)
        {
            BlockTemplate other = Cells[index + FaceOffsets[face]];

            if (other == null || !other.Solid)
                return true;
        }

        return false;
    }

    private bool IsSolid(int x, int y, int z)
    {
        BlockTemplate template = Cells[(x + 1) + Pad * ((y + 1) + Pad * (z + 1))];

        return template != null && template.Solid;
    }

    private bool IsFree(int x, int y, int z)
    {
        return IsSolid(x, y, z) && !taken[x + Edge * (y + Edge * z)];
    }

    //chciwe scalanie lity komorek w prostopadloscienne pudla: najpierw wzdluz x, potem z, potem y
    private void AddSolidBoxes()
    {
        for (int i = 0; i < taken.Length; ++i)
            taken[i] = false;

        for (int z = 0; z < Edge; ++z)
        {
            for (int y = 0; y < Edge; ++y)
            {
                for (int x = 0; x < Edge; ++x)
                {
                    if (!IsFree(x, y, z))
                        continue;

                    int x1 = x;
                    int y1 = y;
                    int z1 = z;

                    while (x1 + 1 < Edge && IsFree(x1 + 1, y, z))
                        ++x1;

                    while (z1 + 1 < Edge && RowFree(x, x1, y, z1 + 1))
                        ++z1;

                    while (y1 + 1 < Edge && SlabFree(x, x1, y1 + 1, z, z1))
                        ++y1;

                    for (int bz = z; bz <= z1; ++bz)
                        for (int by = y; by <= y1; ++by)
                            for (int bx = x; bx <= x1; ++bx)
                                taken[bx + Edge * (by + Edge * bz)] = true;

                    Boxes.Add(new Box
                    {
                        Center = new Vector3((x + x1) * 0.5f, (y + y1) * 0.5f, (z + z1) * 0.5f),
                        Extent = new Vector3(x1 - x + 1, y1 - y + 1, z1 - z + 1),
                        Trigger = false
                    });
                }
            }
        }
    }

    private bool RowFree(int x0, int x1, int y, int z)
    {
        for (int x = x0; x <= x1; ++x)
            if (!IsFree(x, y, z))
                return false;

        return true;
    }

    private bool SlabFree(int x0, int x1, int y, int z0, int z1)
    {
        for (int z = z0; z <= z1; ++z)
            if (!RowFree(x0, x1, y, z))
                return false;

        return true;
    }
}
