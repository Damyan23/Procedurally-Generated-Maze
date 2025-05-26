using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Main maze generator controller - coordinates all maze generation components.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MazeGenerator : MonoBehaviour
{
    public enum MazeAlgorithmType { DFS, Prim, Wilson }

    [Header("Generation Settings")]
    public bool InstantMazeGeneration = false;
    public MazeAlgorithmType AlgorithmType = MazeAlgorithmType.DFS;

    [Header("Entrance and Exit")]
    [SerializeField] private bool createEntranceAndExit = true;

    [Header("Pathfinding")]
    [SerializeField] private bool showPathfindingPath = true;
    public bool InstantPathDrawing = false;
    [SerializeField] private float lineRendererWidth;
    [SerializeField] private Material pathMaterial;
    private LineRenderer lr;

    [Header("Components")]
    private IMazeGrid mazeGrid;
    private Dictionary<MazeAlgorithmType, IMazeAlgorithm> algorithms;
    private MazePathfinder pathfinder;
    private MazeEntranceExitManager entranceExitManager;

    [Header("State")]
    private Cell startCell;
    private Cell exitCell;
    private List<Cell> visitOrder = new ();

    /// <summary>
    /// Initializes the maze generator and all required components.
    /// </summary>
    void Awake()
    {
        if (lr == null)
            lr = GetComponent<LineRenderer>();

        lr.startWidth = lineRendererWidth;
        lr.endWidth = lineRendererWidth;
        lr.material = pathMaterial;
            
        initializeComponents();
    }

    /// <summary>
    /// Initializes algorithm, pathfinder, and entrance/exit manager components.
    /// </summary>
    private void initializeComponents()
    {
        // Initialize algorithms
        algorithms = new Dictionary<MazeAlgorithmType, IMazeAlgorithm>
        {
            { MazeAlgorithmType.DFS, new DFSMazeAlgorithm() },
            { MazeAlgorithmType.Prim, new PrimMazeAlgorithm() },
            { MazeAlgorithmType.Wilson, new WilsonMazeAlgorithm() }
        };
        
        pathfinder = new MazePathfinder();
        entranceExitManager = new MazeEntranceExitManager();
    }

    /// <summary>
    /// Initializes the maze grid, resets state, and starts maze generation.
    /// </summary>
    public void Init()
    {
        // Get grid component
        mazeGrid = GetComponent<IMazeGrid>();
        if (mazeGrid == null)
        {
            Debug.LogError("GridController must implement IMazeGrid interface!");
            return;
        }
        
        startCell = mazeGrid.GetStartCell();
        exitCell = null;
        
        ResetMazeState();
        
        if (createEntranceAndExit)
        {
            entranceExitManager.CreateEntrance(mazeGrid, startCell);
        }

        // Start maze generation (instant or coroutine)
        if (InstantMazeGeneration)
        {
            generateMazeInstant();
        }
        else
        {
            StartCoroutine(generateMazeCoroutine());
        }
    }

    /// <summary>
    /// Resets all cells in the maze to their initial state.
    /// </summary>
    public void ResetMazeState()
    {
        if (mazeGrid == null || mazeGrid.TotalCellCount == 0) return;

        visitOrder.Clear();

        for (int x = 0; x < mazeGrid.GridWidth; x++)
        {
            for (int y = 0; y < mazeGrid.GridHeight; y++)
            {
                Cell cell = mazeGrid.Grid[x, y];
                if (cell != null)
                {
                    mazeGrid.SetVisited(cell, false);
                    cell.ResetCell();
                }
            }
        }
    }

    /// <summary>
    /// Instantly generates the maze using the selected algorithm.
    /// </summary>
    private void generateMazeInstant()
    {
        if (algorithms.TryGetValue(AlgorithmType, out IMazeAlgorithm algorithm))
        {
            algorithm.GenerateInstant(mazeGrid, startCell);
            completeMazeGeneration();
        }
        else
        {
            Debug.LogError($"Algorithm {AlgorithmType} not found!");
        }
    }

    /// <summary>
    /// Generates the maze using the selected algorithm as a coroutine (animated).
    /// </summary>
    private IEnumerator generateMazeCoroutine()
    {
        if (algorithms.TryGetValue(AlgorithmType, out IMazeAlgorithm algorithm))
        {
            yield return StartCoroutine(algorithm.GenerateCoroutine(mazeGrid, startCell));
            completeMazeGeneration();
        }
        else
        {
            Debug.LogError($"Algorithm {AlgorithmType} not found!");
        }
    }

    /// <summary>
    /// Called after maze generation is complete. Handles exit creation and pathfinding.
    /// </summary>
    private void completeMazeGeneration()
    {
        // Track all visited cells for exit creation
        collectVisitedCells();
        
        if (createEntranceAndExit)
        {
            exitCell = entranceExitManager.CreateExit(mazeGrid, visitOrder);
        }

        if (showPathfindingPath && startCell != null && exitCell != null)
        {
            findAndDrawPath();
        }

        Debug.Log($"Maze generated using {algorithms[AlgorithmType].AlgorithmName}");
    }

    /// <summary>
    /// Collects all visited cells in the maze for use in exit creation.
    /// </summary>
    private void collectVisitedCells()
    {
        visitOrder.Clear();
        for (int x = 0; x < mazeGrid.GridWidth; x++)
        {
            for (int y = 0; y < mazeGrid.GridHeight; y++)
            {
                Cell cell = mazeGrid.Grid[x, y];
                if (cell != null && mazeGrid.IsVisited(cell))
                {
                    visitOrder.Add(cell);
                }
            }
        }
    }

    /// <summary>
    /// Finds a path from the entrance to the exit and draws it using the LineRenderer.
    /// </summary>
    private void findAndDrawPath()
    {
        List<Cell> pathToExit = pathfinder.FindPath(mazeGrid, startCell, exitCell);

        if (pathToExit != null && pathToExit.Count > 0)
        {
            if (InstantPathDrawing)
            {
                pathfinder.DrawPathInstant(lr, mazeGrid, pathToExit);
            }
            else
            {
                StartCoroutine(pathfinder.DrawPathAnimated(lr, mazeGrid, pathToExit));
            }
            Debug.Log($"Path found from entrance to exit with {pathToExit.Count} cells");
        }
        else
        {
            Debug.LogWarning("No path found from entrance to exit");
        }
    }

    /// <summary>
    /// Change algorithm and regenerate maze.
    /// </summary>
    public void SetAlgorithm(MazeAlgorithmType newAlgorithm)
    {
        AlgorithmType = newAlgorithm;
    }
    
    /// <summary>
    /// Get available algorithm names.
    /// </summary>
    public string[] GetAvailableAlgorithms()
    {
        List<string> algorithmNames = new List<string>();
        foreach (var kvp in algorithms)
        {
            algorithmNames.Add(kvp.Value.AlgorithmName);
        }
        return algorithmNames.ToArray();
    }
    
    /// <summary>
    /// Clear current path visualization.
    /// </summary>
    public void ClearPath()
    {
        if (lr != null)
        {
            lr.positionCount = 0;
        }
    }
    
    /// <summary>
    /// Regenerate maze with current settings.
    /// </summary>
    public void RegenerateMaze()
    {
        Init();
    }
}