using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Terrain.Tools;
using System;

namespace Uncooked.Train
{
    public class BoilerCar : TrainCar
    {
        [Space]
        [SerializeField] private Transform liquid;
        [SerializeField] [Range(0, 1)] private float warningPercent = 0.2f;

        private float liquidPercent = 1;
        private bool liquidIsLow;

        protected float LiquidPercent
        {
            get => liquidPercent;
            private set
            {
                liquidPercent = value;
                liquid.localScale = new Vector3(1, liquidPercent, 1);
            }
        }
        public override bool IsWarning => liquidIsLow;

        protected override void Start()
        {
            base.Start();

            if (currentRail) _ = StartCoroutine(UseLiquid());
        }

        private IEnumerator UseLiquid()
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
                else if (LiquidPercent < warningPercent && !liquidIsLow)
                {
                    liquidIsLow = true;
                    MakeWarning();
                }

                yield return new WaitForSeconds(1f);
                yield return new WaitUntil(() => GameManager.IsPlaying());
            }
        }

        public override Interaction TryInteractUsing(IPickupable item)
        {
            if (item is Bucket bucket && bucket.IsFull)
            {
                LiquidPercent = 1;
                liquidIsLow = false;

                if (base.TryInteractUsing(item) == Interaction.None) bucket.IsFull = false;

                return Interaction.Interacted;
            }
            else return Interaction.None;
        }
    }
}