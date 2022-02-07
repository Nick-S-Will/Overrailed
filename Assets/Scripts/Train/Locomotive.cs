using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.Train
{
    public class Locomotive : TrainCar
    {
        public System.Action OnStartTrain;

        [Space]
        [SerializeField] private ParticleSystem smokeParticlePrefab;
        [SerializeField] private Transform smokePoint;

        protected ParticleSystem smokeParticles;

        public int MaxCarCount => 4 + 2 * tier;
        public int CarCount { get; private set; } = 4;
        public bool IsDriving => smokeParticles.emission.enabled;

        protected override void Start()
        {
            OnStartDriving += StartEmittingSmoke;
            OnPauseDriving += StopEmittingSmoke;
            
            base.Start();

            smokeParticles = Instantiate(smokeParticlePrefab, smokePoint);
            SetEmitSmoke(false);
        }

        protected void StartEmittingSmoke() => SetEmitSmoke(true);
        protected void StopEmittingSmoke() => SetEmitSmoke(false);

        private void SetEmitSmoke(bool emit)
        {
            var emissionSettings = smokeParticles.emission;
            emissionSettings.enabled = emit;
        }

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