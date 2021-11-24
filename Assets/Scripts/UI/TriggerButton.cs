using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.UI
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class TriggerButton : MonoBehaviour
    {
        public event System.Action OnClick;

        [SerializeField] private Transform buttonFill, loadingBar;
        [SerializeField] private Vector2 buttonSize = Vector2.one;
        [SerializeField] private float loadTime = 2f;

        private Coroutine barLoading;

        private IEnumerator LoadAction()
        {
            float percent = 0;

            while (percent < 1)
            {
                yield return null;

                percent += Time.deltaTime / loadTime;
                loadingBar.localScale = new Vector3(Mathf.Lerp(0, buttonSize.x, percent), loadingBar.localScale.y, loadingBar.localScale.z);
            }

            barLoading = null;
            OnClick?.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag.Equals("Player")) barLoading = StartCoroutine(LoadAction());
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag.Equals("Player"))
            {
                if (barLoading != null)
                {
                    StopCoroutine(barLoading);
                    barLoading = null;
                }

                loadingBar.localScale = new Vector3(0, loadingBar.localScale.y, loadingBar.localScale.z);
            }
        }

        private void OnValidate()
        {
            if ((Vector2)buttonFill.localScale != buttonSize)
            {
                GetComponent<BoxCollider>().size = new Vector3(buttonSize.x, buttonSize.y, 0.25f);
                GetComponent<SpriteRenderer>().size = buttonSize;
                buttonFill.localScale = buttonSize;
                loadingBar.localPosition = new Vector3(-buttonSize.x / 2, loadingBar.localPosition.y);
            }
        }
    }
}