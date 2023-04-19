using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockController : MonoBehaviour
{[SerializeField] float length = 8.0f;

    [SerializeField] private string Tag="Selectable";
    private Transform selection;

    public void Build()
    {
        var prefab=this.GetComponent<BlockMap>().blockMap[0x20].BlockPrefab;
        var blocks = this.GetComponent<MapGenerator>().Blocks();
        var mapOffset = this.GetComponent<MapGenerator>().MapOffset();

        if (selection != null)
            selection = null;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, length))
        {
            var position = hit.transform.position - transform.forward;

            var select = hit.transform.GetComponent<MeshRenderer>();

            hit.transform.position=Vector3Int.FloorToInt(position);

            if (select.CompareTag(Tag))
            {
                var block = hit.transform.gameObject;

                Destroy(block);

                block = Instantiate(prefab, position, Quaternion.identity);

                blocks[(int)position.x + mapOffset.x, (int)position.y + mapOffset.y, (int)position.z + mapOffset.z] = block;

                if (selection != null)
                {
                    select = block.GetComponent<MeshRenderer>();

                    selection = null;
                }
            }
        }
    }

    public void DestroyBlock()
    {
        var prefab = this.GetComponent<BlockMap>().blockMap[0x10].BlockPrefab;
        var blocks = this.GetComponent<MapGenerator>().Blocks();
        var mapOffset = this.GetComponent<MapGenerator>().MapOffset();

        if (selection != null)
            selection = null;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, length))
        {
            var position = hit.transform.position;

            var select = hit.transform.GetComponent<MeshRenderer>();

            if (select.CompareTag(Tag))
            {
                var block = hit.transform.gameObject;

                Destroy(block);

                block = Instantiate(prefab, position, Quaternion.identity);

                blocks[(int)position.x + mapOffset.x, (int)position.y + mapOffset.y, (int)position.z + mapOffset.z] = block;

                if (selection != null)
                {
                    select = block.GetComponent<MeshRenderer>();

                    selection = null;
                }
            }
        }
    }
}
