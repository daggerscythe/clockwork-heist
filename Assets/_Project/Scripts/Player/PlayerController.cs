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

    private CharacterController controller;
    private PlayerControls controls;
    private Vector3 horizontalVelocity; // m/s
    private float verticalVelocity; // m/s
    private float pitch;
    private float currentHeight;

    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

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

    private void Update()
    {
        Look();
        UpdateCrouch();
        Move();
    }

    // mouse look
    private void Look()
    {
        Vector2 delta = controls.Player.Look.ReadValue<Vector2>() * settings.lookSensitivity;

        // turn the body left and right
        transform.Rotate(0f, delta.x, 0f);

        // tilt the camera up and down and stop at the limit
        pitch = Mathf.Clamp(pitch - delta.y, -MaxLookAngle, MaxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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

    // walking, sprinting, jumping and gravity
    private void Move()
    {
        Vector2 input = controls.Player.Move.ReadValue<Vector2>();

        // sprint only works while moving and not crouched
        IsSprinting = controls.Player.Run.IsPressed() && !IsCrouching && input.sqrMagnitude > 0.01f;

        // pick the speed for the current state
        float speed = settings.playerWalkSpeed;
        if (IsCrouching) speed = settings.playerCrouchSpeed;
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

    // checks the space above with the standing capsule
    private bool CanStandUp()
    {
        float radius = settings.capsuleRadius - 0.02f;
        Vector3 bottom = transform.position + Vector3.up * (radius + 0.05f);
        Vector3 top = transform.position + Vector3.up * (settings.capsuleHeight - radius);
        int mask = ~LayerMask.GetMask("Player");
        return !Physics.CheckCapsule(bottom, top, radius, mask, QueryTriggerInteraction.Ignore);
    }

    // sets the capsule height and keeps the camera near the top of the head
    private void ApplyHeight(float height)
    {
        controller.height = height;
        controller.center = new Vector3(0f, height / 2f, 0f);
        cameraTransform.localPosition = new Vector3(0f, height - EyeBelowTop, 0f);
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