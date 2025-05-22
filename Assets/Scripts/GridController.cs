using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public enum GridType
    {
        Hexagonal,
        Cubic
    }
    
    [Header("Grid Type")]
    [SerializeField] private GridType gridType = GridType.Hexagonal;
    
    [Header("Grid Settings")]
    public float height;
    private float lastHeight;
    
    [SerializeField] private GameObject hexPrefab;
    [SerializeField] private GameObject cubePrefab;
    
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public float hexSize = 1f; // Controls hex-specific sizing
    
    [SerializeField] private float yStartPosition = 10f;
    
    // Grid arrays for both types
    [HideInInspector] public HexGennerator[,] hexGrid;
    [HideInInspector] public CubeGenerator[,] cubeGrid;
    
    private MazeGenerator mazeGenerator;
    
    void Start()
    {
        mazeGenerator = GetComponent<MazeGenerator>();
        
        // Initialize the appropriate grid
        if (gridType == GridType.Hexagonal)
        {
            hexGrid = new HexGennerator[gridWidth, gridHeight];
            InitializeHexPrefab();
        }
        else
        {
            cubeGrid = new CubeGenerator[gridWidth, gridHeight];
            InitializeCubePrefab();
        }
        
        lastHeight = height;
        
        // Start grid generation
        StartCoroutine(GenerateGrid());
    }
    
    void Update()
    {
        UpdateCellHeights();
    }
    
    void InitializeHexPrefab()
    {
        if (hexPrefab != null)
        {
            HexGennerator hexPrefabGen = hexPrefab.GetComponent<HexGennerator>();
            if (hexPrefabGen != null)
            {
                hexPrefabGen.height = height;
                hexPrefabGen.outerSize = hexSize; // Use hexSize instead of cellSize
            }
        }
    }
    
    void InitializeCubePrefab()
    {
        if (cubePrefab != null)
        {
            CubeGenerator cubePrefabGen = cubePrefab.GetComponent<CubeGenerator>();
            if (cubePrefabGen != null)
            {
                cubePrefabGen.height = height;
                cubePrefabGen.outerSize = cellSize;
            }
        }
    }
    
    void UpdateCellHeights()
    {
        if (!Mathf.Approximately(height, lastHeight))
        {
            lastHeight = height;
            UpdateAllCellHeights(height);
        }
    }
    
    public void UpdateAllCellHeights(float newHeight)
    {
        if (gridType == GridType.Hexagonal && hexGrid != null)
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
        else if (gridType == GridType.Cubic && cubeGrid != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    CubeGenerator cube = cubeGrid[x, y];
                    if (cube != null)
                    {
                        cube.height = newHeight;
                        cube.GenerateMesh();
                    }
                }
            }
        }
    }
    
    private IEnumerator GenerateGrid()
    {
        if (gridType == GridType.Hexagonal)
        {
            yield return StartCoroutine(GenerateHexGrid());
        }
        else
        {
            yield return StartCoroutine(GenerateCubeGrid());
        }
        
        yield return new WaitForSeconds(0.1f);
        if (mazeGenerator != null)
        {
            mazeGenerator.Init();
        }
    }
    
    private IEnumerator GenerateHexGrid()
    {
        float hexHeight = Mathf.Sqrt(3) * hexSize; // Use hexSize for hex calculations
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float xPos = x * cellSize;
                float zPos = y * cellSize;

                
                if (x % 2 == 1)
                {
                    zPos += hexHeight / 2f;
                }
                
                Vector3 position = new Vector3(xPos, yStartPosition, zPos);
                InstantiateHex(position, x, y);
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
    
    private IEnumerator GenerateCubeGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float xPos = x * cellSize * 2f; // Double the cell size for spacing
                float zPos = y * cellSize * 2f;
                
                Vector3 position = new Vector3(xPos, yStartPosition, zPos);
                InstantiateCube(position, x, y);
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
    
    void InstantiateHex(Vector3 position, int x, int y)
    {
        if (hexPrefab == null) return;
        
        GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);
        hex.name = "Hex_" + x + "_" + y;
        
        Vector3 landingPosition = new Vector3(position.x, 0, position.z);
        StartCoroutine(MoveCell(hex, landingPosition));
        
        HexGennerator hexComponent = hex.GetComponent<HexGennerator>();
        if (hexComponent != null)
        {
            hexComponent.outerSize = hexSize;
            hexComponent.height = height;
            hexComponent.gridX = x;
            hexComponent.gridY = y;
            hexGrid[x, y] = hexComponent;
        }
    }
    
    void InstantiateCube(Vector3 position, int x, int y)
    {
        if (cubePrefab == null) return;
        
        GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity, transform);
        cube.name = "Cube_" + x + "_" + y;
        
        Vector3 landingPosition = new Vector3(position.x, 0, position.z);
        StartCoroutine(MoveCell(cube, landingPosition));
        
        CubeGenerator cubeComponent = cube.GetComponent<CubeGenerator>();
        if (cubeComponent != null)
        {
            cubeComponent.outerSize = cellSize;
            cubeComponent.height = height;
            cubeComponent.gridX = x;
            cubeComponent.gridY = y;
            cubeGrid[x, y] = cubeComponent;
        }
    }
    
    private IEnumerator MoveCell(GameObject cell, Vector3 targetPosition)
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
    
    // Generic methods that work with both grid types
    public HexGennerator GetHexAtPosition(int x, int y)
    {
        if (gridType != GridType.Hexagonal || hexGrid == null) return null;
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return null;
        return hexGrid[x, y];
    }
    
    public CubeGenerator GetCubeAtPosition(int x, int y)
    {
        if (gridType != GridType.Cubic || cubeGrid == null) return null;
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return null;
        return cubeGrid[x, y];
    }
    
    // Unified neighbor finding that works for both hex and cube grids
    public object GetNeighborInDirection(object cell, HexGennerator.HexDirection direction)
    {
        if (gridType == GridType.Hexagonal && cell is HexGennerator hex)
        {
            return GetHexNeighborInDirection(hex, direction);
        }
        else if (gridType == GridType.Cubic && cell is CubeGenerator cube)
        {
            return GetCubeNeighborInDirection(cube, direction);
        }
        return null;
    }
    
    public HexGennerator GetHexNeighborInDirection(HexGennerator hex, HexGennerator.HexDirection direction)
    {
        if (gridType != GridType.Hexagonal || hexGrid == null) return null;
        
        // Hexagonal grid neighbor logic
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
        
        if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
        {
            return hexGrid[newX, newY];
        }
        return null;
    }
    
    public CubeGenerator GetCubeNeighborInDirection(CubeGenerator cube, HexGennerator.HexDirection direction)
    {
        if (gridType != GridType.Cubic || cubeGrid == null) return null;
        
        // For cubes, we'll map hex directions to cardinal directions
        // Up = North, UpRight = East, Down = South, DownLeft = West
        int dx = 0, dy = 0;
        
        switch (direction)
        {
            case HexGennerator.HexDirection.Up:        // North
                dy = 1; break;
            case HexGennerator.HexDirection.UpRight:   // East  
            case HexGennerator.HexDirection.DownRight:
                dx = 1; break;
            case HexGennerator.HexDirection.Down:      // South
                dy = -1; break;
            case HexGennerator.HexDirection.DownLeft:  // West
            case HexGennerator.HexDirection.UpLeft:
                dx = -1; break;
        }
        
        int newX = cube.gridX + dx;
        int newY = cube.gridY + dy;
        
        if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
        {
            return cubeGrid[newX, newY];
        }
        return null;
    }
    
    public List<object> GetVisitedNeighbors(object cell)
    {
        List<object> visited = new List<object>();
        
        if (gridType == GridType.Hexagonal && cell is HexGennerator hex)
        {
            foreach (HexGennerator.HexDirection dir in System.Enum.GetValues(typeof(HexGennerator.HexDirection)))
            {
                var neighbor = GetHexNeighborInDirection(hex, dir);
                if (neighbor != null && neighbor.visited)
                {
                    visited.Add(neighbor);
                }
            }
        }
        else if (gridType == GridType.Cubic && cell is CubeGenerator cube)
        {
            // Only check cardinal directions for cubes
            HexGennerator.HexDirection[] cardinalDirs = {
                HexGennerator.HexDirection.Up,
                HexGennerator.HexDirection.UpRight,
                HexGennerator.HexDirection.Down,
                HexGennerator.HexDirection.DownLeft
            };
            
            foreach (var dir in cardinalDirs)
            {
                var neighbor = GetCubeNeighborInDirection(cube, dir);
                if (neighbor != null && neighbor.visited)
                {
                    visited.Add(neighbor);
                }
            }
        }
        
        return visited;
    }
    
    public List<HexGennerator> GetVisitedNeighbors(HexGennerator hex)
    {
        List<HexGennerator> visited = new List<HexGennerator>();
        
        if (gridType == GridType.Hexagonal)
        {
            foreach (HexGennerator.HexDirection dir in System.Enum.GetValues(typeof(HexGennerator.HexDirection)))
            {
                var neighbor = GetHexNeighborInDirection(hex, dir);
                if (neighbor != null && neighbor.visited)
                {
                    visited.Add(neighbor);
                }
            }
        }
        
        return visited;
    }
    
    public List<CubeGenerator> GetVisitedCubeNeighbors(CubeGenerator cube)
    {
        List<CubeGenerator> visited = new List<CubeGenerator>();
        
        if (gridType == GridType.Cubic)
        {
            // Only check cardinal directions for cubes
            HexGennerator.HexDirection[] cardinalDirs = {
                HexGennerator.HexDirection.Up,
                HexGennerator.HexDirection.UpRight,
                HexGennerator.HexDirection.Down,
                HexGennerator.HexDirection.DownLeft
            };
            
            foreach (var dir in cardinalDirs)
            {
                var neighbor = GetCubeNeighborInDirection(cube, dir);
                if (neighbor != null && neighbor.visited)
                {
                    visited.Add(neighbor);
                }
            }
        }
        
        return visited;
    }
    
    public HexGennerator.HexDirection GetOppositeDirection(HexGennerator.HexDirection direction)
    {
        return (HexGennerator.HexDirection)(((int)direction + 3) % 6);
    }
    
    public HexGennerator.HexDirection GetDirectionBetween(object from, object to)
    {
        if (gridType == GridType.Hexagonal && from is HexGennerator hexFrom && to is HexGennerator hexTo)
        {
            foreach (HexGennerator.HexDirection dir in System.Enum.GetValues(typeof(HexGennerator.HexDirection)))
            {
                var neighbor = GetHexNeighborInDirection(hexFrom, dir);
                if (neighbor == hexTo)
                {
                    return dir;
                }
            }
            Debug.LogWarning($"Direction not found between {hexFrom.name} and {hexTo.name}");
        }
        else if (gridType == GridType.Cubic && from is CubeGenerator cubeFrom && to is CubeGenerator cubeTo)
        {
            return GetDirectionBetweenCubes(cubeFrom, cubeTo);
        }
        
        return HexGennerator.HexDirection.Up;
    }
    
    public HexGennerator.HexDirection GetDirectionBetween(HexGennerator from, HexGennerator to)
    {
        if (gridType == GridType.Hexagonal)
        {
            foreach (HexGennerator.HexDirection dir in System.Enum.GetValues(typeof(HexGennerator.HexDirection)))
            {
                var neighbor = GetHexNeighborInDirection(from, dir);
                if (neighbor == to)
                {
                    return dir;
                }
            }
        }
        
        Debug.LogWarning($"Direction not found between {from.name} and {to.name}");
        return HexGennerator.HexDirection.Up;
    }
    
    public HexGennerator.HexDirection GetDirectionBetweenCubes(CubeGenerator from, CubeGenerator to)
    {
        if (gridType != GridType.Cubic) return HexGennerator.HexDirection.Up;

        int dx = to.gridX - from.gridX;
        int dy = to.gridY - from.gridY;

        if (dx == 1 && dy == 0)
            return HexGennerator.HexDirection.UpRight;   // East
        if (dx == -1 && dy == 0)
            return HexGennerator.HexDirection.DownLeft;  // West
        if (dx == 0 && dy == 1)
            return HexGennerator.HexDirection.Up;        // North
        if (dx == 0 && dy == -1)
            return HexGennerator.HexDirection.Down;      // South

        Debug.LogWarning($"Unmapped direction between cube {from.name} and {to.name}");
        return HexGennerator.HexDirection.Up;
    }

    
    // Method to disable walls between cells (works for both types)
    public void DisableWallBetween(int fromX, int fromY, int toX, int toY)
    {
        if (gridType == GridType.Hexagonal && hexGrid != null)
        {
            HexGennerator from = hexGrid[fromX, fromY];
            HexGennerator to = hexGrid[toX, toY];
            
            if (from != null && to != null)
            {
                var direction = GetDirectionBetween(from, to);
                var oppositeDirection = GetOppositeDirection(direction);
                
                from.DisableFace(direction);
                to.DisableFace(oppositeDirection);
            }
        }
        else if (gridType == GridType.Cubic && cubeGrid != null)
        {
            CubeGenerator from = cubeGrid[fromX, fromY];
            CubeGenerator to = cubeGrid[toX, toY];
            
            if (from != null && to != null)
            {
                var direction = GetDirectionBetweenCubes(from, to);
                var oppositeDirection = GetOppositeDirection(direction);
                
                from.DisableFace(direction);
                to.DisableFace(oppositeDirection);
            }
        }
    }
    
    // Helper methods for maze generation
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }
    
    public void SetCellVisited(int x, int y, bool visited)
    {
        if (gridType == GridType.Hexagonal && hexGrid != null)
        {
            var hex = hexGrid[x, y];
            if (hex != null) hex.visited = visited;
        }
        else if (gridType == GridType.Cubic && cubeGrid != null)
        {
            var cube = cubeGrid[x, y];
            if (cube != null) cube.visited = visited;
        }
    }
    
    public void SetCellAsStart(int x, int y)
    {
        if (gridType == GridType.Hexagonal && hexGrid != null)
        {
            var hex = hexGrid[x, y];
            if (hex != null) hex.isStart = true;
        }
        else if (gridType == GridType.Cubic && cubeGrid != null)
        {
            var cube = cubeGrid[x, y];
            if (cube != null) cube.isStart = true;
        }
    }
    
    // Methods for maze generation compatibility
    public object GetStartCell()
    {
        if (gridType == GridType.Hexagonal && hexGrid != null)
        {
            return hexGrid[0, gridHeight - 1];
        }
        else if (gridType == GridType.Cubic && cubeGrid != null)
        {
            return cubeGrid[0, gridHeight - 1];
        }
        return null;
    }
    
    public object[,] GetCurrentGrid()
    {
        if (gridType == GridType.Hexagonal && hexGrid != null)
        {
            object[,] grid = new object[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = hexGrid[x, y];
                }
            }
            return grid;
        }
        else if (gridType == GridType.Cubic && cubeGrid != null)
        {
            object[,] grid = new object[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = cubeGrid[x, y];
                }
            }
            return grid;
        }
        return null;
    }
    
    public bool IsVisited(object cell)
    {
        if (cell is HexGennerator hex)
            return hex.visited;
        else if (cell is CubeGenerator cube)
            return cube.visited;
        return false;
    }
    
    public void SetVisited(object cell, bool visited)
    {
        if (cell is HexGennerator hex)
            hex.visited = visited;
        else if (cell is CubeGenerator cube)
            cube.visited = visited;
    }
    
    public HexGennerator.HexDirection[] GetShuffledDirections(object cell)
    {
        if (cell is HexGennerator hex)
            return hex.GetShuffeldDirections();
        else if (cell is CubeGenerator cube)
            return cube.GetShuffeldDirections();
        
        // Default fallback
        return new HexGennerator.HexDirection[0];
    }
    
    public void DisableFaceBetween(object cell1, object cell2, HexGennerator.HexDirection direction)
    {
        if (cell1 is HexGennerator hex1 && cell2 is HexGennerator hex2)
        {
            hex1.DisableFace(direction);
            hex2.DisableFace(GetOppositeDirection(direction));
        }
        else if (cell1 is CubeGenerator cube1 && cell2 is CubeGenerator cube2)
        {
            cube1.DisableFace(direction);
            cube2.DisableFace(GetOppositeDirection(direction));
        }
    }
    
    public string GetCellName(object cell)
    {
        if (cell is HexGennerator hex)
            return hex.name;
        else if (cell is CubeGenerator cube)
            return cube.name;
        return "Unknown";
    }
    
    public Vector3 GetCellPosition(object cell)
    {
        if (cell is HexGennerator hex)
            return hex.transform.position;
        else if (cell is CubeGenerator cube)
            return cube.transform.position;
        return Vector3.zero;
    }
    
    // Get current grid type
    public GridType GetGridType()
    {
        return gridType;
    }
}