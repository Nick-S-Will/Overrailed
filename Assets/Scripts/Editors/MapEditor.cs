using UnityEngine;
using UnityEditor;

using Unrailed.Terrain;
using System.Collections.Generic;

namespace Unrailed.Editors
{
    [CustomEditor(typeof(MapManager))]
    public class MapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(15);

            if (GUILayout.Button("Generate Map")) ((MapManager)target).GenerateMap();
        }
    }
}