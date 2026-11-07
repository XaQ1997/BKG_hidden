using UnityEngine;

//spadajacy blok (np. zwir): porusza sie w dol z przyspieszeniem, a po dotarciu do podloza wraca do swiata jako zwykly blok
public class FallingBlock : MonoBehaviour
{
    private const float Gravity = 28f;

    private MapGenerator map;
    private string id;
    private int x;
    private int z;
    private int cell;
    private int landing;
    private float velocity;
    private float offsetY;

    public void Setup(MapGenerator generator, string blockId, int cellX, int cellY, int cellZ)
    {
        map = generator;
        id = blockId;
        x = cellX;
        z = cellZ;
        cell = cellY;
        offsetY = generator.MapOffset().y;
        landing = map.FindLanding(x, cell, z);
    }

    private void Update()
    {
        velocity += Gravity * Time.deltaTime;

        float y = transform.position.y - velocity * Time.deltaTime;
        int current = Mathf.RoundToInt(y - offsetY);

        if (current != cell || cell - landing <= 2)
        {
            cell = current;
            landing = map.FindLanding(x, cell, z);
        }

        if (y <= landing + offsetY)
        {
            map.LandFallingBlock(x, landing, z, id);

            Destroy(gameObject);

            return;
        }

        transform.position = new Vector3(x, y, z);
    }
}
