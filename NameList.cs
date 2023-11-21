using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "NameList")]
public class NameList : ScriptableObject
{
    public MapLogic.Terrain specificTerrain;
    public List<string> names = new List<string>();
    public List<string> prefixes = new List<string>();
    public List<string> sufixes = new List<string>();
}