using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages creation of maze entrances and exits.
/// </summary>
public class MazeEntranceExitManager
{
    /// <summary>
    /// Creates an entrance at the specified cell by removing a wall at the maze boundary.
    /// </summary>
    /// <param name="grid">The maze grid.</param>
    /// <param name="cell">The cell where the entrance will be created.</param>
    public void CreateEntrance(IMazeGrid grid, Cell cell)
    {
        if (cell == null) return;

        // Get all possible directions for this cell type
        Cell.Direction[] allDirections = cell.GetAllDirections();

        // Try each direction to find a boundary (no neighbor)
        foreach (Cell.Direction direction in allDirections)
        {
            Cell neighbor = grid.GetNeighborInDirection(cell, direction);
            if (neighbor == null)
            {
                // Remove the wall at the boundary to create an entrance
                cell.DisableFace((int)direction);
                Debug.Log($"Created entrance at {cell.name} in direction {direction}");
                return;
            }
        }
    }
    
    /// <summary>
    /// Creates an exit in the last column of visited cells.
    /// </summary>
    /// <param name="grid">The maze grid.</param>
    /// <param name="visitOrder">The order in which cells were visited during generation.</param>
    /// <returns>The cell where the exit was created.</returns>
    public Cell CreateExit(IMazeGrid grid, List<Cell> visitOrder)
    {
        // Get all visited cells in the last column
        List<Cell> lastColumnCells = GetLastColumnCells(grid);

        if (lastColumnCells.Count == 0)
        {
            // Fallback: use the last visited cell if no visited cells in last column
            Debug.LogWarning("No visited cells found in last column, falling back to last visited cell");
            if (visitOrder.Count > 0)
            {
                Cell fallbackExit = visitOrder[visitOrder.Count - 1];
                ForceCreateExitAtCell(grid, fallbackExit);
                Debug.Log($"Forced exit creation at {fallbackExit.name}");
                return fallbackExit;
            }
            return null;
        }

        // Shuffle the list for randomness
        ShuffleList(lastColumnCells);

        // Try to create a natural exit at a boundary cell
        foreach (Cell candidate in lastColumnCells)
        {
            if (TryCreateExitAtCell(grid, candidate))
            {
                Debug.Log($"Created exit at {candidate.name} in last column");
                return candidate;
            }
        }

        // If no natural exit found, force create at a random cell in the last column
        if (lastColumnCells.Count > 0)
        {
            Cell forcedExit = lastColumnCells[Random.Range(0, lastColumnCells.Count)];
            ForceCreateExitAtCell(grid, forcedExit);
            Debug.Log($"Forced exit creation at {forcedExit.name} in last column");
            return forcedExit;
        }

        return null;
    }
    
    /// <summary>
    /// Gets all visited cells in the last column of the grid.
    /// </summary>
    private List<Cell> GetLastColumnCells(IMazeGrid grid)
    {
        List<Cell> lastColumnCells = new ();
        int lastColumnIndex = grid.GridWidth - 1;

        // Iterate through all rows in the last column
        for (int y = 0; y < grid.GridHeight; y++)
        {
            Cell cell = grid.Grid[lastColumnIndex, y];
            if (cell != null && grid.IsVisited(cell))
            {
                lastColumnCells.Add(cell);
            }
        }

        return lastColumnCells;
    }
    
    /// <summary>
    /// Tries to create an exit at the specified cell by removing a wall at the boundary.
    /// </summary>
    private bool TryCreateExitAtCell(IMazeGrid grid, Cell cell)
    {
        Cell.Direction[] allDirections = cell.GetAllDirections();

        // Try each direction to find a boundary (no neighbor)
        foreach (Cell.Direction direction in allDirections)
        {
            Cell neighbor = grid.GetNeighborInDirection(cell, direction);
            if (neighbor == null)
            {
                cell.DisableFace((int)direction);
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Forces creation of an exit at the specified cell, even if not at a boundary.
    /// </summary>
    private void ForceCreateExitAtCell(IMazeGrid grid, Cell cell)
    {
        Cell.Direction[] allDirections = cell.GetAllDirections();

        // Try to find a boundary direction first
        foreach (Cell.Direction direction in allDirections)
        {
            Cell neighbor = grid.GetNeighborInDirection(cell, direction);
            if (neighbor == null)
            {
                cell.DisableFace((int)direction);
                return;
            }
        }

        // If no boundary found, just disable the first wall as a fallback
        cell.DisableFace((int)allDirections[0]);
    }
    
    /// <summary>
    /// Shuffles a list in place using Fisher-Yates algorithm.
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}