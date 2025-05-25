using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Depth-First Search maze generation algorithm
/// </summary>
public class DFSMazeAlgorithm : IMazeAlgorithm
{
    public string AlgorithmName => "Depth-First Search";
    
    // Used for coroutine-based generation to track the current path
    private List<Cell> pathStack = new ();

    
    public void GenerateInstant(IMazeGrid grid, Cell startCell)
    {
        // Stack for DFS traversal
        Stack<Cell> stack = new();
        stack.Push(startCell);

        int visitedCount = 0;

        // Continue until all cells are visited or stack is empty
        while (stack.Count > 0 && visitedCount < grid.TotalCellCount)
        {
            Cell current = stack.Peek();

            // Mark the cell as visited if not already
            if (!grid.IsVisited(current))
            {
                grid.SetVisited(current, true);
                visitedCount++;
            }

            // Get directions in random order for maze randomness
            Cell.Direction[] directions = current.GetShuffledDirections();
            bool foundValidNeighbor = false;

            // Try each direction to find an unvisited neighbor
            foreach (Cell.Direction dir in directions)
            {
                Cell nextCell = grid.GetNeighborInDirection(current, dir);
                // Skip if neighbor is null or already visited
                if (nextCell == null || grid.IsVisited(nextCell))
                    continue;

                foundValidNeighbor = true;
                // Remove wall between current and neighbor
                grid.DisableFaceBetween(current, nextCell, dir);
                // Mark neighbor as visited
                grid.SetVisited(nextCell, true);
                visitedCount++;
                // Push neighbor to stack for further exploration
                stack.Push(nextCell);
                break;
            }

            // If no unvisited neighbors, backtrack
            if (!foundValidNeighbor)
            {
                stack.Pop();
            }
        }
    }

    public IEnumerator GenerateCoroutine(IMazeGrid grid, Cell startCell)
    {
        // Clear the path stack and add the starting cell
        pathStack.Clear();
        pathStack.Add(startCell);

        int visitedCount = 0;

        // Continue until all cells are visited or stack is empty
        while (pathStack.Count > 0 && visitedCount < grid.TotalCellCount)
        {
            // Wait for a short duration to animate the process
            yield return new WaitForSeconds(0.1f);

            // Get the current cell (top of the stack)
            Cell current = pathStack[pathStack.Count - 1];
            SetCurrentCellState(current);

            // Mark the cell as visited if not already
            if (!grid.IsVisited(current))
            {
                grid.SetVisited(current, true);
                visitedCount++;
            }

            // Get directions in random order for maze randomness
            Cell.Direction[] directions = current.GetShuffledDirections();
            bool foundValidNeighbor = false;

            // Try each direction to find an unvisited neighbor
            foreach (Cell.Direction dir in directions)
            {
                Cell nextCell = grid.GetNeighborInDirection(current, dir);
                // Skip if neighbor is null or already visited
                if (nextCell == null || grid.IsVisited(nextCell))
                    continue;

                foundValidNeighbor = true;
                // Remove wall between current and neighbor
                grid.DisableFaceBetween(current, nextCell, dir);
                // Mark neighbor as visited
                grid.SetVisited(nextCell, true);
                visitedCount++;
                // Add neighbor to the path stack for further exploration
                pathStack.Add(nextCell);
                break;
            }

            // If no unvisited neighbors, backtrack and update cell state
            if (!foundValidNeighbor)
            {
                pathStack.RemoveAt(pathStack.Count - 1);
                SetBacktrackedState(current);
            }
        }

        // After generation, reset all cell states to visited
        ClearCurrentCellStates();
    }
    
    /// <summary>
    /// Sets the current cell's state for visualization.
    /// </summary>
    protected void SetCurrentCellState(Cell cell)
    {
        if (cell != null)
        {
            // Mark the cell as the current cell for visualization
            cell.SetCellState(Cell.CellState.Current);
        }
    }
    
    /// <summary>
    /// Sets the cell's state to backtracked or visited for visualization.
    /// </summary>
    private void SetBacktrackedState(Cell cell)
    {
        if (cell != null && cell.visitCount > 1)
        {
            // If the cell has been visited more than once, mark as backtracked
            cell.SetCellState(Cell.CellState.Backtracked);
        }
        else if (cell != null)
        {
            // Otherwise, mark as visited
            cell.SetCellState(Cell.CellState.Visited);
        }
    }
    
    /// <summary>
    /// Clears the state of all cells marked as current or backtracked.
    /// </summary>
    private void ClearCurrentCellStates()
    {
        // Set all cells in the path stack to visited state
        foreach (var cell in pathStack)
        {
            if (cell != null)
            {
                cell.SetCellState(Cell.CellState.Visited);
            }
        }
        // Clear the path stack
        pathStack.Clear();
    }
}