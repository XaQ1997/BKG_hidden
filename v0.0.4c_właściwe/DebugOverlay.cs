using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

//okno debugowania w stylu F3 z Minecrafta, otwierane i zamykane klawiszem F4: po lewej polozenie, kierunek, swiatlo, czas i celowany blok,
//po prawej wydajnosc, pamiec, chunki i sprzet; tekst jest odswiezany kilka razy na sekunde, rysowany przez IMGUI na wierzchu calego interfejsu
public class DebugOverlay : MonoBehaviour
{
    private const float RefreshInterval = 0.2f;

    private readonly List<string> left = new List<string>();
    private readonly List<string> right = new List<string>();

    private MapGenerator map;
    private ChunkManager chunkManager;
    private Controller controller;
    private BlockController blockController;
    private DayNightCycle dayNight;
    private GUIStyle style;
    private Texture2D background;
    private bool visible;
    private float refreshTimer;
    private float fpsTimer;
    private int frames;
    private float frameTime;
    private float fps;
    private float worstFrame;
    private float worstWindow;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.f4Key.wasPressedThisFrame)
        {
            visible = !visible;

            refreshTimer = RefreshInterval;
        }

        //fps liczone z czasu rzeczywistego (takze przy pauzie), w oknach po pol sekundy
        ++frames;

        fpsTimer += Time.unscaledDeltaTime;
        worstWindow = Mathf.Max(worstWindow, Time.unscaledDeltaTime);

        if (fpsTimer >= 0.5f)
        {
            fps = frames / fpsTimer;
            frameTime = fpsTimer / frames * 1000f;
            worstFrame = worstWindow * 1000f;
            frames = 0;
            fpsTimer = 0f;
            worstWindow = 0f;
        }

        if (!visible)
            return;

        refreshTimer += Time.unscaledDeltaTime;

        if (refreshTimer < RefreshInterval)
            return;

        refreshTimer = 0f;

        Refresh();
    }

    private void Refresh()
    {
        if (map == null)
            map = FindObjectOfType<MapGenerator>();

        if (chunkManager == null)
            chunkManager = FindObjectOfType<ChunkManager>();

        if (controller == null)
            controller = FindObjectOfType<Controller>();

        if (blockController == null)
            blockController = FindObjectOfType<BlockController>();

        if (dayNight == null)
            dayNight = FindObjectOfType<DayNightCycle>();

        left.Clear();
        right.Clear();

        left.Add("BKG " + Application.version + " (Unity " + Application.unityVersion + ")");
        left.Add(fps.ToString("0") + " fps (" + frameTime.ToString("0.0") + " ms, worst " + worstFrame.ToString("0.0") + " ms)");

        if (map != null)
            left.Add("Seed: " + map.Seed());

        left.Add("");

        Vector3 position = controller != null ? controller.transform.position : Vector3.zero;

        if (controller != null && map != null)
            AddPosition(position);

        Camera camera = ActiveCamera();

        if (camera != null)
            AddFacing(camera.transform.forward);

        if (dayNight != null)
        {
            float time = dayNight.TimeOfDay;
            int minutes = Mathf.FloorToInt(time * 24f * 60f);

            left.Add("Time: " + (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00") + " (" + time.ToString("0.000") + ")");
        }

        AddTarget();

        right.Add("Unity " + Application.unityVersion);
        right.Add("Mem: " + (Profiler.GetTotalAllocatedMemoryLong() / 1048576L) + " MB allocated, " + (System.GC.GetTotalMemory(false) / 1048576L) + " MB managed");

        if (map != null)
        {
            right.Add("");
            right.Add("Chunks: " + map.LoadedChunkCount() + " loaded / " + map.GeneratedChunkCount() + " generated");

            if (chunkManager != null)
                right.Add("Queued: " + chunkManager.QueuedGenerate() + " to generate, " + chunkManager.QueuedLoad() + " to load");

            right.Add("Meshes: " + map.MeshCount() + " sub-chunks, " + map.PendingMeshCount() + " pending");
            right.Add("Distance: render " + map.RenderDistance() + ", generate " + map.GenerateDistance());
        }

        right.Add("");
        right.Add("Display: " + Screen.width + "x" + Screen.height + (Screen.fullScreen ? " (fullscreen)" : ""));
        right.Add(SystemInfo.graphicsDeviceName);
        right.Add(SystemInfo.processorType + " (" + SystemInfo.processorCount + " threads)");
    }

    private void AddPosition(Vector3 position)
    {
        int offsetY = map.MapOffset().y;
        int x = Mathf.FloorToInt(position.x + 0.5f);
        int y = Mathf.FloorToInt(position.y + 0.5f) - offsetY;
        int z = Mathf.FloorToInt(position.z + 0.5f);

        left.Add("XYZ: " + position.x.ToString("0.000") + " / " + position.y.ToString("0.000") + " / " + position.z.ToString("0.000"));
        left.Add("Block: " + x + " " + y + " " + z);
        left.Add("Chunk: " + (x & 15) + " " + (y & 15) + " " + (z & 15) + " in " + (x >> 4) + " " + (y >> 4) + " " + (z >> 4));

        float light = map.SkyLight(x, y, z);

        left.Add("Sky light: " + light.ToString("0.00") + " (column top y " + map.ColumnTop(x, z) + ")");
    }

    //kierunki jak w Minecrafcie: poludnie to +Z, polnoc to -Z, wschod to +X, zachod to -X
    private void AddFacing(Vector3 forward)
    {
        float yaw = Mathf.Atan2(-forward.x, forward.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        string direction;

        if (Mathf.Abs(forward.z) >= Mathf.Abs(forward.x))
            direction = forward.z >= 0f ? "south (Towards positive Z)" : "north (Towards negative Z)";
        else
            direction = forward.x >= 0f ? "east (Towards positive X)" : "west (Towards negative X)";

        left.Add("Facing: " + direction + " (" + yaw.ToString("0.0") + " / " + pitch.ToString("0.0") + ")");
    }

    private void AddTarget()
    {
        if (blockController == null || map == null)
            return;

        if (blockController.TryTarget(out Vector3Int cell, out string id))
        {
            left.Add("");
            left.Add("Targeted Block: " + cell.x + ", " + (cell.y - map.MapOffset().y) + ", " + cell.z);

            if (map.GetBlockMap().blockMap.TryGetValue(id, out Block block))
                left.Add(id + " " + block.BlockName);
            else
                left.Add(id);
        }
    }

    private Camera ActiveCamera()
    {
        foreach (Camera camera in Camera.allCameras)
            if (camera.targetTexture == null && camera.isActiveAndEnabled)
                return camera;

        return null;
    }

    private void OnGUI()
    {
        if (!visible || Event.current.type != EventType.Repaint)
            return;

        if (style == null)
            CreateStyle();

        style.fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height / 45f));

        DrawColumn(left, false);
        DrawColumn(right, true);
    }

    //kazda linia na ciemnym, polprzezroczystym pasku jak w Minecrafcie; puste linie to odstepy
    private void DrawColumn(List<string> lines, bool alignRight)
    {
        float y = 2f;

        foreach (string line in lines)
        {
            if (line.Length == 0)
            {
                y += style.fontSize * 0.5f;

                continue;
            }

            Vector2 size = style.CalcSize(new GUIContent(line));
            float x = alignRight ? Screen.width - size.x - 6f : 2f;
            Rect rect = new Rect(x, y, size.x + 4f, size.y);

            GUI.DrawTexture(rect, background);
            GUI.Label(new Rect(rect.x + 2f, rect.y, size.x, size.y), line, style);

            y += size.y + 1f;
        }
    }

    private void CreateStyle()
    {
        style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleLeft;
        style.wordWrap = false;
        style.padding = new RectOffset(0, 0, 0, 0);
        style.margin = new RectOffset(0, 0, 0, 0);

        background = new Texture2D(1, 1);
        background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
        background.Apply();
    }

    private void OnDestroy()
    {
        if (background != null)
            Destroy(background);
    }
}
