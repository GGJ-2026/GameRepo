using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFlyCam : MonoBehaviour
{
    [Header("Settings")]
    public float lookSensitivity = 0.2f;
    public float moveSpeed = 10f;
    public float boostMultiplier = 3f; // Hold Shift to move faster

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Lock cursor for standard FPS/Fly control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        if (Mouse.current == null) return;

        // Direct Read: No Action Asset needed
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        rotationY += mouseDelta.x * lookSensitivity;
        rotationX -= mouseDelta.y * lookSensitivity;

        // Clamp vertical look to avoid flipping
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        // Base speed
        float speed = moveSpeed * Time.deltaTime;

        // Check for Boost (Shift)
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            speed *= boostMultiplier;
        }

        Vector3 move = Vector3.zero;

        // Direct Read: WASD
        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;
        
        // Optional: Q/E for Up/Down
        if (Keyboard.current.eKey.isPressed) move += transform.up;
        if (Keyboard.current.qKey.isPressed) move -= transform.up;

        transform.position += move * speed;
    }
}