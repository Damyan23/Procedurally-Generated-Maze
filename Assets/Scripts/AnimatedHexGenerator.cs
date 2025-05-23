using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class extends the existing HexGenerator with animation features
// Attach this alongside your existing HexGenerator
[RequireComponent(typeof(HexGennerator))]
public class AnimatedHexGenerator : MonoBehaviour
{
    private HexGennerator hexGenerator;
    
    [Header("Wall Animation Settings")]
    [SerializeField] private float wallRemovalDuration = 0.5f;
    [SerializeField] private AnimationCurve wallRemovalCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float wallDissolveHeight = 2.0f;
    
    [Header("Visual Effects")]
    [SerializeField] private bool useParticleEffects = true;
    [SerializeField] private ParticleSystem wallParticlePrefab;
    [SerializeField] private Color wallParticleColor = Color.cyan;
    
    [Header("Audio")]
    [SerializeField] private bool playSound = true;
    [SerializeField] private AudioClip wallBreakSound;
    [SerializeField] private float volumeScale = 0.5f;
    
    [Header("Highlight Settings")]
    [SerializeField] private float highlightDuration = 0.3f;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color visitedColor = Color.blue;
    
    private MeshRenderer meshRenderer;
    private AudioSource audioSource;
    private Material originalMaterial;
    private Dictionary<int, GameObject> wallModels = new Dictionary<int, GameObject>();
    private Dictionary<int, bool> animatingWalls = new Dictionary<int, bool>();
    
    // References to wall vertices for each direction
    private Dictionary<int, Vector3[]> wallVertices = new Dictionary<int, Vector3[]>();
    
    void Awake()
    {
        hexGenerator = GetComponent<HexGennerator>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Create audio source if needed
        if (playSound && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.volume = volumeScale;
        }
        
        // Store reference to original material
        if (meshRenderer.sharedMaterial != null)
        {
            originalMaterial = meshRenderer.sharedMaterial;
        }
        
        // Override the DisableFace methods in HexGenerator
        // We use reflection to get the private methods
        var methodInfo = typeof(HexGennerator).GetMethod("DisableFace", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            null,
            new System.Type[] { typeof(int) },
            null);
            
        if (methodInfo != null)
        {
            // Create a detour for the method
            var originalMethod = System.Delegate.CreateDelegate(
                typeof(System.Action<int>), hexGenerator, methodInfo) as System.Action<int>;
                
            // Replace with our own implementation that adds animation
            System.Action<int> newMethod = (dir) => {
                AnimateWallRemoval(dir);
                originalMethod(dir);
            };
            
            // Store this somewhere, but we can't actually replace the method at runtime
            // without more advanced techniques beyond the scope of this example
        }
    }
    
    public void AnimateWallRemoval(int direction)
    {
        // Normalize direction index
        direction = ((direction % 6) + 6) % 6;
        
        // Skip if this wall is already being animated or is already removed
        if (animatingWalls.ContainsKey(direction) && animatingWalls[direction])
            return;
            
        if (!hexGenerator.walls[direction])
            return;
            
        // Mark this wall as being animated
        animatingWalls[direction] = true;
        
        // Start the coroutine
        StartCoroutine(AnimateWallDissolve(direction));
    }
    
    public void AnimateWallRemoval(HexGennerator.HexDirection direction)
    {
        AnimateWallRemoval((int)direction);
    }
    
    private IEnumerator AnimateWallDissolve(int direction)
    {
        // Get the vertices for this wall
        Vector3[] vertices = GetWallVertices(direction);
        if (vertices == null || vertices.Length < 4)
        {
            Debug.LogWarning("Wall vertices not found for direction: " + direction);
            animatingWalls[direction] = false;
            yield break;
        }
        
        // Create a temporary wall model to animate
        GameObject wallModel = CreateWallModel(vertices, direction);
        MeshRenderer wallRenderer = wallModel.GetComponent<MeshRenderer>();
        
        // Create particle effect
        if (useParticleEffects)
        {
            SpawnParticleEffect(GetWallCenter(vertices));
        }
        
        // Play sound effect
        if (playSound && wallBreakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wallBreakSound, volumeScale);
        }
        
        // Animate the wall
        float elapsed = 0;
        Vector3 startScale = wallModel.transform.localScale;
        Vector3 startPosition = wallModel.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * wallDissolveHeight;
        
        Material wallMat = wallRenderer.material;
        Color startColor = wallMat.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);
        
        while (elapsed < wallRemovalDuration)
        {
            elapsed += Time.deltaTime;
            float t = wallRemovalCurve.Evaluate(elapsed / wallRemovalDuration);
            
            // Scale down
            wallModel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            // Move up
            wallModel.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            
            // Fade out
            wallMat.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }
        
        // Clean up
        Destroy(wallModel);
        wallModels.Remove(direction);
        animatingWalls[direction] = false;
    }
    
    private Vector3[] GetWallVertices(int direction)
    {
        // If we already cached the vertices for this direction, return them
        if (wallVertices.ContainsKey(direction))
            return wallVertices[direction];
            
        // Otherwise, get the mesh data and extract the vertices for this wall
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null)
            return null;
            
        Vector3[] meshVertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        // This is a simplification - actual implementation would need to identify
        // which triangles correspond to which wall based on your mesh structure
        // For now, we'll just create a wall based on where it should be
        
        float outerSize = hexGenerator.outerSize;
        float innerSize = outerSize * 0.7f; // Same calculation as in your HexGenerator
        float height = hexGenerator.height;
        
        // Get the points that define this wall
        Vector3 pointA = GetPoint(outerSize, 0, direction);
        Vector3 pointB = GetPoint(outerSize, 0, (direction < 5) ? direction + 1 : 0);
        Vector3 pointC = GetPoint(outerSize, height, (direction < 5) ? direction + 1 : 0);
        Vector3 pointD = GetPoint(outerSize, height, direction);
        
        // Create vertices array
        Vector3[] vertices = new Vector3[] { pointA, pointB, pointC, pointD };
        
        // Cache for future use
        wallVertices[direction] = vertices;
        
        return vertices;
    }
    
    private Vector3 GetPoint(float size, float height, int index)
    {
        // This should match your getPoint method in HexGenerator
        float angleD = 120 - (60 * index);
        float angleR = angleD * Mathf.Deg2Rad;
        
        return new Vector3(
            size * Mathf.Cos(angleR), 
            height, 
            size * Mathf.Sin(angleR)
        );
    }
    
    private Vector3 GetWallCenter(Vector3[] vertices)
    {
        // Calculate center of wall from vertices
        Vector3 center = Vector3.zero;
        foreach (Vector3 vertex in vertices)
        {
            center += vertex;
        }
        center /= vertices.Length;
        
        // Apply object's transformation
        center = transform.TransformPoint(center);
        
        return center;
    }
    
    private GameObject CreateWallModel(Vector3[] vertices, int direction)
    {
        // Create a new game object for the wall
        GameObject wallObject = new GameObject("Wall_" + direction);
        wallObject.transform.parent = transform;
        wallObject.transform.localPosition = Vector3.zero;
        wallObject.transform.localRotation = Quaternion.identity;
        wallObject.transform.localScale = Vector3.one;
        
        // Add mesh filter and renderer
        MeshFilter meshFilter = wallObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = wallObject.AddComponent<MeshRenderer>();
        
        // Create mesh for the wall
        Mesh mesh = new Mesh();
        
        // Set vertices
        mesh.vertices = vertices;
        
        // Set triangles (assuming vertices are ordered correctly)
        mesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };
        
        // Set UVs
        mesh.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        
        // Recalculate normals
        mesh.RecalculateNormals();
        
        // Assign mesh to filter
        meshFilter.mesh = mesh;
        
        // Assign material
        Material wallMaterial = new Material(originalMaterial);
        meshRenderer.material = wallMaterial;
        
        // Store reference
        wallModels[direction] = wallObject;
        
        return wallObject;
    }
    
    private void SpawnParticleEffect(Vector3 position)
    {
        if (!useParticleEffects)
            return;
            
        ParticleSystem particles;
        
        if (wallParticlePrefab != null)
        {
            // Instantiate the prefab
            particles = Instantiate(wallParticlePrefab, position, Quaternion.identity);
        }
        else
        {
            // Create a simple particle system
            GameObject particleObj = new GameObject("WallParticles");
            particleObj.transform.position = position;
            particles = particleObj.AddComponent<ParticleSystem>();
            
            // Configure the particle system
            var main = particles.main;
            main.startColor = wallParticleColor;
            main.startSize = 0.1f;
            main.startSpeed = 1f;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, 20)
            });
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            var lifetime = particles.main;
            lifetime.startLifetime = 1.0f;
            
            // Add a renderer
            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            }
        }
        
        // Set to destroy after playing
        ParticleSystem.MainModule mainModule = particles.main;
        mainModule.stopAction = ParticleSystemStopAction.Destroy;
        particles.Play();
    }
    
    // Methods to highlight the hex
    public void Highlight(Color color, float duration = 0)
    {
        if (meshRenderer == null || meshRenderer.materials.Length < 2)
            return;
            
        Material[] mats = meshRenderer.materials;
        Material wallMat = new Material(mats[1]);
        wallMat.color = color;
        mats[1] = wallMat;
        meshRenderer.materials = mats;
        
        if (duration > 0)
        {
            StartCoroutine(ResetHighlight(duration));
        }
    }
    
    private IEnumerator ResetHighlight(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (meshRenderer == null || meshRenderer.materials.Length < 2)
            yield break;
            
        Material[] mats = meshRenderer.materials;
        mats[1] = originalMaterial;
        meshRenderer.materials = mats;
    }
    
    public void HighlightActive()
    {
        Highlight(activeColor, highlightDuration);
    }
    
    public void HighlightVisited()
    {
        Highlight(visitedColor, 0); // Permanent until reset
    }
    
    // Method to reset animations and highlights
    public void ResetAnimations()
    {
        // Stop all coroutines
        StopAllCoroutines();
        
        // Reset highlight
        if (meshRenderer != null && originalMaterial != null && meshRenderer.materials.Length > 1)
        {
            Material[] mats = meshRenderer.materials;
            mats[1] = originalMaterial;
            meshRenderer.materials = mats;
        }
        
        // Destroy wall models
        foreach (var wallModel in wallModels.Values)
        {
            if (wallModel != null)
                Destroy(wallModel);
        }
        wallModels.Clear();
        animatingWalls.Clear();
    }
}