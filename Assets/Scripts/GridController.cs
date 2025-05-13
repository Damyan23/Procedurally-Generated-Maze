using UnityEngine;

public class GridController : MonoBehaviour
{
    [SerializeField] private GameObject hexPrefab;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float hexSize = 1f;

    void Start()
    {
        generateGrid();
    }

    void generateGrid()
    {
        float height = Mathf.Sqrt(3) * hexSize;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float xPos = x * (hexSize * 1.5f);
                float zPos = y * height;

                // Offset every odd column
                if (x % 2 == 1)
                {
                    zPos += height / 2f;
                }

                Vector3 position = new Vector3(xPos, 0, zPos);
                instantaiteHex(position);
            }
        }
    }

    void instantaiteHex (Vector3 position)
    {
        
        GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);

        HexGennerator hexComponent = hex.GetComponent<HexGennerator>();
        hexComponent.outerSize = hexSize;
        hexComponent.height = 1f;
    }

}
