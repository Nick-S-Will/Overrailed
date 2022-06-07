using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Overrailed.Terrain.Generation;

namespace Overrailed.Managers.Cameras
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Camera camera1;
        [SerializeField] private Camera camera2;
        [Header("Transition")]
        [SerializeField] private RenderTexture fadeRenderTexture;
        [SerializeField] private RawImage fadeMask;
        [SerializeField] private Vector2 endSize;
        [SerializeField] private float fadeDuration;

        private float startOffsetX;

        public Camera Main => camera1;

        void Start()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += TransitionEditMode;
                gm.OnEndCheckpoint += TransitionGameMode;
            }

            startOffsetX = camera1.transform.position.x;
            _ = StartCoroutine(FollowLocomotivesRoutine());
        }

        public IEnumerator FollowLocomotivesRoutine()
        {
            yield return new WaitWhile(() => MapManager.Locomotives == null);

            while (this && Manager.instance is GameManager)
            {
                Vector3 oldPos = camera1.transform.position;
                try
                {
                    camera1.transform.position = new Vector3(startOffsetX + Utils.GetAverageX(MapManager.Locomotives), oldPos.y, oldPos.z);
                }
                catch (MissingReferenceException)
                {
                    yield break;
                }

                yield return null;
                yield return new WaitUntil(() => Manager.IsPlaying());
            }
        }

        public static async Task SlideToStart()
        {
            var camTransform = Camera.main.transform;
            var finalPos = new Vector3(4, camTransform.position.y, camTransform.position.z);

            while (camTransform.position != finalPos)
            {
                camTransform.position = Vector3.MoveTowards(camTransform.position, finalPos, 5 * Time.deltaTime);

                await Task.Yield();
            }
        }

        public void TransitionEditMode() => TransitionWipe(camera1, camera2);
        public void TransitionGameMode() => TransitionWipe(camera2, camera1);
        private void TransitionWipe(Camera startCam, Camera endCam) => TransitionWipe(startCam, endCam, fadeRenderTexture, fadeMask, endSize, fadeDuration);
        private static async void TransitionWipe(Camera startCam, Camera endCam, RenderTexture fadeTexture, RawImage fadeShape, Vector2 shapeEndSize, float duration)
        {
            if (!startCam.enabled) throw new System.Exception("Start camera not enabled");

            // Starts rendering final texture
            endCam.targetTexture = fadeTexture;
            endCam.enabled = true;
            // Starts displaying final texture
            fadeShape.rectTransform.sizeDelta = Vector2.one;
            fadeShape.gameObject.SetActive(true);

            // Grow the part of final texture that is visible
            float completion = 0;
            while (completion < 1)
            {
                fadeShape.rectTransform.sizeDelta = Vector2.Lerp(Vector2.zero, shapeEndSize, completion);
                completion += Time.deltaTime / duration;
                await Task.Yield();
            }

            // Transfer active cam to end cam
            endCam.targetTexture = null;
            startCam.enabled = false;
            // Hide final texture
            fadeShape.gameObject.SetActive(false);

            fadeTexture.Release();
        }

        private void OnDestroy()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint -= TransitionEditMode;
                gm.OnEndCheckpoint -= TransitionGameMode;
            }
        }
    }
}