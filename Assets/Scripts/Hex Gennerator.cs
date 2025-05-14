using System.Collections.Generic;
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
        West = 0,
        SouthWest = 1,
        SouthEast = 2,
        East = 3,
        NorthEast = 4,
        NorthWest = 5
    }


    [Header("Settings")]
    [HideInInspector] public float outerSize = 1f;
    private float innerSize;
    [HideInInspector] public float height = 1f;

    [Header("Side Visibility")]
    [HideInInspector] public bool[] walls = new bool[6] { true, true, true, true, true, true };
    [HideInInspector] public HexDirection[] path = new HexDirection[6] { HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast, HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest };
    [HideInInspector] public bool visited = false;
    [HideInInspector] public bool isStart = false;
    [HideInInspector] public int gridX = 0;
    [HideInInspector] public int gridY = 0;


    [Tooltip("Controls whether the top face is visible")]
    [SerializeField] private bool topVisible = true;
    
    [Tooltip("Controls whether the bottom face is visible")]
    [SerializeField] private bool bottomVisible = true;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    [Header("References")]
    [SerializeField] private Material material;

    private List<Face> faces = new();

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

        innerSize = outerSize - (outerSize % 1 + 0.2f);
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

    protected Vector3 GetPoint(float size, float height, int index)
    {
        float angleD = 60 * index;
        float angleR = angleD * Mathf.Deg2Rad;

        return new Vector3(size * Mathf.Cos(angleR), height, size * Mathf.Sin(angleR));
    }

    private Face CreateFace(float radiusBottom, float radiusTop, float heightBottom, float heightTop, int point, bool reverse = false)
    {
        Vector3 pointA = GetPoint(radiusBottom, heightBottom, point);
        Vector3 pointB = GetPoint(radiusBottom, heightBottom, (point < 5) ? point + 1 : 0);
        Vector3 pointC = GetPoint(radiusTop, heightTop, (point < 5) ? point + 1 : 0);
        Vector3 pointD = GetPoint(radiusTop, heightTop, point);

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

    void DrawFaces()
    {
        faces = new List<Face>();
        
        // Add top face if visible
        for (int point = 0; point < 6; point++)
        {
            if (walls[point])
            {
                faces.Add(CreateFace(innerSize, outerSize, height / 2, height / 2, point));
            }
        }

        // Add bottom face
        for (int point = 0; point < 6; point++)
        {
            faces.Add(CreateFace(outerSize, 0, -height / 2, -height / 2, point, true));
        }
        
        
        // Add outer walls (sides) where visible
        for (int point = 0; point < 6; point++)
        {
            if (walls[point])
            {
                faces.Add(CreateFace(outerSize, outerSize, -height / 2, height / 2, point, true));
            }
        }
        
        // Add inner walls (sides) where visible
        for (int point = 0; point < 6; point++)
        {
            if (walls[point])
            {
                faces.Add(CreateFace(innerSize, innerSize, -height / 2, height / 2, point));
            }
        }
    }

    void CombineFaces()
    {
        List<Vector3> vertecies = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        for (int i = 0; i < faces.Count; i++)
        {
            Face currentFace = faces[i];

            int vertexOffset = vertecies.Count;
            vertecies.AddRange(currentFace.vertecies);
            uvs.AddRange(currentFace.uvs);

            foreach (int index in currentFace.triangles)
            {
                triangles.Add(index + vertexOffset);
            }
        }

        mesh.Clear();
        mesh.vertices = vertecies.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();
    }

    public void DissableFace (int dir)
    {  
        int direction = dir % 6;
        if (direction < 0 || direction > 5)
        {
            Debug.LogError("Invalid direction. Must be between 0 and 5.");
            return;
        }
        walls[direction] = false;
        if (mesh != null)
        {
            GenerateMesh();
        }
    }
}