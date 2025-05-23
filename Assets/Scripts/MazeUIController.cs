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

        widthSlider.value = gridController.GridWidth;
        heightSlider.value = gridController.GridHeight;

        widthValueText.text = gridController.GridWidth.ToString();
        heightValueText.text = gridController.GridHeight.ToString();

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
        gridController.GridWidth = width;
        widthValueText.text = width.ToString();

        if (evenSizeToggle.isOn)
        {
            gridController.GridHeight = width;
            heightSlider.value = width;
            heightValueText.text = width.ToString();
        }
    }

    void OnHeightSliderChanged(float val)
    {
        int height = Mathf.RoundToInt(val);
        if (!evenSizeToggle.isOn)
        {
            gridController.GridHeight = height;
        }
        else
        {
            gridController.GridWidth = height;
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
            gridController.GridHeight = width;
            heightSlider.value = width;
            heightValueText.text = width.ToString();
        }
    }

    void OnHexSizeChanged(string val)
    {
        if (float.TryParse(val, out float hexSize))
        {
            gridController.HexSize = Mathf.Max(0.1f, hexSize);
        }
    }

    void OnGenerateClicked()
    {
        gridController.StopAllCoroutines();

        int algorithmIndex = algorithmDropdown.value;
        mazeGenerator.AlgorithmType = (MazeGenerator.MazeAlgorithmType)algorithmIndex;

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
        gridController.HexGrid = new HexGennerator[gridController.GridWidth, gridController.GridHeight];
        OnGenerateClicked();
    }

    void PopulateAlgorithmDropdown()
    {
        algorithmDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>(System.Enum.GetNames(typeof(MazeGenerator.MazeAlgorithmType)));
        algorithmDropdown.AddOptions(options);
    }
}
