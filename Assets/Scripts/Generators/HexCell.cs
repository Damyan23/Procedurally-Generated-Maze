using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Hexagonal cell implementation that inherits from Cell.
/// Handles mesh generation and wall management for hex cells.
/// </summary>
public class HexCell : Cell
{
    [Header("Hex-Specific Settings")]
    private float innerSize;

    [Header("Hex Wall Configuration")]
    public bool[] walls = new bool[6] { true, true, true, true, true, true };

    [Header("Debug")]
    [SerializeField] private bool showDirections = false;
    public Color gizmoColor = Color.cyan;

    // Define which directions this cell type uses
    private static readonly Direction[] hexDirections = new Direction[]
    {
        Direction.Up,
        Direction.UpRight,
        Direction.DownRight,
        Direction.Down,
        Direction.DownLeft,
        Direction.UpLeft
    };

    /// <summary>
    /// Gets the number of walls for a hex cell.
    /// </summary>
    public override int WallCount => 6;

    /// <summary>
    /// Gets or sets the wall states for this cell.
    /// </summary>
    public override bool[] Walls
    {
        get => walls;
        set => walls = value;
    }

    /// <summary>
    /// Returns all valid directions for a hex cell.
    /// </summary>
    public override Direction[] GetAllDirections()
    {
        return hexDirections;
    }

    /// <summary>
    /// Returns a shuffled array of directions for random traversal.
    /// </summary>
    public override Direction[] GetShuffledDirections()
    {
        // Clone the directions array to avoid modifying the original
        Direction[] directions = (Direction[])hexDirections.Clone();
        System.Random random = new System.Random();

        // Fisher-Yates shuffle for randomness
        for (int i = directions.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Direction temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }

        return directions;
    }

    /// <summary>
    /// Initializes mesh and sets inner size for the hex cell.
    /// </summary>
    protected override void InitIfNeeded()
    {
        base.InitIfNeeded();
        innerSize = outerSize * 0.7f;
    }

    /// <summary>
    /// Generates the mesh for the hex cell.
    /// </summary>
    public override void GenerateMesh()
    {
        InitIfNeeded();
        DrawFaces();
        CombineFaces();
    }

    /// <summary>
    /// Calculates a point on the hexagon for mesh generation.
    /// </summary>
    private Vector3 getPoint(float size, float height, int index)
    {
        float angleD = 120 - (60 * index);
        float angleR = angleD * Mathf.Deg2Rad;
        return new Vector3(size * Mathf.Cos(angleR), height, size * Mathf.Sin(angleR));
    }

    /// <summary>
    /// Creates a quad face for the mesh.
    /// </summary>
    private Face CreateFace(float radiusBottom, float radiusTop, float heightBottom, float heightTop, int point, bool reverse = false)
    {
        Vector3 pointA = getPoint(radiusBottom, heightBottom, point);
        Vector3 pointB = getPoint(radiusBottom, heightBottom, (point < 5) ? point + 1 : 0);
        Vector3 pointC = getPoint(radiusTop, heightTop, (point < 5) ? point + 1 : 0);
        Vector3 pointD = getPoint(radiusTop, heightTop, point);

        List<Vector3> vertecies = new List<Vector3>
        {
            pointA, // 0
            pointB, // 1
            pointC, // 2
            pointD  // 3
        };

        List<int> triangles = reverse
            ? new List<int> { 0, 3, 2, 2, 1, 0 } // flipped winding
            : new List<int> { 0, 1, 2, 2, 3, 0 }; // normal winding

        List<Vector2> uvs = new List<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        return new Face(vertecies, triangles, uvs);
    }

    /// <summary>
    /// Creates an end cap for a wall segment.
    /// </summary>
    private Face CreateWallEndCap(int wallIndex, bool isStart, float wallHeight, bool reverse = false)
    {
        // Determine if we're creating the cap at the start or end of the wall
        int pointIndex = isStart ? wallIndex : (wallIndex < 5 ? wallIndex + 1 : 0);

        // Get the points for inner and outer vertices at the specified height
        Vector3 innerPoint = getPoint(innerSize, wallHeight, pointIndex);
        Vector3 outerPoint = getPoint(outerSize, wallHeight, pointIndex);
        Vector3 innerPointBottom = getPoint(innerSize, 0, pointIndex);
        Vector3 outerPointBottom = getPoint(outerSize, 0, pointIndex);

        // Create vertices list
        List<Vector3> vertices = new List<Vector3>
        {
            innerPointBottom,  // 0: inner bottom
            outerPointBottom,  // 1: outer bottom
            outerPoint,        // 2: outer top
            innerPoint         // 3: inner top
        };

        // Create triangles - standard quad
        List<int> triangles = reverse ? new List<int> { 0, 3, 2, 2, 1, 0 } : new List<int> { 0, 1, 2, 2, 3, 0 };

        // Simple UVs for the cap
        List<Vector2> uvs = new List<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        return new Face(vertices, triangles, uvs);
    }

    /// <summary>
    /// Draws all faces (floor, walls, caps) for the hex cell mesh.
    /// </summary>
    protected override void DrawFaces()
    {
        faces = new List<Face>();
        Direction[] directions = GetAllDirections();

        // Create floor faces
        for (int i = 0; i < directions.Length; i++)
        {
            faces.Add(CreateFace(outerSize, 0, 0, 0, i));
        }

        // Add outer walls (sides) where visible
        for (int i = 0; i < directions.Length; i++)
        {
            if (walls[i])
            {
                // Add top face
                faces.Add(CreateFace(innerSize, outerSize, height, height, i, true));

                // Outer wall
                faces.Add(CreateFace(outerSize, outerSize, 0, height, i));

                // Inner wall (for thickness)
                faces.Add(CreateFace(innerSize, innerSize, 0, height, i, true));

                // Check if we need end caps (when adjacent walls are disabled)
                int prevWallIndex = (i + 5) % 6; // Previous wall
                int nextWallIndex = (i + 1) % 6; // Next wall

                // Add start cap if the previous wall is disabled
                if (!walls[prevWallIndex])
                {
                    faces.Add(CreateWallEndCap(i, true, height, false));
                }

                // Add end cap if the next wall is disabled
                if (!walls[nextWallIndex])
                {
                    faces.Add(CreateWallEndCap(i, false, height, true));
                }
            }
        }
    }

    /// <summary>
    /// Combines all faces into a single mesh and assigns materials.
    /// </summary>
    protected override void CombineFaces()
    {
        List<Vector3> vertecies = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<int> floorTriangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int vertexOffset = 0;
        for (int i = 0; i < faces.Count; i++)
        {
            Face currentFace = faces[i];
            vertecies.AddRange(currentFace.vertecies);
            uvs.AddRange(currentFace.uvs);

            // Assume the first 6 faces are the floor (adjust if needed)
            if (i < 6)
            {
                foreach (int index in currentFace.triangles)
                    floorTriangles.Add(index + vertexOffset);
            }
            else
            {
                foreach (int index in currentFace.triangles)
                    triangles.Add(index + vertexOffset);
            }
            vertexOffset += currentFace.vertecies.Count;
        }

        mesh.Clear();
        mesh.vertices = vertecies.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(floorTriangles.ToArray(), 0); // Floor
        mesh.SetTriangles(triangles.ToArray(), 1);      // Walls
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        // Assign materials based on cell state
        Material floorMat = GetFloorMaterial();
        meshRenderer.materials = new Material[] { floorMat, material };
    }

    /// <summary>
    /// Disables the wall/face at the given wall index and regenerates the mesh.
    /// </summary>
    public override void DisableFace(int dir)
    {
        // Normalize and validate direction
        int direction = ((dir % 6) + 6) % 6; // Handle negative values properly
        walls[direction] = false;
        GenerateMesh();
    }

    /// <summary>
    /// Draws debug gizmos for the hex cell, including direction indicators if enabled.
    /// </summary>
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos(); // Call base implementation for common state visualization

        if (!showDirections) return;

        Gizmos.color = gizmoColor;
        Vector3 center = transform.position;
        float arrowLength = outerSize * 0.8f;

        // Draw direction indicators
        Direction[] directions = GetAllDirections();
        for (int i = 0; i < directions.Length; i++)
        {
            if (walls[i])
            {
                // Direction vector
                float angleD = 60 * i;
                float angleR = angleD * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angleR), 0, Mathf.Sin(angleR));

                // Draw wall indicator
                Gizmos.DrawLine(center, center + direction * arrowLength);

                // Label the direction
#if UNITY_EDITOR
                if (UnityEditor.Selection.activeGameObject == gameObject)
                {
                    UnityEditor.Handles.Label(center + direction * (arrowLength + 0.2f),
                        $"{i}: {directions[i]}");
                }
#endif
            }
        }
    }
}