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
            new Vector2Int(+1,  0), // East
            new Vector2Int( 0, -1), // NE
            new Vector2Int(-1, -1), // NW
            new Vector2Int(-1,  0), // West
            new Vector2Int(-1, +1), // SW
            new Vector2Int( 0, +1), // SE
        };

        Vector2Int[] neighborOffsetsOdd = new Vector2Int[]
        {
            new Vector2Int(+1,  0), // East
            new Vector2Int(+1, -1), // NE
            new Vector2Int( 0, -1), // NW
            new Vector2Int(-1,  0), // West
            new Vector2Int( 0, +1), // SW
            new Vector2Int(+1, +1), // SE
        };

        Vector2Int[] neighborOffsets = neighborOffsetsEven;

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
        // Calculate differences in axial coordinates (assuming hex1 and hex2 have gridX and gridY as axial coordinates)
        int dx = hex2.gridX - hex1.gridX;
        int dy = hex2.gridY - hex1.gridY;

        // Check each direction based on axial coordinates
        if (dx == 1 && dy == 0) return 0;  // East
        if (dx == 0 && dy == -1) return 1; // NE
        if (dx == -1 && dy == -1) return 2; // NW
        if (dx == -1 && dy == 0) return 3; // West
        if (dx == 0 && dy == 1) return 4;  // SW
        if (dx == 1 && dy == 1) return 5;  // SE

        return -1; // Invalid direction (shouldn't happen if hexes are neighbors)
    }

}
