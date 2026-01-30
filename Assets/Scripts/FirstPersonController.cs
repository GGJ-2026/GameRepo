using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Input Setup")]
    public InputAction moveAction;
    public InputAction lookAction;
    public InputAction jumpAction;
    public InputAction crouchAction;
    public InputAction interactAction;

    [Header("Movement Parameters")]
    [SerializeField] private float _moveSpeed = 5.0f;
    [SerializeField] private float _crouchSpeed = 2.5f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _jumpHeight = 1.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float _crouchHeight = 1.0f;
    [SerializeField] private float _standHeight = 2.0f;
    [SerializeField] private float _crouchTransitionSpeed = 10.0f;

    [Header("Look Parameters")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _mouseSensitivity = 0.1f;
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
        crouchAction.Enable();
        interactAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        crouchAction.Disable();
        interactAction.Disable();
    }

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _interactionController = GetComponentInChildren<InteractionController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleCrouch();
        HandleInteraction();
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * _mouseSensitivity;
        float mouseY = lookInput.y * _mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _rotationX += -mouseY;
        _rotationX = Mathf.Clamp(_rotationX, -_lookXLimit, _lookXLimit);
        
        if (_cameraTransform != null)
            _cameraTransform.localRotation = Quaternion.Euler(_rotationX, 0, 0);
    }

    private void HandleCrouch()
    {
        // Read input - default to 0 if action is not set or not pressed
        bool isCrouching = crouchAction != null && crouchAction.ReadValue<float>() > 0.5f;
        
        float targetHeight = isCrouching ? _crouchHeight : _standHeight;
        float currentHeight = _characterController.height;
        
        // Use a simple lerp for height
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            float newHeight = Mathf.MoveTowards(currentHeight, targetHeight, _crouchTransitionSpeed * Time.deltaTime);
            
            _characterController.height = newHeight;
            _characterController.center = new Vector3(0, newHeight / 2.0f, 0);
        }
    }

    private void HandleMovement()
    {
        // Read Vector2 from the Move Action
        Vector2 inputVector = moveAction.ReadValue<Vector2>();

        Vector3 move = transform.right * inputVector.x + transform.forward * inputVector.y;

        float currentSpeed = (crouchAction != null && crouchAction.ReadValue<float>() > 0.5f) ? _crouchSpeed : _moveSpeed;
        _characterController.Move(move * currentSpeed * Time.deltaTime);

        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        // Check if Jump was pressed this frame
        if (jumpAction.WasPerformedThisFrame() && _characterController.isGrounded)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void HandleInteraction()
    {
        // Only try to interact if the key was pressed this frame
        if (interactAction.WasPerformedThisFrame())
        {
            if (_interactionController != null)
            {
                _interactionController.Interact();
            }
        }
    }
}