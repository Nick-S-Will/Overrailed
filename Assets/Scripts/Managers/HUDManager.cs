using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Uncooked.Terrain;

namespace Uncooked.Managers
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private GameObject toolHUDPrefab;
        [SerializeField] [Range(0, 1)] private float warningScreenPercentage;
        [SerializeField] private Tool[] tools;

        private ToolHUD[] toolHUDs;

        void Start()
        {
            toolHUDs = new ToolHUD[tools.Length];

            for (int i = 0; i < tools.Length; i++)
            {
                tools[i].OnPickupEvent += OnToolPickup;
                tools[i].OnDropEvent += OnToolDrop;

                var toolHUD = Instantiate(toolHUDPrefab, transform).GetComponent<RectTransform>();
                MoveRectToTransform(toolHUD, tools[i].transform);

                toolHUDs[i] = new ToolHUD(tools[i], toolHUD);
            }
        }

        private void Update()
        {
            foreach (var toolHUD in toolHUDs)
            {
                if (toolHUD.Rect.gameObject.activeSelf)
                {
                    MoveRectToTransform(toolHUD.Rect, toolHUD.Tool.transform);
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
            foreach (var t in toolHUDs) if (tool == t.Tool) t.Rect.gameObject.SetActive(isVisible);
        }

        private IEnumerator FlashColor(ToolHUD tool)
        {
            tool.isInDanger = true;

            Image image = tool.Rect.GetComponentInChildren<Image>();
            Color originalColor = image.color, highlightColor = new Color(1, 0, 0, originalColor.a);

            while (tool.ScreenPercent < warningScreenPercentage)
            {
                image.color = Color.Lerp(originalColor, highlightColor, Mathf.PingPong(2 * Time.time, 1));

                yield return null;
            }

            image.color = originalColor;

            tool.isInDanger = false;
        }

        private struct ToolHUD
        {
            public bool isInDanger;

            private Tool tool;
            private RectTransform rect;

            public Tool Tool => tool;
            public RectTransform Rect => rect;
            public float ScreenPercent => (rect.position.x + Screen.width) / Screen.width - 1;

            public ToolHUD(Tool _tool, RectTransform _rect)
            {
                tool = _tool;
                rect = _rect;
                isInDanger = false;
            }
        }
    }
}