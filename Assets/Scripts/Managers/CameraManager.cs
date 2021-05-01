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
        [Space]
        [SerializeField] private List<Locomotive> followPoints;

        private float startOffsetX;

        public Camera Main => mainCamera;
        public Locomotive FirstTarget => followPoints[0];

        public static CameraManager instance;

        void Start()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple CameraManagers Exist");

            startOffsetX = mainCamera.transform.position.x - GetAverageFollowX();
        }

        void Update()
        {
            Vector3 oldPos = mainCamera.transform.position;
            mainCamera.transform.position = new Vector3(GetAverageFollowX() + startOffsetX, oldPos.y, oldPos.z);
        }

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