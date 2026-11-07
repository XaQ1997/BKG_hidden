using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

//ekwipunek trybu kreatywnego (jak w Minecrafcie): pasek 10 slotow trzyma id blokow z BlockMap, a panel to paleta wszystkich blokow
//klik w paletce podnosi blok na kursor, klik w slot paska odklada go (lub zamienia z tym, co w slocie), klik w tle (albo w ten sam blok w paletce) wyrzuca blok z kursora, a w inny blok w paletce podmienia go
//shift + klik w paletce wrzuca blok do pierwszego wolnego slotu, a cyfra nad blokiem w paletce wstawia go w dany slot
//klikniecia i najechanie sa liczone wprost z pozycji kursora nad prostokatami komorek (a nie przez EventSystem), wiec zadna inna grafika interfejsu ich nie zaslania
public class InventoryManager : MonoBehaviour
{
    public const int HotbarSize = 10;

    private const int Columns = 8;
    private const float CellSize = 56f;
    private const float Spacing = 8f;

    //obiekty slotow w HUD; "FirstBlock" lezy po prawej, a "TenthBlock" po lewej, wiec slot 0 (klawisz 1) to obiekt z konca listy
    private static readonly string[] HotbarNames = { "FirstBlock", "SecondBlock", "ThirdBlock", "FourthBlock", "FifthBlock", "SixthBlock", "SeventhBlock", "EigthBlock", "NinethBlock", "TenthBlock" };

    private readonly string[] hotbar = new string[HotbarSize];
    private readonly RawImage[] hotbarIcons = new RawImage[HotbarSize];

    private BlockMap map;
    private Func<int> selectedSlot;
    private Action<int> selectSlot;
    //podpowiedz z nazwa bloku przy kursorze (jak w Minecrafcie): ciemne tlo i jasny tekst, czytelne na kazdym tle
    private RectTransform tooltip;
    private Text tooltipText;
    private string shownId;

    private string held;
    private string hovered;
    private RawImage heldIcon;
    private Canvas canvas;
    private Transform panelRoot;
    private bool overSlot;

    private readonly RectTransform[] hotbarRects = new RectTransform[HotbarSize];
    private readonly List<RectTransform> paletteRects = new List<RectTransform>();
    private readonly List<string> paletteIds = new List<string>();

    public string BlockAt(int slot)
    {
        return hotbar[slot];
    }

    //polozenie slotu w ukladzie HUD (do podswietlenia wybranego slotu); null, gdy slotu nie ma w scenie
    public Vector3? SlotPosition(int slot)
    {
        return hotbarRects[slot] != null ? hotbarRects[slot].localPosition : (Vector3?)null;
    }

    public void SetSlot(int slot, string id)
    {
        hotbar[slot] = id;

        RefreshHotbar();
    }

    //blok z palety, nad ktorym jest kursor (null poza paleta)
    public string Hovered()
    {
        return hovered;
    }

    public void Hover(string id)
    {
        hovered = id;

        ShowName(id);
    }

    public void ShowName(string id)
    {
        shownId = id;

        RefreshTooltip();
    }

    //nazwa i, mniejszym szarym drukiem, id bloku (odroznia warianty o tej samej nazwie); nic nie pokazujemy, gdy gracz niesie blok
    private void RefreshTooltip()
    {
        if (tooltip == null)
            return;

        bool show = shownId != null && held == null && panelRoot != null && panelRoot.gameObject.activeInHierarchy && map.blockMap.ContainsKey(shownId);

        tooltip.gameObject.SetActive(show);

        if (!show)
            return;

        string text = map.blockMap[shownId].BlockName + "\n<size=17><color=#a89cb8>" + shownId + "</color></size>";

        //HandlePointer wola to co klatke, a rozmiar tekstu liczymy tylko przy zmianie
        if (tooltipText.text == text)
            return;

        tooltipText.text = text;

        tooltip.sizeDelta = new Vector2(tooltipText.preferredWidth + 20f, tooltipText.preferredHeight + 12f);
    }

    //podpowiedz jest przy kursorze, po jego prawej i pod nim, a przy krawedzi ekranu przeskakuje na druga strone
    private void PlaceTooltip()
    {
        if (tooltip == null || !tooltip.gameObject.activeSelf || Pointer.current == null)
            return;

        Vector2 screen = Pointer.current.position.ReadValue();
        bool flipX = screen.x > Screen.width * 0.7f;
        bool flipY = screen.y < Screen.height * 0.15f;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        tooltip.pivot = new Vector2(flipX ? 1f : 0f, flipY ? 0f : 1f);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, screen + new Vector2(flipX ? -18f : 18f, flipY ? 18f : -18f), camera, out Vector3 world))
            tooltip.position = world;
    }

    private void BuildTooltip()
    {
        GameObject box = new GameObject("BlockTooltip", typeof(RectTransform), typeof(Image));

        box.transform.SetParent(canvas.transform, false);

        tooltip = box.GetComponent<RectTransform>();
        tooltip.anchorMin = tooltip.anchorMax = new Vector2(0.5f, 0.5f);

        Image background = box.GetComponent<Image>();

        background.color = new Color(0.07f, 0.03f, 0.12f, 0.94f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));

        textObject.transform.SetParent(tooltip, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);

        //czcionka systemowa (dynamiczna) ma polskie znaki, a domyslny atlas TextMeshPro ich nie ma
        tooltipText = textObject.GetComponent<Text>();
        tooltipText.font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "DejaVu Sans", "Segoe UI" }, 24);
        tooltipText.fontSize = 24;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAnchor.UpperLeft;
        tooltipText.horizontalOverflow = HorizontalWrapMode.Overflow;
        tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        tooltipText.supportRichText = true;
        tooltipText.raycastTarget = false;

        tooltip.gameObject.SetActive(false);
    }

    public void ClickPalette(string id)
    {
        //ten sam blok co na kursorze: odloz go; inny blok zastepuje to, co jest na kursorze (klik nigdy nie jest "stracony")
        if (held == id)
            SetHeld(null);
        else if (IsShiftPressed())
            SetSlot(FirstEmptySlot(), id);
        else
            SetHeld(id);
    }

    //slot dostaje blok z kursora, a jego dotychczasowa zawartosc trafia na kursor
    public void ClickSlot(int slot)
    {
        string previous = hotbar[slot];

        hotbar[slot] = held;

        SetHeld(previous);
        RefreshHotbar();
    }

    public void DropHeld()
    {
        SetHeld(null);
    }

    //zamkniecie ekwipunku odklada blok z kursora
    public void Close()
    {
        SetHeld(null);
        Hover(null);
    }

    //srodkowy przycisk myszy na bloku w swiecie: blok z paska jest wybierany, inny trafia do pierwszego wolnego slotu (albo zastepuje wybrany)
    public void PickBlock(string id)
    {
        if (id == null || !map.blockMap.ContainsKey(id))
            return;

        int slot = Array.IndexOf(hotbar, id);

        if (slot < 0)
        {
            slot = FirstEmptySlot();

            SetSlot(slot, id);
        }

        selectSlot(slot);
    }

    //pasek startuje pusty, gracz uzupelnia go z palety w panelu ekwipunku
    public void Initialize(BlockMap blockMap, Transform hud, Transform panel, Func<int> selected, Action<int> select)
    {
        map = blockMap;
        selectedSlot = selected;
        selectSlot = select;

        EnsureUiInput();

        //tlo ekwipunku lezy pod HUD-em, zeby pasek byl widoczny nad nim
        panelRoot = panel;
        panel.SetAsFirstSibling();

        BuildHotbarIcons(hud);
        BuildPalette(panel);
        BuildHeldIcon(panel);

        RefreshHotbar();
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.gameObject.activeInHierarchy)
            HandlePointer();

        FollowPointer();
        PlaceTooltip();
    }

    //komorka pod kursorem: slot paska ma pierwszenstwo przed paletka; klik poza nimi wyrzuca blok z kursora
    private void HandlePointer()
    {
        Pointer pointer = Pointer.current;

        if (pointer == null || canvas == null)
            return;

        Vector2 screen = pointer.position.ReadValue();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        int slot = -1;
        string id = null;

        for (int i = 0; i < HotbarSize && slot < 0; ++i)
            if (hotbarRects[i] != null && RectTransformUtility.RectangleContainsScreenPoint(hotbarRects[i], screen, camera))
                slot = i;

        for (int i = 0; i < paletteRects.Count && slot < 0 && id == null; ++i)
            if (RectTransformUtility.RectangleContainsScreenPoint(paletteRects[i], screen, camera))
                id = paletteIds[i];

        if (slot >= 0)
        {
            hovered = null;
            overSlot = true;

            ShowName(hotbar[slot]);
        }
        else if (id != hovered || overSlot)
        {
            overSlot = false;

            Hover(id);
        }

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (slot >= 0)
                ClickSlot(slot);
            else if (id != null)
                ClickPalette(id);
            else
                DropHeld();
        }
        else if (mouse.rightButton.wasPressedThisFrame && slot >= 0)
            SetSlot(slot, null);
    }

    //pierwszy wolny slot, a gdy pasek jest pelny wybrany
    private int FirstEmptySlot()
    {
        for (int i = 0; i < HotbarSize; ++i)
            if (hotbar[i] == null)
                return i;

        return selectedSlot();
    }

    private static bool IsShiftPressed()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    private void SetHeld(string id)
    {
        held = id;

        RefreshTooltip();

        if (heldIcon == null)
            return;

        SetIcon(heldIcon, held);

        heldIcon.transform.SetAsLastSibling();

        FollowPointer();
    }

    private void FollowPointer()
    {
        if (heldIcon == null || !heldIcon.enabled || Pointer.current == null)
            return;

        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, Pointer.current.position.ReadValue(), camera, out Vector3 world))
            heldIcon.rectTransform.position = world;
    }

    //blok niesiony na kursorze rysowany jest na wierzchu calego interfejsu
    private void BuildHeldIcon(Transform panel)
    {
        canvas = panel.GetComponentInParent<Canvas>();

        heldIcon = NewIcon("HeldBlock", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        heldIcon.rectTransform.sizeDelta = Vector2.one * (CellSize - 12f) * panel.localScale.x;
        heldIcon.enabled = false;

        BuildTooltip();
    }

    //EventSystem ze sceny ma StandaloneInputModule (stary Input); przy samym nowym Input System UI nie dostaje klikniec
    private void EnsureUiInput()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        EventSystem system = UnityEngine.Object.FindObjectOfType<EventSystem>();

        if (system == null || system.GetComponent<InputSystemUIInputModule>() != null)
            return;

        StandaloneInputModule standalone = system.GetComponent<StandaloneInputModule>();

        if (standalone != null)
            Destroy(standalone);

        system.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#endif
    }

    private void RefreshHotbar()
    {
        for (int i = 0; i < HotbarSize; ++i)
        {
            if (hotbarIcons[i] == null)
                continue;

            SetIcon(hotbarIcons[i], hotbar[i]);
        }
    }

    private void SetIcon(RawImage image, string id)
    {
        if (id == null || !TryGetIcon(map.blockMap[id].BlockPrefab, out Texture texture, out Rect uv))
        {
            image.enabled = false;

            return;
        }

        image.texture = texture;
        image.uvRect = uv;
        image.enabled = true;
    }

    //ikona to sciana +Z bloku: prostokat UV z mesha (atlas) albo cala tekstura (zwykly szescian)
    private static bool TryGetIcon(GameObject prefab, out Texture texture, out Rect uv)
    {
        texture = null;
        uv = new Rect(0f, 0f, 1f, 1f);

        if (prefab == null)
            return false;

        MeshRenderer renderer = prefab.GetComponentInChildren<MeshRenderer>();
        MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>();

        if (renderer == null || renderer.sharedMaterial == null || renderer.sharedMaterial.mainTexture == null)
            return false;

        texture = renderer.sharedMaterial.mainTexture;

        Mesh mesh = filter != null ? filter.sharedMesh : null;

        if (mesh != null && mesh.isReadable)
        {
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool found = false;

            for (int i = 0; i < normals.Length && i < uvs.Length; ++i)
            {
                if (normals[i].z > 0.5f)
                {
                    min = Vector2.Min(min, uvs[i]);
                    max = Vector2.Max(max, uvs[i]);

                    found = true;
                }
            }

            if (found)
                uv = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        return true;
    }

    private void BuildHotbarIcons(Transform hud)
    {
        for (int i = 0; i < HotbarSize; ++i)
        {
            Transform slot = hud.Find(HotbarNames[HotbarSize - 1 - i]);

            if (slot == null)
                continue;

            Graphic old = slot.GetComponent<Graphic>();

            if (old != null)
                old.enabled = false;

            RawImage icon = NewIcon("BlockIcon", slot, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero);

            hotbarIcons[i] = icon;

            hotbarRects[i] = slot as RectTransform;
        }
    }

    private static RawImage NewIcon(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 margin)
    {
        GameObject iconObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));

        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = margin;
        rect.offsetMax = -margin;

        RawImage image = iconObject.GetComponent<RawImage>();

        image.raycastTarget = false;

        return image;
    }

    private void BuildPalette(Transform panel)
    {
        List<string> ids = new List<string>();

        foreach (var block in map.blockMap)
            if (block.Key != "10" && block.Value.BlockPrefab != null && (!Fluid.IsWater(block.Key) || block.Key == Fluid.Source))
                ids.Add(block.Key);

        ids.Sort(string.CompareOrdinal);

        int rows = Mathf.Max(1, (ids.Count + Columns - 1) / Columns);
        float width = Columns * CellSize + (Columns - 1) * Spacing;
        float height = rows * CellSize + (rows - 1) * Spacing;

        GameObject gridObject = new GameObject("Palette", typeof(RectTransform), typeof(GridLayoutGroup));

        gridObject.transform.SetParent(panel, false);

        RectTransform gridRect = gridObject.GetComponent<RectTransform>();

        gridRect.anchorMin = gridRect.anchorMax = gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(width, height);
        gridRect.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();

        grid.cellSize = new Vector2(CellSize, CellSize);
        grid.spacing = new Vector2(Spacing, Spacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.childAlignment = TextAnchor.MiddleCenter;

        foreach (string id in ids)
        {
            GameObject cell = new GameObject(id, typeof(RectTransform), typeof(Image));

            cell.transform.SetParent(gridObject.transform, false);
            cell.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            cell.GetComponent<Image>().raycastTarget = false;

            paletteRects.Add(cell.GetComponent<RectTransform>());
            paletteIds.Add(id);

            RawImage icon = NewIcon("Icon", cell.transform, Vector2.zero, Vector2.one, new Vector2(6f, 6f));

            SetIcon(icon, id);
        }
    }
}
