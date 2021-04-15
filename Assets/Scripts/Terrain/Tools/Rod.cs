using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Terrain.Tools
{
    public class Rod : Tool
    {
        [SerializeField] private Transform bobberParent, bobber;
        [SerializeField] private float bobHeight = 0.05f, fishReactionTime = 1;

        // Add line renderer for fishing line
        private bool isCast, tryCatchFish;

        public void Use(LiquidTile tile)
        {
            isCast = !isCast;

            bobber.parent = isCast ? tile.SurfacePoint : bobberParent;
            bobber.localPosition = Vector3.zero;

            if (isCast) StartCoroutine(Fishing());
            else tryCatchFish = true;
        }

        private IEnumerator Fishing()
        {
            while (isCast)
            {
                float foundFishTime = 10 * Random.value + Time.time;

                // Bob normally
                while (Time.time < foundFishTime)
                {
                    bobber.localPosition = (Mathf.PingPong(Time.time, 2 * bobHeight) - bobHeight) * Vector3.up;
                    yield return null;
                }
                bobber.localPosition = Vector3.zero;

                // Thrust down to indicate fish
                while (bobber.localPosition.y > -2 * bobHeight)
                {
                    bobber.localPosition += 2 * Time.deltaTime * Vector3.down;
                    yield return null;
                }

                float waitTime = fishReactionTime + Time.time;
                yield return new WaitUntil(() => Time.time > waitTime || tryCatchFish);

                // Pick fish
                if (tryCatchFish)
                {
                    print("caught fish");
                    isCast = false;
                }
            }

            tryCatchFish = false;
        }
    }
}