using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Wilson's algorithm for maze generation
/// </summary>
public class WilsonMazeAlgorithm : IMazeAlgorithm
{
    public string AlgorithmName => "Wilson's Algorithm";
    
    public void GenerateInstant(IMazeGrid grid, Cell startCell)
    {
        // Get all cells in the grid
        List<Cell> unvisited = GetAllCells(grid);
        
        // Start with a random cell and mark it as visited
        Cell first = unvisited[Random.Range(0, unvisited.Count)];
        grid.SetVisited(first, true);
        unvisited.Remove(first);

        // Continue until all cells are visited
        while (unvisited.Count > 0)
        {
            // Pick a random unvisited cell to start a random walk
            Cell current = unvisited[Random.Range(0, unvisited.Count)];
            List<Cell> walk = PerformRandomWalk(grid, current);

            // Connect the walk to the maze and mark cells as visited
            ConnectWalkToMaze(grid, walk, unvisited);
        }
    }

    public IEnumerator GenerateCoroutine(IMazeGrid grid, Cell startCell)
    {
        // Get all cells in the grid
        List<Cell> unvisited = GetAllCells(grid);
        
        // Start with a random cell and mark it as visited
        Cell first = unvisited[Random.Range(0, unvisited.Count)];
        grid.SetVisited(first, true);
        unvisited.Remove(first);
        SetCurrentCellState(first);

        // Continue until all cells are visited
        while (unvisited.Count > 0)
        {
            // Pick a random unvisited cell to start a random walk
            Cell current = unvisited[Random.Range(0, unvisited.Count)];
            SetCurrentCellState(current);

            List<Cell> walk = PerformRandomWalk(grid, current);

            // Animate the connection of the walk to the maze
            yield return ConnectWalkToMazeAnimated(grid, walk, unvisited);
        }

        // After generation, reset all cell states to visited
        ClearCurrentCellStates(grid);
    }
    
    /// <summary>
    /// Returns a list of all cells in the grid.
    /// </summary>
    private List<Cell> GetAllCells(IMazeGrid grid)
    {
        List<Cell> allCells = new ();
        
        for (int x = 0; x < grid.GridWidth; x++)
        {
            for (int y = 0; y < grid.GridHeight; y++)
            {
                Cell cell = grid.Grid[x, y];
                if (cell != null)
                {
                    allCells.Add(cell);
                }
            }
        }
        
        return allCells;
    }
    
    /// <summary>
    /// Performs a loop-erased random walk from the start cell until a visited cell is reached.
    /// </summary>
    private List<Cell> PerformRandomWalk(IMazeGrid grid, Cell start)
    {
        List<Cell> walk = new List<Cell> { start };

        // Continue walking until a visited cell is reached
        while (!grid.IsVisited(walk.Last()))
        {
            var directions = grid.GetShuffledDirections(walk.Last());
            if (directions.Length == 0) break;
            
            var direction = directions[0];
            var next = grid.GetNeighborInDirection(walk.Last(), direction);

            if (next == null)
                continue;

            // If the next cell is already in the walk, erase the loop
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
    
    /// <summary>
    /// Connects the random walk to the maze by removing walls and marking cells as visited.
    /// </summary>
    private void ConnectWalkToMaze(IMazeGrid grid, List<Cell> walk, List<Cell> unvisited)
    {
        for (int i = 0; i < walk.Count - 1; i++)
        {
            Cell from = walk[i];
            Cell to = walk[i + 1];
            
            // Find the direction from 'from' to 'to'
            Cell.Direction direction = grid.GetDirectionBetween(from, to);
            // Remove the wall between the two cells
            grid.DisableFaceBetween(from, to, direction);

            // Mark the cell as visited and remove from unvisited list
            grid.SetVisited(from, true);
            unvisited.Remove(from);
        }

        // Mark the last cell as visited if not already
        if (walk.Count > 0)
        {
            Cell lastCell = walk.Last();
            if (!grid.IsVisited(lastCell))
            {
                grid.SetVisited(lastCell, true);
                unvisited.Remove(lastCell);
            }
        }
    }
    
    /// <summary>
    /// Animates the connection of the random walk to the maze.
    /// </summary>
    private IEnumerator ConnectWalkToMazeAnimated(IMazeGrid grid, List<Cell> walk, List<Cell> unvisited)
    {
        for (int i = 0; i < walk.Count - 1; i++)
        {
            Cell from = walk[i];
            Cell to = walk[i + 1];
            
            SetCurrentCellState(from);
            
            // Find the direction from 'from' to 'to'
            Cell.Direction direction = grid.GetDirectionBetween(from, to);
            // Remove the wall between the two cells
            grid.DisableFaceBetween(from, to, direction);

            // Mark the cell as visited and remove from unvisited list
            grid.SetVisited(from, true);
            unvisited.Remove(from);

            // Wait for a short duration to animate the process
            yield return new WaitForSeconds(0.03f);
        }

        // Mark the last cell as visited if not already
        if (walk.Count > 0)
        {
            Cell lastCell = walk.Last();
            SetCurrentCellState(lastCell);
            
            if (!grid.IsVisited(lastCell))
            {
                grid.SetVisited(lastCell, true);
                unvisited.Remove(lastCell);
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
    }
}