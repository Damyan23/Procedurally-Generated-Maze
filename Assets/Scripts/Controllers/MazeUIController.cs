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
        // Set slider min/max values
        widthSlider.minValue = MIN_SIZE;
        widthSlider.maxValue = MAX_SIZE;
        heightSlider.minValue = MIN_SIZE;
        heightSlider.maxValue = MAX_SIZE;

        // Set slider values to current grid size
        widthSlider.value = gridController.GridWidth;
        heightSlider.value = gridController.GridHeight;

        // Update text fields to show current grid size
        widthValueText.text = gridController.GridWidth.ToString();
        heightValueText.text = gridController.GridHeight.ToString();

        // Populate the algorithm dropdown with available algorithms
        PopulateAlgorithmDropdown();
    }

    /// <summary>
    /// Sets up UI event listeners.
    /// </summary>
    private void Awake()
    {
        // Button click listeners
        generateButton.onClick.AddListener(OnGenerateClicked);
        regenerateButton.onClick.AddListener(OnRegenerateClicked);
        fitCameraButton.onClick.AddListener(OnFitCameraClicked);

        // Slider and toggle listeners
        widthSlider.onValueChanged.AddListener(OnWidthSliderChanged);
        heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
        evenSizeToggle.onValueChanged.AddListener(OnToggleEvenChanged);
    }

    /// <summary>
    /// Handles changes to the width slider and updates grid width.
    /// </summary>
    private void OnWidthSliderChanged(float val)
    {
        int width = Mathf.RoundToInt(val);
        gridController.GridWidth = width;
        widthValueText.text = width.ToString();

        // If even size is toggled, update height to match width
        if (evenSizeToggle.isOn)
        {
            gridController.GridHeight = width;
            heightSlider.value = width;
            heightValueText.text = width.ToString();
        }
    }

    /// <summary>
    /// Handles changes to the height slider and updates grid height.
    /// </summary>
    private void OnHeightSliderChanged(float val)
    {
        int height = Mathf.RoundToInt(val);
        if (!evenSizeToggle.isOn)
        {
            // If not even, just update height
            gridController.GridHeight = height;
        }
        else
        {
            // If even, update width to match height
            gridController.GridWidth = height;
            widthSlider.value = height;
            widthValueText.text = height.ToString();
        }
        heightValueText.text = height.ToString();
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
            heightSlider.value = width;
            heightValueText.text = width.ToString();
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
        }
    }

    /// <summary>
    /// Handles the generate button click, starts maze generation with the selected algorithm.
    /// </summary>
    private void OnGenerateClicked()
    {
        // Stop any running coroutines on the grid controller
        gridController.StopAllCoroutines();

        // Set the selected algorithm type in the maze generator
        int algorithmIndex = algorithmDropdown.value;
        mazeGenerator.AlgorithmType = (MazeGenerator.MazeAlgorithmType)algorithmIndex;

        // Use reflection to invoke the private generateGrid coroutine
        gridController.StartCoroutine(gridController.GetType()
            .GetMethod("generateGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(gridController, null) as IEnumerator);
    }

    /// <summary>
    /// Handles the regenerate button click, clears the grid and regenerates the maze.
    /// </summary>
    private void OnRegenerateClicked()
    {
        // Destroy all child objects (cells) in the grid
        foreach (Transform child in gridController.transform)
        {
            Destroy(child.gameObject);
        }
        // Reset the grid array
        gridController.Grid = new Cell[gridController.GridWidth, gridController.GridHeight];
        // Start maze generation again
        OnGenerateClicked();
    }

    /// <summary>
    /// Handles the fit camera button click, fits the camera to the maze.
    /// </summary>
    private void OnFitCameraClicked()
    {
        if (cameraController != null)
        {
            // Fit the camera to the maze grid
            cameraController.FitCameraToMaze(gridController);
        }
        else
        {
            Debug.LogWarning("CameraController reference is not set in MazeUIController!");
        }
    }

    /// <summary>
    /// Populates the algorithm dropdown with available maze generation algorithms.
    /// </summary>
    private void PopulateAlgorithmDropdown()
    {
        // Clear existing options
        algorithmDropdown.ClearOptions();
        // Get all algorithm names from the MazeGenerator enum
        var options = new System.Collections.Generic.List<string>(System.Enum.GetNames(typeof(MazeGenerator.MazeAlgorithmType)));
        // Add options to the dropdown
        algorithmDropdown.AddOptions(options);
    }
}