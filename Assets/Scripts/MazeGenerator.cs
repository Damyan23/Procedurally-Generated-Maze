using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MazeGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public bool InstantMazeGeneration = false;
    public MazeAlgorithmType AlgorithmType = MazeAlgorithmType.DFS;

    [Header("Entrance and Exit")]
    public bool CreateEntranceAndExit = true;

    [Header("Pathfinding")]
    public bool ShowPathfindingPath = true;
    public bool InstantPathDrawing = false;
    public Color PathColor = Color.green;

    public LineRenderer Lr;

    public enum MazeAlgorithmType { DFS, Prim, Wilson }

    private GridController grid;
    private HexGennerator startCell;
    private HexGennerator exitCell;
    private HexGennerator currentCell;

    private int visitedCellCount = 0;
    private int totalCellCount = 0;

    private List<HexGennerator> pathDFS = new List<HexGennerator>();
    private List<HexGennerator> frontierPrim = new List<HexGennerator>();
    private List<HexGennerator> visitOrder = new List<HexGennerator>();

    void Awake()
    {
        if (Lr == null)
            Lr = GetComponent<LineRenderer>();
    }

    public void Init()
    {
        grid = GetComponent<GridController>();
        startCell = grid.GetStartCell();
        exitCell = null;
        totalCellCount = grid.GridWidth * grid.GridHeight;
        visitedCellCount = 0;
        currentCell = null;
        
        resetMazeState();
        
        if (CreateEntranceAndExit)
        {
            createEntrance(startCell);
        }

        if (InstantMazeGeneration)
        {
            generateMazeInstant();
        }
        else
        {
            generateMazeCoroutine();
        }
    }

    private void resetMazeState()
    {
        visitOrder.Clear();
        pathDFS.Clear();
        frontierPrim.Clear();

        var currentGrid = grid.HexGrid;
        if (currentGrid != null)
        {
            for (int x = 0; x < grid.GridWidth; x++)
            {
                for (int y = 0; y < grid.GridHeight; y++)
                {
                    HexGennerator hex = currentGrid[x, y];
                    if (hex != null)
                    {
                        grid.SetVisited(hex, false);
                        hex.ResetCell();
                    }
                }
            }
        }
    }

    private void generateMazeInstant()
    {
        switch (AlgorithmType)
        {
            case MazeAlgorithmType.DFS:
                generateMazeDFSInstant(startCell);
                break;
            case MazeAlgorithmType.Prim:
                generateMazePrimInstant(startCell);
                break;
            case MazeAlgorithmType.Wilson:
                generateMazeWilsonInstant();
                break;
        }

        completeMazeGeneration();
    }

    private void generateMazeCoroutine()
    {
        switch (AlgorithmType)
        {
            case MazeAlgorithmType.DFS:
                StartCoroutine(generateMazeDFSCoroutine(startCell));
                break;
            case MazeAlgorithmType.Prim:
                StartCoroutine(generateMazePrimCoroutine(startCell));
                break;
            case MazeAlgorithmType.Wilson:
                StartCoroutine(generateMazeWilsonCoroutine());
                break;
        }
    }

    private void completeMazeGeneration()
    {
        clearCurrentCell();
        
        if (CreateEntranceAndExit)
        {
            createExit();
        }

        if (ShowPathfindingPath && startCell != null && exitCell != null)
        {
            findAndDrawPath();
        }

        Debug.Log($"Maze generated ({AlgorithmType})");
    }

    #region Entrance and Exit Creation
    private void createEntrance(HexGennerator cell)
    {
        HexGennerator.HexDirection[] allDirections = getAllDirections();

        foreach (HexGennerator.HexDirection direction in allDirections)
        {
            HexGennerator neighbor = grid.GetNeighborInDirection(cell, direction);
            if (neighbor == null)
            {
                cell.DisableFace(direction);
                Debug.Log($"Created entrance at {cell.name} in direction {direction}");
                break;
            }
        }
    }

    private void createExit()
    {
        List<HexGennerator> lastColumnCells = getLastColumnCells();

        if (lastColumnCells.Count == 0)
        {
            Debug.LogWarning("No visited cells found in last column, falling back to last visited cell");
            if (visitOrder.Count > 0)
            {
                exitCell = visitOrder[visitOrder.Count - 1];
                forceCreateExitAtCell(exitCell);
                Debug.Log($"Forced exit creation at {exitCell.name}");
            }
            return;
        }

        shuffleList(lastColumnCells);

        foreach (HexGennerator candidate in lastColumnCells)
        {
            if (tryCreateExitAtCell(candidate))
            {
                exitCell = candidate;
                Debug.Log($"Created exit at {candidate.name} in last column");
                return;
            }
        }

        if (lastColumnCells.Count > 0)
        {
            exitCell = lastColumnCells[Random.Range(0, lastColumnCells.Count)];
            forceCreateExitAtCell(exitCell);
            Debug.Log($"Forced exit creation at {exitCell.name} in last column");
        }
    }

    private List<HexGennerator> getLastColumnCells()
    {
        List<HexGennerator> lastColumnCells = new List<HexGennerator>();
        var currentGrid = grid.HexGrid;
        int lastColumnIndex = grid.GridWidth - 1;

        for (int y = 0; y < grid.GridHeight; y++)
        {
            HexGennerator cell = currentGrid[lastColumnIndex, y];
            if (cell != null && grid.IsVisited(cell))
            {
                lastColumnCells.Add(cell);
            }
        }

        return lastColumnCells;
    }

    private bool tryCreateExitAtCell(HexGennerator cell)
    {
        HexGennerator.HexDirection[] allDirections = getAllDirections();

        foreach (HexGennerator.HexDirection direction in allDirections)
        {
            HexGennerator neighbor = grid.GetNeighborInDirection(cell, direction);
            if (neighbor == null)
            {
                cell.DisableFace(direction);
                return true;
            }
        }
        return false;
    }

    private void forceCreateExitAtCell(HexGennerator cell)
    {
        HexGennerator.HexDirection[] allDirections = getAllDirections();

        foreach (HexGennerator.HexDirection direction in allDirections)
        {
            HexGennerator neighbor = grid.GetNeighborInDirection(cell, direction);
            if (neighbor == null)
            {
                cell.DisableFace(direction);
                return;
            }
        }

        cell.DisableFace(allDirections[0]);
    }
    #endregion

    #region DFS Algorithm
    private void generateMazeDFSInstant(HexGennerator currentHex)
    {
        Stack<HexGennerator> stack = new Stack<HexGennerator>();
        stack.Push(currentHex);

        while (stack.Count > 0 && visitedCellCount < totalCellCount)
        {
            HexGennerator current = stack.Peek();
            
            if (!grid.IsVisited(current))
            {
                grid.SetVisited(current, true);
                visitedCellCount++;
                trackVisitedCell(current);
            }

            HexGennerator.HexDirection[] directions = current.GetShuffeldDirections();
            bool foundValidNeighbor = false;

            foreach (HexGennerator.HexDirection dir in directions)
            {
                HexGennerator nextCell = grid.GetNeighborInDirection(current, dir);
                if (nextCell == null || grid.IsVisited(nextCell))
                    continue;

                foundValidNeighbor = true;
                removeWalls(current, nextCell, dir);
                grid.SetVisited(nextCell, true);
                visitedCellCount++;
                trackVisitedCell(nextCell);
                stack.Push(nextCell);
                break;
            }

            if (!foundValidNeighbor)
            {
                stack.Pop();
            }
        }
    }

    private IEnumerator generateMazeDFSCoroutine(HexGennerator currentHex)
    {
        if (visitedCellCount >= totalCellCount)
        {
            completeMazeGeneration();
            yield break;
        }

        if (pathDFS.Count > 0)
            yield return new WaitForSeconds(0.1f);
            
        setCurrentCell(currentHex);

        if (!grid.IsVisited(currentHex))
        {
            grid.SetVisited(currentHex, true);
            visitedCellCount++;
            trackVisitedCell(currentHex);
        }

        HexGennerator.HexDirection[] directions = currentHex.GetShuffeldDirections();
        bool foundValidNeighbor = false;

        foreach (HexGennerator.HexDirection dir in directions)
        {
            HexGennerator nextCell = grid.GetNeighborInDirection(currentHex, dir);
            if (nextCell == null || grid.IsVisited(nextCell))
                continue;

            foundValidNeighbor = true;
            removeWalls(currentHex, nextCell, dir);
            grid.SetVisited(nextCell, true);
            visitedCellCount++;
            trackVisitedCell(nextCell);
            pathDFS.Add(nextCell);

            yield return StartCoroutine(generateMazeDFSCoroutine(nextCell));
            break;
        }

        if (!foundValidNeighbor && pathDFS.Count > 0)
        {
            pathDFS.RemoveAt(pathDFS.Count - 1);
            if (pathDFS.Count > 0)
            {
                HexGennerator lastCell = pathDFS[pathDFS.Count - 1];
                lastCell.visitCount++;
                yield return StartCoroutine(generateMazeDFSCoroutine(lastCell));
            }
        }
        yield return new WaitForSeconds(0.1f);
    }
    #endregion

    #region Prim Algorithm
    private void generateMazePrimInstant(HexGennerator start)
    {
        setCurrentCell(start);
        grid.SetVisited(start, true);
        visitedCellCount++;
        trackVisitedCell(start);
        addFrontier(start);

        while (visitedCellCount < totalCellCount && frontierPrim.Count > 0)
        {
            HexGennerator frontierCell = frontierPrim[Random.Range(0, frontierPrim.Count)];
            var neighbors = grid.GetVisitedNeighbors(frontierCell);

            if (neighbors.Count > 0)
            {
                HexGennerator neighbor = neighbors[Random.Range(0, neighbors.Count)];
                var dir = grid.GetDirectionBetween(neighbor, frontierCell);
                removeWalls(neighbor, frontierCell, dir);
            }

            grid.SetVisited(frontierCell, true);
            visitedCellCount++;
            trackVisitedCell(frontierCell);
            addFrontier(frontierCell);
            frontierPrim.Remove(frontierCell);
        }
    }

    private IEnumerator generateMazePrimCoroutine(HexGennerator start)
    {
        setCurrentCell(start);
        grid.SetVisited(start, true);
        visitedCellCount++;
        trackVisitedCell(start);
        addFrontier(start);

        while (visitedCellCount < totalCellCount)
        {
            if (frontierPrim.Count == 0) break;

            HexGennerator frontierCell = frontierPrim[Random.Range(0, frontierPrim.Count)];
            setCurrentCell(frontierCell);
            
            var neighbors = grid.GetVisitedNeighbors(frontierCell);

            if (neighbors.Count > 0)
            {
                HexGennerator neighbor = neighbors[Random.Range(0, neighbors.Count)];
                var dir = grid.GetDirectionBetween(neighbor, frontierCell);
                removeWalls(neighbor, frontierCell, dir);
            }

            grid.SetVisited(frontierCell, true);
            visitedCellCount++;
            trackVisitedCell(frontierCell);
            addFrontier(frontierCell);
            frontierPrim.Remove(frontierCell);

            yield return new WaitForSeconds(0.05f);
        }

        completeMazeGeneration();
    }
    #endregion

    #region Wilson Algorithm
    private void generateMazeWilsonInstant()
    {
        List<HexGennerator> unvisited = getAllCells();
        
        HexGennerator first = unvisited[Random.Range(0, unvisited.Count)];
        grid.SetVisited(first, true);
        visitedCellCount++;
        trackVisitedCell(first);
        unvisited.Remove(first);

        while (unvisited.Count > 0)
        {
            HexGennerator current = unvisited[Random.Range(0, unvisited.Count)];
            List<HexGennerator> walk = performRandomWalk(current);

            for (int i = 0; i < walk.Count - 1; i++)
            {
                HexGennerator from = walk[i];
                HexGennerator to = walk[i + 1];
                
                HexGennerator.HexDirection dir = grid.GetDirectionBetween(from, to);
                removeWalls(from, to, dir);

                grid.SetVisited(from, true);
                visitedCellCount++;
                trackVisitedCell(from);
                unvisited.Remove(from);
            }

            if (walk.Count > 0)
            {
                HexGennerator lastCell = walk.Last();
                grid.SetVisited(lastCell, true);
                visitedCellCount++;
                trackVisitedCell(lastCell);
                unvisited.Remove(lastCell);
            }
        }
    }

    private IEnumerator generateMazeWilsonCoroutine()
    {
        List<HexGennerator> unvisited = getAllCells();
        
        HexGennerator first = unvisited[Random.Range(0, unvisited.Count)];
        setCurrentCell(first);
        grid.SetVisited(first, true);
        visitedCellCount++;
        trackVisitedCell(first);
        unvisited.Remove(first);

        while (unvisited.Count > 0)
        {
            HexGennerator current = unvisited[Random.Range(0, unvisited.Count)];
            setCurrentCell(current);
            
            List<HexGennerator> walk = performRandomWalk(current);

            for (int i = 0; i < walk.Count - 1; i++)
            {
                HexGennerator from = walk[i];
                HexGennerator to = walk[i + 1];
                setCurrentCell(from);
                
                HexGennerator.HexDirection dir = grid.GetDirectionBetween(from, to);
                removeWalls(from, to, dir);

                grid.SetVisited(from, true);
                visitedCellCount++;
                trackVisitedCell(from);
                unvisited.Remove(from);

                yield return new WaitForSeconds(0.03f);
            }

            if (walk.Count > 0)
            {
                HexGennerator lastCell = walk.Last();
                setCurrentCell(lastCell);
                grid.SetVisited(lastCell, true);
                visitedCellCount++;
                trackVisitedCell(lastCell);
                unvisited.Remove(lastCell);
            }
        }

        completeMazeGeneration();
    }

    private List<HexGennerator> performRandomWalk(HexGennerator start)
    {
        List<HexGennerator> walk = new List<HexGennerator> { start };

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
                walk.Add(next);
            }
        }

        return walk;
    }
    #endregion

    #region Pathfinding
    private void findAndDrawPath()
    {
        if (startCell == null || exitCell == null) return;

        List<HexGennerator> pathToExit = findPathBFS(startCell, exitCell);
        
        if (pathToExit != null && pathToExit.Count > 0)
        {
            if (InstantPathDrawing)
            {
                drawPathInstant(pathToExit);
            }
            else
            {
                StartCoroutine(drawPathCoroutine(pathToExit));
            }
            Debug.Log($"Path found from entrance to exit with {pathToExit.Count} cells");
        }
        else
        {
            Debug.LogWarning("No path found from entrance to exit");
        }
    }

    private List<HexGennerator> findPathBFS(HexGennerator start, HexGennerator target)
    {
        if (start == null || target == null) return null;

        Queue<HexGennerator> queue = new Queue<HexGennerator>();
        Dictionary<HexGennerator, HexGennerator> cameFrom = new Dictionary<HexGennerator, HexGennerator>();
        HashSet<HexGennerator> visited = new HashSet<HexGennerator>();

        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            HexGennerator current = queue.Dequeue();

            if (current == target)
            {
                return reconstructPath(cameFrom, target);
            }

            HexGennerator.HexDirection[] allDirections = getAllDirections();

            foreach (HexGennerator.HexDirection direction in allDirections)
            {
                HexGennerator neighbor = grid.GetNeighborInDirection(current, direction);
                
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    if (!grid.HasWallBetween(current, neighbor, direction))
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return null;
    }

    private List<HexGennerator> reconstructPath(Dictionary<HexGennerator, HexGennerator> cameFrom, HexGennerator target)
    {
        List<HexGennerator> path = new List<HexGennerator>();
        HexGennerator pathCell = target;
        
        while (pathCell != null)
        {
            path.Add(pathCell);
            cameFrom.TryGetValue(pathCell, out pathCell);
        }
        
        path.Reverse();
        return path;
    }

    private void drawPathInstant(List<HexGennerator> pathToDraw)
    {
        if (Lr == null || pathToDraw == null || pathToDraw.Count == 0) return;

        Lr.positionCount = pathToDraw.Count;
        Lr.startColor = PathColor;
        Lr.endColor = PathColor;

        for (int i = 0; i < pathToDraw.Count; i++)
        {
            Vector3 position = grid.GetCellPosition(pathToDraw[i]);
            Lr.SetPosition(i, position + Vector3.up * 0.1f);
        }
    }

    private IEnumerator drawPathCoroutine(List<HexGennerator> pathToDraw)
    {
        if (Lr == null || pathToDraw == null || pathToDraw.Count == 0) yield break;

        yield return new WaitForSeconds(0.5f);

        Lr.positionCount = 0;
        Lr.startColor = PathColor;
        Lr.endColor = PathColor;

        for (int i = 0; i < pathToDraw.Count; i++)
        {
            Lr.positionCount = i + 1;
            Vector3 position = grid.GetCellPosition(pathToDraw[i]);
            Lr.SetPosition(i, position + Vector3.up * 0.1f);

            yield return new WaitForSeconds(0.05f);
        }
    }
    #endregion

    #region Helper Methods
    private void setCurrentCell(HexGennerator newCurrentCell)
    {
        if (currentCell != null && currentCell.visited)
        {
            if (currentCell.visitCount > 1)
            {
                currentCell.SetCellState(HexGennerator.CellState.Backtracked);
            }
            else
            {
                currentCell.SetCellState(HexGennerator.CellState.Visited);
            }
        }

        currentCell = newCurrentCell;
        if (currentCell != null)
        {
            currentCell.SetCellState(HexGennerator.CellState.Current);
        }
    }

    private void clearCurrentCell()
    {
        if (currentCell != null)
        {
            currentCell.SetCellState(HexGennerator.CellState.Visited);
            currentCell = null;
        }
    }

    private void trackVisitedCell(HexGennerator cell)
    {
        if (!visitOrder.Contains(cell))
        {
            visitOrder.Add(cell);
        }
    }

    private void addFrontier(HexGennerator hex)
    {
        var directions = grid.GetShuffledDirections(hex);
        foreach (var dir in directions)
        {
            var neighbor = grid.GetNeighborInDirection(hex, dir);
            if (neighbor != null && !grid.IsVisited(neighbor) && !frontierPrim.Contains(neighbor))
                frontierPrim.Add(neighbor);
        }
    }

    private void removeWalls(HexGennerator hex1, HexGennerator hex2, HexGennerator.HexDirection direction)
    {
        Debug.Log($"For cell: {hex1}, found neighbor: {hex2}, in direction: {direction}");
        grid.DisableFaceBetween(hex1, hex2, direction);
    }

    private HexGennerator.HexDirection[] getAllDirections()
    {
        return System.Enum.GetValues(typeof(HexGennerator.HexDirection))
            .Cast<HexGennerator.HexDirection>().ToArray();
    }

    private List<HexGennerator> getAllCells()
    {
        List<HexGennerator> allCells = new List<HexGennerator>();
        var currentGrid = grid.HexGrid;
        
        for (int x = 0; x < grid.GridWidth; x++)
        {
            for (int y = 0; y < grid.GridHeight; y++)
            {
                HexGennerator cell = currentGrid[x, y];
                if (cell != null)
                {
                    allCells.Add(cell);
                }
            }
        }
        
        return allCells;
    }

    private void shuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    #endregion
}