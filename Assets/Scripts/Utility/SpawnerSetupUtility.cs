using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpawnerSetupUtility
{
    public static List<Vector3> GetTileEdgePoints(Tilemap targetTilemap)
    {
        targetTilemap.CompressBounds();
        List<Vector3> edgePoints = new List<Vector3>();
        
        //Vector3 tileMapOrigin = targetTilemap.CellToWorld(targetTilemap.origin);
        Vector3 tileMapOrigin = targetTilemap.transform.position;

        foreach (Vector3Int edgePointCell in targetTilemap.cellBounds.allPositionsWithin)
        {
            if (targetTilemap.HasTile(edgePointCell))
            {
                var edgePoint = targetTilemap.CellToWorld(edgePointCell);
                var directionalVector = edgePoint - tileMapOrigin;
                directionalVector.Normalize();
                edgePoint = edgePoint + directionalVector * 1.5f;
                edgePoints.Add(edgePoint);
            }
        }

        return edgePoints;

    }
}
