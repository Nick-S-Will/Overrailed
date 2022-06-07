using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Mob;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Terrain.Tools
{
    [RequireComponent(typeof(LineRenderer))]
    public class Rod : Tool
    {
        [Space]
        [SerializeField] private float bobHeight = 0.05f;
        [SerializeField] private float fishReactionTime = 1;
        [SerializeField] [Min(0)] private float catchTimeMin = 2, catchTimeMax = 6;
        [Space]
        [SerializeField] private ParticleSystem splashParticles;
        [SerializeField] private Transform bobberParent, bobber;

        private LineRenderer line;
        private Coroutine fishing;
        private bool isCast, tryCatchFish, isWaiting;

        override protected void Start()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
        }

        private void Update()
        {
            line.SetPositions(new Vector3[] { bobberParent.position, bobber.position });
        }

        public void UseOn(LiquidTile tile)
        {
            isCast = !isCast;

            bobber.parent = isCast ? tile.SurfacePoint : bobberParent;
            bobber.localPosition = Vector3.zero;

            if (isCast) fishing = StartCoroutine(Fishing());
            else if (isWaiting) tryCatchFish = true;
            else StopCoroutine(fishing);
        }

        /// <summary>
        /// Intercepts Rod drop if bobber is out
        /// </summary>
        /// <returns></returns>
        public override bool OnTryDrop()
        {
            if (isCast) UseOn(null);
            else return true;

            return false;
        }

        /// <summary>
        /// Makes bobber bob for [catchTimeMin, catchTimeMax] seconds, then gives a catching window of fishReactionTime seconds
        /// </summary>
        private IEnumerator Fishing()
        {
            while (isCast)
            {
                float foundFishTime = Random.Range(catchTimeMin, catchTimeMax) + Time.time;

                // Bob normally
                while (Time.time < foundFishTime)
                {
                    bobber.localPosition = (Mathf.PingPong(0.1f * Time.time, 2 * bobHeight) - bobHeight) * Vector3.up;
                    yield return null;
                }

                // Thrust down
                while (bobber.localPosition.y > -2 * bobHeight)
                {
                    bobber.localPosition += 2 * Time.deltaTime * Vector3.down;
                    yield return null;
                }

                // Spawn particles to indicate fish
                var splash = Instantiate(splashParticles, bobber);
                Destroy(splash.gameObject, splash.main.duration);

                // Wait
                float waitTime = fishReactionTime + Time.time;
                isWaiting = true;
                yield return new WaitUntil(() => Time.time > waitTime || tryCatchFish);
                isWaiting = false;

                // Catch fish
                if (tryCatchFish)
                {
                    var pos = Vector3Int.RoundToInt(transform.parent.parent.parent.parent.position + Vector3.up);
                    MobManager.instance.RandomFish.transform.position = pos;
                    isCast = false;
                }
            }

            tryCatchFish = false;
        }
    }
}