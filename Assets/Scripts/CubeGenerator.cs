using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CubeGenerator : MonoBehaviour
{
    public float outerSize = 1f;
    public float innerSize = 0.7f;
    public float height = 1f;

    [Header("Side Visibility")]
    public bool[] walls = new bool[6] { true, true, true, true, true, true };

    [Header("Debug")]
    public bool visited = false;
    public bool isStart = false;
    public int gridX = 0;
    public int gridY = 0;

    [Tooltip("Controls whether the bottom face is visible")]
    [SerializeField] private bool bottomVisible = true;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    [Header("References")]
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material floorMaterial;

    private void InitIfNeeded()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Cube";
            meshFilter.sharedMesh = mesh;
        }
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

        List<Vector3> vertices = new();
        List<int> floorTriangles = new();
        List<int> wallTriangles = new();
        List<Vector2> uvs = new();

        int v = 0;

        Vector3[] baseCorners = new Vector3[] {
            new Vector3(-outerSize, 0, -outerSize),
            new Vector3( outerSize, 0, -outerSize),
            new Vector3( outerSize, 0,  outerSize),
            new Vector3(-outerSize, 0,  outerSize)
        };

        Vector3[] topCorners = new Vector3[] {
            new Vector3(-outerSize, height, -outerSize),
            new Vector3( outerSize, height, -outerSize),
            new Vector3( outerSize, height,  outerSize),
            new Vector3(-outerSize, height,  outerSize)
        };

        Vector3[] baseInner = new Vector3[] {
            new Vector3(-innerSize, 0, -innerSize),
            new Vector3( innerSize, 0, -innerSize),
            new Vector3( innerSize, 0,  innerSize),
            new Vector3(-innerSize, 0,  innerSize)
        };

        Vector3[] topInner = new Vector3[] {
            new Vector3(-innerSize, height, -innerSize),
            new Vector3( innerSize, height, -innerSize),
            new Vector3( innerSize, height,  innerSize),
            new Vector3(-innerSize, height,  innerSize)
        };

        void AddFace(List<Vector3> face)
        {
            vertices.AddRange(face);
            wallTriangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 3, v });
            uvs.AddRange(new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            v += 4;
        }

        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;

            if (walls[i])
            {
                AddFace(CreateFace(baseCorners[i], baseCorners[next], topCorners[next], topCorners[i], true));
                AddFace(CreateFace(baseInner[next], baseInner[i], topInner[i], topInner[next], true));
                AddFace(CreateFace(topCorners[i], topCorners[next], topInner[next], topInner[i], true));
            }
            else
            {
                // Left cap for previous wall if needed
                int prev = (i + 3) % 4;
                if (walls[prev])
                    AddFace(CreateFace(baseCorners[i], baseInner[i], topInner[i], topCorners[i], true));

                // Right cap for next wall if needed
                int nextWall = (i + 1) % 4;
                if (walls[nextWall])
                    AddFace(CreateFace(baseInner[next], baseCorners[next], topCorners[next], topInner[next], true));
            }
        }

        if (bottomVisible)
        {
            var bottom = CreateFace(baseCorners[0], baseCorners[1], baseCorners[2], baseCorners[3], true);
            vertices.AddRange(bottom);
            floorTriangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 3, v });
            uvs.AddRange(new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            v += 4;
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(floorTriangles.ToArray(), 0);
        mesh.SetTriangles(wallTriangles.ToArray(), 1);
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        meshRenderer.materials = new Material[] { floorMaterial, wallMaterial };
    }

    public void DisableFace(int dir)
    {
        int index = ((dir % 6) + 6) % 6;
        walls[index] = false;
        GenerateMesh();
    }

    public void DisableFace(HexGennerator.HexDirection dir)
    {
        DisableFace((int)dir);
    }

    public HexGennerator.HexDirection[] GetShuffeldDirections()
    {
        HexGennerator.HexDirection[] directions = new HexGennerator.HexDirection[6]
        {
            HexGennerator.HexDirection.Up,
            HexGennerator.HexDirection.UpRight,
            HexGennerator.HexDirection.DownRight,
            HexGennerator.HexDirection.Down,
            HexGennerator.HexDirection.DownLeft,
            HexGennerator.HexDirection.UpLeft
        };

        System.Random rng = new();
        int n = directions.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            var temp = directions[n];
            directions[n] = directions[k];
            directions[k] = temp;
        }

        return directions;
    }

    private List<Vector3> CreateFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool reverse = false)
    {
        return reverse ? new List<Vector3> { a, d, c, b } : new List<Vector3> { a, b, c, d };
    }
}
