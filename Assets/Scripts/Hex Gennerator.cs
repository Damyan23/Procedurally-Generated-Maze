using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HexGennerator : MonoBehaviour
{
    private struct Face
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

    public enum HexDirection
    {
        Up = 0,
        UpRight = 1,
        DownRight = 2,
        Down = 3,
        DownLeft = 4,
        UpLeft = 5,
    }


    [Header("Settings")]
    [HideInInspector] public float outerSize = 1f;
    private float innerSize;
    [HideInInspector] public float height = 1f;

    [Header("Side Visibility")]
    public bool[] walls = new bool[6] { true, true, true, true, true, true };
    [HideInInspector] private HexDirection[] clockwiseDirections = new HexDirection[6]
    {
        HexDirection.Up, 
        HexDirection.UpRight, 
        HexDirection.DownRight, 
        HexDirection.Down, 
        HexDirection.DownLeft, 
        HexDirection.UpLeft
    };


    [HideInInspector] public bool visited = false;
    [HideInInspector] public bool isStart = false;
    [HideInInspector] public int gridX = 0;
    [HideInInspector] public int gridY = 0;
    [Header("Debug")]
    [SerializeField] private bool showDirections = false;


    [Tooltip("Controls whether the top face is visible")]
    [SerializeField] private bool topVisible = true;

    [Tooltip("Controls whether the bottom face is visible")]
    [SerializeField] private bool bottomVisible = true;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    [Header("References")]
    [SerializeField] private Material material;
    [SerializeField] private Material floorMaterial;

    private List<Face> faces = new List<Face>();
    private Dictionary<HexDirection, int> wallFaceIndices = new Dictionary<HexDirection, int>();

    // For debugging wall removal
    public Color gizmoColor = Color.cyan;

    private void InitIfNeeded()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Hex";
            meshFilter.sharedMesh = mesh;
        }

        if (material != null)
        {
            meshRenderer.sharedMaterial = material;
        }

        innerSize = outerSize * 0.7f;
    }

    void OnEnable()
    {
        InitIfNeeded();
        GenerateMesh();
    }

    void OnValidate()
    {
        if (mesh != null)
        {
            GenerateMesh();
        }
    }

    public void GenerateMesh()
    {
        InitIfNeeded();
        DrawFaces();
        CombineFaces();
    }

    private Vector3 getPoint(float size, float height, int index)
    {
        float angleD = 120 - (60 * index);
        float angleR = angleD * Mathf.Deg2Rad;

        return new Vector3(size * Mathf.Cos(angleR), height, size * Mathf.Sin(angleR));
    }


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

    // New method to create wall end caps
    private Face CreateWallEndCap(int wallIndex, bool isStart, float wallHeight, bool reverse = false)
    {
        // Determine if we're creating the cap at the start or end of the wall
        int pointIndex = isStart ? wallIndex : (wallIndex < 5 ? wallIndex + 1 : 0);

        // Get the points for inner and outer vertices at the specified height
        Vector3 innerPoint = getPoint(innerSize, wallHeight / 2, pointIndex);
        Vector3 outerPoint = getPoint(outerSize, wallHeight / 2, pointIndex);
        Vector3 innerPointBottom = getPoint(innerSize, -wallHeight / 2, pointIndex);
        Vector3 outerPointBottom = getPoint(outerSize, -wallHeight / 2, pointIndex);

        // Create vertices list
        List<Vector3> vertices = new List<Vector3>
        {
            innerPointBottom,  // 0: inner bottom
            outerPointBottom,  // 1: outer bottom
            outerPoint,        // 2: outer top
            innerPoint         // 3: inner top
        };

        // Create triangles - standard quad
        List<int> triangles = reverse ? new List<int> { 0, 3, 2, 2, 1, 0 } : new List<int> { 0, 1, 2, 2, 3, 0 }; // normal winding;

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


    void DrawFaces()
    {
        faces = new List<Face>();

        foreach (HexDirection direction in clockwiseDirections)
        {
            int point = (int)direction;
            faces.Add(CreateFace(outerSize, 0, -height / 2, -height / 2, point));
        }

        // Add outer walls (sides) where visible
        foreach (HexDirection direction in clockwiseDirections)
        {
            int point = (int)direction;
            if (walls[point])
            {
                // Add top face
                faces.Add(CreateFace(innerSize, outerSize, height / 2, height / 2, point, true));

                // Outer wall
                faces.Add(CreateFace(outerSize, outerSize, -height / 2, height / 2, point));

                // Inner wall (for thickness)
                faces.Add(CreateFace(innerSize, innerSize, -height / 2, height / 2, point, true));

                // Check if we need end caps (when adjacent walls are disabled)
                int prevWallIndex = (point + 5) % 6; // Previous wall
                int nextWallIndex = (point + 1) % 6; // Next wall

                // Add start cap if the previous wall is disabled
                if (!walls[prevWallIndex])
                {
                    faces.Add(CreateWallEndCap(point, true, height, false));
                }

                // Add end cap if the next wall is disabled
                if (!walls[nextWallIndex])
                {
                    faces.Add(CreateWallEndCap(point, false, height, true));
                }
            }
        }
    }

    void CombineFaces()
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

        // Assign both materials
        meshRenderer.materials = new Material[] { floorMaterial, material };
    }

    // Combined method with better error handling
    public void DisableFace(HexDirection dir)
    {
        int direction = (int)dir;
        DisableFace(direction);
    }

    public void DisableFace(int dir)
    {
        // Normalize and validate direction
        int direction = ((dir % 6) + 6) % 6; // Handle negative values properly
        walls[direction] = false;
        GenerateMesh();
    }

    public HexDirection[] GetShuffeldDirections()
    {
        HexDirection[] directions = clockwiseDirections;
        System.Random random = new System.Random();
        int n = directions.Length;

        while (n > 1)
        {
            int k = random.Next(n--);
            HexDirection value = directions[k];
            directions[k] = directions[n];
            directions[n] = value;
        }

        return directions;
    }

    // Debug gizmos to show wall directions
    private void OnDrawGizmos()
    {
        if (!showDirections) return;

        Gizmos.color = gizmoColor;
        Vector3 center = transform.position;
        float arrowLength = outerSize * 0.8f;

        // Draw direction indicators
        for (int i = 0; i < 6; i++)
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
                    UnityEditor.Handles.Label(center + direction * (arrowLength + 0.2f), i.ToString());
                }
#endif
            }
        }

        // If this is the start hex, mark it
        if (isStart)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(center + Vector3.up * 0.5f, 0.2f);
        }

        // If this hex has been visited, mark it
        if (visited)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(center + Vector3.up * 0.25f, new Vector3(0.1f, 0.1f, 0.1f));
        }
    }
}