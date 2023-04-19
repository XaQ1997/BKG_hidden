using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockProperties : MonoBehaviour
{
    [SerializeField] private Color baseColor;
    [SerializeField] private Color highlightedColor;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Color BaseColor()
    {
        return baseColor;
    }

    public Color HighlightedColor()
    {
        return highlightedColor;
    }
}
