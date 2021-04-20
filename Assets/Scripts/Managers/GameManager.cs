using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Train;

namespace Uncooked.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] [Min(0)] private float trainSpeed = 0.1f, trainInitialDelay = 8;
        [SerializeField] private bool isEditing, isPaused;

        public float TrainSpeed => trainSpeed;
        public bool IsEditing => isEditing;
        public bool IsPaused => isPaused || isEditing;

        private TrainCar[] cars;

        public static GameManager instance;

        void Awake()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple GameManagers Exist");
        }
        
        void Start()
        {
            cars = FindObjectsOfType<TrainCar>();

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

        private void OnDestroy()
        {
            instance = null;
        }
    }
}