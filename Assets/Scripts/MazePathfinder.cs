using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Provides pathfinding utilities for navigating a generated maze grid.
/// Supports finding paths using Breadth-First Search (BFS) and visualizing them with a LineRenderer.
/// </summary>
public class MazePathfinder
{
    /// <summary>
    /// Finds a path from the start cell to the target cell using Breadth-First Search (BFS).
    /// Returns the path as a list of cells, or null if no path exists.
    /// </summary>
    public List<Cell> FindPath(IMazeGrid grid, Cell start, Cell target)
    {
        // Return null if start or target is not set
        if (start == null || target == null) return null;

        // Initialize BFS structures
        Queue<Cell> queue = new ();
        Dictionary<Cell, Cell> cameFrom = new (); // Tracks the path
        HashSet<Cell> visited = new ();

        // Start BFS from the start cell
        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = null;

        // BFS loop
        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();

            // If we've reached the target, reconstruct and return the path
            if (current == target)
            {
                var path = ReconstructPath(cameFrom, target);

                Debug.Log($"Path found ({path.Count} steps):\n" +
                    string.Join(" → ", path.Select(c => $"{c.name}({c.gridX},{c.gridY})")));

                return path;
            }

            // Explore all possible directions from the current cell
            Cell.Direction[] allDirections = current.GetAllDirections();

            foreach (Cell.Direction direction in allDirections)
            {
                // Get the neighbor in this direction
                Cell neighbor = grid.GetNeighborInDirection(current, direction);

                // Only consider unvisited neighbors
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    // Only proceed if there is no wall between current and neighbor
                    if (!grid.HasWallBetween(current, neighbor, direction))
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current; // Track the path
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // No path found
        return null;
    }
    
    /// <summary>
    /// Draws the given path instantly on the provided LineRenderer.
    /// </summary>
    public void DrawPathInstant(LineRenderer lineRenderer, IMazeGrid grid, List<Cell> path)
    {
        // Validate input
        if (lineRenderer == null || path == null || path.Count == 0) return;

        // Set the number of points in the line
        lineRenderer.positionCount = path.Count;

        // Set each point in the LineRenderer to the cell's position (slightly above the cell)
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 position = grid.GetCellPosition(path[i]);
            lineRenderer.SetPosition(i, position + Vector3.up * 0.1f);
        }
    }
    
    /// <summary>
    /// Draws the given path on the LineRenderer with animation, revealing one segment at a time.
    /// </summary>
    public IEnumerator DrawPathAnimated(LineRenderer lineRenderer, IMazeGrid grid, List<Cell> path, float delay = 0.05f)
    {
        // Validate input
        if (lineRenderer == null || path == null || path.Count == 0) yield break;

        // Optional initial delay before starting the animation
        yield return new WaitForSeconds(0.5f);

        // Start with no points in the line
        lineRenderer.positionCount = 0;

        // Animate each segment of the path
        for (int i = 0; i < path.Count; i++)
        {
            // Increase the number of points by one
            lineRenderer.positionCount = i + 1;

            // Set the new point's position
            Vector3 position = grid.GetCellPosition(path[i]);
            lineRenderer.SetPosition(i, position + Vector3.up * 0.1f);

            // Wait for the specified delay before drawing the next segment
            yield return new WaitForSeconds(delay);
        }
    }
    
    /// <summary>
    /// Reconstructs the path from the BFS search using the cameFrom dictionary.
    /// </summary>
    /// <returns>List of cells representing the path from start to target.</returns>
    private List<Cell> ReconstructPath(Dictionary<Cell, Cell> cameFrom, Cell target)
    {
        List<Cell> path = new ();
        Cell pathCell = target;
        
        // Walk backwards from the target to the start using the cameFrom dictionary
        while (pathCell != null)
        {
            path.Add(pathCell);
            cameFrom.TryGetValue(pathCell, out pathCell);
        }
        
        // The path is built backwards, so reverse it before returning
        path.Reverse();
        return path;
    }
}