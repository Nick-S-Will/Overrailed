using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Overrailed.Terrain;

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
        [SerializeField] [Min(0.1f)] private float fadeSpeed = 1f;

        private Coroutine followingTrains;
        private float startOffsetX;

        public Camera Main => camera1;

        void Start()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += TransitionEditMode;
                gm.OnEndCheckpoint += TransitionGameMode;

                FindObjectOfType<MapManager>().OnFinishAnimateFirstChunk += () => followingTrains = StartCoroutine(FollowLocomotivesRoutine());
                gm.OnGameEnd += StopFollowingTrains;
            }

            startOffsetX = camera1.transform.position.x - 4;
        }

        private IEnumerator FollowLocomotivesRoutine()
        {
            yield return new WaitWhile(() => MapManager.Locomotives == null);

            while (this && Manager.Exists && MapManager.Locomotives.Length != 0)
            {
                Vector3 oldPos = camera1.transform.position;
                camera1.transform.position = new Vector3(startOffsetX + Utils.GetAverageX(MapManager.Locomotives), oldPos.y, oldPos.z);

                yield return null;
                yield return new WaitUntil(() => Manager.IsPlaying());
            }
        }

        private void StopFollowingTrains()
        {
            StopCoroutine(followingTrains);
            followingTrains = null;
        }

        public static IEnumerator SlideToStart()
        {
            var camTransform = Camera.main.transform;
            var finalPos = new Vector3(4, camTransform.position.y, camTransform.position.z);

            while (camTransform.position != finalPos)
            {
                camTransform.position = Vector3.MoveTowards(camTransform.position, finalPos, 5 * Time.deltaTime);

                yield return null;
            }
        }

        public void TransitionEditMode() => TransitionWipe(camera1, camera2);
        public void TransitionGameMode() => TransitionWipe(camera2, camera1);
        private void TransitionWipe(Camera startCam, Camera endCam) => _ = StartCoroutine(TransitionWipe(startCam, endCam, fadeRenderTexture, fadeMask, endSize, fadeSpeed));
        private static IEnumerator TransitionWipe(Camera startCam, Camera endCam, RenderTexture fadeTexture, RawImage fadeShape, Vector2 shapeEndSize, float speed)
        {
            if (!startCam.enabled) throw new System.Exception("Start camera not enabled");

            // Starts rendering final texture
            endCam.targetTexture = fadeTexture;
            endCam.enabled = true;
            // Starts displaying final texture
            fadeShape.rectTransform.sizeDelta = Vector2.one;
            fadeShape.gameObject.SetActive(true);

            // Grow the part of final texture that is visible
            float percentage = Time.deltaTime;
            while (percentage < 1f)
            {
                fadeShape.rectTransform.sizeDelta = Vector2.Lerp(Vector2.zero, shapeEndSize, percentage);
                percentage += speed * percentage * Time.deltaTime;
                yield return null;
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