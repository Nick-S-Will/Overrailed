using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Overrailed.Train;

namespace Overrailed.Managers
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private Camera camera1;
        [SerializeField] private Camera camera2;
        [Header("Transition")]
        [SerializeField] private RenderTexture fadeRenderTexture;
        [SerializeField] private RawImage fadeMask;
        [SerializeField] private Vector2 endSize;
        [SerializeField] private float fadeDuration;

        private float startOffsetX;

        public Camera Main => camera1;

        public static CameraManager instance;

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                Debug.LogError("Multiple CameraManagers Found");
            }
            else instance = this;
        }

        void Start()
        {
            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint += TransitionEditMode;
                GameManager.instance.OnEndCheckpoint += TransitionGameMode;
            }

            startOffsetX = camera1.transform.position.x - GetAverageX(FindObjectsOfType<Locomotive>(), camera1.transform.position.x);
            _ = StartCoroutine(FollowLocomotives());
        }

        public IEnumerator FollowLocomotives()
        {
            yield return new WaitUntil(() => GameManager.Locomotives != null);

            while (instance && GameManager.instance)
            {
                Vector3 oldPos = camera1.transform.position;
                try
                {
                    camera1.transform.position = new Vector3(GetAverageX(GameManager.Locomotives, camera1.transform.position.x) + startOffsetX, oldPos.y, oldPos.z);
                }
                catch (MissingReferenceException)
                {
                    yield break;
                }

                yield return null;
                yield return new WaitUntil(() => GameManager.IsPlaying());
            }
        }

        public async Task SlideToStart()
        {
            var camTransform = camera1.transform;
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
        public void TransitionEditMode() => TransitionWipe(camera1, camera2);
        public void TransitionGameMode() => TransitionWipe(camera2, camera1);

        /// <summary>
        /// Calculates the averages transform.position.x in followPoints (field)
        /// </summary>
        /// <returns>The average x position in followPoints (field)</returns>
        private float GetAverageX(Locomotive[] locomotives, float startPos)
        {
            float newPos = startPos;
            foreach (var locomotive in locomotives)
            {
                if (locomotive) newPos += (locomotive.transform.position.x - startPos) / locomotives.Length;
                else continue;
            }

            return newPos;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;

            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint -= TransitionEditMode;
                GameManager.instance.OnEndCheckpoint -= TransitionGameMode;
            }
        }
    }
}