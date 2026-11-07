using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BlockController : MonoBehaviour
{
    private const string ShaderName = "BKG/PixelBlock";

    [SerializeField] float length = 8.0f;

    [SerializeField] private string Tag="Selectable";
    [SerializeField] private Transform InCursor;

    //nakladka na zaznaczonym bloku: przyciemnia go (kolor z przezroczystoscia)
    [SerializeField] private Color highlightColor = new Color(0f, 0f, 0f, 0.3f);

    private readonly Collider[] overlaps = new Collider[16];
    private GameObject highlight;

    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private BlockMap blockMap;

    private void Start()
    {
        CreateHighlight();
    }

    public void Update()
    {
        Highlight();
    }

    //blok kladziemy w komorce przy trafionej scianie, tylko w wolnym miejscu (powietrze albo woda) i nie w graczu
    public void Build(string id)
    {
        if (!blockMap.blockMap.TryGetValue(id, out Block block) || block.BlockPrefab == null || !TryAimCell(out Vector3Int cell, out Vector3Int normal))
            return;

        Vector3Int pos = cell + normal;

        if (!mapGenerator.TryGetBlockAt(pos, out string current) || (current != null && !Fluid.IsWater(current)) || IntersectsPlayer(pos))
            return;

        mapGenerator.SetBlock(pos, id);
    }

    public void DestroyBlock()
    {
        if (!TryAimCell(out Vector3Int cell, out Vector3Int normal))
            return;

        if (!mapGenerator.TryGetBlockAt(cell, out string current) || current == null)
            return;

        mapGenerator.SetBlock(cell, null);
    }

    //komorka i id bloku, na ktory wskazuje celownik (do okna debugowania)
    public bool TryTarget(out Vector3Int cell, out string id)
    {
        id = null;

        if (!TryAimCell(out cell, out Vector3Int normal))
            return false;

        return mapGenerator.TryGetBlockAt(cell, out id) && id != null;
    }

    //id bloku, na ktory wskazuje celownik (null, gdy na zaden)
    public string TargetBlockId()
    {
        if (!TryAimCell(out Vector3Int cell, out Vector3Int normal))
            return null;

        mapGenerator.TryGetBlockAt(cell, out string id);

        return id;
    }

    //promien z celownika trafia tylko collidery z tagiem Tag (woda nie ma colliderow, wiec jest przezroczysta dla celowania)
    private bool TryAim(out RaycastHit hit)
    {
        return Physics.Raycast(InCursor.position, InCursor.forward, out hit, length * Vector3.Magnitude(InCursor.forward), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) && hit.transform.CompareTag(Tag);
    }

    //komorka trafionego bloku (punkt trafienia przesuniety o kawalek w glab, zeby krawedzie nie wybieraly sasiada) i os trafionej sciany
    private bool TryAimCell(out Vector3Int cell, out Vector3Int normal)
    {
        cell = Vector3Int.zero;
        normal = Vector3Int.zero;

        if (!TryAim(out RaycastHit hit))
            return false;

        Vector3 inside = hit.point + InCursor.forward.normalized * 0.01f;
        Vector3 n = hit.normal;

        cell = Vector3Int.RoundToInt(inside);

        if (Mathf.Abs(n.x) >= Mathf.Abs(n.y) && Mathf.Abs(n.x) >= Mathf.Abs(n.z))
            normal = new Vector3Int(n.x > 0f ? 1 : -1, 0, 0);
        else if (Mathf.Abs(n.y) >= Mathf.Abs(n.z))
            normal = new Vector3Int(0, n.y > 0f ? 1 : -1, 0);
        else
            normal = new Vector3Int(0, 0, n.z > 0f ? 1 : -1);

        return true;
    }

    private bool IntersectsPlayer(Vector3Int cell)
    {
        int count = Physics.OverlapBoxNonAlloc(cell, Vector3.one * 0.45f, overlaps, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; ++i)
            if (overlaps[i].transform.IsChildOf(transform))
                return true;

        return false;
    }

    //zaznaczony blok dostaje przezroczysta, ciemna nakladke (jeden wspolny szescian): bloki sa czescia siatki chunka, wiec nie da sie ich barwic osobno
    public void Highlight()
    {
        if (highlight == null)
            return;

        if (TryAimCell(out Vector3Int cell, out Vector3Int normal) && mapGenerator.TryGetBlockAt(cell, out string id) && id != null && !Fluid.IsWater(id))
        {
            highlight.transform.position = cell;
            highlight.SetActive(true);
        }
        else
            highlight.SetActive(false);
    }

    public void ClearHighlight()
    {
        if (highlight != null)
            highlight.SetActive(false);
    }

    private void CreateHighlight()
    {
        Shader shader = Shader.Find(ShaderName);

        if (shader == null)
            return;

        Material material = new Material(shader);

        material.color = highlightColor;
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = (int)RenderQueue.Transparent + 10;

        highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlight.name = "BlockHighlight";
        highlight.transform.localScale = Vector3.one * 1.004f;
        highlight.GetComponent<Collider>().enabled = false;

        MeshRenderer meshRenderer = highlight.GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        highlight.SetActive(false);
    }

    private void OnDisable()
    {
        ClearHighlight();
    }

    private void OnDestroy()
    {
        if (highlight != null)
            Destroy(highlight);
    }
}
