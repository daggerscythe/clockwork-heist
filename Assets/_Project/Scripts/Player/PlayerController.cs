using UnityEngine;

// moves and turns the player
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;

    private const float MaxLookAngle = 85f;
    private const float EyeBelowTop = 0.2f; // m from top of head to the eyes
    private const float CrouchBlendSpeed = 8f; // m/s of height change
    private const float GroundedPull = -2f; // keeps player on ground
    private const float LeanSpeed = 5f; // full leans per s so one lean takes 0.2 s
    private const float CameraRadius = 0.15f; // m kept between the leaning camera and walls
    private const float MinVault = 0.5f; // m lower ledges are stepped or jumped onto instead
    private const float LedgeStep = 0.1f; // m between the checks forward for the edge of a ledge
    private const float LedgeProbe = 0.3f; // m above the highest ledge the downward check starts
    private const float TopFacing = 0.7f; // a ledge top must face up at least this much to stand on
    private const float VaultClearance = 0.05f; // m the feet clear the ledge before moving over it
    private const float RisePart = 0.6f; // share of the climb spent going up before going forward
    private const float SkinGap = 0.02f; // m the fit checks shrink the capsule so touching walls is fine

    private CharacterController controller;
    private PlayerControls controls;
    private Vector3 horizontalVelocity; // m/s
    private float verticalVelocity; // m/s
    private float pitch;
    private float currentHeight;
    private bool isLeaning; // true while the lean key is held
    private float lean; // -1 full left 0 upright 1 full right
    private int solidMask; // walls the leaning camera stops at and things a climb must fit between
    private int ledgeMask; // what can be climbed onto so never a guard's head or a small pickup
    private bool isVaulting; // true while climbing onto a ledge
    private bool vaultCrouch; // true while climbing into a space too low to stand in
    private float vaultTimer; // s into the current climb
    private Vector3 vaultStart; // feet where the climb began
    private Vector3 vaultEnd; // feet on top of the ledge
    private float vaultRiseY; // height the feet go up to before moving forward

    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

    // set while the player has a body on their shoulder
    public bool IsCarrying { get; set; }

    // set while the ability wheel is open so the mouse picks a slot instead of turning
    public bool LookLocked { get; set; }

    // true when player moving along the ground
    public bool IsMoving
    {
        get
        {
            Vector3 velocity = controller.velocity;
            return new Vector2(velocity.x, velocity.z).sqrMagnitude > 0.01f;
        }
    }

    // true while the feet are on the ground
    public bool IsGrounded => controller.isGrounded;

    // input shared with the other player scripts
    public PlayerControls Controls => controls;

    // set up the capsule from the settings
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();
        controller.radius = settings.capsuleRadius;
        currentHeight = settings.capsuleHeight;
        solidMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast");
        ledgeMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast", "Enemy", "Interactable");
        ApplyHeight(currentHeight);
    }

    // start listening to input
    private void OnEnable()
    {
        controls.Player.Enable();
    }

    // stop listening to input and drop any climb in progress
    private void OnDisable()
    {
        controls.Player.Disable();
        EndVault();
    }

    // free the input object
    private void OnDestroy()
    {
        controls.Dispose();
    }

    // hide and lock the mouse
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // look then crouch and lean then move or climb and place the camera last
    private void Update()
    {
        Look();
        UpdateCrouch();
        UpdateLean();
        if (isVaulting) UpdateVault();
        else Move();
        PlaceCamera();
    }

    // mouse look
    private void Look()
    {
        if (LookLocked) return;
        Vector2 delta = controls.Player.Look.ReadValue<Vector2>() * settings.lookSensitivity * GameOptions.Sensitivity;

        // turn the body left and right
        transform.Rotate(0f, delta.x, 0f);

        // tilt up and down and stop at the limit and the camera gets it in place camera
        pitch = Mathf.Clamp(pitch - delta.y, -MaxLookAngle, MaxLookAngle);
    }

    // crouch while the key is held and stay crouched if something is above
    private void UpdateCrouch()
    {
        bool wantsCrouch = controls.Player.Crouch.IsPressed() || vaultCrouch;
        IsCrouching = wantsCrouch || (IsCrouching && !CanStandUp());

        // change the height smoothly toward the target
        float targetHeight = IsCrouching ? settings.crouchCapsuleHeight : settings.capsuleHeight;
        currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, CrouchBlendSpeed * Time.deltaTime);
        ApplyHeight(currentHeight);
    }

    // hold the lean key and press a or d to lean without moving the feet
    private void UpdateLean()
    {
        isLeaning = controls.Player.Lean.IsPressed();
        float side = controls.Player.Move.ReadValue<Vector2>().x;

        float target = 0f;
        if (isLeaning && side > 0.1f) target = 1f;
        else if (isLeaning && side < -0.1f) target = -1f;

        // leaning is part of looking so slow motion never slows it
        lean = Mathf.MoveTowards(lean, target, LeanSpeed * Time.unscaledDeltaTime);
    }

    // walking, sprinting, jumping and gravity
    private void Move()
    {
        // leaning plants the feet so a and d only lean
        Vector2 input = isLeaning ? Vector2.zero : controls.Player.Move.ReadValue<Vector2>();

        // sprint only works while moving and not crouched or carrying
        IsSprinting = controls.Player.Run.IsPressed() && !IsCrouching && !IsCarrying && input.sqrMagnitude > 0.01f;

        // pick the speed for the current state and carrying a body is as slow as crouching
        float speed = settings.playerWalkSpeed;
        if (IsCrouching || IsCarrying) speed = settings.playerCrouchSpeed;
        else if (IsSprinting) speed = settings.playerSprintSpeed;

        // build the direction from the input and cap it so diagonals are not faster
        Vector3 wanted = transform.right * input.x + transform.forward * input.y;
        wanted = Vector3.ClampMagnitude(wanted, 1f) * speed;

        // full control on the ground and slow steering in the air
        if (controller.isGrounded)
            horizontalVelocity = wanted;
        else
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wanted, settings.airAcceleration * Time.deltaTime);

        // stay stuck to the ground after landing
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = GroundedPull;

        // jump next to a ledge climbs it instead and works in the air too
        if (controls.Player.Jump.WasPressedThisFrame() && TryVault()) return;

        // jump only from the ground and not while crouched
        if (controller.isGrounded && !IsCrouching && controls.Player.Jump.WasPressedThisFrame())
            verticalVelocity = Mathf.Sqrt(-2f * Physics.gravity.y * settings.jumpHeight);

        // gravity pulls us down every frame
        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    // puts the camera at eye height then slides and tilts it for the lean
    private void PlaceCamera()
    {
        Vector3 eye = new Vector3(0f, currentHeight - EyeBelowTop, 0f);
        float direction = Mathf.Sign(lean);
        float reach = Mathf.Abs(lean) * settings.leanDistance;

        // stop short of walls so the camera never pokes through to the other side
        Vector3 side = transform.right * direction;
        if (reach > 0f && Physics.SphereCast(transform.TransformPoint(eye), CameraRadius, side, out RaycastHit hit, reach, solidMask, QueryTriggerInteraction.Ignore))
            reach = hit.distance;

        cameraTransform.localPosition = eye + Vector3.right * direction * reach;
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, -lean * settings.leanAngle);
    }

    // checks the space above with the standing capsule
    private bool CanStandUp()
    {
        float radius = settings.capsuleRadius - 0.02f;
        Vector3 bottom = transform.position + Vector3.up * (radius + 0.05f);
        Vector3 top = transform.position + Vector3.up * (settings.capsuleHeight - radius);
        int mask = ~LayerMask.GetMask("Player");
        return !Physics.CheckCapsule(bottom, top, radius, mask, QueryTriggerInteraction.Ignore);
    }

    // starts climbing onto a ledge in front and returns false when there is none
    public bool TryVault()
    {
        if (isVaulting || !FindLedge(out Vector3 end, out float riseY, out bool crouched)) return false;
        isVaulting = true;
        vaultCrouch = crouched;
        vaultTimer = 0f;
        vaultStart = transform.position;
        vaultEnd = end;
        vaultRiseY = riseY;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        // shrink at once so the capsule fits through the low gap on the way up
        if (crouched)
        {
            IsCrouching = true;
            currentHeight = settings.crouchCapsuleHeight;
            ApplyHeight(currentHeight);
        }
        return true;
    }

    // goes straight up first then forward over the edge
    private void UpdateVault()
    {
        vaultTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(vaultTimer / settings.vaultTime);

        Vector3 wanted;
        if (progress < RisePart)
        {
            float up = Mathf.SmoothStep(0f, 1f, progress / RisePart);
            wanted = new Vector3(vaultStart.x, Mathf.Lerp(vaultStart.y, vaultRiseY, up), vaultStart.z);
        }
        else
        {
            float over = Mathf.SmoothStep(0f, 1f, (progress - RisePart) / (1f - RisePart));
            Vector3 top = new Vector3(vaultStart.x, vaultRiseY, vaultStart.z);
            Vector3 end = new Vector3(vaultEnd.x, vaultRiseY, vaultEnd.z);
            wanted = Vector3.Lerp(top, end, over);
        }

        // the controller still collides so a guard stepping in the way only stops the climb short
        controller.Move(wanted - transform.position);
        if (progress >= 1f) EndVault();
    }

    // hands control back to walking and gravity drops the last bit onto the ledge
    private void EndVault()
    {
        isVaulting = false;
        vaultCrouch = false;
    }

    // looks in front for the nearest ledge top the player can reach and fit on
    private bool FindLedge(out Vector3 end, out float riseY, out bool crouched)
    {
        end = Vector3.zero;
        riseY = 0f;
        crouched = false;
        Vector3 feet = transform.position;
        Vector3 forward = transform.forward;
        float radius = controller.radius;

        // walk forward until the check finds the edge
        for (float distance = radius + LedgeStep; distance <= radius + settings.vaultReach; distance += LedgeStep)
        {
            if (!FindTop(feet + forward * distance, feet.y, out Vector3 edge)) continue;

            // the feet end up a capsule width past the edge so the whole player stands on top
            if (!FindTop(feet + forward * (distance + radius), feet.y, out Vector3 landing)) return false;
            riseY = Mathf.Max(edge.y, landing.y) + VaultClearance;

            // stand on top if there is room or crouch under a low ceiling
            end = landing;
            Vector3 above = landing + Vector3.up * VaultClearance;
            crouched = !CapsuleFits(above, settings.capsuleHeight);
            float height = crouched ? settings.crouchCapsuleHeight : settings.capsuleHeight;
            if (crouched && !CapsuleFits(above, height)) return false;

            // the way straight up over the spot the climb starts from has to be open too
            return CapsuleFits(new Vector3(feet.x, riseY, feet.z), height);
        }
        return false;
    }

    // checks down onto a point for a flat top between the lowest and highest climbable height above the feet
    private bool FindTop(Vector3 point, float feetY, out Vector3 top)
    {
        top = Vector3.zero;
        Vector3 start = new Vector3(point.x, feetY + settings.vaultHeight + LedgeProbe, point.z);

        // a wall taller than the highest ledge has no top to grab
        if (Physics.CheckSphere(start, SkinGap, ledgeMask, QueryTriggerInteraction.Ignore)) return false;
        if (!Physics.Raycast(start, Vector3.down, out RaycastHit hit, settings.vaultHeight + LedgeProbe, ledgeMask, QueryTriggerInteraction.Ignore)) return false;

        float rise = hit.point.y - feetY;
        if (hit.normal.y < TopFacing || rise < MinVault || rise > settings.vaultHeight) return false;
        top = hit.point;
        return true;
    }

    // true when a capsule of this height with its feet here touches nothing
    private bool CapsuleFits(Vector3 feet, float height)
    {
        float radius = controller.radius - SkinGap;
        Vector3 bottom = feet + Vector3.up * (radius + SkinGap);
        Vector3 top = feet + Vector3.up * (height - radius);
        return !Physics.CheckCapsule(bottom, top, radius, solidMask, QueryTriggerInteraction.Ignore);
    }

    // sets the capsule height and keeps it standing on the floor
    private void ApplyHeight(float height)
    {
        controller.height = height;
        controller.center = new Vector3(0f, height / 2f, 0f);
    }

    // moves the player instantly and drops all momentum
    public void TeleportTo(Vector3 position)
    {
        // the controller would snap the player back if it stayed on
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        EndVault();
    }

    // drops all ground speed so a switched off controller reports the player as standing still
    public void Stop()
    {
        horizontalVelocity = Vector3.zero;
        IsSprinting = false;
        controller.Move(Vector3.zero);
    }
}