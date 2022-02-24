using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Uncooked.Terrain.Generation;
using Uncooked.Terrain.Tools;
using Uncooked.Player;
using Uncooked.Train;

namespace Uncooked.Managers
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private MapManager map;
        [Header("Transforms")]
        [SerializeField] private Transform toolHUDParent;
        [SerializeField] private Transform warningHUDParent;
        [Header("Texts")]
        [SerializeField] private Text seedText;
        [SerializeField] private Text speedText, coinsText;
        [Header("Tools")]
        [SerializeField] private GameObject toolHUDPrefab;
        [SerializeField] private ToolType[] toolTypes;
        [SerializeField] private Vector2 opacityDistance = new Vector2(2, 4);
        [SerializeField] private Color warningColor = Color.red;
        [SerializeField] [Range(0, 1)] private float warningScreenPercentage = 0.1f;
        [SerializeField] [Min(1)] private float warningBlinkSpeed = 2;
        [Header("Warnings")]
        [SerializeField] private GameObject warningHUDPrefab;

        private List<ToolHUD> toolHUDs = new List<ToolHUD>();
        private int seedStartLength, speedStartLength, coinsStartLength;
        [HideInInspector] public bool isUpdating;

        public static HUDManager instance;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple HUDManagers Exist");

            FindObjectOfType<Locomotive>().OnSpeedChange += UpdateSpeedText;
            map.OnSeedChange += UpdateSeedText;
            foreach (var m in FindObjectsOfType<TrainStoreManager>()) m.OnCoinsChange += UpdateCoinsText;

            seedStartLength = seedText.text.Length;
            speedStartLength = speedText.text.Length;
            coinsStartLength = coinsText.text.Length;
        }

        void Start()
        {
            // Spawns Tool HUDs
            foreach (var tool in FindObjectsOfType<Tool>())
            {
                var toolType = ToolType.GetType(tool);
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
            }

            foreach (var car in FindObjectsOfType<TrainCar>()) car.OnWarning += MakeWarningHUD;

            _ = StartCoroutine(UpdateToolHUDs());
        }

        #region Set Stats HUD Texts
        public void UpdateSeedText(string newSeed) => UpdateText(seedText, seedStartLength, newSeed);
        public void UpdateSpeedText(string newSpeed) => UpdateText(speedText, speedStartLength, newSpeed);
        public void UpdateCoinsText(string newCoinCount) => UpdateText(coinsText, coinsStartLength, newCoinCount);
        private void UpdateText(Text textElement, int baseLength, string newString)
        {
            textElement.text = textElement.text.Substring(0, baseLength) + newString;
        }
        #endregion

        #region Tool HUDs
        private IEnumerator UpdateToolHUDs()
        {
            while (instance == this)
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
                if (!GameManager.IsPlaying())
                {
                    foreach (var toolHUD in toolHUDs) SetToolHUD(toolHUD.tool, false);

                    yield return new WaitUntil(() => GameManager.IsPlaying());

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
                float toolToPlayerDst = PlayerController.MinDistanceToPlayer(toolHUD.tool.transform.position);
                opacity = Mathf.InverseLerp(instance.opacityDistance.x, instance.opacityDistance.y, toolToPlayerDst);
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
        private void MakeWarningHUD(TrainCar car) => _ = StartCoroutine(ManageWarningHUD(car));
        private IEnumerator ManageWarningHUD(TrainCar car)
        {
            var warningHUD = Instantiate(warningHUDPrefab, warningHUDParent).GetComponent<RectTransform>();
            var image = warningHUD.GetComponentInChildren<Image>();

            while (car.IsWarning)
            {
                MoveRectToWorldPosition(warningHUD, car.transform.position);
                image.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(warningBlinkSpeed * Time.time, 1));

                yield return null;
            }

            Destroy(warningHUD.gameObject);
        }
        #endregion

        private void MoveRectToWorldPosition(RectTransform rect, Vector3 position)
        {
            rect.position = CameraManager.instance.Main.WorldToScreenPoint(position);
            rect.rotation = Quaternion.Inverse(CameraManager.instance.Main.transform.rotation);
        }

        private void OnDestroy()
        {
            instance = null;

            if (map) map.OnSeedChange -= UpdateSeedText;
            foreach (var m in FindObjectsOfType<TrainStoreManager>()) m.OnCoinsChange -= UpdateCoinsText;
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

            public static ToolType GetType(Tool tool)
            {
                foreach (var type in instance.toolTypes) if (tool.name.StartsWith(type.ToolName)) return type;
                return null;
            }
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