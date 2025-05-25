using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for maze generation algorithms
/// </summary>
public interface IMazeAlgorithm
{
    /// <summary>
    /// Generate maze instantly
    /// </summary>
    void GenerateInstant(IMazeGrid grid, Cell startCell);
    
    /// <summary>
    /// Generate maze with animation
    /// </summary>
    IEnumerator GenerateCoroutine(IMazeGrid grid, Cell startCell);
    
    /// <summary>
    /// Algorithm name for display purposes
    /// </summary>
    string AlgorithmName { get; }
}