
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(MapLogic))]
public class MapLogicInspectorHex : UnityEditor.Editor
{
    //Custom editor class
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        //Will clear the existing tile map and regenerate a new map
        if (GUILayout.Button("Regenerate")) {
            MapLogic MpLogic = (MapLogic)target;
            MpLogic.DrawHexMap();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
