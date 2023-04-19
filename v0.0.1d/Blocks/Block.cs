using UnityEngine;

public class Block:Object
{
    private string Named;
    private GameObject prefab;

    public Block(){}

    public Block(string _name, GameObject _prefab)
    {
        Named = _name;
        prefab = _prefab;
    }

    public GameObject Prefab()
    {
        return prefab;
    }

    public string Name()
    {
        return Named;
    }
}