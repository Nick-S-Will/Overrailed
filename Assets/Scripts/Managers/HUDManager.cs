using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Uncooked.Terrain.Generation;
using Uncooked.Terrain.Tools;

namespace Uncooked.Managers
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private MapManager map;
        [Header("Tools")]
        [SerializeField] private GameObject toolHUDPrefab;
        [SerializeField] [Range(0, 1)] private float warningScreenPercentage = 0.1f;
        [SerializeField] private ToolHUD[] tools;
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

            FindObjectOfType<GameManager>().OnSpeedChange += UpdateSpeedText;
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
                toolHUD.GetChild(0).GetChild(0).GetComponent<Image>().sprite = tools[i].ToolImage;

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
                        MoveRectToTransform(toolHUD.rect, toolHUD.Tool.transform);
                        if (toolHUD.ScreenPercent < warningScreenPercentage && !toolHUD.isInDanger) _ = StartCoroutine(FlashColor(toolHUD));
                    }
                }

                yield return null;
                if (!GameManager.instance.IsPlaying())
                {
                    foreach (var toolHUD in tools) SetToolHUD(toolHUD.Tool, false);

                    yield return new WaitUntil(() => GameManager.instance.IsPlaying());

                    foreach (var toolHUD in tools) SetToolHUD(toolHUD.Tool, true);
                }
            }
        }

        private void MoveRectToTransform(RectTransform hudElement, Transform worldElement)
        {
            hudElement.position = CameraManager.instance.Main.WorldToScreenPoint(worldElement.transform.position);
            hudElement.rotation = Quaternion.Inverse(CameraManager.instance.Main.transform.rotation);
        }

        public void UpdateSeedText(string newSeed) => UpdateText(seedText, seedStartLength, newSeed);
        public void UpdateSpeedText(string newSpeed) => UpdateText(speedText, speedStartLength, newSpeed);
        public void UpdateCoinsText(string newCoinCount) => UpdateText(coinsText, coinsStartLength, newCoinCount);
        private void UpdateText(Text textElement, int baseLength, string newString)
        {
            textElement.text = textElement.text.Substring(0, baseLength) + newString;
        }

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

        private IEnumerator FlashColor(ToolHUD tool)
        {
            tool.isInDanger = true;

            Image image = tool.rect.GetComponentInChildren<Image>();
            Color originalColor = image.color, highlightColor = new Color(1, 0, 0, originalColor.a);

            while (tool.ScreenPercent < warningScreenPercentage)
            {
                image.color = Color.Lerp(originalColor, highlightColor, Mathf.PingPong(2 * Time.time, 1));

                yield return null;
            }

            image.color = originalColor;

            tool.isInDanger = false;
        }

        private void OnDestroy()
        {
            instance = null;

            if (GameManager.instance) GameManager.instance.OnSpeedChange -= UpdateSpeedText;
            if (map) map.OnSeedChange -= UpdateSeedText;
            foreach (var m in FindObjectsOfType<TrainStoreManager>()) m.OnCoinsChange -= UpdateCoinsText;
        }

        [System.Serializable]
        private struct ToolHUD
        {
            [SerializeField] private Tool tool;
            [SerializeField] private Sprite toolImage;

            [HideInInspector] public RectTransform rect;
            [HideInInspector] public bool isInDanger;

            public Tool Tool => tool;
            public Sprite ToolImage => toolImage;
            public float ScreenPercent => (rect.position.x + Screen.width) / Screen.width - 1;
        }
    }
}