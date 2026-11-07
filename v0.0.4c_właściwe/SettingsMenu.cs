using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//ustawienia gry w menu pauzy: suwak zasiegu renderowania od MapGenerator.MinRenderDistance do MaxRenderDistance co 0.5 chunka
//suwak sam liczy trafienie kursora w swoj prostokat (jak ekwipunek), wiec nie zalezy od EventSystem; wartosc jest stosowana po puszczeniu przycisku
public class SettingsMenu : MonoBehaviour
{
    private const float Step = 0.5f;
    private const float TrackWidth = 330f;
    private const float TrackHeight = 14f;
    private const float TrackY = 108f;
    private const float LabelY = 140f;

    //powyzej tych wartosci zasieg mocno obciaza pamiec i procesor (kolor etykiety ostrzega)
    private const float HeavyDistance = 8f;
    private const float ExtremeDistance = 16f;

    private MapGenerator map;
    private Transform menu;
    private Canvas canvas;
    private RectTransform track;
    private RectTransform hitArea;
    private RectTransform fill;
    private RectTransform handle;
    private TMP_Text label;
    private bool dragging;
    private bool wasOpen;
    private float value;

    public void Initialize(MapGenerator generator, Transform pauseMenu)
    {
        map = generator;
        menu = pauseMenu;
        canvas = pauseMenu.GetComponentInParent<Canvas>();

        track = NewRect("RenderDistanceTrack", new Vector2(TrackWidth, TrackHeight), new Vector2(0f, TrackY), typeof(Image));
        track.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        track.GetComponent<Image>().raycastTarget = false;

        fill = NewRect("Fill", Vector2.zero, Vector2.zero, typeof(Image), track);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0.5f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.3f, 0.7f, 0.35f, 1f);
        fill.GetComponent<Image>().raycastTarget = false;

        handle = NewRect("Handle", new Vector2(12f, 30f), Vector2.zero, typeof(Image), track);
        handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.GetComponent<Image>().color = Color.white;
        handle.GetComponent<Image>().raycastTarget = false;

        //obszar chwytu jest wyzszy niz pasek, zeby latwo go trafic
        hitArea = NewRect("RenderDistanceHit", new Vector2(TrackWidth + 30f, 40f), new Vector2(0f, TrackY));

        RectTransform labelRect = NewRect("RenderDistanceLabel", new Vector2(480f, 32f), new Vector2(0f, LabelY), typeof(TextMeshProUGUI));

        label = labelRect.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 22f;
        label.raycastTarget = false;

        Show(map.RenderDistance());
    }

    private RectTransform NewRect(string name, Vector2 size, Vector2 position, System.Type component = null, RectTransform parent = null)
    {
        GameObject created = component != null ? new GameObject(name, typeof(RectTransform), component) : new GameObject(name, typeof(RectTransform));

        created.transform.SetParent(parent != null ? parent : menu, false);

        RectTransform rect = created.GetComponent<RectTransform>();

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        return rect;
    }

    private void Update()
    {
        bool open = menu != null && menu.gameObject.activeInHierarchy;

        if (!open)
        {
            dragging = false;
            wasOpen = false;

            return;
        }

        //przy kazdym otwarciu menu pokazujemy aktualna wartosc
        if (!wasOpen)
        {
            wasOpen = true;

            Show(map.RenderDistance());
        }

        Pointer pointer = Pointer.current;
        Mouse mouse = Mouse.current;

        if (pointer == null || mouse == null)
            return;

        Vector2 screen = pointer.position.ReadValue();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        if (mouse.leftButton.wasPressedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(hitArea, screen, camera))
            dragging = true;

        if (!dragging)
            return;

        if (mouse.leftButton.isPressed)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(track, screen, camera, out Vector2 local))
            {
                float t = Mathf.InverseLerp(track.rect.xMin, track.rect.xMax, local.x);
                float raw = Mathf.Lerp(MapGenerator.MinRenderDistance, MapGenerator.MaxRenderDistance, t);

                Show(Mathf.Clamp(Mathf.Round(raw / Step) * Step, MapGenerator.MinRenderDistance, MapGenerator.MaxRenderDistance));
            }
        }
        else
        {
            dragging = false;

            map.SetRenderDistance(value);
        }
    }

    private void Show(float distance)
    {
        value = distance;

        float t = Mathf.InverseLerp(MapGenerator.MinRenderDistance, MapGenerator.MaxRenderDistance, distance);

        fill.anchorMax = new Vector2(t, 1f);
        handle.anchorMin = handle.anchorMax = new Vector2(t, 0.5f);
        handle.anchoredPosition = Vector2.zero;

        string hint = distance > ExtremeDistance ? "  (very heavy)" : distance > HeavyDistance ? "  (heavy)" : "";

        label.text = "Render distance: " + distance.ToString("0.0") + " chunks" + hint;
        label.color = distance > ExtremeDistance ? new Color(1f, 0.35f, 0.3f) : distance > HeavyDistance ? new Color(1f, 0.75f, 0.3f) : Color.white;
    }
}
