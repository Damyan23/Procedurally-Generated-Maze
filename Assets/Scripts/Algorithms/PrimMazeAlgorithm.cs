using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prim's algorithm for maze generation
/// </summary>
public class PrimMazeAlgorithm : IMazeAlgorithm
{
    public string AlgorithmName => "Prim's Algorithm";
    
    // List of frontier cells (cells adjacent to the maze)
    private List<Cell> frontier = new ();

    public void GenerateInstant(IMazeGrid grid, Cell startCell)
    {
        // Clear the frontier list
        frontier.Clear();
        
        // Mark the start cell as visited and add its neighbors to the frontier
        grid.SetVisited(startCell, true);
        int visitedCount = 1;
        AddToFrontier(grid, startCell);

        // Continue until all cells are visited or frontier is empty
        while (visitedCount < grid.TotalCellCount && frontier.Count > 0)
        {
            // Pick a random frontier cell
            Cell frontierCell = frontier[Random.Range(0, frontier.Count)];
            // Get its visited neighbors
            var neighbors = grid.GetVisitedNeighbors(frontierCell);

            if (neighbors.Count > 0)
            {
                // Connect to a random visited neighbor
                Cell neighbor = neighbors[Random.Range(0, neighbors.Count)];
                var direction = grid.GetDirectionBetween(neighbor, frontierCell);
                grid.DisableFaceBetween(neighbor, frontierCell, direction);
            }

            // Mark the frontier cell as visited and add its neighbors to the frontier
            grid.SetVisited(frontierCell, true);
            visitedCount++;
            AddToFrontier(grid, frontierCell);
            // Remove the cell from the frontier
            frontier.Remove(frontierCell);
        }
    }

    public IEnumerator GenerateCoroutine(IMazeGrid grid, Cell startCell)
    {
        // Clear the frontier list
        frontier.Clear();
        
        // Mark the start cell as visited and add its neighbors to the frontier
        grid.SetVisited(startCell, true);
        int visitedCount = 1;
        AddToFrontier(grid, startCell);
        
        SetCurrentCellState(startCell);

        // Continue until all cells are visited or frontier is empty
        while (visitedCount < grid.TotalCellCount && frontier.Count > 0)
        {
            // Pick a random frontier cell
            Cell frontierCell = frontier[Random.Range(0, frontier.Count)];
            SetCurrentCellState(frontierCell);
            
            // Get its visited neighbors
            var neighbors = grid.GetVisitedNeighbors(frontierCell);

            if (neighbors.Count > 0)
            {
                // Connect to a random visited neighbor
                Cell neighbor = neighbors[Random.Range(0, neighbors.Count)];
                var direction = grid.GetDirectionBetween(neighbor, frontierCell);
                grid.DisableFaceBetween(neighbor, frontierCell, direction);
            }

            // Mark the frontier cell as visited and add its neighbors to the frontier
            grid.SetVisited(frontierCell, true);
            visitedCount++;
            AddToFrontier(grid, frontierCell);
            // Remove the cell from the frontier
            frontier.Remove(frontierCell);

            // Wait for a short duration to animate the process
            yield return new WaitForSeconds(0.05f);
        }
        
        // After generation, reset all cell states to visited
        ClearCurrentCellStates(grid);
    }
    
    /// <summary>
    /// Adds all unvisited neighbors of the given cell to the frontier list.
    /// </summary>
    private void AddToFrontier(IMazeGrid grid, Cell cell)
    {
        var directions = cell.GetShuffledDirections();
        foreach (var direction in directions)
        {
            var neighbor = grid.GetNeighborInDirection(cell, direction);
            // Add neighbor if it is unvisited and not already in the frontier
            if (neighbor != null && !grid.IsVisited(neighbor) && !frontier.Contains(neighbor))
            {
                frontier.Add(neighbor);
            }
        }
    }
    
    /// <summary>
    /// Sets the current cell's state for visualization.
    /// </summary>
    public void SetCurrentCellState(Cell cell)
    {
        if (cell != null)
        {
            cell.SetCellState(Cell.CellState.Current);
        }
    }
    
    /// <summary>
    /// Clears the state of all cells marked as current or backtracked.
    /// </summary>
    private void ClearCurrentCellStates(IMazeGrid grid)
    {
        // Set all visited cells to visited state
        for (int x = 0; x < grid.GridWidth; x++)
        {
            for (int y = 0; y < grid.GridHeight; y++)
            {
                Cell cell = grid.Grid[x, y];
                if (cell != null && grid.IsVisited(cell))
                {
                    cell.SetCellState(Cell.CellState.Visited);
                }
            }
        }
        // Clear the frontier list
        frontier.Clear();
    }
}