using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
[CreateAssetMenu(menuName = "WaveFunctionProvince")]
public class WaveFunctionProvince : ScriptableObject
{
    public string tileName;

    public int minTempVal = 50; //0 = coldest/bot level, 50 = equator, 100 = top/hottest level
    public int maxTempVal = 50;

    public WaveFunctionTile defaultTile;
    public List<WaveFunctionTileJoin> tileOptions = new List<WaveFunctionTileJoin>();
}