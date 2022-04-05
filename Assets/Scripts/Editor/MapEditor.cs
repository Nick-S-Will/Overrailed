using UnityEngine;
using UnityEditor;

using Overrailed.Terrain.Generation;

namespace Overrailed.Editors
{
    [CustomEditor(typeof(MapManager))]
    public class MapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(15);

            if (GUILayout.Button("Generate Map")) ((MapManager)target).GenerateMap();
            if (GUILayout.Button("Add Chunk")) ((MapManager)target).AddChunk();
        }
    }
}