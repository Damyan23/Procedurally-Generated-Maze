using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [Header("Hexes settings")]
    public float height;
    private float lastHeight;

    private MazeGenerator mazeGenerator;
    [SerializeField] private GameObject hexPrefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float hexSize = 1f;

    [SerializeField] private float yStartPosition = 10f;

    [HideInInspector] public HexGennerator[,] hexGrid;

    void Start()
    {
        mazeGenerator = GetComponent<MazeGenerator>();

        hexGrid = new HexGennerator[gridWidth, gridHeight];

        lastHeight = height;
        HexGennerator hexPrefabGen = hexPrefab.GetComponent<HexGennerator>();
        hexPrefabGen.height = height;
        hexPrefabGen.outerSize = hexSize;
        // StartCoroutine(generateGrid());
    }

    void Update()
    {
        updateHexesHeights();
    }

    void updateHexesHeights()
    {
        if (!Mathf.Approximately(height, lastHeight))
        {
            lastHeight = height;
            UpdateHexHeights(height);
        }
    }

    public void UpdateHexHeights(float newHeight)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                HexGennerator hex = hexGrid[x, y];
                if (hex != null)
                {
                    hex.height = newHeight;
                    hex.GenerateMesh();
                }
            }
        }
    }

    private IEnumerator generateGrid()
    {
        float height = Mathf.Sqrt(3) * hexSize;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float xPos = x * (hexSize * 1.5f);
                float zPos = y * height;

                if (x % 2 == 1)
                {
                    zPos += height / 2f;
                }

                Vector3 position = new Vector3(xPos, yStartPosition, zPos);
                instantaiteHex(position, x, y);
                yield return new WaitForSeconds(0.05f);
            }
        }

        yield return new WaitForSeconds(0.1f);
        mazeGenerator.Init();
    }

    void instantaiteHex(Vector3 position, int x, int y)
    {
        GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);
        hex.name = "Hex_" + x + "_" + y;

        Vector3 landingPosition = new Vector3(position.x, 0, position.z);
        StartCoroutine(moveHex(hex, landingPosition));

        HexGennerator hexComponent = hex.GetComponent<HexGennerator>();
        hexComponent.outerSize = hexSize;
        hexComponent.gridX = x;
        hexComponent.gridY = y;
        hexGrid[x, y] = hexComponent;
    }

    private IEnumerator moveHex(GameObject hex, Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        float duration = 1.5f;

        Vector3 startPosition = hex.transform.position;

        while (elapsedTime < duration)
        {
            hex.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        hex.transform.position = targetPosition;
    }

    public HexGennerator getHexAtPosition(int x, int y)
    {
        return hexGrid[x, y];
    }

    public HexGennerator GetNeighborInDirection(HexGennerator hex, HexGennerator.HexDirection direction)
    {
        int[,] evenColOffsets = new int[,]
        {
            {0, 1},   // Up
            {1, 0},  // UpRight
            {1, -1},  // DownRight
            {0, -1},  // Down
            {-1, -1},  // DownLeft
            {-1, 0}   // UpLeft
        };

        int[,] oddColOffsets = new int[,]
        {
            {0, 1},    // Up
            {1, 1},  // UpRight
            {1, 0},    // DownRight
            {0, -1},   // Down
            {-1, 0},   // DownLeft
            {-1, 1}    // UpLeft
        };

        int dir = (int)direction;
        bool evenCol = hex.gridX % 2 == 0;
        int dx, dy;

        if (evenCol)
        {
            dx = evenColOffsets[dir, 0]; 
            dy = evenColOffsets[dir, 1];
        }
        else
        {
            dx = oddColOffsets[dir, 0];
            dy = oddColOffsets[dir, 1];
        }

        int newX = hex.gridX + dx;
        int newY = hex.gridY + dy;

        if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
        {
            return hexGrid[newX, newY];
        }
        return null;
    }

    public List<HexGennerator> GetVisitedNeighbors(HexGennerator hex)
    {
        List<HexGennerator> visited = new();
        foreach (HexGennerator.HexDirection dir in System.Enum.GetValues(typeof(HexGennerator.HexDirection)))
        {
            var neighbor = GetNeighborInDirection(hex, dir);
            if (neighbor != null && neighbor.visited)
            {
                visited.Add(neighbor);
            }
        }
        return visited;
    }

    public HexGennerator.HexDirection GetOppositeDirection(HexGennerator.HexDirection direction)
    {
        return (HexGennerator.HexDirection)(((int)direction + 3) % 6);
    }

    public HexGennerator.HexDirection GetDirectionBetween(HexGennerator from, HexGennerator to)
    {
        foreach (HexGennerator.HexDirection dir in System.Enum.GetValues(typeof(HexGennerator.HexDirection)))
        {
            var neighbor = GetNeighborInDirection(from, dir);
            if (neighbor == to)
            {
                return dir;
            }
        }
        Debug.LogWarning($"Direction not found between {from.name} and {to.name}");
        return HexGennerator.HexDirection.Up;
    }
} 
