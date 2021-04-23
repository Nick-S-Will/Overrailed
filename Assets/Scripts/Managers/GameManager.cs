using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Train;

namespace Uncooked.Managers
{
    public class GameManager : MonoBehaviour
    {
        public event System.Action OnCheckpoint, OnEndCheckpoint;

        [SerializeField] [Min(0)] private float baseTrainSpeed = 0.1f, checkpointSpeedMultiplier = 2, trainInitialDelay = 8;
        [SerializeField] private bool isEditing, isPaused;

        public float TrainSpeed => trainSpeed;
        public bool IsEditing => isEditing;
        public bool IsPaused => isPaused || isEditing;

        private TrainCar[] cars;
        private float trainSpeed;

        public static GameManager instance;

        void Awake()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple GameManagers Exist");
        }
        
        void Start()
        {
            cars = FindObjectsOfType<TrainCar>();
            trainSpeed = baseTrainSpeed;

            StartTrainWithDelay(trainInitialDelay);
        }

        public void StartTrainWithDelay(float delayTime) => _ = StartCoroutine(StartTrain(delayTime));
        
        private IEnumerator StartTrain(float delay)
        {
            yield return new WaitForSeconds(delay - 5);

            for (int countDown = 5; countDown > 0; countDown--)
            {
                yield return new WaitWhile(() => isPaused);

                Debug.Log(countDown);
                yield return new WaitForSeconds(1);
            }

            foreach (var car in cars) if (car.HasRail) car.StartDriving();
        }

        public void SpeedToCheckpoint()
        {
            trainSpeed = checkpointSpeedMultiplier * baseTrainSpeed;
        }

        public void ReachCheckpoint()
        {
            isEditing = true;
            trainSpeed = baseTrainSpeed;
            CameraManager.instance.TransitionEditMode(true);

            OnCheckpoint?.Invoke();
            // TODO: Make edit menu, with train upgrades
        }

        public void ContinueFromCheckpoint()
        {
            isEditing = false;
            CameraManager.instance.TransitionEditMode(false);

            OnEndCheckpoint?.Invoke();
        }

        public static void MoveToLayer(Transform root, int layer)
        {
            Stack<Transform> moveTargets = new Stack<Transform>();
            moveTargets.Push(root);

            Transform currentTarget;
            while (moveTargets.Count != 0)
            {
                currentTarget = moveTargets.Pop();
                currentTarget.gameObject.layer = layer;
                foreach (Transform child in currentTarget)
                    moveTargets.Push(child);
            }
        }

        private void OnDestroy()
        {
            instance = null;
        }
    }
}