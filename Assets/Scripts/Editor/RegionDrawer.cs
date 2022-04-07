using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Overrailed.Terrain.Generation;

namespace Overrailed.Editors
{
    [CustomPropertyDrawer(typeof(Region))]
    public class RegionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var field = fieldInfo.GetValue(property.serializedObject.targetObject);
            Event guiEvent = Event.current;

            Region[] targets;
            if (field is Region r) targets = new Region[] { r };
            else targets = (field as List<Region>).ToArray();

            float nameWidth = GUI.skin.label.CalcSize(label).x + 5;
            Rect textureRect = new Rect(position.x + nameWidth, position.y + 2, position.width - nameWidth, position.height - 4);

            int index = int.Parse(label.text.Split(' ')[1]);
            if (guiEvent.type == EventType.Repaint)
            {
                // Var name
                GUI.Label(position, label);

                // Tile gradient
                GUI.DrawTexture(textureRect, targets[index].GetTexture((int)position.width));
            }
            else if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && textureRect.Contains(guiEvent.mousePosition))
            {
                // Open Gradient Editor
                var window = EditorWindow.GetWindow<RegionWindow>();
                window.SetRegion(targets[index]);
            }
        }
    }
}