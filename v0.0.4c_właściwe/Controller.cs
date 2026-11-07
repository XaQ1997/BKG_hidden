using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public enum GameMode
{
    Observator,
    Creative
}

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
public class Controller : MonoBehaviour
{
    //basic arguments
    private float horizontalSpeed = 10f;
    private float verticalSpeed = 2f;

    //predkosc ruchu w blokach na sekunde (wejscie Move daje wektor o dlugosci 1)
    [SerializeField] private float moveSpeed = 5f;
    private bool gamePaused = false;
    private float scroll;
    private int nrSlot = 0;
    private float v;
    private float h;
    private bool isInventory = false;

    //scripts
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private BlockController blockController;

    //gravity arguments
    [SerializeField] private LayerMask layer;
    [SerializeField] private float jumpHeight = 1.25f;
    private bool isGrounded;
    private InputAction jumpAction;
    private Vector3 moveInput;

    private const float MaxFallSpeed = 50f;
    //promien kuli sprawdzajacej podloze: mniejszy niz polowa korpusu, zeby kula nie wystawala poza stopy gracza
    private const float GroundRadius = 0.2f;

    //podloze to blok maksymalnie tyle pod najnizszym colliderem gracza
    private const float GroundSkin = 0.1f;

    private Collider[] bodyColliders;

    //poziome normalne scian, z ktorymi kapsula styka sie w ostatnim kroku fizyki (kierunek od sciany do gracza)
    private readonly List<Vector3> wallNormals = new List<Vector3>();
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    private const float RepeatInterval = 0.2f;
    private float nextDestroy;
    private float nextBuild;

    private const float SwimUpSpeed = 3.5f;
    private const float SwimSinkSpeed = 1.2f;
    private const float SwimSlowdown = 0.6f;
    //grawitacja dziala w trybie Creative, dopoki gracz nie wzbije sie klawiszami gora/dol (E/C); spacja przywraca ja i skacze
    private bool isGravity = true;

    //controller arguments
    [SerializeField] private PlayerInput input;
    [SerializeField] private Rigidbody rb;

    //gamemode arguments
    [SerializeField] private GameMode gm = GameMode.Creative;

    //input system actions (z ControllerMap.inputactions, mapa Player)
    private InputAction lookAction;
    private InputAction scrollAction;
    private InputAction destroyAction;
    private InputAction buildAction;
    private InputAction killAction;
    private InputAction pauseAction;
    private InputAction inventoryAction;
    private InputAction pickAction;
    private InputAction[] slotActions = new InputAction[10];
    private InputAction[] cameraActions = new InputAction[3];
    private InventoryManager inventoryManager;

    //camera arguments
    [SerializeField] private GameObject firstPersonCamera;
    [SerializeField] private GameObject secondPersonCamera;
    [SerializeField] private GameObject thirdPersonCamera;

    //UI arguments
    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject controls;
    [SerializeField] private GameObject inventory;
    [SerializeField] private TMP_Text seedLabel;

    //animation arguments
    [SerializeField] private GameObject head;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private GameObject leftLeg;
    [SerializeField] private GameObject rightLeg;
    [SerializeField] private float swingAngle = 30f;
    [SerializeField] private float swingSpeed = 10f;
    [SerializeField] private float swingBlendSpeed = 6f;
    [SerializeField] private float bodyTurnSpeed = 540f;
    private float swingPhase;
    private float swingBlend;
    private Transform body;

    // Start is called before the first frame update
    void Awake()
    {
        Resume();

        //celownik i tlo paska to tylko grafika: nie moga przechwytywac klikniec przeznaczonych dla ekwipunku
        foreach (Graphic graphic in cursor.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        Graphic hudGraphic = hud.GetComponent<Graphic>();

        if (hudGraphic != null)
            hudGraphic.raycastTarget = false;

        rb.detectCollisions = true;

        WhitenVertexColors();

        //fizyke gracza tworza pudelka modelu postaci (twarz, korpus, rece, nogi) jako collidery dzieci obiektu z Rigidbody
        bodyColliders = GetComponentsInChildren<Collider>();

        //przy prawdziwej grawitacji gracz spada szybko, wiec zwykla (dyskretna) detekcja moglaby go przepuscic przez cienki teren
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        firstPersonCamera.SetActive(true);
        secondPersonCamera.SetActive(false);
        thirdPersonCamera.SetActive(false);

        body = leftHand.transform.parent;
    }

    void Start()
    {
        if (input == null)
            input = GetComponent<PlayerInput>();

        InputActionAsset actions = input.actions;

        lookAction = actions.FindAction("Look", true);
        scrollAction = actions.FindAction("Scroll", true);
        destroyAction = actions.FindAction("Destroy", true);
        buildAction = actions.FindAction("Build", true);
        killAction = actions.FindAction("Kill", true);
        pauseAction = actions.FindAction("Pause", true);
        inventoryAction = actions.FindAction("Inventory", true);
        pickAction = actions.FindAction("Pick", false);
        jumpAction = actions.FindAction("Jump", false);

        for (int i = 0; i < slotActions.Length; ++i)
            slotActions[i] = actions.FindAction("Slot" + (i + 1), true);

        for (int i = 0; i < cameraActions.Length; ++i)
            cameraActions[i] = actions.FindAction("Camera" + (i + 1), true);

        inventoryManager = GetComponent<InventoryManager>();

        if (inventoryManager == null)
            inventoryManager = gameObject.AddComponent<InventoryManager>();

        inventoryManager.Initialize(mapGenerator.GetBlockMap(), hud.transform, inventory.transform, () => nrSlot, slot => nrSlot = slot);

        //ustawienia w menu pauzy (suwak zasiegu renderowania)
        gameObject.AddComponent<SettingsMenu>().Initialize(mapGenerator, panel.transform);
    }

    // Update is called once per frame
    void Update()
    {
        scroll = scrollAction.ReadValue<Vector2>().y;

        if (gamePaused == false)
        {
            bool inWater = gm != GameMode.Observator && mapGenerator.IsLiquidAt(transform.position + Vector3.up * 0.5f);

            DayNightCycle.Underwater = mapGenerator.IsLiquidAt(head.transform.position);

            //ruch: poziomo wzdluz poziomych osi glowy (spojrzenie w gore/dol nie zwalnia), pionowo E/C; predkosc ustawiana co klatke,
            //zeby tarcie o podloze jej nie wygaszalo
            Vector3 movement = isInventory ? Vector3.zero : moveInput;

            if (movement.y != 0)
                isGravity = false;

            if (jumpAction != null && jumpAction.WasPressedThisFrame() && isInventory == false && gm != GameMode.Observator)
                isGravity = true;

            Vector3 right = Vector3.ProjectOnPlane(head.transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            Vector3 walk = (movement.x * right + movement.z * forward) * moveSpeed;

            //nie pchamy sie w sciane: skladowa ruchu w jej strone jest odrzucana, wiec gracz slizga sie wzdluz niej zamiast stawac w miejscu przez tarcie
            foreach (Vector3 wall in wallNormals)
            {
                float into = Vector3.Dot(walk, wall);

                if (into < 0f)
                    walk -= wall * into;
            }

            //pusta maska (domyslna w scenie) oznacza wszystkie warstwy; inaczej promien nie trafialby w nic
            int groundLayers = layer.value != 0 ? layer.value : Physics.DefaultRaycastLayers;

            isGrounded = !inWater && IsOnGround(groundLayers);

            rb.useGravity = isGravity && gm != GameMode.Observator && !inWater;

            if (inWater)
            {
                //plywanie: bez grawitacji, spacja wynurza, inaczej powolne opadanie
                bool swimUp = jumpAction != null && jumpAction.IsPressed();

                rb.velocity = new Vector3(walk.x * SwimSlowdown, swimUp ? SwimUpSpeed : -SwimSinkSpeed, walk.z * SwimSlowdown);
            }
            else
            {
                float y = rb.useGravity ? Mathf.Max(rb.velocity.y, -MaxFallSpeed) : movement.y * moveSpeed;

                //przytrzymana spacja skacze przy kazdym ladowaniu
                if (jumpAction != null && jumpAction.IsPressed() && isInventory == false && gm != GameMode.Observator && isGrounded && rb.velocity.y <= 0.1f)
                {
                    isGravity = true;
                    rb.useGravity = true;

                    y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
                }

                rb.velocity = new Vector3(walk.x, y, walk.z);
            }

            Vector2 lookDelta = isInventory ? Vector2.zero : lookAction.ReadValue<Vector2>();

            h = horizontalSpeed * lookDelta.x;
            v = verticalSpeed * lookDelta.y;

            head.transform.Rotate(0, h, 0, Space.World);
            head.transform.Rotate(-v, 0, 0, Space.Self);

            bool isMoving = rb.velocity.x != 0 || rb.velocity.z != 0;

            swingBlend = Mathf.MoveTowards(swingBlend, isMoving ? 1f : 0f, swingBlendSpeed * Time.deltaTime);

            if (isMoving)
                swingPhase += swingSpeed * Time.deltaTime;

            float swing = Mathf.Sin(swingPhase) * swingAngle * swingBlend;

            leftHand.transform.localRotation = Quaternion.Euler(swing, 0, 0);
            rightLeg.transform.localRotation = Quaternion.Euler(swing, 0, 0);

            leftLeg.transform.localRotation = Quaternion.Euler(-swing, 0, 0);
            rightHand.transform.localRotation = Quaternion.Euler(-swing, 0, 0);

            if (isMoving && body)
            {
                Vector3 look = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);

                if (look.sqrMagnitude > 0.0001f)
                    body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(look), bodyTurnSpeed * Time.deltaTime);
            }

            if (gm == GameMode.Observator)
            {
                rb.detectCollisions = false;

                cursor.SetActive(false);
                hud.SetActive(false);
            }
            else
            {
                rb.detectCollisions = true;

                //celownik znika, gdy otwarty jest ekwipunek (jest nad nim w hierarchii i zaslanialby klikniecia)
                if (firstPersonCamera.activeSelf == true && isInventory == false)
                    cursor.SetActive(true);

                hud.SetActive(true);
            }

            if (gm != GameMode.Observator && isInventory == false)
            {
                if (ActionReady(destroyAction, ref nextDestroy))
                    blockController.DestroyBlock();

                if (ActionReady(buildAction, ref nextBuild))
                {
                    string blockId = inventoryManager.BlockAt(nrSlot);

                    if (blockId != null)
                        blockController.Build(blockId);
                }

                if (pickAction != null && pickAction.WasPressedThisFrame())
                    inventoryManager.PickBlock(blockController.TargetBlockId());
            }

            if (killAction.IsPressed())
            {
                mapGenerator.TrySpawnHeight(out int height);

                gameSettings.Spawn(height);

                rb.velocity = Vector3.zero;
            }

            if (scroll != 0 && isInventory == false)
            {
                if (scroll > 0)
                    nrSlot--;
                else
                    nrSlot++;

                if (nrSlot > 9)
                    nrSlot = 0;
                if (nrSlot < 0)
                    nrSlot = 9;
            }

            //w ekwipunku cyfra nad blokiem w palecie wstawia go w ten slot, w innym miejscu wybiera slot
            for (int i = 0; i < slotActions.Length; ++i)
            {
                if (!slotActions[i].WasPressedThisFrame())
                    continue;

                string over = isInventory ? inventoryManager.Hovered() : null;

                if (over != null)
                    inventoryManager.SetSlot(i, over);
                else
                    nrSlot = i;
            }

            //ramka stoi na wybranym slocie (te same obiekty, na ktorych inventoryManager rysuje ikony i ktore klika)
            Vector3? slotPosition = inventoryManager != null ? inventoryManager.SlotPosition(nrSlot) : null;
            Vector3 pos = highlight.transform.localPosition;

            pos.x = slotPosition.HasValue ? slotPosition.Value.x : 88 * nrSlot - 396;
            highlight.transform.localPosition = pos;
        }

        if (pauseAction.WasPressedThisFrame())
        {
            if (gamePaused)
            {
                Resume();
            }
            else
            {
                if (isInventory == false)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Time.timeScale = 0;
                    gamePaused = true;

                    cursor.SetActive(false);
                    hud.SetActive(false);
                    panel.SetActive(true);

                    if (seedLabel)
                        seedLabel.text = "Seed: " + mapGenerator.Seed();
                }
                else
                    SetInventory(false);
            }
        }

        if (gm != GameMode.Observator && gamePaused == false && inventoryAction.WasPressedThisFrame())
            SetInventory(!isInventory);

        if(cameraActions[0].WasPressedThisFrame())
        {
            firstPersonCamera.SetActive(true);
            secondPersonCamera.SetActive(false);
            thirdPersonCamera.SetActive(false);

            cursor.SetActive(isInventory == false);
        }
        if(cameraActions[1].WasPressedThisFrame())
        {
            firstPersonCamera.SetActive(false);
            secondPersonCamera.SetActive(true);
            thirdPersonCamera.SetActive(false);

            cursor.SetActive(false);
        }
        if(cameraActions[2].WasPressedThisFrame())
        {
            firstPersonCamera.SetActive(false);
            secondPersonCamera.SetActive(false);
            thirdPersonCamera.SetActive(true);

            cursor.SetActive(false);
        }
    }

    //shader blokow mnozy swiatlo przez kolor wierzcholkow, a wbudowany szescian modelu postaci go nie ma, wiec kazdy dostaje wlasna kopie siatki z bialymi
    private void WhitenVertexColors()
    {
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.mesh;

            if (mesh == null || !mesh.isReadable || mesh.vertexCount == 0)
                continue;

            Color32[] colors = new Color32[mesh.vertexCount];

            for (int i = 0; i < colors.Length; ++i)
                colors[i] = new Color32(255, 255, 255, 255);

            mesh.colors32 = colors;
        }
    }

    private void FixedUpdate()
    {
        wallNormals.Clear();
    }

    private void OnCollisionEnter(Collision collision)
    {
        CollectWalls(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CollectWalls(collision);
    }

    private void CollectWalls(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; ++i)
        {
            ContactPoint contact = collision.GetContact(i);

            if (Mathf.Abs(contact.normal.y) >= 0.5f)
                continue;

            Vector3 away = transform.position - contact.point;

            away.y = 0f;

            if (away.sqrMagnitude > 0.0001f)
                wallNormals.Add(away.normalized);
        }
    }

    //czy pod graczem jest podloze: rzut kula w dol siega GroundSkin pod najnizszy collider (nogi), a trafienia we wlasne collidery gracza
    //i w sciany (normalna nie skierowana do gory) sie nie licza, wiec w powietrzu nie da sie skoczyc
    private bool IsOnGround(int layers)
    {
        Vector3 origin = transform.position;
        float bottom = origin.y;

        foreach (Collider collider in bodyColliders)
            if (collider != null && collider.enabled && !collider.isTrigger)
                bottom = Mathf.Min(bottom, collider.bounds.min.y);

        float distance = Mathf.Max(0.01f, origin.y - bottom - GroundRadius + GroundSkin);
        int count = Physics.SphereCastNonAlloc(origin, GroundRadius, Vector3.down, groundHits, distance, layers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; ++i)
            if (!groundHits[i].collider.transform.IsChildOf(transform) && groundHits[i].normal.y > 0.5f)
                return true;

        return false;
    }

    //ekwipunek jak w Minecrafcie: kursor wolny, gracz stoi w miejscu, a po zamknieciu wraca ruch z trzymanych klawiszy
    private void SetInventory(bool open)
    {
        isInventory = open;

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        inventory.SetActive(open);
        cursor.SetActive(!open && firstPersonCamera.activeSelf);

        if (!open)
            inventoryManager.Close();
    }

    //klikniecie wykonuje akcje od razu, a przytrzymanie powtarza ja co RepeatInterval (a nie w kazdej klatce)
    private static bool ActionReady(InputAction action, ref float next)
    {
        if (!action.WasPressedThisFrame() && !(action.IsPressed() && Time.time >= next))
            return false;

        next = Time.time + RepeatInterval;

        return true;
    }

    public void Resume()
    {
        //przycisk, ktory zamknal menu, nie moze od razu zburzyc ani postawic bloku
        nextDestroy = nextBuild = Time.time + RepeatInterval;

        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
        gamePaused = false;

        cursor.SetActive(true);
        hud.SetActive(true);
        controls.SetActive(false);
        panel.SetActive(false);
        inventory.SetActive(false);
    }

    public void Reset()
    {
        mapGenerator.CleanMap();
        mapGenerator.InitGame();

        rb.velocity = Vector3.zero;

        Resume();
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector3>();
    }

    //skok obsluguje Update (odpytywanie akcji Jump); metoda zostaje, bo jest podpieta w zdarzeniach PlayerInput w scenie
    public void OnJump(InputAction.CallbackContext ctx)
    {
    }

    public void ChangeGM()
    {
        if (gm == GameMode.Creative)
            gm = GameMode.Observator;
        else
            gm = GameMode.Creative;
    }
}