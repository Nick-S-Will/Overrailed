using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Terrain.Tools;

namespace Uncooked.Train
{
    public class BoilerCar : TrainCar
    {
        [Space]
        [SerializeField] private Transform liquid;

        protected float liquidPercent { get; private set; } = 1;

        protected override void Start()
        {
            base.Start();

            if (currentRail) _ = StartCoroutine(UseLiquid());
        }

        private IEnumerator UseLiquid()
        {
            while (liquid)
            {
                if (liquidPercent > 0) liquidPercent -= (1 - 0.2f * tier) * Time.fixedDeltaTime;
                else
                {
                    liquidPercent = 0;
                    liquid.gameObject.SetActive(false);
                }

                liquid.localScale = new Vector3(1, liquidPercent, 1);
                if (liquidPercent == 0)
                {
                    _ = StartCoroutine(Ignite());
                    yield return new WaitWhile(() => liquidPercent == 0);
                    liquid.gameObject.SetActive(true);
                }

                yield return new WaitForSeconds(1.9f);
                yield return new WaitUntil(() => GameManager.IsPlaying());
            }
        }

        public override bool TryInteractUsing(IPickupable item)
        {
            if (item is Bucket bucket && bucket.IsFull)
            {
                liquidPercent = 1;
                if (!base.TryInteractUsing(item)) bucket.IsFull = false;
            }
            else return false;

            return true;
        }
    }
}