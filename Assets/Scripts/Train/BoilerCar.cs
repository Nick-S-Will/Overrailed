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
        [SerializeField] private float liquidUsageInterval = 1;

        protected float liquidPercent = 1;

        protected override void Start()
        {
            base.Start();

            if (currentRail) _ = StartCoroutine(UseLiquid());
        }

        private IEnumerator UseLiquid()
        {
            while (liquid)
            {
                if (liquidPercent > 0) liquidPercent -= Time.fixedDeltaTime * (1 - 0.2f * tier);
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

                yield return new WaitForSeconds(liquidUsageInterval);
                yield return new WaitUntil(() => GameManager.instance.CurrentState == GameState.Play);
            }
        }

        public override bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is Bucket bucket && bucket.IsFull)
            {
                liquidPercent = 1;
                _ = base.TryInteractUsing(item, hitInfo);
            }
            else return false;

            return true;
        }
    }
}