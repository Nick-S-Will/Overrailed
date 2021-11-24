using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Train;

namespace Uncooked.Managers
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera editCamera;

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

        /// <summary>
        /// Transitions to or from edit mode
        /// </summary>
        /// <param name="editMode">True for edit mode view, false for normal view</param>
        public void TransitionEditMode(bool editMode)
        {
            // TODO: Make transition smooth
            mainCamera.enabled = !editMode;
            editCamera.enabled = editMode;
        }

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