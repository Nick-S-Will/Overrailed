using UnityEngine;
using UnityEditor;

using Overrailed.Terrain;

namespace Overrailed.Editors
{
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(15);

            if (GUILayout.Button("Animate Spawn")) ((MapManager)target).AnimateNewChunk();
        }
    }
}