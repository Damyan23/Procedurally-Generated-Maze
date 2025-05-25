using UnityEngine;

/// <summary>
/// Controls the camera for navigating and viewing the maze scene.
/// Supports movement, rotation, zoom, and auto-fitting to the maze bounds.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 20f;
    public float mouseSensitivity = 2f;
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 50f;
    
    [Header("Auto Fit Settings")]
    public float fitPadding = 5f;
    public float fitTransitionSpeed = 2f;
    
    private Camera cam;
    private bool isTransitioning = false;
    private Vector3 targetPosition;
    private float targetFOV;
    private float rotationX = 0f;
    private float rotationY = 0f;
    
    void Start()
    {
        // Get the Camera component or fallback to Camera.main
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        // Initialize rotation based on current camera rotation
        Vector3 currentRotation = transform.eulerAngles;
        rotationY = currentRotation.y;
        rotationX = currentRotation.x;
        
        // Lock cursor for better camera control
        Cursor.lockState = CursorLockMode.None;
    }
    
    void Update()
    {
        // If camera is auto-fitting, handle transition
        if (isTransitioning)
        {
            HandleAutoFitTransition();
        }
        else
        {
            // Otherwise, handle manual input
            HandleInput();
        }
    }
    
    /// <summary>
    /// Handles all camera input: movement, rotation, zoom, and cursor lock.
    /// </summary>
    private void HandleInput()
    {
        // Toggle cursor lock with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }
        
        // Only handle camera controls if cursor is locked or right mouse button is held
        bool shouldControlCamera = Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1);
        
        if (shouldControlCamera)
        {
            HandleMouseLook();
        }
        
        HandleMovement();
        HandleZoom();
    }
    
    /// <summary>
    /// Handles mouse look for rotating the camera.
    /// </summary>
    private void HandleMouseLook()
    {
        // Get mouse movement
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Update rotation values
        rotationY += mouseX;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        
        // Apply rotation to the camera
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
    
    /// <summary>
    /// Handles WASD and QE movement for the camera.
    /// </summary>
    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;
        
        // WASD movement
        if (Input.GetKey(KeyCode.W))
            moveDirection += transform.forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection -= transform.forward;
        if (Input.GetKey(KeyCode.A))
            moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D))
            moveDirection += transform.right;
        
        // QE for up/down movement
        if (Input.GetKey(KeyCode.Q))
            moveDirection += Vector3.down;
        if (Input.GetKey(KeyCode.E))
            moveDirection += Vector3.up;
        
        // Speed modifier
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        
        // Move the camera
        transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
    }
    
    /// <summary>
    /// Handles zooming in and out with the mouse scroll wheel.
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (cam.orthographic)
            {
                // Adjust orthographic size
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
            else
            {
                // Adjust field of view
                cam.fieldOfView -= scroll * zoomSpeed * 10f;
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 10f, 120f);
            }
        }
    }
    
    /// <summary>
    /// Fits the camera to view the entire maze, with smooth transition.
    /// </summary>
    public void FitCameraToMaze(HexGridGenerator gridController)
    {
        if (gridController == null || gridController.Grid == null)
        {
            Debug.LogWarning("GridController or HexGrid is null!");
            return;
        }
        
        // Calculate maze bounds
        Bounds mazeBounds = CalculateMazeBounds(gridController);
        
        if (mazeBounds.size == Vector3.zero)
        {
            Debug.LogWarning("Maze bounds are zero!");
            return;
        }
        
        // Calculate optimal camera position and settings
        Vector3 mazeCenter = mazeBounds.center;
        float mazeWidth = mazeBounds.size.x;
        float mazeDepth = mazeBounds.size.z;
        float mazeHeight = mazeBounds.size.y;
        
        // Position camera above and slightly behind the maze center
        float distance = Mathf.Max(mazeWidth, mazeDepth) + fitPadding;
        float height = mazeHeight + distance * 0.7f;
        
        targetPosition = new Vector3(mazeCenter.x, height, mazeCenter.z - distance * 0.3f);
        
        // Set camera to look down at the maze at an angle
        Vector3 lookDirection = (mazeCenter - targetPosition).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        
        // Extract rotation values for smooth transition
        Vector3 eulerAngles = lookRotation.eulerAngles;
        rotationX = eulerAngles.x;
        rotationY = eulerAngles.y;
        
        // Calculate optimal field of view or orthographic size
        if (cam.orthographic)
        {
            targetFOV = Mathf.Max(mazeWidth, mazeDepth) * 0.6f + fitPadding;
        }
        else
        {
            float requiredFOV = 2f * Mathf.Atan((Mathf.Max(mazeWidth, mazeDepth) * 0.5f) / distance) * Mathf.Rad2Deg;
            targetFOV = Mathf.Clamp(requiredFOV + 10f, 30f, 100f);
        }
        
        // Start transition
        isTransitioning = true;
        
        Debug.Log($"Fitting camera to maze. Bounds: {mazeBounds}, Target Position: {targetPosition}");
    }
    
    /// <summary>
    /// Calculates the bounds of the maze based on all cell positions.
    /// </summary>
    /// <returns>The bounds of the maze.</returns>
    private Bounds CalculateMazeBounds(HexGridGenerator gridController)
    {
        bool firstCell = true;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        
        // Iterate over all cells to find min and max positions
        for (int x = 0; x < gridController.GridWidth; x++)
        {
            for (int y = 0; y < gridController.GridHeight; y++)
            {
                Cell hex = gridController.Grid[x, y];
                if (hex != null)
                {
                    Vector3 pos = hex.transform.position;
                    
                    if (firstCell)
                    {
                        min = max = pos;
                        firstCell = false;
                    }
                    else
                    {
                        min = Vector3.Min(min, pos);
                        max = Vector3.Max(max, pos);
                    }
                }
            }
        }
        
        if (firstCell)
        {
            // No cells found, return empty bounds
            return new Bounds();
        }
        
        // Add some padding based on hex size
        float hexSize = gridController.CellSize;
        Vector3 padding = new Vector3(hexSize, gridController.Height, hexSize);
        
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min - padding, max + padding);
        
        return bounds;
    }
    
    /// <summary>
    /// Handles smooth transition of the camera to the target position, rotation, and FOV.
    /// </summary>
    private void HandleAutoFitTransition()
    {
        // Smoothly transition position
        transform.position = Vector3.Lerp(transform.position, targetPosition, fitTransitionSpeed * Time.deltaTime);
        
        // Smoothly transition rotation
        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, fitTransitionSpeed * Time.deltaTime);
        
        // Smoothly transition FOV size
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fitTransitionSpeed * Time.deltaTime);
        
        // Check if transition is complete
        float positionDistance = Vector3.Distance(transform.position, targetPosition);
        float rotationDistance = Quaternion.Angle(transform.rotation, Quaternion.Euler(rotationX, rotationY, 0f));
        float fovDistance = Mathf.Abs(cam.fieldOfView - targetFOV);
        
        if (positionDistance < 0.1f && rotationDistance < 1f && fovDistance < 0.1f)
        {
            isTransitioning = false;
            Debug.Log("Camera fit transition completed");
        }
    }
    
    /// <summary>
    /// Stops any ongoing camera transition.
    /// </summary>
    public void StopTransition()
    {
        isTransitioning = false;
    }
}