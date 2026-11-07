using System.Collections.Generic;
using UnityEngine;

//sciany szescianu w kolejnosci +y, -y, +z, -z, +x, -x (jak w FaceOffsets w ChunkMesher)
public static class CubeFaces
{
    public const int Count = 6;

    public static int FromNormal(Vector3 normal)
    {
        Vector3 a = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

        if (a.y >= a.x && a.y >= a.z)
            return normal.y > 0f ? 0 : 1;

        if (a.z >= a.x)
            return normal.z > 0f ? 2 : 3;

        return normal.x > 0f ? 4 : 5;
    }
}

//jedna sciana bloku wyciagnieta z siatki prefabu: pozycje wzgledem srodka bloku, UV z atlasu i trojkaty (indeksy lokalne)
public class FaceTemplate
{
    public Vector3[] Positions;
    public Vector2[] Uvs;
    public int[] Triangles;

    //czworokat o ukladzie trojkatow (0,2,1), (0,3,2): mozna odwrocic jego przekatna
    public bool Quad;
}

public class BlockTemplate
{
    public Material Material;
    public FaceTemplate[] Faces = new FaceTemplate[CubeFaces.Count];
    public bool Transparent;
    public bool Water;
    public bool Passable;

    //ma fizyczne collidery (wszystko poza woda i blokami, przez ktore gracz przechodzi)
    public bool Solid;
}

//wyglad blokow do budowania siatek chunkow: sciany, UV i material czytane raz z prefabow z BlockMap (atlas z BuildBlockAtlas)
public class BlockTemplates
{
    private static readonly int[] QuadTriangles = { 0, 2, 1, 0, 3, 2 };

    private readonly Dictionary<string, BlockTemplate> templates = new Dictionary<string, BlockTemplate>();

    //tag, ktory prefaby nadawaja blokom do celowania
    public string Tag = "Untagged";

    public BlockTemplates(BlockMap map)
    {
        foreach (var entry in map.blockMap)
        {
            BlockTemplate template = Create(entry.Key, entry.Value);

            if (template != null)
                templates[entry.Key] = template;
        }
    }

    public BlockTemplate Get(string id)
    {
        templates.TryGetValue(id, out BlockTemplate template);

        return template;
    }

    private BlockTemplate Create(string id, Block block)
    {
        GameObject prefab = block.BlockPrefab;

        if (prefab == null)
            return null;

        MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>();
        MeshRenderer renderer = prefab.GetComponentInChildren<MeshRenderer>();

        if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null)
            return null;

        Mesh mesh = filter.sharedMesh;

        if (!mesh.isReadable)
        {
            Debug.LogWarning("Siatka bloku " + id + " nie jest czytelna (Read/Write), blok nie bedzie rysowany: " + mesh.name);

            return null;
        }

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;

        if (normals.Length != vertices.Length || uvs.Length != vertices.Length)
        {
            Debug.LogWarning("Siatka bloku " + id + " nie ma normalnych lub UV, blok nie bedzie rysowany: " + mesh.name);

            return null;
        }

        //przeksztalcenie siatki wzgledem korzenia prefabu
        Matrix4x4 matrix = Matrix4x4.identity;

        for (Transform t = filter.transform; t != null && t != prefab.transform; t = t.parent)
            matrix = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * matrix;

        List<int>[] faceTriangles = new List<int>[CubeFaces.Count];

        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            int face = CubeFaces.FromNormal(matrix.MultiplyVector(normals[triangles[i]]));

            if (faceTriangles[face] == null)
                faceTriangles[face] = new List<int>();

            faceTriangles[face].Add(triangles[i]);
            faceTriangles[face].Add(triangles[i + 1]);
            faceTriangles[face].Add(triangles[i + 2]);
        }

        BlockTemplate template = new BlockTemplate();

        template.Material = renderer.sharedMaterial;
        template.Transparent = BlockKind.IsTransparent(id);
        template.Water = Fluid.IsWater(id);
        template.Passable = block.IsPassable;
        template.Solid = !template.Water && !template.Passable;

        for (int face = 0; face < CubeFaces.Count; ++face)
            if (faceTriangles[face] != null)
                template.Faces[face] = CreateFace(faceTriangles[face], vertices, uvs, matrix);

        if (Tag == "Untagged")
            Tag = prefab.tag;

        return template;
    }

    //wierzcholki sciany w kolejnosci ich indeksow w siatce, wiec czworokat z BuildBlockAtlas zachowuje uklad (0,2,1), (0,3,2)
    private static FaceTemplate CreateFace(List<int> triangles, Vector3[] vertices, Vector2[] uvs, Matrix4x4 matrix)
    {
        List<int> unique = new List<int>();

        foreach (int index in triangles)
            if (!unique.Contains(index))
                unique.Add(index);

        unique.Sort();

        FaceTemplate face = new FaceTemplate();

        face.Positions = new Vector3[unique.Count];
        face.Uvs = new Vector2[unique.Count];
        face.Triangles = new int[triangles.Count];

        for (int i = 0; i < unique.Count; ++i)
        {
            face.Positions[i] = matrix.MultiplyPoint3x4(vertices[unique[i]]);
            face.Uvs[i] = uvs[unique[i]];
        }

        for (int i = 0; i < triangles.Count; ++i)
            face.Triangles[i] = unique.IndexOf(triangles[i]);

        face.Quad = unique.Count == 4 && face.Triangles.Length == QuadTriangles.Length;

        for (int i = 0; face.Quad && i < QuadTriangles.Length; ++i)
            face.Quad = face.Triangles[i] == QuadTriangles[i];

        return face;
    }
}
