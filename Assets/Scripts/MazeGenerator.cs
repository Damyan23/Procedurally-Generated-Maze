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

    public LineRenderer lr;

    [Header("Maze Algorithm")]
    public MazeAlgorithmType algorithmType = MazeAlgorithmType.DFS;

    private int currentRecursionDepth = 0;
    private int visitedHexCount = 0;
    private int totalHexCount = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugPath = true;

    private List<HexGennerator> path = new(); // DFS
    private List<HexGennerator> frontier = new(); // Prim

    public enum MazeAlgorithmType { DFS, Prim, Wilson }

    public void Init()
    {
        grid = GetComponent<GridController>();
        startHex = grid.hexGrid[0, grid.gridHeight - 1];
        totalHexCount = grid.gridWidth * grid.gridHeight;
        visitedHexCount = 0;

        foreach (var hex in grid.hexGrid)
            hex.visited = false;

        Handles.color = Color.red;

        switch (algorithmType)
        {
            case MazeAlgorithmType.DFS:
                path.Clear();
                StartCoroutine(generateMazeDFS(startHex));
                break;
            case MazeAlgorithmType.Prim:
                frontier.Clear();
                StartCoroutine(generateMazePrim(startHex));
                break;
            case MazeAlgorithmType.Wilson:
                StartCoroutine(generateMazeWilson());
                break;
        }
    }

    private IEnumerator generateMazeDFS(HexGennerator currentHex)
    {
        if (visitedHexCount >= totalHexCount)
        {            
            Debug.Log("Maze generated (DFS)");
            yield break;
        }

        if (currentRecursionDepth > 0) yield return null;
        if (path.Count > 0)
            yield return new WaitForSeconds(0.1f);

        currentRecursionDepth++;

        if (!currentHex.visited)
        {
            currentHex.visited = true;
            visitedHexCount++;
        }

        HexGennerator.HexDirection[] directions = currentHex.GetShuffeldDirections();
        bool foundValidNeighbor = false;

        foreach (HexGennerator.HexDirection dir in directions)
        {
            HexGennerator nextHex = grid.GetNeighborInDirection(currentHex, dir);
            if (nextHex == null || nextHex.visited)
                continue;

            foundValidNeighbor = true;
            removeWalls(currentHex, nextHex, dir);
            nextHex.visited = true;
            visitedHexCount++;
            path.Add(nextHex);

            drawDebugPath(currentHex);

            yield return StartCoroutine(generateMazeDFS(nextHex));
            break;
        }

        if (!foundValidNeighbor && path.Count > 0)
        {
            path.RemoveAt(path.Count - 1);
            if (path.Count > 0)
            {
                HexGennerator lastHex = path[path.Count - 1];
                yield return StartCoroutine(generateMazeDFS(lastHex));
            }
        }

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator generateMazePrim(HexGennerator start)
    {
        start.visited = true;
        visitedHexCount++;
        AddFrontier(start);

        while (visitedHexCount < totalHexCount)
        {
            if (frontier.Count == 0) break;

            HexGennerator frontierHex = frontier[Random.Range(0, frontier.Count)];
            var neighbors = grid.GetVisitedNeighbors(frontierHex);

            if (neighbors.Count > 0)
            {
                HexGennerator neighbor = neighbors[Random.Range(0, neighbors.Count)];
                var dir = grid.GetDirectionBetween(neighbor, frontierHex);
                removeWalls(neighbor, frontierHex, dir);
            }

            frontierHex.visited = true;
            visitedHexCount++;
            AddFrontier(frontierHex);
            frontier.Remove(frontierHex);

            drawDebugPath(frontierHex);
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("Maze generated (Prim)");
    }

    private IEnumerator generateMazeWilson()
    {
        List<HexGennerator> unvisited = new();
        foreach (var hex in grid.hexGrid)
            unvisited.Add(hex);

        HexGennerator first = unvisited[Random.Range(0, unvisited.Count)];
        first.visited = true;
        visitedHexCount++;
        unvisited.Remove(first);

        while (unvisited.Count > 0)
        {
            HexGennerator current = unvisited[Random.Range(0, unvisited.Count)];
            Dictionary<HexGennerator, HexGennerator.HexDirection> path = new();
            List<HexGennerator> walk = new()current };

            while (!walk.Last().visited)
            {
                var dir = walk.Last().GetShuffeldDirections()[0];
                var next = grid.GetNeighborInDirection(walk.Last(), dir);

                if (next == null)
                    continue;

                if (walk.Contains(next))
                {
                    int loopIndex = walk.IndexOf(next);
                    walk.RemoveRange(loopIndex + 1, walk.Count - loopIndex - 1);
                }
                else
                {
                    path[next] = grid.GetDirectionBetween(walk.Last(), next);
                    walk.Add(next);
                }
            }

            for (int i = 0; i < walk.Count - 1; i++)
            {
                var from = walk[i];
                var to = walk[i + 1];
                var dir = grid.GetDirectionBetween(from, to);
                removeWalls(from, to, dir);
                from.visited = true;
                visitedHexCount++;
                unvisited.Remove(from);
                drawDebugPath(from);
                yield return new WaitForSeconds(0.03f);
            }

            walk.Last().visited = true;
            visitedHexCount++;
            unvisited.Remove(walk.Last());
        }

        Debug.Log("Maze generated (Wilson)");
    }

    private void AddFrontier(HexGennerator hex)
    {
        foreach (var dir in hex.GetShuffeldDirections())
        {
            var neighbor = grid.GetNeighborInDirection(hex, dir);
            if (neighbor != null && !neighbor.visited && !frontier.Contains(neighbor))
                frontier.Add(neighbor);
        }
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