using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;

namespace Overrailed.Train
{
    public class Locomotive : TrainCar
    {
        public event Action<string> OnSpeedChange;
        public event Action OnStartTrain;
        public override event Action OnPickUp;
        public override event Action OnDrop;

        [Space]
        [SerializeField] private ParticleSystem smokeParticlePrefab;
        [SerializeField] private Transform smokePoint;
        [Space]
        [SerializeField] [Min(0)] private float baseSpeed = 0.05f;
        [SerializeField] [Min(0)] private float speedIncrement = 0.03f, speedUpMultiplier = 40;

        protected ParticleSystem smokeParticles;
        /// <summary>
        /// For setting speed discreetly
        /// </summary>
        private float trainSpeed;

        /// <summary>
        /// For setting speed and invoking <see cref="OnSpeedChange"/> event 
        /// </summary>
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
        public bool IsDriving => smokeParticles.emission.enabled && smokeParticles.isPlaying;
        public override bool IsWarning => false;

        protected override void Start()
        {
            base.Start();
            
            OnStartTrain += StartEmittingSmoke;
            OnDeath += SpeedUp;
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += StopEmittingSmoke;
                gm.OnEndCheckpoint += SetToBaseSpeed;
                OnDeath += gm.EndGame;
            }

            smokeParticles = Instantiate(smokeParticlePrefab, smokePoint);
            if (Manager.instance is GameManager) SetToBaseSpeed();
            else TrainSpeed = 1;
            SetEmitSmoke(false);

            Manager.OnPause += PauseSmokeParticles;
            Manager.OnResume += ResumeSmokeParticles;
        }

        public void StartTrain() => OnStartTrain?.Invoke();

        public void ResumeTrain() => StartEmittingSmoke();

        #region Smoke Particles
        protected void StartEmittingSmoke() => SetEmitSmoke(true);
        protected void StopEmittingSmoke() => SetEmitSmoke(false);
        private void SetEmitSmoke(bool emit)
        {
            if (smokeParticles == null) return;

            var emissionSettings = smokeParticles.emission;
            emissionSettings.enabled = emit;
        }

        protected void PauseSmokeParticles() => smokeParticles.Pause();
        protected void ResumeSmokeParticles() => smokeParticles.Play();
        #endregion

        public override async void Ignite()
        {
            base.Ignite();
            await Manager.Delay(5);

            if (burningParticles) Die();
        }

        private float GetSpeed()
        {
            float increase = speedIncrement * (Manager.instance is GameManager gm ? gm.CheckpointCount : 0);
            return baseSpeed + increase;
        }
        public void SpeedUp() => trainSpeed = speedUpMultiplier * GetSpeed();
        public void SetToBaseSpeed() => TrainSpeed = GetSpeed();
        
        // TODO: Implement usage in edit mode
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

        private void OnDestroy()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnEndCheckpoint -= SetToBaseSpeed;
                gm.OnCheckpoint -= StopEmittingSmoke;
                gm.OnEndCheckpoint -= StartEmittingSmoke;
            }

            Manager.OnPause -= PauseSmokeParticles;
            Manager.OnResume -= ResumeSmokeParticles;
        }
    }
}