using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [SerializeField] float length = 8.0f;

    [SerializeField] private string Tag="Selectable";
    [SerializeField] private Transform InCursor;

    private GameObject highlight;

    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private BlockMap blockMap;

    public void Update()
    {
        Highlight();
    }

    public void Build(GameObject prefab)
    {
        var blocks = mapGenerator.Blocks();
        var chunks = mapGenerator.Chunks();
        var mapOffset = mapGenerator.MapOffset();

        Vector3Int pos = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);

        if (Physics.Raycast(InCursor.position, InCursor.forward, out RaycastHit hitInfo, length * Vector3.Magnitude(InCursor.forward)))
        {
            if (hitInfo.transform.tag == Tag)
            {
                pos = new Vector3Int(Mathf.RoundToInt(hitInfo.point.x + hitInfo.normal.x / 2), Mathf.RoundToInt(hitInfo.point.y + hitInfo.normal.y / 2), Mathf.RoundToInt(hitInfo.point.z + hitInfo.normal.z / 2));
                Destroy(blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z]);
                blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z] = Instantiate(prefab, pos, Quaternion.identity);
            }
            else
            {
                pos = new Vector3Int(Mathf.RoundToInt(hitInfo.point.x), Mathf.RoundToInt(hitInfo.point.y), Mathf.RoundToInt(hitInfo.point.z));
                Destroy(blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z]);
                blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z] = Instantiate(prefab, pos, Quaternion.identity);
            }
        }

        var chunkBlocks = chunks[new Vector2Int(pos.x/16, pos.z/16)].Blocks;

        string id = "";

        foreach (var block in blockMap.blockMap.Keys)
            if (blockMap.blockMap[block].BlockPrefab == prefab)
                id = block;

        chunkBlocks[pos.x % 16, pos.y % 16, pos.z % 16] = id;

        Chunk chunk = new Chunk(new Vector2Int(pos.x / 16, pos.z / 16), chunkBlocks);

        mapGenerator.ChunkReload(chunk, new Vector2Int(pos.x / 16, pos.z / 16));
    }

    public void DestroyBlock()
    {
        var blocks = mapGenerator.Blocks();
        var mapOffset = mapGenerator.MapOffset();
        var chunks = mapGenerator.Chunks();

        if (Physics.Raycast(InCursor.position, InCursor.forward, out RaycastHit hitInfo, length * Vector3.Magnitude(InCursor.forward)))
            if (hitInfo.transform.tag == Tag)
            {
                Destroy(hitInfo.transform.gameObject);

                var chunkBlocks = chunks[new Vector2Int((int)hitInfo.transform.position.x / 16, (int)hitInfo.transform.position.z / 16)].Blocks;

                chunkBlocks[(int)hitInfo.transform.position.x % 16, (int)hitInfo.transform.position.y % 16, (int)hitInfo.transform.position.z % 16] = null;

                Chunk chunk = new Chunk(new Vector2Int((int)hitInfo.transform.position.x / 16, (int)hitInfo.transform.position.z / 16), chunkBlocks);

                mapGenerator.ChunkReload(chunk, new Vector2Int((int)hitInfo.transform.position.x / 16, (int)hitInfo.transform.position.z / 16));
            }
    }

    public void Highlight()
    {
        if (Physics.Raycast(InCursor.position, InCursor.forward, out RaycastHit hitInfo, length * Vector3.Magnitude(InCursor.forward)))
        {
            if (hitInfo.transform.tag == Tag)
            {
                if(highlight==null)
                {
                    highlight = hitInfo.transform.gameObject;
                    highlight.gameObject.GetComponent<Renderer>().material.color = hitInfo.transform.gameObject.GetComponent<BlockProperties>().HighlightedColor();
                }
                else if (highlight != hitInfo.transform.gameObject)
                {
                    highlight.GetComponent<Renderer>().material.color=highlight.GetComponent<BlockProperties>().BaseColor();
                    hitInfo.transform.gameObject.GetComponent<Renderer>().material.color=hitInfo.transform.gameObject.GetComponent<BlockProperties>().HighlightedColor();

                    highlight=hitInfo.transform.gameObject;
                }
            }
        }
    }
}
