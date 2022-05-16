using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Overrailed.Player;

namespace Overrailed.UI
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class TriggerButton : MonoBehaviour
    {
        public event System.Action OnPress;

        [SerializeField] private System.Action Press;
        [SerializeField] private Transform buttonFill, loadingBar;
        [SerializeField] private RectTransform text;
        [SerializeField] private Vector2 buttonSize = Vector2.one;
        [SerializeField] private float loadTime = 2f;
        [SerializeField] private bool requireEmptyHand;

        private Coroutine barLoading;

        private IEnumerator LoadAction()
        {
            if (barLoading != null) yield break;

            float percent = 0;

            while (percent < 1)
            {
                yield return null;

                percent += Time.deltaTime / loadTime;
                loadingBar.localScale = new Vector3(Mathf.Lerp(0, buttonSize.x, percent), loadingBar.localScale.y, loadingBar.localScale.z);
            }

            barLoading = null;
            OnPress?.Invoke();

            loadingBar.localScale = new Vector3(0, loadingBar.localScale.y, loadingBar.localScale.z);
        }

        private void CancelLoading()
        {
            if (barLoading == null) return;

            StopCoroutine(barLoading);
            barLoading = null;

            loadingBar.localScale = new Vector3(0, loadingBar.localScale.y, loadingBar.localScale.z);
        }

        private async void UpdateSize()
        {
            await Task.Yield();

            GetComponent<BoxCollider>().size = new Vector3(buttonSize.x, buttonSize.y, 0.25f);
            GetComponent<SpriteRenderer>().size = buttonSize;

            buttonFill.localScale = buttonSize;
            loadingBar.localPosition = new Vector3(-buttonSize.x / 2, loadingBar.localPosition.y);
            text.sizeDelta = buttonSize;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag.Equals("Player") && (!other.GetComponent<PlayerController>().IsHoldingItem )) barLoading = StartCoroutine(LoadAction());
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag.Equals("Player")) CancelLoading();
        }

        private void OnValidate()
        {
            if ((Vector2)buttonFill.localScale != buttonSize) UpdateSize();
        }
    }
}