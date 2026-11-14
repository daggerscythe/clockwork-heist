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

    private CharacterController controller;
    private PlayerControls controls;
    private Vector3 horizontalVelocity; // m/s
    private float verticalVelocity; // m/s
    private float pitch;
    private float currentHeight;
    private bool isLeaning; // true while the lean key is held
    private float lean; // -1 full left 0 upright 1 full right
    private int cameraMask; // walls the leaning camera stops at

    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

    // set while the player has a body on their shoulder
    public bool IsCarrying { get; set; }

    // true when player moving along the ground
    public bool IsMoving
    {
        get
        {
            Vector3 velocity = controller.velocity;
            return new Vector2(velocity.x, velocity.z).sqrMagnitude > 0.01f;
        }
    }

    // input shared with the other player scripts
    public PlayerControls Controls => controls;

    // set up the capsule from the settings
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();
        controller.radius = settings.capsuleRadius;
        currentHeight = settings.capsuleHeight;
        cameraMask = ~LayerMask.GetMask("Player", "LightVolume", "Ignore Raycast");
        ApplyHeight(currentHeight);
    }

    // start listening to input
    private void OnEnable()
    {
        controls.Player.Enable();
    }

    // stop listening to input
    private void OnDisable()
    {
        controls.Player.Disable();
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

    // look then crouch and lean then move and place the camera last
    private void Update()
    {
        Look();
        UpdateCrouch();
        UpdateLean();
        Move();
        PlaceCamera();
    }

    // mouse look
    private void Look()
    {
        Vector2 delta = controls.Player.Look.ReadValue<Vector2>() * settings.lookSensitivity;

        // turn the body left and right
        transform.Rotate(0f, delta.x, 0f);

        // tilt up and down and stop at the limit and the camera gets it in place camera
        pitch = Mathf.Clamp(pitch - delta.y, -MaxLookAngle, MaxLookAngle);
    }

    // crouch while the key is held and stay crouched if something is above
    private void UpdateCrouch()
    {
        bool wantsCrouch = controls.Player.Crouch.IsPressed();
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
        if (reach > 0f && Physics.SphereCast(transform.TransformPoint(eye), CameraRadius, side, out RaycastHit hit, reach, cameraMask, QueryTriggerInteraction.Ignore))
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
    }
}