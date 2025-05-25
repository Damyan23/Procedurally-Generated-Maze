using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for maze grid operations.
/// </summary>
public interface IMazeGrid
{
    // Grid properties

    /// <summary>
    /// The width of the grid (number of cells in X).
    /// </summary>
    int GridWidth { get; }

    /// <summary>
    /// The height of the grid (number of cells in Y).
    /// </summary>
    int GridHeight { get; }

    /// <summary>
    /// The total number of cells in the grid.
    /// </summary>
    int TotalCellCount { get; }

    /// <summary>
    /// The 2D array of cells in the grid.
    /// </summary>
    Cell[,] Grid { get; }
    
    // Cell operations

    /// <summary>
    /// Returns the designated start cell for the maze.
    /// </summary>
    Cell GetStartCell();

    /// <summary>
    /// Returns the neighbor of a cell in the specified direction.
    /// </summary>
    Cell GetNeighborInDirection(Cell cell, Cell.Direction direction);

    /// <summary>
    /// Returns the world position of a cell.
    /// </summary>
    Vector3 GetCellPosition(Cell cell);
    
    // Visited state management

    /// <summary>
    /// Returns true if the cell has been visited.
    /// </summary>
    bool IsVisited(Cell cell);

    /// <summary>
    /// Sets the visited state of a cell.
    /// </summary>
    void SetVisited(Cell cell, bool visited);
    
    // Neighbor operations

    /// <summary>
    /// Returns a list of visited neighbors for a cell.
    /// </summary>
    List<Cell> GetVisitedNeighbors(Cell cell);

    /// <summary>
    /// Returns the direction from one cell to another.
    /// </summary>
    Cell.Direction GetDirectionBetween(Cell from, Cell to);

    /// <summary>
    /// Returns a shuffled array of directions for a cell.
    /// </summary>
    Cell.Direction[] GetShuffledDirections(Cell cell);
    
    // Wall operations

    /// <summary>
    /// Disables the wall/face between two adjacent cells.
    /// </summary>
    void DisableFaceBetween(Cell cell1, Cell cell2, Cell.Direction direction);

    /// <summary>
    /// Returns true if there is a wall between two adjacent cells.
    /// </summary>
    bool HasWallBetween(Cell cell1, Cell cell2, Cell.Direction direction);
}