using Unity.Collections;
using UnityEngine;

public class GridController : MonoBehaviour
{
    private MazeGenerator mazeGenerator;
    [SerializeField] private GameObject hexPrefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float hexSize = 1f;

    [HideInInspector] public HexGennerator[, ] hexGrid;

    void Start()
    {
        mazeGenerator = GetComponent<MazeGenerator>();

        hexGrid = new HexGennerator[gridWidth, gridHeight];

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
                instantaiteHex(position, x, y);
            }
        }

        mazeGenerator.Init();
    }

    void instantaiteHex (Vector3 position, int x, int y)
    {
        
        GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);
        hex.name = "Hex_" + x + "_" + y;

        HexGennerator hexComponent = hex.GetComponent<HexGennerator>();
        hexComponent.outerSize = hexSize;
        hexComponent.height = 1f;
        hexComponent.gridX = x;
        hexComponent.gridY = y;
        hexGrid[x, y] = hexComponent;
    }

    public HexGennerator getHexAtPosition(int x, int y)
    {
        return hexGrid[x, y];
    }

    public HexGennerator GetRandomNeighbor(HexGennerator hex)
    {
        int x = hex.gridX;
        int y = hex.gridY;

        // Define the 6 possible neighbor offsets 
        Vector2Int[] neighborOffsetsEven = new Vector2Int[]
        {
            new Vector2Int(+1, 0),   // East
            new Vector2Int(0, +1),   // NE
            new Vector2Int(-1, +1),  // NW
            new Vector2Int(-1, 0),   // West
            new Vector2Int(-1, -1),  // SW
            new Vector2Int(0, -1),   // SE
        };

        Vector2Int[] neighborOffsetsOdd = new Vector2Int[]
        {
            new Vector2Int(+1, 0),   // East
            new Vector2Int(+1, +1),  // NE
            new Vector2Int(0, +1),   // NW
            new Vector2Int(-1, 0),   // West
            new Vector2Int(0, -1),   // SW
            new Vector2Int(+1, -1),  // SE
        };

        Vector2Int[] neighborOffsets = (x % 2 == 0) ? neighborOffsetsEven : neighborOffsetsOdd;

        int attempts = 0;
        while (attempts < 20) // prevent infinite loop
        {
            int index = Random.Range(0, 6);
            Vector2Int offset = neighborOffsets[index];

            int nx = x + offset.x;
            int ny = y + offset.y;

            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
            {
                HexGennerator randomHex = hexGrid[nx, ny];
                if (randomHex != null && randomHex.visited == false)
                {
                    return randomHex;
                }
            }

            attempts++;
        }

        return null;
    }

    public int GetDirection(HexGennerator hex1, HexGennerator hex2)
    {
        int x = hex1.gridX;
        int dx = hex2.gridX - hex1.gridX;
        int dy = hex2.gridY - hex1.gridY;

        // Use proper neighbor offsets based on parity of X
        Vector2Int[] neighborOffsetsEven = new Vector2Int[]
        {
            new Vector2Int(+1, 0),   // 0 - East
            new Vector2Int(0, +1),   // 1 - NE
            new Vector2Int(-1, +1),  // 2 - NW
            new Vector2Int(-1, 0),   // 3 - West
            new Vector2Int(-1, -1),  // 4 - SW
            new Vector2Int(0, -1),   // 5 - SE
        };

        Vector2Int[] neighborOffsetsOdd = new Vector2Int[]
        {
            new Vector2Int(+1, 0),   // 0 - East
            new Vector2Int(+1, +1),  // 1 - NE
            new Vector2Int(0, +1),   // 2 - NW
            new Vector2Int(-1, 0),   // 3 - West
            new Vector2Int(0, -1),   // 4 - SW
            new Vector2Int(+1, -1),  // 5 - SE
        };

        Vector2Int[] neighborOffsets = (x % 2 == 0) ? neighborOffsetsEven : neighborOffsetsOdd;

        for (int i = 0; i < 6; i++)
        {
            if (neighborOffsets[i].x == dx && neighborOffsets[i].y == dy)
                return i;
        }

        Debug.Log("Invalid neighbor direction: dx = " + dx + ", dy = " + dy);
        return -1; // Not a direct neighbor
    }


}
