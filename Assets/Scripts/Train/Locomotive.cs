using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.Train
{
    public class Locomotive : TrainCar
    {
        [Space]
        [SerializeField] private ParticleSystem smokeParticlePrefab;
        [SerializeField] private Transform smokePoint;

        protected ParticleSystem smokeParticles;

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

        protected override void Die()
        {
            CameraManager.instance.StopFollowing();
            GameManager.instance.SpeedUp(); 
            base.Die();
        }
    }
}