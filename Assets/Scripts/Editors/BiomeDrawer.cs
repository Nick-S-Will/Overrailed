﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Uncooked.Terrain.Generation;

namespace Uncooked.Editors
{
    [CustomPropertyDrawer(typeof(Biome))]
    public class BiomeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Biome target = (Biome)fieldInfo.GetValue(property.serializedObject.targetObject);
            Event guiEvent = Event.current;

            float nameWidth = GUI.skin.label.CalcSize(label).x + 5;
            Rect textureRect = new Rect(position.x + nameWidth, position.y + 2, position.width - nameWidth, position.height - 4);

            if (guiEvent.type == EventType.Repaint)
            {
                // Var name
                GUI.Label(position, label);

                // Tile gradient
                GUI.DrawTexture(textureRect, target.GetTexture((int)position.width));
            }
            else if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && textureRect.Contains(guiEvent.mousePosition))
            {
                // Open Gradient Editor
                var window = EditorWindow.GetWindow<BiomeWindow>();
                window.SetBiome(target);
            }
        }
    }
}