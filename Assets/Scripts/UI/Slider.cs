using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Overrailed.UI
{
    public class Slider : MonoBehaviour
    {
        public System.Action OnSlide;

        [SerializeField] private Transform knob;

        private Transform knobParent;

        private static Coroutine checkSlide;

        private void Start()
        {
            knobParent = knob.parent;
        }

        public void Slide(Vector3 point)
        {
            float distance = Vector3.Distance(knobParent.position, point);
            knob.localPosition = (float)Math.Round(distance / transform.localScale.x, 1) * Vector3.right;
        }

        public float ReadValue() => knob.localPosition.x;
        public void WriteValue(float value) => knob.localPosition = value * Vector3.right;

        public static void StartClickCheck(MonoBehaviour routineAnchor, Camera cam, Mouse mouse)
        {
            if (checkSlide == null) checkSlide = routineAnchor.StartCoroutine(CheckSlideRoutine(cam, mouse));
        }
        public static void StopClickCheck(MonoBehaviour routineAnchor)
        {
            if (checkSlide != null)
            {
                routineAnchor.StopCoroutine(checkSlide);
                checkSlide = null;
            }
        }
        private static IEnumerator CheckSlideRoutine(Camera cam, Mouse mouse)
        {
            while (cam)
            {
                if (mouse.leftButton.isPressed)
                {
                    Vector2 mouseScreenPos = mouse.position.ReadValue();
                    Ray mouseRay = cam.ScreenPointToRay(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 25));
                    if (Physics.Raycast(mouseRay, out RaycastHit hitInfo, 25, LayerMask.GetMask("UI")))
                    {
                        var slider = hitInfo.transform.GetComponent<Slider>();
                        if (slider) slider.Slide(hitInfo.point);
                    }
                }

                yield return null;
            }
        }
    }
}