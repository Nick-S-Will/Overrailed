using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.Train
{
    public class Locomotive : TrainCar
    {
        public event System.Action<string> OnSpeedChange;
        public event System.Action OnStartTrain;

        [Space]
        [SerializeField] private ParticleSystem smokeParticlePrefab;
        [SerializeField] private Transform smokePoint;

        protected ParticleSystem smokeParticles;
        private float trainSpeed;

        public float TrainSpeed
        {
            get { return trainSpeed; }
            private set
            {
                trainSpeed = value;
                OnSpeedChange?.Invoke(trainSpeed.ToString());
            }
        }
        public int MaxCarCount => 4 + 2 * tier;
        public int CarCount { get; private set; } = 4;
        public bool IsDriving => smokeParticles.emission.enabled;

        protected override void Start()
        {
            OnStartDriving += StartEmittingSmoke;
            OnPauseDriving += StopEmittingSmoke;
            OnDeath += SpeedUp;
            GameManager.instance.OnEndCheckpoint += SetToBaseSpeed;
            
            base.Start();

            smokeParticles = Instantiate(smokeParticlePrefab, smokePoint);
            SetToBaseSpeed();
            SetEmitSmoke(false);
        }

        public void StartTrain() => OnStartTrain?.Invoke();

        protected void StartEmittingSmoke() => SetEmitSmoke(true);
        protected void StopEmittingSmoke() => SetEmitSmoke(false);

        private void SetEmitSmoke(bool emit)
        {
            var emissionSettings = smokeParticles.emission;
            emissionSettings.enabled = emit;
        }

        /// <summary>
        /// Gives train temporary speed buff until it reaches the next checkpoint
        /// </summary>
        public void SpeedUp() => trainSpeed = GameManager.GetBoostTrainSpeed();
        
        public void SetToBaseSpeed() => TrainSpeed = GameManager.GetBaseTrainSpeed();
        
        public bool TryAddCar()
        {
            if (CarCount < MaxCarCount)
            {
                CarCount++;
                return true;
            }
            else return false;
        }

        public void RemoveCar() => CarCount--;
    }
}