using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
[CreateAssetMenu(menuName = "WaveFunctionTile")]
public class WaveFunctionTile : ScriptableObject
{
    public string tileName;
    public TileBase tile;
    public MapLogic.Terrain terrain;

    public int minTempVal = 50; //0 = coldest/bot level, 50 = equator, 100 = top/hottest level
    public int maxTempVal = 50;

    public TileBase aboveGroundTile;

    public List<WaveFunctionTileJoin> joinOptions = new List<WaveFunctionTileJoin>();
}

[System.Serializable]
public class WaveFunctionTileJoin
{
    public WaveFunctionTile joinedTile;
    public float probability = 0; //adjust the probability of this option being picked, 0 = normal, 1 = double, 2 = triple
}