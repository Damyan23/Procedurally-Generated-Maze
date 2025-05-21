using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MazeUIController : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private Toggle evenSizeToggle;
    [SerializeField] private TMP_InputField hexSizeInput;

    [Header("Buttons")]
    [SerializeField] private Button generateButton;
    [SerializeField] private Button regenerateButton;

    [Header("Target References")]
    [SerializeField] private GridController gridController;
    [SerializeField] private MazeGenerator mazeGenerator;

    private const int MIN_SIZE = 10;
    private const int MAX_SIZE = 250;

    private void Awake()
    {
        generateButton.onClick.AddListener(OnGenerateClicked);
        regenerateButton.onClick.AddListener(OnRegenerateClicked);

        widthInput.onValueChanged.AddListener(OnWidthChanged);
        heightInput.onValueChanged.AddListener(OnHeightChanged);
        hexSizeInput.onValueChanged.AddListener(OnHexSizeChanged);
        evenSizeToggle.onValueChanged.AddListener(OnToggleEvenChanged);
    }

    void OnWidthChanged(string val)
    {
        int width = ClampInput(val);
        gridController.gridWidth = width;

        if (evenSizeToggle.isOn)
        {
            gridController.gridHeight = width;
            heightInput.text = width.ToString();
        }
    }

    void OnHeightChanged(string val)
    {
        if (!evenSizeToggle.isOn)
        {
            int height = ClampInput(val);
            gridController.gridHeight = height;
        }
    }

    void OnToggleEvenChanged(bool isOn)
    {
        if (isOn)
        {
            int width = ClampInput(widthInput.text);
            gridController.gridHeight = width;
            heightInput.text = width.ToString();
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
        gridController.StartCoroutine(gridController.GetType()
            .GetMethod("generateGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(gridController, null) as IEnumerator);
    }

    void OnRegenerateClicked()
    {
        // Destroy existing hexes
        foreach (Transform child in gridController.transform)
        {
            Destroy(child.gameObject);
        }
        gridController.hexGrid = new HexGennerator[gridController.gridWidth, gridController.gridHeight];
        OnGenerateClicked();
    }

    int ClampInput(string val)
    {
        if (!int.TryParse(val, out int parsed)) parsed = MIN_SIZE;
        return Mathf.Clamp(parsed, MIN_SIZE, MAX_SIZE);
    }
}
