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
        var blocks = this.GetComponent<MapGenerator>().Blocks();
        var mapOffset = this.GetComponent<MapGenerator>().MapOffset();

        if (Physics.Raycast(InCursor.position, InCursor.forward, out RaycastHit hitInfo, length * Vector3.Magnitude(InCursor.forward)))
        {
            if (hitInfo.transform.tag == Tag)
            {
                Vector3Int pos = new Vector3Int(Mathf.RoundToInt(hitInfo.point.x + hitInfo.normal.x / 2), Mathf.RoundToInt(hitInfo.point.y + hitInfo.normal.y / 2), Mathf.RoundToInt(hitInfo.point.z + hitInfo.normal.z / 2));
                Destroy(blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z]);
                blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z] = Instantiate(prefab, pos, Quaternion.identity);
            }
            else
            {
                Vector3Int pos = new Vector3Int(Mathf.RoundToInt(hitInfo.point.x), Mathf.RoundToInt(hitInfo.point.y), Mathf.RoundToInt(hitInfo.point.z));
                Destroy(blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z]);
                blocks[pos.x + mapOffset.x, pos.y + mapOffset.y, pos.z + mapOffset.z] = Instantiate(prefab, pos, Quaternion.identity);
            }
        }
    }

    public void DestroyBlock()
    {
        var blocks = this.GetComponent<MapGenerator>().Blocks();
        var mapOffset = this.GetComponent<MapGenerator>().MapOffset();

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
