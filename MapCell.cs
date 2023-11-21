using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class MapCell
{
    public int cellId;
    public Vector3 cellPosition;
    public Vector3Int cellIntPosition;
    public MapLogic.Terrain terrain;
    public int unitSortingOrder;

    public List<MapLogic.Terrain> possibleTerrains = new List<MapLogic.Terrain>();
}
