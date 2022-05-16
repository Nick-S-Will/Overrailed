using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Overrailed.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ClickButton : MonoBehaviour
    {
        public System.Action OnClick;

        [SerializeField] private float clickInterval = 1f;

        private SpriteRenderer sr;
        private Coroutine clicking;

        private static Coroutine checkClick;

        private void Start() => sr = GetComponent<SpriteRenderer>();

        public void Click()
        {
            if (clicking == null) clicking = StartCoroutine(ClickRoutine());
        }
        private IEnumerator ClickRoutine()
        {
            OnClick?.Invoke();

            var startColor = sr.color;
            sr.color = 0.8f * startColor;
            yield return new WaitForSeconds(clickInterval);
            sr.color = startColor;

            clicking = null;
        }

        public static void StartClickCheck(MonoBehaviour routineAnchor, Camera cam, Mouse mouse)
        {
            if (checkClick == null) checkClick = routineAnchor.StartCoroutine(ClickCheckRoutine(cam, mouse));
        }
        public static void StopClickCheck(MonoBehaviour routineAnchor)
        {
            if (checkClick != null)
            {
                routineAnchor.StopCoroutine(checkClick);
                checkClick = null;
            }
        }
        private static IEnumerator ClickCheckRoutine(Camera cam, Mouse mouse)
        {
            while (cam)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 mouseScreenPos = mouse.position.ReadValue();
                    Ray mouseRay = cam.ScreenPointToRay(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 25));
                    if (Physics.Raycast(mouseRay, out RaycastHit hitInfo, 25, LayerMask.GetMask("UI")))
                    {
                        var clickButton = hitInfo.transform.GetComponent<ClickButton>();
                        if (clickButton) clickButton.Click();
                    }
                }

                yield return null;
            }
        }
    }
}