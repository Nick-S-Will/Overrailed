using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Uncooked.Train;

namespace Uncooked.Managers
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera editCamera;
        [Header("Transition")]
        [SerializeField] private RenderTexture fadeRenderTexture;
        [SerializeField] private RawImage fadeMask;
        [SerializeField] private Vector2 endSize;
        [SerializeField] private float fadeDuration;

        private float startOffsetX;

        public Camera Main => mainCamera;

        public static CameraManager instance;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple CameraManagers Exist");
        }

        void Start()
        {
            GameManager.instance.OnCheckpoint += TransitionEditMode;
            GameManager.instance.OnEndCheckpoint += TransitionGameMode;

            startOffsetX = mainCamera.transform.position.x - GetAverageX(FindObjectsOfType<Locomotive>());
            _ = StartCoroutine(FollowLocomotives());
        }

        public IEnumerator FollowLocomotives()
        {
            yield return new WaitUntil(() => GameManager.instance.Locomotives != null);

            while (GameManager.instance)
            {
                Vector3 oldPos = mainCamera.transform.position;
                try
                {
                    mainCamera.transform.position = new Vector3(GetAverageX(GameManager.instance.Locomotives) + startOffsetX, oldPos.y, oldPos.z);
                }
                catch (MissingReferenceException)
                {
                    yield break;
                }

                yield return null;
                yield return new WaitUntil(() => GameManager.instance.IsPlaying());
            }
        }

        public async Task SlideToStart()
        {
            var camTransform = mainCamera.transform;
            var finalPos = new Vector3(4, camTransform.position.y, camTransform.position.z);

            while (camTransform.position != finalPos)
            {
                camTransform.position = Vector3.MoveTowards(camTransform.position, finalPos, 5 * Time.deltaTime);

                await Task.Yield();
            }
        }

        private static async void TransitionWipe(Camera startCam, Camera endCam, RenderTexture fadeTexture, RawImage fadeShape, Vector2 shapeEndSize, float duration)
        {
            if (!startCam.enabled) throw new System.Exception("Start camera not enabled");
            if (endCam.enabled) throw new System.Exception("End camera already enabled");

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
                fadeShape.rectTransform.sizeDelta = Vector2.Lerp(Vector2.one, shapeEndSize, completion);
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

        private void TransitionWipe(Camera startCam, Camera endCam) => TransitionWipe(startCam, endCam, fadeRenderTexture, fadeMask, endSize, fadeDuration);
        public void TransitionEditMode() => TransitionWipe(mainCamera, editCamera);
        public void TransitionGameMode() => TransitionWipe(editCamera, mainCamera);

        /// <summary>
        /// Calculates the averages transform.position.x in followPoints (field)
        /// </summary>
        /// <returns>The average x position in followPoints (field)</returns>
        private float GetAverageX(Locomotive[] locomotives)
        {
            float averageOffset = 0;
            foreach (var locomotive in locomotives) averageOffset += locomotive.transform.position.x;
            averageOffset /= locomotives.Length;

            return averageOffset;
        }

        private void OnDestroy()
        {
            instance = null;

            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint -= TransitionEditMode;
                GameManager.instance.OnEndCheckpoint -= TransitionGameMode;
            }
        }
    }
}