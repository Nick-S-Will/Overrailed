using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Uncooked.Terrain.Generation;
using Uncooked.Train;
using Uncooked.Terrain.Tools;

namespace Uncooked.Managers
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private MapManager map;
        [Header("Tools")]
        [SerializeField] private GameObject toolHUDPrefab;
        [SerializeField] private ToolHUD[] tools;
        [SerializeField] private Vector2 opacityDistance = new Vector2(2, 4);
        [SerializeField] private Color warningColor = Color.red;
        [SerializeField] [Range(0, 1)] private float warningScreenPercentage = 0.1f;
        [SerializeField] [Min(1)] private float warningBlinkSpeed = 2;
        [Header("Transforms")]
        [SerializeField] private Transform hudParent;
        [Header("Texts")]
        [SerializeField] private Text seedText;
        [SerializeField] private Text speedText, coinsText;

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
            for (int i = 0; i < tools.Length; i++)
            {
                tools[i].Tool.OnPickup += OnToolPickup;
                tools[i].Tool.OnDropTool += OnToolDrop;

                var toolHUD = Instantiate(toolHUDPrefab, hudParent).GetComponent<RectTransform>();
                tools[i].background = toolHUD.GetChild(0).GetComponent<Image>();
                tools[i].icon = toolHUD.GetChild(0).GetChild(0).GetComponent<Image>();

                tools[i].background.color = tools[i].Tint;
                tools[i].icon.sprite = tools[i].ToolImage;

                tools[i].rect = toolHUD;
            }

            _ = StartCoroutine(UpdateHUD());
        }

        private IEnumerator UpdateHUD()
        {
            while (instance == this)
            {
                foreach (var toolHUD in tools)
                {
                    if (toolHUD.rect.gameObject.activeSelf)
                    {
                        MoveRectToTransform(toolHUD);
                        UpdateOpacity(toolHUD);
                    }
                }

                yield return null;
                if (!GameManager.IsPlaying())
                {
                    foreach (var toolHUD in tools) SetToolHUD(toolHUD.Tool, false);

                    yield return new WaitUntil(() => GameManager.IsPlaying());

                    foreach (var toolHUD in tools) SetToolHUD(toolHUD.Tool, true);
                }
            }
        }

        private void MoveRectToTransform(ToolHUD toolHUD)
        {
            toolHUD.rect.position = CameraManager.instance.Main.WorldToScreenPoint(toolHUD.Tool.transform.position);
            toolHUD.rect.rotation = Quaternion.Inverse(CameraManager.instance.Main.transform.rotation);
        }

        private void UpdateOpacity(ToolHUD toolHUD)
        {
            float toolToPlayerDst = Player.PlayerController.MinDistanceToPlayer(toolHUD.Tool.transform.position);
            float opacity;
            if (toolHUD.ScreenPercent < warningScreenPercentage)
            {
                opacity = Mathf.PingPong(warningBlinkSpeed * Time.time, 1);
                toolHUD.background.color = warningColor;
            }
            else
            {
                opacity = 0.8f * Mathf.InverseLerp(instance.opacityDistance.x, instance.opacityDistance.y, toolToPlayerDst);
                toolHUD.background.color = toolHUD.Tint;
            }

            var newColor = toolHUD.background.color;
            newColor.a = opacity;
            toolHUD.background.color = newColor;

            newColor = toolHUD.icon.color;
            newColor.a = opacity;
            toolHUD.icon.color = newColor;
        }

        #region Set HUD Text
        public void UpdateSeedText(string newSeed) => UpdateText(seedText, seedStartLength, newSeed);
        public void UpdateSpeedText(string newSpeed) => UpdateText(speedText, speedStartLength, newSpeed);
        public void UpdateCoinsText(string newCoinCount) => UpdateText(coinsText, coinsStartLength, newCoinCount);
        private void UpdateText(Text textElement, int baseLength, string newString)
        {
            textElement.text = textElement.text.Substring(0, baseLength) + newString;
        }
        #endregion

        #region Toggle Tool HUDs
        private void OnToolPickup(Tool tool) => SetToolHUD(tool, false);
        private void OnToolDrop(Tool tool) => SetToolHUD(tool, true);
        private void SetToolHUD(Tool tool, bool isVisible)
        {
            foreach (var t in tools)
            {
                if (tool == t.Tool)
                {
                    t.rect.gameObject.SetActive(isVisible);
                    break;
                }
            }
        }
        #endregion

        private void OnDestroy()
        {
            instance = null;

            if (map) map.OnSeedChange -= UpdateSeedText;
            foreach (var m in FindObjectsOfType<TrainStoreManager>()) m.OnCoinsChange -= UpdateCoinsText;
        }

        [System.Serializable]
        private struct ToolHUD
        {
            [SerializeField] private Tool tool;
            [SerializeField] private Sprite toolImage;
            [SerializeField] private Color tint;

            [HideInInspector] public RectTransform rect;
            [HideInInspector] public Image background, icon;
            [HideInInspector] public bool isInDanger;

            public Tool Tool => tool;
            public Sprite ToolImage => toolImage;
            public Color Tint => tint;
            public float ScreenPercent => (rect.position.x + Screen.width) / Screen.width - 1;
        }
    }
}