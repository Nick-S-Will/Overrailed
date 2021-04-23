using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tools;

namespace Uncooked.Train
{
    public class BoilerCar : TrainCar
    {
        [Space]
        [SerializeField] private Transform liquid;
        [SerializeField] private float liquidUsageInterval = 1;

        protected float liquidPercent = 1;

        protected override void Start()
        {
            base.Start();

            if (currentRail) StartCoroutine(UseLiquid());
        }

        private IEnumerator UseLiquid()
        {
            while (liquid)
            {
                if (liquidPercent > 0) liquidPercent -= Time.fixedDeltaTime / tier;
                else
                {
                    liquidPercent = 0;
                    liquid.gameObject.SetActive(false);
                }

                liquid.localScale = new Vector3(1, liquidPercent, 1);
                if (liquidPercent == 0)
                {
                    StartCoroutine(Ignite());
                    yield return new WaitWhile(() => liquidPercent == 0);
                    liquid.gameObject.SetActive(true);
                }
                yield return new WaitForSeconds(liquidUsageInterval);
            }
        }

        public override bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is Bucket bucket && bucket.TryUse())
            {
                liquidPercent = 1;
                bucket.isFull = true;
                _ = base.TryInteractUsing(item, hitInfo);
                bucket.isFull = false;
            }
            else return false;

            return true;
        }
    }
}