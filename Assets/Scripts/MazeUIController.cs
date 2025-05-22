using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Dropdown for Algorithm")]
    [SerializeField] private TMP_Dropdown algorithmDropdown;

    [Header("Target References")]
    [SerializeField] private GridController gridController;
    [SerializeField] private MazeGenerator mazeGenerator;

    private const int MIN_SIZE = 1;
    private const int MAX_SIZE = 250;

    private void Start()
    {
        widthSlider.minValue = MIN_SIZE;
        widthSlider.maxValue = MAX_SIZE;
        heightSlider.minValue = MIN_SIZE;
        heightSlider.maxValue = MAX_SIZE;

        widthSlider.value = gridController.gridWidth;
        heightSlider.value = gridController.gridHeight;

        widthValueText.text = gridController.gridWidth.ToString();
        heightValueText.text = gridController.gridHeight.ToString();

        PopulateAlgorithmDropdown();
    }

    private void Awake()
    {
        generateButton.onClick.AddListener(OnGenerateClicked);
        regenerateButton.onClick.AddListener(OnRegenerateClicked);

        widthSlider.onValueChanged.AddListener(OnWidthSliderChanged);
        heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
        evenSizeToggle.onValueChanged.AddListener(OnToggleEvenChanged);
    }

    void OnWidthSliderChanged(float val)
    {
        int width = Mathf.RoundToInt(val);
        gridController.gridWidth = width;
        widthValueText.text = width.ToString();

        if (evenSizeToggle.isOn)
        {
            gridController.gridHeight = width;
            heightSlider.value = width;
            heightValueText.text = width.ToString();
        }
    }

    void OnHeightSliderChanged(float val)
    {
        int height = Mathf.RoundToInt(val);
        if (!evenSizeToggle.isOn)
        {
            gridController.gridHeight = height;
        }
        else
        {
            gridController.gridWidth = height;
            widthSlider.value = height;
            widthValueText.text = height.ToString();
        }
        heightValueText.text = height.ToString();
    }

    void OnToggleEvenChanged(bool isOn)
    {
        if (isOn)
        {
            int width = Mathf.RoundToInt(widthSlider.value);
            gridController.gridHeight = width;
            heightSlider.value = width;
            heightValueText.text = width.ToString();
        }
    }

    void OnHexSizeChanged(string val)
    {
        if (float.TryParse(val, out float hexSize))
        {
            gridController.hexSize = Mathf.Max(0.1f, hexSize);
        }
    }

    void OnGenerateClicked()
    {
        gridController.StopAllCoroutines();

        int algorithmIndex = algorithmDropdown.value;
        mazeGenerator.algorithmType = (MazeGenerator.MazeAlgorithmType)algorithmIndex;

        gridController.StartCoroutine(gridController.GetType()
            .GetMethod("generateGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(gridController, null) as IEnumerator);
    }

    void OnRegenerateClicked()
    {
        foreach (Transform child in gridController.transform)
        {
            Destroy(child.gameObject);
        }
        gridController.hexGrid = new HexGennerator[gridController.gridWidth, gridController.gridHeight];
        OnGenerateClicked();
    }

    void PopulateAlgorithmDropdown()
    {
        algorithmDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>(System.Enum.GetNames(typeof(MazeGenerator.MazeAlgorithmType)));
        algorithmDropdown.AddOptions(options);
    }
}
