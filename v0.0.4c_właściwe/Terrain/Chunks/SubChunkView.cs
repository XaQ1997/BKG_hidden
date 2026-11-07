using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//obiekt pod-chunka 16x16x16: jedna siatka (podsiatka na material) i pula collidera-pudel, zamiast obiektu na kazdy blok
//pozycja obiektu to rog pod-chunka, a komorka (x, y, z) lezy w jego ukladzie w punkcie (x, y, z) (blok ma srodek w calkowitych wspolrzednych)
public class SubChunkView : MonoBehaviour
{
    private static readonly Bounds LocalBounds = new Bounds(new Vector3(7.5f, 7.5f, 7.5f), new Vector3(17f, 17f, 17f));

    private readonly List<BoxCollider> boxes = new List<BoxCollider>();
    private Mesh mesh;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "SubChunk";

        meshFilter.sharedMesh = mesh;

        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }

    public void SetMesh(ChunkMesher mesher)
    {
        mesh.Clear();

        if (mesher.Vertices.Count == 0)
        {
            meshRenderer.enabled = false;

            return;
        }

        mesh.indexFormat = mesher.Vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(mesher.Vertices);
        mesh.SetNormals(mesher.Normals);
        mesh.SetUVs(0, mesher.Uvs);
        mesh.SetColors(mesher.Colors);
        mesh.subMeshCount = mesher.Materials.Count;

        for (int i = 0; i < mesher.Materials.Count; ++i)
            mesh.SetTriangles(mesher.Triangles[i], i, false);

        mesh.bounds = LocalBounds;

        meshRenderer.sharedMaterials = mesher.Materials.ToArray();
        meshRenderer.enabled = true;
    }

    public void SetColliders(List<ChunkMesher.Box> list)
    {
        for (int i = 0; i < list.Count; ++i)
        {
            if (i == boxes.Count)
                boxes.Add(gameObject.AddComponent<BoxCollider>());

            BoxCollider box = boxes[i];

            box.center = list[i].Center;
            box.size = list[i].Extent;
            box.isTrigger = list[i].Trigger;
            box.enabled = true;
        }

        for (int i = list.Count; i < boxes.Count; ++i)
            boxes[i].enabled = false;
    }
}
