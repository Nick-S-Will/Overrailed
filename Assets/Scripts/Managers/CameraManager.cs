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

        private List<Locomotive> followPoints;
        private float startOffsetX;
        private bool isFollowing = true;

        public Camera Main => mainCamera;
        public Locomotive FirstTarget => followPoints[0];

        public static CameraManager instance;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple CameraManagers Exist");
        }

        void Start()
        {
            followPoints = new List<Locomotive>();
            foreach (Locomotive l in FindObjectsOfType<Locomotive>()) followPoints.Add(l);
            if (followPoints.Count == 0) throw new System.Exception("No Locomotive in scene");

            startOffsetX = mainCamera.transform.position.x - GetAverageFollowX();
            _ = StartCoroutine(Follow());
        }

        public void ContinueFollowing() => isFollowing = true;
        public void StopFollowing() => isFollowing = false;

        private IEnumerator Follow()
        {
            while (this)
            {
                if (isFollowing)
                {
                    Vector3 oldPos = mainCamera.transform.position;
                    mainCamera.transform.position = new Vector3(GetAverageFollowX() + startOffsetX, oldPos.y, oldPos.z);
                }

                yield return null;
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
        private float GetAverageFollowX()
        {
            float averageOffset = 0;

            followPoints.ForEach(l => averageOffset += l.transform.position.x);
            averageOffset /= followPoints.Count;

            return averageOffset;
        }

        private void OnDestroy()
        {
            instance = null;
        }
    }
}