using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Managers
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Transform mainFollow;
        [Header("Cameras")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera editCamera;

        private float startOffsetX;

        public Camera Main => mainCamera;

        public static CameraManager instance;

        void Start()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple CameraManagers Exist");

            startOffsetX = mainCamera.transform.position.x - mainFollow.position.x;
        }

        void Update()
        {
            Vector3 oldPos = mainCamera.transform.position;
            mainCamera.transform.position = new Vector3(mainFollow.position.x + startOffsetX, oldPos.y, oldPos.z);
        }

        public void TransitionEditMode(bool editMode)
        {
            // TODO: Make transition smooth
            mainCamera.enabled = !editMode;
            editCamera.enabled = editMode;
        }

        private void OnDestroy()
        {
            instance = null;
        }
    }
}