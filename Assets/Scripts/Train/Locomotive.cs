using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            base.Start();

            smokeParticles = Instantiate(smokeParticlePrefab, smokePoint);
        }
    }
}