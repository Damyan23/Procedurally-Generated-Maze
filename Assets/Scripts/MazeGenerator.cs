using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    private GridController grid;
    private object startCell;

    public LineRenderer lr;

    [Header("Maze Algorithm")]
    public MazeAlgorithmType algorithmType = MazeAlgorithmType.DFS;

    private int currentRecursionDepth = 0;
    private int visitedCellCount = 0;
    private int totalCellCount = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugPath = true;

    private List<object> path = new(); // DFS
    private List<object> frontier = new(); // Prim

    public enum MazeAlgorithmType { DFS, Prim, Wilson }

    public void Init()
    {
        grid = GetComponent<GridController>();
        startCell = grid.GetStartCell();
        totalCellCount = grid.gridWidth * grid.gridHeight;
        visitedCellCount = 0;

        // Reset all cells
        var currentGrid = grid.GetCurrentGrid();
        if (currentGrid != null)
        {
            for (int x = 0; x < grid.gridWidth; x++)
            {
                for (int y = 0; y < grid.gridHeight; y++)
                {
                    var cell = currentGrid[x, y];
                    if (cell != null)
                    {
                        grid.SetVisited(cell, false);
                    }
                }
            }
        }

        switch (algorithmType)
        {
            case MazeAlgorithmType.DFS:
                path.Clear();
                StartCoroutine(GenerateMazeDFS(startCell));
                break;
            case MazeAlgorithmType.Prim:
                frontier.Clear();
                StartCoroutine(GenerateMazePrim(startCell));
                break;
            case MazeAlgorithmType.Wilson:
                StartCoroutine(GenerateMazeWilson());
                break;
        }
    }

    private IEnumerator GenerateMazeDFS(object currentCell)
    {
        if (visitedCellCount >= totalCellCount)
        {
            Debug.Log($"Maze generated (DFS) - {grid.GetGridType()}");
            yield break;
        }

        if (currentRecursionDepth > 0) yield return null;
        if (path.Count > 0)
            yield return new WaitForSeconds(0.1f);

        currentRecursionDepth++;

        if (!grid.IsVisited(currentCell))
        {
            grid.SetVisited(currentCell, true);
            visitedCellCount++;
        }

        HexGennerator.HexDirection[] directions = grid.GetShuffledDirections(currentCell);
        bool foundValidNeighbor = false;

        foreach (HexGennerator.HexDirection dir in directions)
        {
            object nextCell = grid.GetNeighborInDirection(currentCell, dir);
            if (nextCell == null || grid.IsVisited(nextCell))
                continue;

            foundValidNeighbor = true;
            RemoveWalls(currentCell, nextCell, dir);
            grid.SetVisited(nextCell, true);
            visitedCellCount++;
            path.Add(nextCell);

            DrawDebugPath(currentCell);

            yield return StartCoroutine(GenerateMazeDFS(nextCell));
            break;
        }

        if (!foundValidNeighbor && path.Count > 0)
        {
            path.RemoveAt(path.Count - 1);
            if (path.Count > 0)
            {
                object lastCell = path[path.Count - 1];
                yield return StartCoroutine(GenerateMazeDFS(lastCell));
            }
        }

        currentRecursionDepth--;
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator GenerateMazePrim(object start)
    {
        grid.SetVisited(start, true);
        visitedCellCount++;
        AddFrontier(start);

        while (visitedCellCount < totalCellCount)
        {
            if (frontier.Count == 0) break;

            object frontierCell = frontier[Random.Range(0, frontier.Count)];
            var neighbors = grid.GetVisitedNeighbors(frontierCell);

            if (neighbors.Count > 0)
            {
                object neighbor = neighbors[Random.Range(0, neighbors.Count)];
                var dir = grid.GetDirectionBetween(neighbor, frontierCell);
                RemoveWalls(neighbor, frontierCell, dir);
            }

            grid.SetVisited(frontierCell, true);
            visitedCellCount++;
            AddFrontier(frontierCell);
            frontier.Remove(frontierCell);

            DrawDebugPath(frontierCell);
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log($"Maze generated (Prim) - {grid.GetGridType()}");
    }

    private IEnumerator GenerateMazeWilson()
    {
        List<object> unvisited = new();
        var currentGrid = grid.GetCurrentGrid();
        
        for (int x = 0; x < grid.gridWidth; x++)
        {
            for (int y = 0; y < grid.gridHeight; y++)
            {
                var cell = currentGrid[x, y];
                if (cell != null)
                {
                    unvisited.Add(cell);
                }
            }
        }

        object first = unvisited[Random.Range(0, unvisited.Count)];
        grid.SetVisited(first, true);
        visitedCellCount++;
        unvisited.Remove(first);

        while (unvisited.Count > 0)
        {
            object current = unvisited[Random.Range(0, unvisited.Count)];
            Dictionary<object, HexGennerator.HexDirection> pathDict = new();
            List<object> walk = new() { current };

            while (!grid.IsVisited(walk.Last()))
            {
                var directions = grid.GetShuffledDirections(walk.Last());
                if (directions.Length == 0) break;
                
                var dir = directions[0];
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
                    pathDict[next] = grid.GetDirectionBetween(walk.Last(), next);
                    walk.Add(next);
                }
            }

            for (int i = 0; i < walk.Count - 1; i++)
            {
                var from = walk[i];
                var to = walk[i + 1];
                var dir = grid.GetDirectionBetween(from, to);
                RemoveWalls(from, to, dir);
                grid.SetVisited(from, true);
                visitedCellCount++;
                unvisited.Remove(from);
                DrawDebugPath(from);
                yield return new WaitForSeconds(0.03f);
            }

            if (walk.Count > 0)
            {
                grid.SetVisited(walk.Last(), true);
                visitedCellCount++;
                unvisited.Remove(walk.Last());
            }
        }

        Debug.Log($"Maze generated (Wilson) - {grid.GetGridType()}");
    }

    private void AddFrontier(object cell)
    {
        var directions = grid.GetShuffledDirections(cell);
        foreach (var dir in directions)
        {
            var neighbor = grid.GetNeighborInDirection(cell, dir);
            if (neighbor != null && !grid.IsVisited(neighbor) && !frontier.Contains(neighbor))
                frontier.Add(neighbor);
        }
    }

    void DrawDebugPath(object currentCell)
    {
        if (!showDebugPath || lr == null) return;
        
        Vector3 position = grid.GetCellPosition(currentCell);
        lr.positionCount = Mathf.Max(1, currentRecursionDepth);
        lr.SetPosition(Mathf.Max(0, currentRecursionDepth - 1), position + Vector3.up);
        lr.startColor = Color.red;
        lr.endColor = Color.red;
    }

    private void RemoveWalls(object cell1, object cell2, HexGennerator.HexDirection direction)
    {
        Debug.Log($"For cell: {grid.GetCellName(cell1)}, found neighbor: {grid.GetCellName(cell2)}, in direction: {direction}");
        grid.DisableFaceBetween(cell1, cell2, direction);
    }
}