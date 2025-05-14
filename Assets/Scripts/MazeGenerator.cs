using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    private GridController grid;
    private HexGennerator startHex;

    private List<HexGennerator> path = new ();

    public void Init ()
    {   
        grid = GetComponent<GridController>();
        startHex = grid.hexGrid[0, grid.gridHeight - 1]; // Get top left hex
        generateMaze(startHex);
    }

    private void generateMaze (HexGennerator currentHex)
    {
        currentHex.visited = true; // Mark it as visited
        currentHex.isStart = true; // Mark it as start

        HexGennerator nextHex = grid.GetRandomNeighbor(currentHex);
        while (nextHex == null || nextHex.visited)
        {
            nextHex = grid.GetRandomNeighbor(currentHex);
        }
        nextHex.visited = true; // Mark it as visited
        path.Append(nextHex); // Add it to the path
        removeWalls (currentHex, nextHex); // Remove walls between start and next hex
        
        generateMaze(nextHex); // Recursively generate maze from next hex
    }

    private void removeWalls (HexGennerator hex1, HexGennerator hex2)
    {
        int direction = grid.GetDirection(hex1, hex2);
        hex1.DissableFace(direction);
        hex2.DissableFace (direction+ 3);
    }
}
