using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Base class for all maze cells - contains common functionality and state.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public abstract class Cell : MonoBehaviour
{
    /// <summary>
    /// Universal direction enum that covers all possible cell types.
    /// Each cell type uses only the directions that apply to it.
    /// </summary>
    public enum Direction
    {
        // For hex grids
        Up = 0,
        UpRight = 1,
        DownRight = 2,
        Down = 3,
        DownLeft = 4,
        UpLeft = 5,
    }

    [System.Serializable]
    protected struct Face
    {
        public List<Vector3> vertecies { get; private set; }
        public List<int> triangles { get; private set; }
        public List<Vector2> uvs { get; private set; }

        public Face(List<Vector3> vertecies, List<int> triangles, List<Vector2> uvs)
        {
            this.vertecies = vertecies;
            this.triangles = triangles;
            this.uvs = uvs;
        }
    }

    public enum CellState
    {
        Unvisited,
        Current,
        Visited,
        Backtracked
    }

    [Header("Base Cell Settings")]
    [HideInInspector] public float outerSize = 1f;
    public float height;

    [Header("Cell State")]
    [HideInInspector] public bool visited = false;
    [HideInInspector] public bool isStart = false;
    [HideInInspector] public int gridX = 0;
    [HideInInspector] public int gridY = 0;
    [HideInInspector] public CellState currentState = CellState.Unvisited;
    [HideInInspector] public int visitCount = 0;

    [Header("Materials")]
    [SerializeField] protected Material material;
    [SerializeField] protected Material floorMaterial;
    [SerializeField] protected Material currentCellMaterial;
    [SerializeField] protected Material visitedCellMaterial;
    [SerializeField] protected Material backtrackedCellMaterial;

    // Protected fields for derived classes
    protected Mesh mesh;
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    protected List<Face> faces = new List<Face>();

    // Abstract properties that derived classes must implement
    public abstract int WallCount { get; }
    public abstract bool[] Walls { get; set; }
    
    // Abstract methods for direction handling - derived classes define their own direction logic
    public abstract Direction[] GetAllDirections();
    public abstract Direction[] GetShuffledDirections();

    /// <summary>
    /// Checks if a direction is valid for this cell type.
    /// </summary>
    public virtual bool IsValidDirection(Direction direction)
    {
        return GetAllDirections().Contains(direction);
    }

    /// <summary>
    /// Converts a direction to the corresponding wall index.
    /// </summary>
    public virtual int DirectionToWallIndex(Direction direction)
    {
        Direction[] validDirections = GetAllDirections();
        for (int i = 0; i < validDirections.Length; i++)
        {
            if (validDirections[i] == direction)
                return i;
        }
        return -1; // Invalid direction for this cell type
    }

    /// <summary>
    /// Initializes mesh and renderer if needed.
    /// </summary>
    protected virtual void InitIfNeeded()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = GetType().Name;
            meshFilter.sharedMesh = mesh;
        }

        if (material != null)
        {
            meshRenderer.sharedMaterial = material;
        }
    }

    /// <summary>
    /// Called when the object is enabled. Initializes and generates the mesh.
    /// </summary>
    protected virtual void OnEnable()
    {
        InitIfNeeded();
        GenerateMesh();
    }

    /// <summary>
    /// Called when a value is changed in the inspector. Regenerates the mesh.
    /// </summary>
    protected virtual void OnValidate()
    {
        if (mesh != null)
        {
            GenerateMesh();
        }
    }

    // Abstract methods that derived classes must implement
    public abstract void GenerateMesh();
    protected abstract void DrawFaces();
    protected abstract void CombineFaces();

    /// <summary>
    /// Sets the cell's state and updates its visual representation.
    /// </summary>
    public virtual void SetCellState(CellState newState)
    {
        // Only increment visitCount and set visited when actually visiting
        if (newState == CellState.Visited)
        {
            visitCount++;
            visited = true;
        }

        // If this cell has been visited more than once, it's being backtracked through
        if (visitCount > 1 && newState != CellState.Current)
        {
            currentState = CellState.Backtracked;
        }
        else
        {
            currentState = newState;
        }

        GenerateMesh(); // Update the visual representation
    }

    /// <summary>
    /// Resets the cell to its initial, unvisited state.
    /// </summary>
    public virtual void ResetCell()
    {
        currentState = CellState.Unvisited;
        visited = false;
        visitCount = 0;
        GenerateMesh();
    }

    /// <summary>
    /// Selects the correct floor material based on the cell's state.
    /// </summary>
    protected virtual Material GetFloorMaterial()
    {
        switch (currentState)
        {
            case CellState.Current:
                return currentCellMaterial != null ? currentCellMaterial : floorMaterial;
            case CellState.Visited:
                return visitedCellMaterial != null ? visitedCellMaterial : floorMaterial;
            case CellState.Backtracked:
                return backtrackedCellMaterial != null ? backtrackedCellMaterial : floorMaterial;
            default:
                return floorMaterial;
        }
    }

    /// <summary>
    /// Disables the wall/face in the specified direction (by index).
    /// </summary>
    public abstract void DisableFace(int direction);
    
    /// <summary>
    /// Disables the wall/face using the Direction enum.
    /// </summary>
    public virtual void DisableFace(Direction direction)
    {
        int wallIndex = DirectionToWallIndex(direction);
        if (wallIndex >= 0)
        {
            DisableFace(wallIndex);
        }
        else
        {
            Debug.LogWarning($"Direction {direction} is not valid for {GetType().Name}");
        }
    }

    /// <summary>
    /// Draws debug gizmos in the editor to visualize cell state.
    /// </summary>
    protected virtual void OnDrawGizmos()
    {
        Vector3 center = transform.position;

        // If this is the start cell, mark it
        if (isStart)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(center + Vector3.up * 0.5f, 0.2f);
        }

        // Color code based on cell state
        switch (currentState)
        {
            case CellState.Current:
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(center + Vector3.up * 0.25f, new Vector3(0.15f, 0.15f, 0.15f));
                break;
            case CellState.Visited:
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(center + Vector3.up * 0.25f, new Vector3(0.1f, 0.1f, 0.1f));
                break;
            case CellState.Backtracked:
                Gizmos.color = Color.red;
                Gizmos.DrawCube(center + Vector3.up * 0.25f, new Vector3(0.12f, 0.12f, 0.12f));
                break;
        }
    }
}