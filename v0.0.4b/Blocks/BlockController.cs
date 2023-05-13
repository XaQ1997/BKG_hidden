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

    public void Update()
    {
        Highlight();
    }

    public void Build(GameObject prefab)
    {
        var chunks = this.GetComponent<MapGenerator>().Chunks();
        var blockMap = this.gameObject.GetComponent<BlockMap>().blockMap;

        if (Physics.Raycast(InCursor.position, InCursor.forward, out RaycastHit hitInfo, length*Vector3.Magnitude(InCursor.forward)))
        {
            if (hitInfo.transform.tag == Tag)
            {
                Vector3Int pos = new Vector3Int(Mathf.RoundToInt(hitInfo.point.x+hitInfo.normal.x/2), Mathf.RoundToInt(hitInfo.point.y + hitInfo.normal.y / 2), Mathf.RoundToInt(hitInfo.point.z + hitInfo.normal.z / 2));

                GameObject block = hitInfo.transform.gameObject;

                int? b = null;

                for (int i = 0; i < blockMap.Count; ++i)
                    if (blockMap[i].BlockPrefab == prefab)
                        b = i;

                Destroy(block);
                var blocks = chunks[new Vector2(pos.x / 16, pos.z / 16)].Blocks;
                blocks[pos.x % 16, pos.y, pos.z % 16] = b;

                chunks[new Vector2(pos.x / 16, pos.z / 16)] = new Chunk(new Vector2(pos.x / 16, pos.z / 16), blocks);
            }
            else
            {
                Vector3Int pos = new Vector3Int(Mathf.RoundToInt(hitInfo.point.x), Mathf.RoundToInt(hitInfo.point.y), Mathf.RoundToInt(hitInfo.point.z));
                
                GameObject block = hitInfo.transform.gameObject;

                int? b=null;

                for (int i = 0; i < blockMap.Count; ++i)
                    if (blockMap[i].BlockPrefab == prefab)
                        b = i;

                Destroy(block);
                var blocks = chunks[new Vector2(pos.x / 16, pos.z / 16)].Blocks;
                blocks[pos.x % 16, pos.y, pos.z % 16] = b;

                chunks[new Vector2(pos.x / 16, pos.z / 16)] = new Chunk(new Vector2(pos.x / 16, pos.z / 16), blocks);
            }
        }
    }

    public void DestroyBlock()
    {
        if (Physics.Raycast(InCursor.position, InCursor.forward, out RaycastHit hitInfo, length * Vector3.Magnitude(InCursor.forward)))
            if (hitInfo.transform.tag == Tag)
                Destroy(hitInfo.transform.gameObject);
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
