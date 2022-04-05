using System.Collections;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tools;

namespace Overrailed.Train
{
    public class BoilerCar : TrainCar
    {
        [Space]
        [SerializeField] private AudioClip refillSound;
        [SerializeField] private Transform liquid;
        [SerializeField] [Range(0, 1)] private float warningPercent = 0.2f;

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
                else if (LiquidPercent < warningPercent && !lowLiquidIsDisplayed)
                {
                    lowLiquidIsDisplayed = true;
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
                lowLiquidIsDisplayed = false;

                if (base.TryInteractUsing(item) == Interaction.None)
                {
                    bucket.IsFull = false;

                    AudioManager.instance.PlaySound(refillSound, transform.position);
                }

                return Interaction.Interacted;
            }
            else return Interaction.None;
        }
    }
}