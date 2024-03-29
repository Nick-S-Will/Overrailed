﻿using System.Collections;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Managers.Audio;
using Overrailed.Terrain.Tools;
using System;

namespace Overrailed.Train
{
    public class BoilerCar : TrainCar
    {
        public override event Action OnPickUp;
        public override event Action OnDrop;

        [Space]
        [SerializeField] private AudioClip refillSound;
        [SerializeField] private Transform liquid;
        [SerializeField] [Range(0, 1)] private float warningPercent = 0.2f;

        private Coroutine liquidUse;
        private float liquidPercent = 1;
        private bool lowLiquidIsDisplayed;

        protected float LiquidPercent
        {
            get => liquidPercent;
            private set
            {
                liquidPercent = value;
                liquid.localScale = new Vector3(1, liquidPercent, 1);
            }
        }
        public override bool IsWarning => lowLiquidIsDisplayed;

        protected override void Start()
        {
            base.Start();

            if (currentRail && Manager.instance is GameManager) LeaderLocomotive.OnStartTrain += StartUsingLiquid;
        }

        public void SetLiquidToWarningLevel()
        {
            LiquidPercent = warningPercent;
            lowLiquidIsDisplayed = true;
            StartWarning();
        }

        public void StopUsingLiquid()
        {
            StopCoroutine(liquidUse);
            lowLiquidIsDisplayed = false;
        }

        public void StartUsingLiquid() => liquidUse = StartCoroutine(UseLiquidRoutine());
        private IEnumerator UseLiquidRoutine()
        {
            while (liquid)
            {
                if (LiquidPercent > 0) LiquidPercent -= (0.5f - 0.1f * tier) * Time.fixedDeltaTime;
                else
                {
                    LiquidPercent = 0;
                    liquid.gameObject.SetActive(false);
                }

                if (LiquidPercent == 0)
                {
                    _ = StartCoroutine(Ignite());
                    yield return new WaitWhile(() => LiquidPercent == 0);
                    liquid.gameObject.SetActive(true);
                }
                else if (LiquidPercent <= warningPercent && !lowLiquidIsDisplayed)
                {
                    lowLiquidIsDisplayed = true;
                    StartWarning();
                }

                yield return new WaitForSeconds(1f);
                yield return new WaitUntil(() => Manager.IsPlaying());
            }
        }

        public override Interaction TryInteractUsing(IPickupable item)
        {
            if (item is Bucket bucket && bucket.IsFull)
            {
                LiquidPercent = 1;
                lowLiquidIsDisplayed = false;

                if (base.TryInteractUsing(item) == Interaction.None)
                {
                    bucket.IsFull = false;

                    _ = StartCoroutine(AudioManager.PlaySound(refillSound, transform.position));
                }

                InvokeOnInteract();
                return Interaction.Interacted;
            }
            else return base.TryInteractUsing(item);
        }
    }
}