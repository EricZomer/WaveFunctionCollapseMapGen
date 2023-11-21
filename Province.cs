using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Province : MonoBehaviour
{
    public string ProvinceName;
    public WaveFunctionProvince waveFunctionProvince;
    public Dictionary<Vector3Int, MapCell> cellList = new Dictionary<Vector3Int, MapCell>();
    public int Population;
}