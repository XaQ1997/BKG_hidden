using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    private static readonly int NightDarknessId = Shader.PropertyToID("_BKG_NightDarkness");

    //0 = polnoc, 0.25 = wschod slonca, 0.5 = poludnie, 0.75 = zachod
    [SerializeField] private float dayLengthSeconds = 1440f;
    [Range(0f, 1f)] [SerializeField] private float timeOfDay = 0.3f;

    [SerializeField] private Color daySky = new Color(0.62f, 0.78f, 0.95f);
    [SerializeField] private Color nightSky = new Color(0.02f, 0.03f, 0.09f);
    [SerializeField] private Color twilightSky = new Color(0.95f, 0.55f, 0.3f);
    [SerializeField] private Color dayAmbient = new Color(0.75f, 0.78f, 0.85f);
    [SerializeField] private Color nightAmbient = new Color(0.06f, 0.07f, 0.14f);

    //ustawiane przez gracza: kamera jest pod woda
    public static bool Underwater;

    [SerializeField] private Color waterSky = new Color(0.1f, 0.25f, 0.45f);
    [SerializeField] private float waterFogEnd = 20f;

    //slonce i ksiezyc: kwadraty z pikselowa tekstura na sferze wokol kamery, widoczne tylko nad horyzontem
    [SerializeField] private float sunSize = 70f;
    [SerializeField] private float moonSize = 55f;

    private const float BodyDistance = 450f;

    //kierunek (obrot wokol osi Y), w ktorym slonce przechodzi przez niebo: 270 = z wschodu (+X) na zachod (-X) przez zenit, jak w Minecrafcie
    private const float SkyYaw = 270f;

    //gwiazdy: nieruchome wzgledem nieba punkty, ktore obracaja sie razem ze sloncem i ksiezycem, widoczne w nocy
    private const int StarCount = 900;
    private const float StarDistance = 420f;
    private const float StarMinSize = 1.4f;
    private const float StarMaxSize = 3.2f;

    //chmury: plaska warstwa na stalej wysokosci swiata (nad zwyklym terenem, w srodku wysokich gor), kolor zalezy od pory dnia
    [SerializeField] private float cloudHeight = 620f;
    [SerializeField] private Color dayCloud = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color nightCloud = new Color(0.1f, 0.12f, 0.2f, 0.85f);
    [SerializeField] private Color twilightCloud = new Color(1f, 0.62f, 0.45f, 0.9f);

    private static Mesh quad;
    private static Mesh starMesh;
    private static Mesh cloudMesh;

    private Camera[] cameras;
    private Light sun;
    private float fogEnd;
    private MapGenerator map;
    private Transform sunBody;
    private Transform moonBody;
    private Material sunMaterial;
    private Material moonMaterial;
    private Transform starBody;
    private Transform cloudBody;
    private Material starMaterial;
    private Material cloudMaterial;

    public float TimeOfDay
    {
        get { return timeOfDay; }
        set { timeOfDay = Mathf.Repeat(value, 1f); }
    }

    private void Start()
    {
        map = FindObjectOfType<MapGenerator>();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = AmbientMode.Flat;

        cameras = FindObjectsOfType<Camera>(true);

        foreach (Camera camera in cameras)
            camera.clearFlags = CameraClearFlags.SolidColor;

        sun = RenderSettings.sun;

        if (sun == null)
        {
            foreach (Light light in FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;

                    break;
                }
            }
        }

        sunBody = CreateBody("Sun", QuadMesh(), out sunMaterial);
        moonBody = CreateBody("Moon", QuadMesh(), out moonMaterial);
        starBody = CreateBody("Stars", StarMesh(), out starMaterial);
        cloudBody = CreateBody("Clouds", CloudMesh(), out cloudMaterial);

        Apply();
    }

    private void Update()
    {
        timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / dayLengthSeconds, 1f);

        Apply();
    }

    private void OnDestroy()
    {
        if (sunMaterial != null)
            Destroy(sunMaterial);

        if (moonMaterial != null)
            Destroy(moonMaterial);

        if (starMaterial != null)
            Destroy(starMaterial);

        if (cloudMaterial != null)
            Destroy(cloudMaterial);

        Shader.SetGlobalFloat(NightDarknessId, 0f);

        Underwater = false;
    }

    private void Apply()
    {
        float elevation = Mathf.Sin((timeOfDay - 0.25f) * 2f * Mathf.PI);
        float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.2f, 0.3f, elevation));
        float twilight = Mathf.Clamp01(1f - Mathf.Abs(elevation) / 0.3f);

        Color sky = Color.Lerp(nightSky, daySky, daylight);

        sky = Color.Lerp(sky, twilightSky, twilight * 0.6f);

        if (Underwater)
            sky = waterSky * Mathf.Lerp(0.15f, 1f, daylight);

        //mgla konczy sie tuz za ostatnim wczytanym chunkiem, wiec nadaza za zmiana zasiegu w menu
        fogEnd = map != null ? (map.RenderDistance() + 1f) * 16f : 64f;

        RenderSettings.fogStartDistance = Underwater ? 0f : fogEnd * 0.6f;
        RenderSettings.fogEndDistance = Underwater ? waterFogEnd : fogEnd;

        foreach (Camera camera in cameras)
            if (camera != null)
                camera.backgroundColor = sky;

        RenderSettings.fogColor = sky;
        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, daylight);

        Shader.SetGlobalFloat(NightDarknessId, 1f - daylight);

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler((timeOfDay - 0.25f) * 360f, SkyYaw, 0f);
            sun.intensity = daylight;
        }

        //kierunek do slonca jest przeciwny do kierunku swiatla; ksiezyc jest po przeciwnej stronie nieba
        Vector3 toSun = -(Quaternion.Euler((timeOfDay - 0.25f) * 360f, SkyYaw, 0f) * Vector3.forward);

        UpdateBodies(toSun, elevation, daylight, twilight);
    }

    //materialy (Resources/Sun, Moon, Stars, Clouds) maja wlasne shadery bez mgly i testu glebokosci, rysowane przed terenem
    private Transform CreateBody(string resource, Mesh mesh, out Material material)
    {
        Material source = Resources.Load<Material>(resource);

        material = null;

        if (source == null)
            return null;

        material = new Material(source);

        GameObject body = new GameObject(resource, typeof(MeshFilter), typeof(MeshRenderer));

        body.transform.SetParent(transform, false);
        body.GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer meshRenderer = body.GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return body.transform;
    }

    private static Mesh QuadMesh()
    {
        if (quad != null)
            return quad;

        quad = new Mesh();
        quad.name = "SkyQuad";
        quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
        quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        quad.RecalculateBounds();

        return quad;
    }

    //900 gwiazd rozmieszczonych rownomiernie na sferze, kazda to maly kwadrat zwrocony ku srodkowi; jasnosc w kolorze wierzcholkow (alfa)
    private static Mesh StarMesh()
    {
        if (starMesh != null)
            return starMesh;

        System.Random random = new System.Random(20240611);
        List<Vector3> vertices = new List<Vector3>(StarCount * 4);
        List<Color> colors = new List<Color>(StarCount * 4);
        List<int> triangles = new List<int>(StarCount * 6);

        for (int i = 0; i < StarCount; ++i)
        {
            double u = random.NextDouble() * 2.0 - 1.0;
            double angle = random.NextDouble() * System.Math.PI * 2.0;
            double ring = System.Math.Sqrt(1.0 - u * u);
            Vector3 normal = new Vector3((float)(ring * System.Math.Cos(angle)), (float)u, (float)(ring * System.Math.Sin(angle)));

            float sizeRoll = (float)random.NextDouble();
            float half = Mathf.Lerp(StarMinSize, StarMaxSize, sizeRoll * sizeRoll * sizeRoll) * 0.5f;
            float bright = Mathf.Lerp(0.45f, 1f, (float)random.NextDouble());
            float warm = (float)random.NextDouble();
            Color color = Color.Lerp(new Color(0.8f, 0.88f, 1f), new Color(1f, 0.95f, 0.8f), warm);

            color.a = bright;

            Vector3 right = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up).normalized * half;
            Vector3 up = Vector3.Cross(normal, right).normalized * half;
            Vector3 center = normal * StarDistance;
            int start = vertices.Count;

            vertices.Add(center - right - up);
            vertices.Add(center + right - up);
            vertices.Add(center + right + up);
            vertices.Add(center - right + up);

            for (int c = 0; c < 4; ++c)
                colors.Add(color);

            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 2);
        }

        starMesh = new Mesh();
        starMesh.name = "Stars";
        starMesh.SetVertices(vertices);
        starMesh.SetColors(colors);
        starMesh.SetTriangles(triangles, 0);
        starMesh.bounds = new Bounds(Vector3.zero, Vector3.one * StarDistance * 2.5f);

        return starMesh;
    }

    //pozioma plaszczyzna 1x1 (skalowana do kilku tysiecy), srodek w poczatku ukladu
    private static Mesh CloudMesh()
    {
        if (cloudMesh != null)
            return cloudMesh;

        cloudMesh = new Mesh();
        cloudMesh.name = "CloudPlane";
        cloudMesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) };
        cloudMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        cloudMesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.01f, 1f));

        return cloudMesh;
    }

    private Camera ActiveCamera()
    {
        foreach (Camera camera in cameras)
            if (camera != null && camera.isActiveAndEnabled)
                return camera;

        return null;
    }

    private void UpdateBodies(Vector3 toSun, float elevation, float daylight, float twilight)
    {
        Camera camera = ActiveCamera();

        if (camera == null)
            return;

        PlaceBody(sunBody, sunMaterial, camera.transform.position, toSun, sunSize, elevation);
        PlaceBody(moonBody, moonMaterial, camera.transform.position, -toSun, moonSize, -elevation);

        UpdateStars(camera.transform.position, daylight);
        UpdateClouds(camera.transform.position, daylight, twilight);
    }

    //gwiazdy pojawiaja sie, gdy dzien gasnie, i obracaja sie jak slonce (ta sama os i kat)
    private void UpdateStars(Vector3 origin, float daylight)
    {
        if (starBody == null)
            return;

        float visibility = Underwater ? 0f : Mathf.Clamp01((0.5f - daylight) / 0.5f);

        starBody.gameObject.SetActive(visibility > 0.01f);

        if (visibility <= 0.01f)
            return;

        starBody.position = origin;
        starBody.rotation = Quaternion.Euler((timeOfDay - 0.25f) * 360f, SkyYaw, 0f);

        Color color = starMaterial.color;

        color.a = visibility;
        starMaterial.color = color;
    }

    //warstwa chmur podaza za kamera tylko w poziomie (wzor jest w wspolrzednych swiata, wiec chmury stoja na niebie i plyna z wiatrem)
    private void UpdateClouds(Vector3 origin, float daylight, float twilight)
    {
        if (cloudBody == null)
            return;

        cloudBody.gameObject.SetActive(!Underwater);

        if (Underwater)
            return;

        cloudBody.position = new Vector3(origin.x, cloudHeight, origin.z);
        cloudBody.localScale = new Vector3(2000f, 1f, 2000f);

        cloudMaterial.color = Color.Lerp(Color.Lerp(nightCloud, dayCloud, daylight), twilightCloud, twilight * 0.5f);
    }

    //cialo jest zawsze w tej samej odleglosci od kamery (jak prawdziwe niebo), zwrocone ku niej, a przy horyzoncie znika (pod horyzontem nie ma go gdzie rysowac)
    private void PlaceBody(Transform body, Material material, Vector3 origin, Vector3 direction, float size, float elevation)
    {
        if (body == null)
            return;

        float visibility = Underwater ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.05f, 0.1f, elevation));

        body.gameObject.SetActive(visibility > 0.01f);

        if (visibility <= 0.01f)
            return;

        body.position = origin + direction * BodyDistance;
        body.rotation = Quaternion.LookRotation(direction, Mathf.Abs(direction.y) > 0.99f ? Vector3.forward : Vector3.up);
        body.localScale = Vector3.one * size;

        Color color = material.color;

        color.a = visibility;
        material.color = color;
    }
}
