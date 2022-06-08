using UnityEngine;
using UnityEditor;

using Overrailed.Terrain.Generation;

namespace Overrailed.Editors
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(15);

            if (GUILayout.Button("Generate Map")) ((MapGenerator)target).GenerateMap();
            if (GUILayout.Button("Add Chunk")) ((MapGenerator)target).AddChunk();
        }
    }
}