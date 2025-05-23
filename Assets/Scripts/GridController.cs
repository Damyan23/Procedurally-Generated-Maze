using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [Header("Grid Settings")]
    public float Height;
    private float lastHeight;
    
    [SerializeField] private GameObject hexPrefab;
    
    public int GridWidth = 10;
    public int GridHeight = 10;
    public float CellSize = 1f;
    public float HexSize = 1f;
    
    [SerializeField] private float yStartPosition = 10f;
    
    [Header("Generation Settings")]
    public bool InstantGeneration = false;
    
    [HideInInspector] public HexGennerator[,] HexGrid;
    
    private MazeGenerator mazeGenerator;
    
    void Start()
    {
        mazeGenerator = GetComponent<MazeGenerator>();
        HexGrid = new HexGennerator[GridWidth, GridHeight];
        initializeHexPrefab();
        lastHeight = Height;
        
        if (InstantGeneration)
        {
            generateGridInstant();
        }
        else
        {
            StartCoroutine(generateGridCoroutine());
        }
    }
    
    void Update()
    {
        updateCellHeights();
    }
    
    private void initializeHexPrefab()
    {
        if (hexPrefab != null)
        {
            HexGennerator hexPrefabGen = hexPrefab.GetComponent<HexGennerator>();
            if (hexPrefabGen != null)
            {
                hexPrefabGen.height = Height;
                hexPrefabGen.outerSize = HexSize;
            }
        }
    }

    private void updateCellHeights()
    {
        if (!Mathf.Approximately(Height, lastHeight))
        {
            lastHeight = Height;
            updateAllCellHeights(Height);
        }
    }
    
    private void updateAllCellHeights(float newHeight)
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                HexGennerator hex = HexGrid[x, y];
                if (hex != null)
                {
                    hex.height = newHeight;
                    hex.GenerateMesh();
                }
            }
        }
    }
    
    private void generateGridInstant()
    {
        float height = Mathf.Sqrt(3) * HexSize;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Vector3 position = calculateHexPosition(x, y, height);
                instantiateHex(position, x, y, true);
            }
        }

        mazeGenerator.Init();
    }
    
    private IEnumerator generateGridCoroutine()
    {
        float height = Mathf.Sqrt(3) * HexSize;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Vector3 position = calculateHexPosition(x, y, height);
                instantiateHex(position, x, y, false);
                yield return new WaitForSeconds(0.05f);
            }
        }

        yield return new WaitForSeconds(0.1f);
        mazeGenerator.Init();
    }
    
    private Vector3 calculateHexPosition(int x, int y, float height)
    {
        float xPos = x * (HexSize * 1.5f);
        float zPos = y * height;

        if (x % 2 == 1)
        {
            zPos += height / 2f;
        }

        return new Vector3(xPos, yStartPosition, zPos);
    }

    private void instantiateHex(Vector3 position, int x, int y, bool instant)
    {
        if (hexPrefab == null) return;
        
        GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);
        hex.name = $"Hex_{x}_{y}";
        
        Vector3 landingPosition = new Vector3(position.x, 0, position.z);
        
        if (instant)
        {
            hex.transform.position = landingPosition;
        }
        else
        {
            StartCoroutine(moveCell(hex, landingPosition));
        }
        
        setupHexComponent(hex, x, y);
    }
    
    private void setupHexComponent(GameObject hex, int x, int y)
    {
        HexGennerator hexComponent = hex.GetComponent<HexGennerator>();
        if (hexComponent != null)
        {
            hexComponent.outerSize = HexSize;
            hexComponent.height = Height;
            hexComponent.gridX = x;
            hexComponent.gridY = y;
            HexGrid[x, y] = hexComponent;
        }
    }
    
    private IEnumerator moveCell(GameObject cell, Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        float duration = 1.5f;
        Vector3 startPosition = cell.transform.position;
        
        while (elapsedTime < duration)
        {
            cell.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        cell.transform.position = targetPosition;
    }
    
    public HexGennerator GetNeighborInDirection(HexGennerator hex, HexGennerator.HexDirection direction)
    {
        int[,] evenColOffsets = new int[,]
        {
            {0, 1},   // Up
            {1, 0},   // UpRight  
            {1, -1},  // DownRight
            {0, -1},  // Down
            {-1, -1}, // DownLeft
            {-1, 0}   // UpLeft
        };

        int[,] oddColOffsets = new int[,]
        {
            {0, 1},   // Up
            {1, 1},   // UpRight
            {1, 0},   // DownRight  
            {0, -1},  // Down
            {-1, 0},  // DownLeft
            {-1, 1}   // UpLeft
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

        if (newX >= 0 && newX < GridWidth && newY >= 0 && newY < GridHeight)
        {
            return HexGrid[newX, newY];
        }
        return null;
    }
    
    public List<HexGennerator> GetVisitedNeighbors(HexGennerator hex)
    {
        List<HexGennerator> visited = new List<HexGennerator>();

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

    public bool HasWallBetween(HexGennerator hex1, HexGennerator hex2, HexGennerator.HexDirection dir)
    {
        return hex1.walls[(int)dir] && hex2.walls[(int)GetOppositeDirection(dir)];
    }

    public void SetVisited(HexGennerator hex, bool visited)
    {
        hex.visited = visited;
    }

    public bool IsVisited(HexGennerator hex)
    {
        return hex.visited;
    }

    public HexGennerator GetStartCell()
    {
        return HexGrid[0, GridHeight - 1];
    }
    
    public HexGennerator.HexDirection[] GetShuffledDirections(HexGennerator hex)
    {
        return hex.GetShuffeldDirections();
    }
    
    public void DisableFaceBetween(HexGennerator hex1, HexGennerator hex2, HexGennerator.HexDirection direction)
    {
        hex1.DisableFace(direction);
        hex2.DisableFace(GetOppositeDirection(direction));
    }
    
    public Vector3 GetCellPosition(HexGennerator hex)
    {
        return hex.transform.position;
    }
}