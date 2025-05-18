using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    private GridController grid;
    private HexGennerator startHex;
    private List<HexGennerator> path = new();

    public LineRenderer lr;

    private int currentRecursionDepth = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugPath = true;
    public void Init()
    {
        grid = GetComponent<GridController>();
        startHex = grid.hexGrid[0, grid.gridHeight - 1]; // Get top left hex
        StartCoroutine(generateMaze(startHex));
        Handles.color = Color.red;
    }

    private IEnumerator generateMaze(HexGennerator currentHex)
    {
        if (currentRecursionDepth > 0) yield return null;

        if (path.Count > 0)
            yield return new WaitForSeconds(0.5f);
        
        currentRecursionDepth++;

        currentHex.visited = true; // Mark it as visited

        // Try all directions in a random order
        HexGennerator.HexDirection[] directions = currentHex.GetShuffeldDirections();

        bool foundValidNeighbor = false;

        // Try each direction to find an unvisited neighbor
        foreach (HexGennerator.HexDirection dir in directions)
        {
            HexGennerator nextHex = grid.GetNeighborInDirection(currentHex, dir);

            // Skip if neighbor is null or already visited
            if (nextHex == null || nextHex.visited)
                continue;

            // Found a valid neighbor
            foundValidNeighbor = true;

            // Remove walls between current and next hex
            removeWalls(currentHex, nextHex, dir);

            // Mark next hex as visited and add it to path
            nextHex.visited = true;
            path.Add(nextHex);

            drawDebugPath(currentHex);

            // Recursively generate maze from next hex
            yield return StartCoroutine(generateMaze(nextHex));
            break; // Once we've processed one neighbor, we're done with this iteration
        }

        // If no valid neighbors were found, backtrack
        if (!foundValidNeighbor && path.Count > 0)
        {
            // Remove current hex from path
            path.RemoveAt(path.Count - 1);

            // If path is not empty, continue from the last hex in the path
            if (path.Count > 0)
            {
                HexGennerator lastHex = path[path.Count - 1];

                yield return StartCoroutine (generateMaze(lastHex));
            }
        }

        yield return new WaitForSeconds(0.1f);
    }

    void drawDebugPath(HexGennerator currentHex)
    {
        if (!showDebugPath) return;

        lr.positionCount = currentRecursionDepth;
        lr.SetPosition(currentRecursionDepth - 1, currentHex.transform.position + Vector3.up);
        lr.startColor = Color.red;
    }

    private void removeWalls(HexGennerator hex1, HexGennerator hex2, HexGennerator.HexDirection direction)
    {
        Debug.Log("For hex:" + hex1.name + ", found neighbor:" + hex2 + ", in direction:" + direction);
        hex1.DisableFace(direction);
        hex2.DisableFace(grid.GetOppositeDirection(direction));
        
    }
}
