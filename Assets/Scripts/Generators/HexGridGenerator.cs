using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and manages a 2D grid of hexagonal cells for the maze.
/// Implements IMazeGrid for cell access, neighbor logic, and wall management.
/// </summary>
public class HexGridGenerator : MonoBehaviour, IMazeGrid
{
    [Header("Grid Settings")]
    public float Height;
    private float lastHeight;

    [SerializeField] private GameObject hexPrefab;

    public int GridWidth = 10;
    public int GridHeight = 10;
    public float CellSize = 1f;

    [SerializeField] private float yStartPosition = 10f;

    [Header("Generation Settings")]
    public bool InstantGeneration = false;

    [HideInInspector] public Cell[,] Grid;

    private MazeGenerator mazeGenerator;

    // IMazeGrid implementation - Properties
    int IMazeGrid.GridWidth => GridWidth;
    int IMazeGrid.GridHeight => GridHeight;
    public int TotalCellCount => GridWidth * GridHeight;
    Cell[,] IMazeGrid.Grid => Grid;

    /// <summary>
    /// Initializes the grid and starts maze generation.
    /// </summary>
    public void Init()
    {
        mazeGenerator = GetComponent<MazeGenerator>();
        Grid = new Cell[GridWidth, GridHeight];
        initializeHexPrefab();
        lastHeight = Height;

        // Generate the grid instantly or with animation
        if (InstantGeneration)
        {
            generateGridInstant();
        }
        else
        {
            StartCoroutine(generateGridCoroutine());
        }
    }

    /// <summary>
    /// Updates cell heights if the Height property has changed.
    /// </summary>
    void Update()
    {
        if (Grid == null || Grid.Length == 0) return; // Ensure grid is initialized
        updateCellHeights();
    }

    /// <summary>
    /// Initializes the hex prefab's size and height.
    /// </summary>
    private void initializeHexPrefab()
    {
        if (hexPrefab != null)
        {
            Cell hexPrefabGen = hexPrefab.GetComponent<Cell>();
            if (hexPrefabGen != null)
            {
                hexPrefabGen.height = Height;
                hexPrefabGen.outerSize = CellSize;
            }
        }
    }

    /// <summary>
    /// Updates all cell heights if the Height property has changed.
    /// </summary>
    private void updateCellHeights()
    {
        if (!Mathf.Approximately(Height, lastHeight))
        {
            lastHeight = Height;
            updateAllCellHeights(Height);
        }
    }

    /// <summary>
    /// Sets the height for all cells in the grid and regenerates their meshes.
    /// </summary>
    private void updateAllCellHeights(float newHeight)
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Cell cell = Grid[x, y];
                if (cell != null)
                {
                    cell.height = newHeight;
                    cell.GenerateMesh();
                }
            }
        }
    }

    /// <summary>
    /// Instantly generates the hex grid and places all cells.
    /// </summary>
    private void generateGridInstant()
    {
        float height = Mathf.Sqrt(3) * CellSize;

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

    /// <summary>
    /// Generates the hex grid with animation (coroutine).
    /// </summary>
    private IEnumerator generateGridCoroutine()
    {
        float height = Mathf.Sqrt(3) * CellSize;

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

    /// <summary>
    /// Calculates the world position for a hex cell at grid coordinates (x, y).
    /// </summary>
    private Vector3 calculateHexPosition(int x, int y, float height)
    {
        float xPos = x * (CellSize * 1.5f);
        float zPos = y * height;

        // Offset every other column for hex grid staggering
        if (x % 2 == 1)
        {
            zPos += height / 2f;
        }

        return new Vector3(xPos, yStartPosition, zPos);
    }

    /// <summary>
    /// Instantiates a hex cell at the given position and sets it up.
    /// </summary>
    private void instantiateHex(Vector3 position, int x, int y, bool instant)
    {
        if (hexPrefab == null) return;

        GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);
        hex.name = $"Hex_{x}_{y}";

        Vector3 landingPosition = new Vector3(position.x, 0, position.z);

        if (instant)
        {
            // Place instantly at ground level
            hex.transform.position = landingPosition;
        }
        else
        {
            // Animate dropping the cell into place
            StartCoroutine(moveCell(hex, landingPosition));
        }

        setupHexComponent(hex, x, y);
    }

    /// <summary>
    /// Sets up the Cell component for a hex cell and stores it in the grid.
    /// </summary>
    private void setupHexComponent(GameObject cell, int x, int y)
    {
        Cell hexComponent = cell.GetComponent<Cell>();
        if (hexComponent != null)
        {
            hexComponent.outerSize = CellSize;
            hexComponent.height = Height;
            hexComponent.gridX = x;
            hexComponent.gridY = y;
            Grid[x, y] = hexComponent;
        }
    }

    /// <summary>
    /// Animates a cell moving from its start position to its landing position.
    /// </summary>
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

    /// <summary>
    /// Returns the neighbor cell in the specified direction for a given cell.
    /// </summary>
    public Cell GetNeighborInDirection(Cell cell, Cell.Direction direction)
    {
        // Offset arrays for even and odd columns in a hex grid
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
        bool evenCol = cell.gridX % 2 == 0;
        int dx, dy;

        // Choose the correct offset array based on column parity
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

        int newX = cell.gridX + dx;
        int newY = cell.gridY + dy;

        // Check bounds and return neighbor if valid
        if (newX >= 0 && newX < GridWidth && newY >= 0 && newY < GridHeight)
        {
            return Grid[newX, newY];
        }
        return null;
    }

    /// <summary>
    /// Returns a list of visited neighbor cells for the given cell.
    /// </summary>
    public List<Cell> GetVisitedNeighbors(Cell cell)
    {
        List<Cell> visited = new();

        // Check all directions for visited neighbors
        foreach (Cell.Direction dir in cell.GetAllDirections())
        {
            var neighbor = GetNeighborInDirection(cell, dir);
            if (neighbor != null && neighbor.visited)
            {
                visited.Add(neighbor);
            }
        }

        return visited;
    }

    /// <summary>
    /// Returns the opposite direction for a given direction.
    /// </summary>
    public Cell.Direction GetOppositeDirection(Cell.Direction direction)
    {
        // For hex grids, opposite is always +3 mod 6
        return (Cell.Direction)(((int)direction + 3) % 6);
    }

    /// <summary>
    /// Returns the direction from one cell to an adjacent cell.
    /// </summary>
    public Cell.Direction GetDirectionBetween(Cell from, Cell to)
    {
        foreach (Cell.Direction dir in from.GetAllDirections())
        {
            var neighbor = GetNeighborInDirection(from, dir);
            if (neighbor == to)
            {
                return dir;
            }
        }

        Debug.LogWarning($"Direction not found between {from.name} and {to.name}");
        return 0;
    }

    /// <summary>
    /// Returns true if there is a wall between two adjacent cells in the given direction.
    /// </summary>
    public bool HasWallBetween(Cell cell1, Cell cell2, Cell.Direction dir)
    {
        return cell1.Walls[(int)dir] && cell2.Walls[(int)GetOppositeDirection(dir)];
    }

    /// <summary>
    /// Sets the visited state of a cell.
    /// </summary>
    public void SetVisited(Cell cell, bool visited)
    {
        cell.visited = visited;
    }

    /// <summary>
    /// Returns true if the cell has been visited.
    /// </summary>
    public bool IsVisited(Cell cell)
    {
        return cell.visited;
    }

    /// <summary>
    /// Returns the designated start cell for maze generation (bottom-left by default).
    /// </summary>
    public Cell GetStartCell()
    {
        return Grid[0, GridHeight - 1];
    }

    /// <summary>
    /// Returns a shuffled array of directions for the given cell.
    /// </summary>
    public Cell.Direction[] GetShuffledDirections(Cell cell)
    {
        return cell.GetShuffledDirections();
    }

    /// <summary>
    /// Returns the world position of the given cell.
    /// </summary>
    public Vector3 GetCellPosition(Cell cell)
    {
        return cell.transform.position;
    }

    /// <summary>
    /// Disables the wall/face between two adjacent cells in the specified direction.
    /// </summary>
    public void DisableFaceBetween(Cell cell1, Cell cell2, Cell.Direction direction)
    {
        cell1.DisableFace(direction);
        cell2.DisableFace(GetOppositeDirection(direction));
    }
}