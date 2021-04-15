using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Uncooked.Terrain.Tools;

namespace Uncooked.Managers
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private GameObject toolHUDPrefab;
        [SerializeField] [Range(0, 1)] private float warningScreenPercentage;
        [SerializeField] private ToolHUD[] tools;

        void Start()
        {
            for (int i = 0; i < tools.Length; i++)
            {
                tools[i].Tool.OnPickup += OnToolPickup;
                tools[i].Tool.OnDrop += OnToolDrop;

                var toolHUD = Instantiate(toolHUDPrefab, transform).GetComponent<RectTransform>();
                toolHUD.GetChild(0).GetChild(0).GetComponent<Image>().sprite = tools[i].ToolImage;
                MoveRectToTransform(toolHUD, tools[i].Tool.transform);

                tools[i].rect = toolHUD;
            }
        }

        private void Update()
        {
            foreach (var toolHUD in tools)
            {
                if (toolHUD.rect.gameObject.activeSelf)
                {
                    MoveRectToTransform(toolHUD.rect, toolHUD.Tool.transform);
                    if (toolHUD.ScreenPercent < warningScreenPercentage && !toolHUD.isInDanger) StartCoroutine(FlashColor(toolHUD));
                }
            }
        }

        private void MoveRectToTransform(RectTransform hudElement, Transform worldElement)
        {
            hudElement.position = Camera.main.WorldToScreenPoint(worldElement.transform.position);
            hudElement.rotation = Quaternion.Inverse(Camera.main.transform.rotation);
        }

        private void OnToolPickup(Tool tool) => SetToolHUD(tool, false);
        private void OnToolDrop(Tool tool) => SetToolHUD(tool, true);
        private void SetToolHUD(Tool tool, bool isVisible)
        {
            foreach (var t in tools) if (tool == t.Tool) t.rect.gameObject.SetActive(isVisible);
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