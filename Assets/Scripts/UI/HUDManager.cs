﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Overrailed.Managers;
using Overrailed.Terrain;
using Overrailed.Terrain.Tools;
using Overrailed.Terrain.Generation;
using Overrailed.Player;
using Overrailed.Train;
using Overrailed.UI.Shop;

namespace Overrailed.UI
{
    public class HUDManager : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField] private MapManager map;
        [Header("Transforms")]
        [SerializeField] private Transform toolHUDParent;
        [SerializeField] private Transform warningHUDParent;
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI seedText;
        [SerializeField] private TextMeshProUGUI speedText, coinsText;
        [Header("Tool HUDs")]
        [SerializeField] private GameObject toolHUDPrefab;
        [SerializeField] private ToolType[] toolTypes;
        [SerializeField] private Vector2 opacityDistance = new Vector2(2, 4);
        [SerializeField] private float opacityFadeSpeed = 2;
        [Space]
        [SerializeField] private Color warningColor = Color.red;
        [SerializeField] [Range(0, 1)] private float warningScreenPercentage = 0.1f;
        [SerializeField] [Min(1)] private float warningBlinkSpeed = 2;
        [Header("Train Warnings")]
        [SerializeField] private GameObject warningHUDPrefab;
        [Header("UI Buttons")]
        [SerializeField] private TriggerButton continueGameButton;
        #endregion

        private List<ToolHUD> toolHUDs = new List<ToolHUD>();
        private int seedStartLength, speedStartLength, coinsStartLength;
        [HideInInspector] public bool isUpdating;

        private void Start()
        {
            if (Manager.instance is GameManager gm)
            {
                foreach (var m in FindObjectsOfType<TrainStoreManager>()) m.OnCoinsChange += UpdateCoinsText;
                map.OnFinishAnimateFirstChunk += CreateToolHUDs;
                map.OnFinishAnimateFirstChunk += UpdateSeedText;
                map.OnFinishAnimateFirstChunk += () => UpdateSpeedText(FindObjectOfType<Locomotive>().TrainSpeed.ToString());
                FindObjectOfType<Locomotive>().OnSpeedChange += UpdateSpeedText;

                coinsStartLength = coinsText.text.Length;
                speedStartLength = speedText.text.Length;
                seedStartLength = seedText.text.Length;

                continueGameButton.gameObject.SetActive(false);

                gm.OnCheckpoint += AlignContinueWithLocomotiveZ;
                gm.OnCheckpoint += EnableContinueButton;
                gm.OnEndCheckpoint += DisableContinueButton;
            }

            TrainCar.OnWarning += MakeWarningHUD;
        }

        #region Stat HUD Texts
        public void UpdateSeedText() => UpdateText(seedText, seedStartLength, FindObjectOfType<MapGenerator>().Seed.ToString());
        public void UpdateSeedText(string newSeed) => UpdateText(seedText, seedStartLength, newSeed);
        public void UpdateSpeedText(string newSpeed) => UpdateText(speedText, speedStartLength, newSpeed);
        public void UpdateCoinsText(string newCoinCount) => UpdateText(coinsText, coinsStartLength, newCoinCount);
        private void UpdateText(TextMeshProUGUI textElement, int baseLength, string newString) => textElement.text = textElement.text.Substring(0, baseLength) + newString;
        #endregion

        #region Tool HUDs
        private void CreateToolHUDs()
        {
            var tools = FindObjectsOfType<Tool>();
            if (tools.Length == toolHUDs.Count && toolHUDs.TrueForAll(t => t.tool == tools[toolHUDs.IndexOf(t)])) return;

            toolHUDs.Clear();

            // Spawns Tool HUDs
            foreach (var tool in tools)
            {
                ToolType toolType = null;
                foreach (var type in toolTypes)
                {
                    if (tool.name.Equals(type.ToolName))
                    {
                        toolType = type;
                        break;
                    }
                }
                if (toolType == null) continue; // Doesn't make a HUD if a type can't be found

                tool.OnPickup += HideToolHUD;
                tool.OnDropTool += ShowToolHUD;

                var hudRect = Instantiate(toolHUDPrefab, toolHUDParent).GetComponent<RectTransform>();
                toolHUDs.Add(
                    new ToolHUD(
                        hudRect,
                        hudRect.GetChild(0).GetComponent<Image>(),
                        hudRect.GetChild(0).GetChild(0).GetComponent<Image>(),
                        toolType,
                        tool,
                        toolType.Tint,
                        toolType.ToolImage));

                // Background
                var color = toolHUDs[toolHUDs.Count - 1].background.color;
                toolHUDs[toolHUDs.Count - 1].background.color = new Color(color.r, color.g, color.b, 0);
                // Foreground
                color = toolHUDs[toolHUDs.Count - 1].icon.color;
                toolHUDs[toolHUDs.Count - 1].icon.color = new Color(color.r, color.g, color.b, 0);
            }

            _ = StartCoroutine(UpdateToolHUDs());
        }

        private IEnumerator UpdateToolHUDs()
        {
            while (this)
            {
                foreach (var toolHUD in toolHUDs)
                {
                    if (toolHUD.rect.gameObject.activeSelf)
                    {
                        MoveRectToWorldPosition(toolHUD.rect, toolHUD.tool.transform.position);
                        UpdateToolHUDOpacity(toolHUD);
                    }
                }

                yield return null;
                if (Manager.IsPaused() || Manager.IsEditing())
                {
                    foreach (var toolHUD in toolHUDs) SetToolHUD(toolHUD.tool, false);

                    yield return new WaitWhile(() => Manager.IsPaused() || Manager.IsEditing());
                    
                    foreach (var toolHUD in toolHUDs) SetToolHUD(toolHUD.tool, true);
                }
            }
        }

        private void UpdateToolHUDOpacity(ToolHUD toolHUD)
        {
            float opacity;
            if (toolHUD.ScreenPercent < warningScreenPercentage)
            {
                opacity = Mathf.PingPong(warningBlinkSpeed * Time.time, 1);
                toolHUD.background.color = warningColor;
            }
            else
            {
                opacity = toolHUD.background.color.a;

                float toolToPlayerDst = PlayerController.MinDistanceToPlayer(toolHUD.tool.transform.position);
                if (toolToPlayerDst < opacityDistance.x) opacity = Mathf.Clamp01(opacity - opacityFadeSpeed * Time.deltaTime);
                else if (toolToPlayerDst > opacityDistance.y) opacity = Mathf.Clamp01(opacity + opacityFadeSpeed * Time.deltaTime);
                else opacity = Mathf.Round(opacity);

                toolHUD.background.color = toolHUD.toolType.Tint;
            }

            var color = toolHUD.background.color;
            toolHUD.background.color = new Color(color.r, color.g, color.b, opacity);

            color = toolHUD.icon.color;
            toolHUD.icon.color = new Color(color.r, color.g, color.b, opacity);
        }

        private void HideToolHUD(Tool tool) => SetToolHUD(tool, false);
        private void ShowToolHUD(Tool tool) => SetToolHUD(tool, true);
        private void SetToolHUD(Tool tool, bool isVisible)
        {
            foreach (var toolHUD in toolHUDs)
            {
                if (tool == toolHUD.tool)
                {
                    toolHUD.rect.gameObject.SetActive(isVisible);
                    break;
                }
            }
        }
        #endregion

        #region Train Warning HUDs
        private IEnumerator MakeWarningHUDRoutine(TrainCar car)
        {
            var warningHUD = Instantiate(warningHUDPrefab, warningHUDParent).GetComponent<RectTransform>();
            warningHUD.SetAsFirstSibling();
            var image = warningHUD.GetComponentInChildren<Image>();

            while (car.IsWarning && car)
            {
                MoveRectToWorldPosition(warningHUD, car.transform.position);
                image.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(warningBlinkSpeed * Time.time, 1));

                yield return null;
                yield return Manager.PauseRoutine;
            }

            Destroy(warningHUD.gameObject);
        }
        private void MakeWarningHUD(TrainCar car) => _ = StartCoroutine(MakeWarningHUDRoutine(car));
        #endregion

        #region UI Buttons
        private void AlignContinueWithLocomotiveZ()
        {
            Vector3 pos = continueGameButton.transform.position;
            continueGameButton.transform.position = new Vector3(pos.x, pos.y, FindObjectOfType<Locomotive>().transform.position.z - 1);
        }

        private void SetButtonActive(TriggerButton button, bool enabled) => button.gameObject.SetActive(enabled);
        private void EnableContinueButton() => SetButtonActive(continueGameButton, true);
        private void DisableContinueButton() => SetButtonActive(continueGameButton, false);
        #endregion

        private void MoveRectToWorldPosition(RectTransform rect, Vector3 position)
        {
            var cam = Camera.main;
            rect.position = cam.WorldToScreenPoint(position);
            rect.rotation = Quaternion.Inverse(cam.transform.rotation);
        }

        private void OnDestroy()
        {
            if (Manager.instance is GameManager gm)
            {
                foreach (var m in FindObjectsOfType<TrainStoreManager>()) m.OnCoinsChange -= UpdateCoinsText;
                if (map) map.OnFinishAnimateFirstChunk -= CreateToolHUDs;
                
                gm.OnCheckpoint -= AlignContinueWithLocomotiveZ;
                gm.OnCheckpoint -= EnableContinueButton;
                gm.OnEndCheckpoint -= DisableContinueButton;
            }

            TrainCar.OnWarning -= MakeWarningHUD;
        }

        [System.Serializable]
        private class ToolType
        {
            [SerializeField] private string toolName;
            [SerializeField] private Sprite toolImage;
            [SerializeField] private Color tint;

            public string ToolName => toolName;
            public Sprite ToolImage => toolImage;
            public Color Tint => tint;
        }

        private struct ToolHUD
        {
            public RectTransform rect;
            public Image background, icon;
            public ToolType toolType;
            public Tool tool;
            public bool isInDanger;

            public ToolHUD(RectTransform rectTransform, Image bg, Image iconImage, ToolType type, Tool newTool, Color bgColor, Sprite iconSprite)
            {
                rect = rectTransform;
                background = bg;
                icon = iconImage;
                toolType = type;
                tool = newTool;
                isInDanger = false;

                background.color = bgColor;
                icon.sprite = iconSprite;
            }

            public float ScreenPercent => (rect.position.x + Screen.width) / Screen.width - 1;
        }
    }
}