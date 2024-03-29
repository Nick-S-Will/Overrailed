﻿using UnityEngine;
using UnityEditor;

using Overrailed.Terrain.Generation;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Editors
{
    public class RegionWindow : EditorWindow
    {
        const float keyWidth = 10, keyHeight = 20;
        const int windowBorder = 10, keyHighlightSize = 2;

        private Region region;
        private int selectedKeyIndex = -1;
        private bool movingKey;

        private void OnGUI()
        {
            Event guiEvent = Event.current;

            Rect gradientRect = new Rect(windowBorder, windowBorder, position.width - 2 * windowBorder, 30);
            GUI.DrawTexture(gradientRect, region.GetTexture((int)gradientRect.width));

            #region Draw Keys
            Rect[] keyBounds = new Rect[region.KeyCount];
            for (int i = 0; i < region.KeyCount; i++)
            {
                // Calculate key bounds
                keyBounds[i] = GetKeyBounds(i);
                if (selectedKeyIndex == i)
                {
                    // Draw highlight bounds
                    EditorGUI.DrawRect(
                        new Rect(
                            keyBounds[i].x - keyHighlightSize,
                            keyBounds[i].y - keyHighlightSize,
                            keyBounds[i].width + 2 * keyHighlightSize,
                            keyBounds[i].height + 2 * keyHighlightSize),
                        Color.white);
                }
                // Draw key bound
                EditorGUI.DrawRect(keyBounds[i], region.GetKey(i).GroundColor);
            }

            Rect settingsRect = new Rect(gradientRect.x, gradientRect.yMax + 2 * windowBorder + keyHeight, gradientRect.width, position.height - gradientRect.height - 4 * windowBorder - keyHeight);
            if (selectedKeyIndex != -1)
            {
                GUILayout.BeginArea(settingsRect);

                EditorGUI.BeginChangeCheck();
                Tile newObstacleTile = (Tile)EditorGUILayout.ObjectField(new GUIContent("Obstacle Tile: "), region.GetKey(selectedKeyIndex).ObstacleTile, typeof(Tile), true);
                _ = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                Tile newGroundTile = (Tile)EditorGUILayout.ObjectField(new GUIContent("Ground Tile: "), region.GetKey(selectedKeyIndex).GroundTile, typeof(Tile), true);
                _ = EditorGUI.EndChangeCheck();

                region.SetTiles(selectedKeyIndex, newGroundTile, newObstacleTile);

                EditorGUI.BeginChangeCheck();
                float newPercent = Mathf.Clamp01(EditorGUILayout.FloatField(new GUIContent("Percent: "), region.GetKey(selectedKeyIndex).Percent));
                if (EditorGUI.EndChangeCheck()) region.SetPercent(selectedKeyIndex, newPercent);

                GUILayout.EndArea();
            }
            #endregion

            #region Window Interact
            if (guiEvent.button == 0)
            {
                if (guiEvent.type == EventType.MouseDown)
                {
                    if (gradientRect.Contains(guiEvent.mousePosition))
                    {
                        // Add key
                        float keyPercent = Mathf.InverseLerp(gradientRect.x, gradientRect.xMax, guiEvent.mousePosition.x);
                        selectedKeyIndex = region.AddKey(new Region.TileKey(null, null, keyPercent));
                    }
                    else
                    {
                        // Move key check
                        movingKey = false;
                        for (int i = 0; i < keyBounds.Length; i++)
                        {
                            if (keyBounds[i].Contains(guiEvent.mousePosition))
                            {
                                selectedKeyIndex = i;
                                movingKey = true;
                                break;
                            }
                        }
                    }
                    Repaint();
                }
                else if (guiEvent.type == EventType.MouseUp)
                {
                    movingKey = false;
                    Repaint();
                }
                else if (movingKey && guiEvent.type == EventType.MouseDrag)
                {
                    // Drag Key
                    float keyPercent = Mathf.InverseLerp(gradientRect.x, gradientRect.xMax, guiEvent.mousePosition.x);
                    selectedKeyIndex = region.MoveKey(selectedKeyIndex, keyPercent);
                    Repaint();
                }
            }
            if ((guiEvent.keyCode == KeyCode.Backspace || guiEvent.keyCode == KeyCode.Delete) && guiEvent.type == EventType.KeyDown)
            {
                region.RemoveKey(selectedKeyIndex);
                selectedKeyIndex = -1;
                Repaint();
            }
            #endregion
        }

        private Rect GetKeyBounds(int index)
        {
            Rect gradientRect = new Rect(windowBorder, windowBorder, position.width - 2 * windowBorder, 30);
            return new Rect(
                gradientRect.x + region.GetKey(index).Percent * gradientRect.width - keyWidth / 2,
                gradientRect.yMax + windowBorder,
                keyWidth,
                keyHeight);
        }

        public void SetRegion(Region newRegion)
        {
            region = newRegion;
        }

        private void OnEnable()
        {
            titleContent.text = "Region Editor";
        }
    }
}