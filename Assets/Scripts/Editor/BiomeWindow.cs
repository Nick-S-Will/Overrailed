using UnityEngine;
using UnityEditor;

using Uncooked.Terrain.Generation;
using Uncooked.Terrain.Tiles;

namespace Uncooked.Editors
{
    public class BiomeWindow : EditorWindow
    {
        Biome biome;
        const float keyWidth = 10, keyHeight = 20;
        const int windowBorder = 10, keyHighlightSize = 2;

        private int selectedKeyIndex = -1;
        private bool movingKey;

        private void OnGUI()
        {
            Event guiEvent = Event.current;

            Rect gradientRect = new Rect(windowBorder, windowBorder, position.width - 2 * windowBorder, 30);
            GUI.DrawTexture(gradientRect, biome.GetTexture((int)gradientRect.width));

            #region Draw Keys
            Rect[] keyBounds = new Rect[biome.KeyCount];
            for (int i = 0; i < biome.KeyCount; i++)
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
                EditorGUI.DrawRect(keyBounds[i], biome.GetKey(i).Color);
            }

            Rect settingsRect = new Rect(gradientRect.x, gradientRect.yMax + 2 * windowBorder + keyHeight, gradientRect.width, position.height - gradientRect.height - 4 * windowBorder - keyHeight);
            if (selectedKeyIndex != -1)
            {
                GUILayout.BeginArea(settingsRect);
                EditorGUI.BeginChangeCheck();
                Tile newTile = (Tile)EditorGUILayout.ObjectField(biome.GetKey(selectedKeyIndex).Tile, typeof(Tile), true);
                if (EditorGUI.EndChangeCheck()) biome.SetTile(selectedKeyIndex, newTile);
                EditorGUI.BeginChangeCheck();
                float newPercent = EditorGUILayout.FloatField(biome.GetKey(selectedKeyIndex).Percent);
                if (EditorGUI.EndChangeCheck()) biome.SetPercent(selectedKeyIndex, newPercent);
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
                        selectedKeyIndex = biome.AddKey(new Biome.TileKey(null, keyPercent));
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
                    selectedKeyIndex = biome.MoveKey(selectedKeyIndex, keyPercent);
                    Repaint();
                }
            }
            if (guiEvent.keyCode == KeyCode.Backspace && guiEvent.type == EventType.KeyDown)
            {
                biome.RemoveKey(selectedKeyIndex);
                selectedKeyIndex = -1;
                Repaint();
            }
            #endregion
        }

        private Rect GetKeyBounds(int index)
        {
            Rect gradientRect = new Rect(windowBorder, windowBorder, position.width - 2 * windowBorder, 30);
            return new Rect(
                gradientRect.x + biome.GetKey(index).Percent * gradientRect.width - keyWidth / 2,
                gradientRect.yMax + windowBorder,
                keyWidth,
                keyHeight);
        }

        public void SetBiome(Biome _biome)
        {
            biome = _biome;
        }

        private void OnEnable()
        {
            titleContent.text = "Biome Regions";
        }
    }
}