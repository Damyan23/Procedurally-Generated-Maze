using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the user interface for maze configuration, generation, and camera controls.
/// Handles UI events and updates grid/maze settings accordingly.
/// </summary>
public class MazeUIController : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private Slider widthSlider;
    [SerializeField] private TMP_Text widthValueText;
    [SerializeField] private Slider heightSlider;
    [SerializeField] private TMP_Text heightValueText;
    [SerializeField] private Toggle evenSizeToggle;
    [SerializeField] private TMP_InputField hexSizeInput;

    [Header("Generation Toggles")]
    [SerializeField] private Toggle instantGridToggle;
    [SerializeField] private Toggle instantMazeToggle;
    [SerializeField] private Toggle instantPathToggle;

    [Header("Buttons")]
    [SerializeField] private Button generateButton;
    [SerializeField] private Button regenerateButton;
    [SerializeField] private Button fitCameraButton;

    [Header("Dropdown for Algorithm")]
    [SerializeField] private TMP_Dropdown algorithmDropdown;

    [Header("Target References")]
    [SerializeField] private HexGridGenerator gridController;
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private CameraController cameraController;

    private const int MIN_SIZE = 1;
    private const int MAX_SIZE = 250;

    /// <summary>
    /// Initializes UI elements and populates the algorithm dropdown on start.
    /// </summary>
    private void Start()
    {
        // Validate references
        if (!ValidateReferences()) return;

        // Set slider min/max values
        widthSlider.minValue = MIN_SIZE;
        widthSlider.maxValue = MAX_SIZE;
        heightSlider.minValue = MIN_SIZE;
        heightSlider.maxValue = MAX_SIZE;

        // Set slider values to current grid size
        widthSlider.value = gridController.GridWidth;
        heightSlider.value = gridController.GridHeight;

        // Update text fields to show current grid size
        UpdateWidthText();
        UpdateHeightText();

        // Initialize hex size input
        if (hexSizeInput != null)
        {
            hexSizeInput.text = gridController.CellSize.ToString("F1");
        }

        // Initialize toggle states from components
        InitializeToggleStates();

        // Populate the algorithm dropdown with available algorithms
        PopulateAlgorithmDropdown();
    }

    /// <summary>
    /// Sets up UI event listeners.
    /// </summary>
    private void Awake()
    {
        SetupEventListeners();
    }

    /// <summary>
    /// Sets up all UI event listeners.
    /// </summary>
    private void SetupEventListeners()
    {
        // Button click listeners
        if (generateButton != null)
            generateButton.onClick.AddListener(OnGenerateClicked);
        if (fitCameraButton != null)
            fitCameraButton.onClick.AddListener(OnFitCameraClicked);

        // Slider and toggle listeners
        if (widthSlider != null)
            widthSlider.onValueChanged.AddListener(OnWidthSliderChanged);
        if (heightSlider != null)
            heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
        if (evenSizeToggle != null)
            evenSizeToggle.onValueChanged.AddListener(OnToggleEvenChanged);

        // Hex size input listener
        if (hexSizeInput != null)
            hexSizeInput.onEndEdit.AddListener(OnHexSizeChanged);

        // Generation toggle listeners
        if (instantGridToggle != null)
            instantGridToggle.onValueChanged.AddListener(OnInstantGridToggleChanged);
        if (instantMazeToggle != null)
            instantMazeToggle.onValueChanged.AddListener(OnInstantMazeToggleChanged);
        if (instantPathToggle != null)
            instantPathToggle.onValueChanged.AddListener(OnInstantPathToggleChanged);

        // Algorithm dropdown listener
        if (algorithmDropdown != null)
            algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);
    }

    /// <summary>
    /// Validates that all required references are set.
    /// </summary>
    private bool ValidateReferences()
    {
        if (gridController == null)
        {
            Debug.LogError("GridController reference is missing in MazeUIController!");
            return false;
        }
        if (mazeGenerator == null)
        {
            Debug.LogError("MazeGenerator reference is missing in MazeUIController!");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Initializes toggle states based on current component settings.
    /// </summary>
    private void InitializeToggleStates()
    {
        if (gridController != null && instantGridToggle != null)
        {
            instantGridToggle.isOn = gridController.InstantGeneration;
        }

        if (mazeGenerator != null)
        {
            if (instantMazeToggle != null)
                instantMazeToggle.isOn = mazeGenerator.InstantMazeGeneration;
            if (instantPathToggle != null)
                instantPathToggle.isOn = mazeGenerator.InstantPathDrawing;
        }
    }

    /// <summary>
    /// Updates the width text display.
    /// </summary>
    private void UpdateWidthText()
    {
        if (widthValueText != null)
            widthValueText.text = gridController.GridWidth.ToString();
    }

    /// <summary>
    /// Updates the height text display.
    /// </summary>
    private void UpdateHeightText()
    {
        if (heightValueText != null)
            heightValueText.text = gridController.GridHeight.ToString();
    }

    /// <summary>
    /// Handles changes to the width slider and updates grid width.
    /// </summary>
    private void OnWidthSliderChanged(float val)
    {
        int width = Mathf.RoundToInt(val);
        gridController.GridWidth = width;
        UpdateWidthText();

        // If even size is toggled, update height to match width
        if (evenSizeToggle != null && evenSizeToggle.isOn)
        {
            gridController.GridHeight = width;
            if (heightSlider != null)
                heightSlider.value = width;
            UpdateHeightText();
        }
    }

    /// <summary>
    /// Handles changes to the height slider and updates grid height.
    /// </summary>
    private void OnHeightSliderChanged(float val)
    {
        int height = Mathf.RoundToInt(val);
        
        if (evenSizeToggle == null || !evenSizeToggle.isOn)
        {
            // If not even, just update height
            gridController.GridHeight = height;
        }
        else
        {
            // If even, update width to match height
            gridController.GridWidth = height;
            gridController.GridHeight = height;
            if (widthSlider != null)
                widthSlider.value = height;
            UpdateWidthText();
        }
        UpdateHeightText();
    }

    /// <summary>
    /// Handles toggling of the even size option, syncing width and height if enabled.
    /// </summary>
    private void OnToggleEvenChanged(bool isOn)
    {
        if (isOn)
        {
            int width = Mathf.RoundToInt(widthSlider.value);
            gridController.GridHeight = width;
            if (heightSlider != null)
                heightSlider.value = width;
            UpdateHeightText();
        }
    }

    /// <summary>
    /// Handles changes to the hex size input field and updates cell size.
    /// </summary>
    private void OnHexSizeChanged(string val)
    {
        // Parse the input and update the cell size, enforcing a minimum value
        if (float.TryParse(val, out float hexSize))
        {
            gridController.CellSize = Mathf.Max(0.1f, hexSize);
            // Update the input field to show the clamped value
            hexSizeInput.text = gridController.CellSize.ToString("F1");
        }
        else
        {
            // Reset to current value if parsing failed
            hexSizeInput.text = gridController.CellSize.ToString("F1");
        }
    }

    /// <summary>
    /// Handles algorithm dropdown changes.
    /// </summary>
    private void OnAlgorithmChanged(int algorithmIndex)
    {
        if (mazeGenerator != null)
        {
            mazeGenerator.AlgorithmType = (MazeGenerator.MazeAlgorithmType)algorithmIndex;
        }
    }

    /// <summary>
    /// Handles instant grid generation toggle changes.
    /// </summary>
    private void OnInstantGridToggleChanged(bool isOn)
    {
        if (gridController != null)
        {
            gridController.InstantGeneration = isOn;
        }
    }

    /// <summary>
    /// Handles instant maze generation toggle changes.
    /// </summary>
    private void OnInstantMazeToggleChanged(bool isOn)
    {
        if (mazeGenerator != null)
        {
            mazeGenerator.InstantMazeGeneration = isOn;
        }
    }

    /// <summary>
    /// Handles instant path generation toggle changes.
    /// </summary>
    private void OnInstantPathToggleChanged(bool isOn)
    {
        if (mazeGenerator != null)
        {
            mazeGenerator.InstantPathDrawing = isOn;
        }
    }

    /// <summary>
    /// Handles the generate button click, completely regenerates grid and maze.
    /// </summary>
    private void OnGenerateClicked()
    {
        if (!ValidateReferences()) return;

        GenerateGridAndMaze();
    }

    /// <summary>
    /// Coroutine that handles complete grid and maze regeneration.
    /// </summary>
    private void GenerateGridAndMaze()
    {
        Debug.Log("Starting complete maze regeneration...");
        SetUIInteractable(false);

        DestroyExistingGrid();
        if (mazeGenerator != null)
        {
            mazeGenerator.ClearPath();
            mazeGenerator.SetAlgorithm((MazeGenerator.MazeAlgorithmType)algorithmDropdown.value);
        }

        // Instead of DestroyExistingGrid and RestartGridController:
        if (gridController != null) gridController.Init();
        

        SetUIInteractable(true);
        Debug.Log("Maze regeneration initiated");
    }
    /// <summary>
    /// Destroys all existing grid cells.
    /// </summary>
    private void DestroyExistingGrid()
    {
        if (gridController.transform.childCount == 0)
        {
            Debug.Log("No existing grid cells to destroy.");
            return;
        }

        // Destroy all child objects (existing cells)
        foreach (Transform child in gridController.transform)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Restarts the grid controller to trigger regeneration.
    /// </summary>
    private void RestartGridController()
    {
        // Stop any running coroutines in the grid controller
        gridController.StopAllCoroutines();
        
        // Force restart by disabling and re-enabling
        gridController.enabled = false;
        gridController.enabled = true;
    }

    /// <summary>
    /// Sets the interactable state of all UI elements.
    /// </summary>
    private void SetUIInteractable(bool interactable)
    {
        if (generateButton != null)
            generateButton.interactable = interactable;
        if (regenerateButton != null)
            regenerateButton.interactable = interactable;
        if (widthSlider != null)
            widthSlider.interactable = interactable;
        if (heightSlider != null)
            heightSlider.interactable = interactable;
        if (algorithmDropdown != null)
            algorithmDropdown.interactable = interactable;
    }

    /// <summary>
    /// Handles the fit camera button click, fits the camera to the maze.
    /// </summary>
    private void OnFitCameraClicked()
    {
        if (cameraController != null && gridController != null)
        {
            // Fit the camera to the maze grid
            cameraController.FitCameraToMaze(gridController);
        }
        else
        {
            Debug.LogWarning("CameraController or GridController reference is not set in MazeUIController!");
        }
    }

    /// <summary>
    /// Populates the algorithm dropdown with available maze generation algorithms.
    /// </summary>
    private void PopulateAlgorithmDropdown()
    {
        if (algorithmDropdown == null) return;
        
        // Clear existing options
        algorithmDropdown.ClearOptions();
        
        // Get all algorithm names from the MazeGenerator enum
        var options = new System.Collections.Generic.List<string>(System.Enum.GetNames(typeof(MazeGenerator.MazeAlgorithmType)));
        
        // Add options to the dropdown
        algorithmDropdown.AddOptions(options);
        
        // Set the current selection to match the maze generator's current algorithm
        if (mazeGenerator != null)
        {
            algorithmDropdown.value = (int)mazeGenerator.AlgorithmType;
        }
    }
}