using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Player;

namespace Overrailed.UI
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class TriggerButton : MonoBehaviour
    {
        public UnityEvent OnPress;

        [SerializeField] private string collisionTag = "Player";
        [SerializeField] private Transform buttonFill, loadingBar;
        [SerializeField] private RectTransform text;
        [SerializeField] private Vector2 buttonSize = Vector2.one;
        [SerializeField] private float loadTime = 2f;
        [SerializeField] private bool requireEmptyHand;

        private BoxCollider boxCollider;
        private bool barIsLoading = false;

        private void Start() => boxCollider = GetComponent<BoxCollider>();

        private async void LoadAction()
        {
            if (barIsLoading) return;

            float percent = 0;
            barIsLoading = true;

            while (percent < 1 && barIsLoading)
            {
                await Manager.Pause;
                await Task.Yield();

                percent += Time.deltaTime / loadTime;
                loadingBar.localScale = new Vector3(Mathf.Lerp(0, buttonSize.x, percent), loadingBar.localScale.y, loadingBar.localScale.z);
            }

            loadingBar.localScale = new Vector3(0, loadingBar.localScale.y, loadingBar.localScale.z);
            if (!barIsLoading) return;

            barIsLoading = false;
            OnPress?.Invoke();
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
            if (other.tag.Equals(collisionTag) && (!other.GetComponent<PlayerController>().IsHoldingItem )) LoadAction();
        }

        private void OnTriggerExit(Collider other)
        {
            var overlaps = Physics.OverlapBox(transform.position, boxCollider.size / 2, transform.rotation);
            foreach (var collider in overlaps) if (collider.tag == collisionTag) return;

            barIsLoading = false;
        }

        private void OnValidate()
        {
            if ((Vector2)buttonFill.localScale != buttonSize) UpdateSize();
        }
    }
}