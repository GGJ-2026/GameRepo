using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Input Setup")]
    public InputAction moveAction;
    public InputAction lookAction;
    public InputAction jumpAction;
    public InputAction interactAction;
    public InputAction stabAction;

    [Header("Movement Parameters")]
    [SerializeField] private float _moveSpeed = 5.0f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _jumpHeight = 1.0f;
    [SerializeField] private float _standHeight = 3.0f;

    [Header("Look Parameters")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] public float _mouseSensitivity = 0.1f;
    [SerializeField] private float _lookXLimit = 85.0f;

    private CharacterController _characterController;
    private Vector3 _velocity;
    private float _rotationX = 0;
    
    private InteractionController _interactionController;

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        interactAction.Enable();
        stabAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        interactAction.Disable();
        stabAction.Disable();
    }

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _interactionController = GetComponentInChildren<InteractionController>();
        
        // Auto-assign camera if missing
        if (_cameraTransform == null)
        {
            _cameraTransform = GetComponentInChildren<Camera>()?.transform;
            if (_cameraTransform == null) _cameraTransform = Camera.main?.transform;
        }

        SetCursorState(locked: true);
        UpdateHeight(); // Set Initial Height
    }

    void Update()
    {
        // Enforce height in Update to allow Inspector tweaking at runtime
        UpdateHeight();

        bool isDialogActive = DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen;
        if (isDialogActive)
        {
            HandleDialogInput();
            ApplyGravityOnly(); 
        }
        else
        {
            HandleLook();
            HandleMovement();
            HandleInteraction();
        }
    }

    // --- Handler for Dialog State ---
    private void HandleDialogInput()
    {
        if (interactAction.WasPerformedThisFrame())
        {
            DialogManager.Instance.AdvanceDialog();
        }
    }

    private void HandleInteraction()
    {
        // Talk action (E key)
        if (interactAction.WasPerformedThisFrame())
        {
            if (_interactionController != null)
            {
                _interactionController.Interact();
            }
        }

        // Stab action (LMB / Attack)
        if (stabAction.WasPerformedThisFrame())
        {
            if (_interactionController != null)
            {
                _interactionController.StabTarget();
            }
        }
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float mouseX = lookInput.x * _mouseSensitivity;
        float mouseY = lookInput.y * _mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _rotationX += -mouseY;
        _rotationX = Mathf.Clamp(_rotationX, -_lookXLimit, _lookXLimit);
        
        if (_cameraTransform != null && _cameraTransform != transform)
        {
            _cameraTransform.localRotation = Quaternion.Euler(_rotationX, 0, 0);
        }
        else
        {
            // Fallback: Single object setup (apply pitch to self, preserve yaw)
            // Note: This applies rotation to the capsule which is non-ideal but functional
            transform.localRotation = Quaternion.Euler(_rotationX, transform.localEulerAngles.y, 0);
        }
    }

    private void HandleMovement()
    {
        Vector2 inputVector = moveAction.ReadValue<Vector2>();
        
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 move = right * inputVector.x + forward * inputVector.y;

        _characterController.Move(move * _moveSpeed * Time.deltaTime);

        ApplyGravityOnly();
    }

    private void ApplyGravityOnly()
    {
        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        // Only allow jumping if NOT in dialog
        bool isDialogActive = DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen;
        if (!isDialogActive && jumpAction.WasPerformedThisFrame() && _characterController.isGrounded)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void UpdateHeight()
    {
        // Force height and camera position
        if (Mathf.Abs(_characterController.height - _standHeight) > 0.01f)
        {
            _characterController.height = _standHeight;
            _characterController.center = new Vector3(0, _standHeight / 2.0f, 0);
        }

        if (_cameraTransform != null)
        {
            Vector3 camPos = _cameraTransform.localPosition;
            camPos.y = _standHeight * 0.9f; // Eye level
            _cameraTransform.localPosition = camPos;
        }
    }

    private void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}