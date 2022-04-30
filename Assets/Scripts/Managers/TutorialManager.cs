using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using Overrailed.Player;
using Overrailed.Terrain.Tools;
using Overrailed.Terrain.Tiles;
using Overrailed.Train;
using Overrailed.Terrain.Generation;

namespace Overrailed.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField] private string titleSceneName = "TitleScreenScene";
        [Header("Target Points")]
        [SerializeField] private BreakTool axe;
        [SerializeField] private BreakTool pick;
        [SerializeField] private Bucket bucket;
        [SerializeField] private Transform treePoint, stonePoint;
        [SerializeField] private LiquidTile bridgeWaterPoint, bucketWaterPoint;
        [SerializeField] private CraftCar craftPoint;
        [SerializeField] private HolderCar holderPoint;
        [SerializeField] private BoilerCar boilerPoint;
        [SerializeField] private Transform[] railPoints;
        [Header("Prefabs")]
        [SerializeField] private BreakableTile treePrefab;
        [SerializeField] private BreakableTile stonePrefab;
        [SerializeField] private LayerMask obstacleMask;
        [Header("HUD Elements")]
        [SerializeField] private Transform pointer;
        [SerializeField] private Vector3 pointerOffset = Vector3.up;
        [SerializeField] private float pointerBobHeight = 0.25f, pointerBobSpeed = 1;
        [SerializeField] private int spinCycleInterval = 5;
        #endregion

        private MapManager map;
        private Coroutine treeSpawning, stoneSpawning;
        private BreakableTile currentTree, currentStone;
        private Transform pointerTarget;
        private string treeCode = "Tree", stoneCode = "Stone", woodType = "Wood", stoneType = "Stone";
        private Step step;

        void Start()
        {
            map = FindObjectOfType<MapManager>();

            map.OnFinishAnimateChunk += StartTutorial;

            if (pointer)
            {
                pointer.parent = transform;
                pointer.localPosition = Vector3.zero;
            }
            else Debug.LogError("Pointer HUD element is set to null");
        }

        private void CompleteStep(Tool unused) => NextStep();
        private void CompleteStep(Tile unused) => NextStep();
        private void NextStep() => step++;
        private void RewindStep(Tool unused) => PreviousStep();
        private void RewindStep(Tile unused) => PreviousStep();
        private void PreviousStep() => step--;

        private void StartTutorial() => _ = StartCoroutine(TutorialRoutine());
        private IEnumerator TutorialRoutine()
        {
            pointerTarget = axe.transform;
            BobPointer();

            // Pick up axe and break tree
            yield return StartCoroutine(ToolAndTile(axe, Step.Axe, treePrefab, treePoint, Step.Tree));

            // Pick up wood and make bridge
            bridgeWaterPoint.OnBridge += NextStep;
            yield return StartCoroutine(StackAndOther(woodType, Step.BridgeWood, bridgeWaterPoint.transform, Step.Bridge));

            // Pick up bucket and fill it
            foreach (var water in FindObjectsOfType<LiquidTile>()) water.OnRefill += NextStep;
            yield return StartCoroutine(ToolAndTile(bucket, Step.Bucket, bucketWaterPoint, null, Step.FillBucket));
            foreach (var water in FindObjectsOfType<LiquidTile>()) water.OnRefill -= NextStep;

            // Refill boiler
            boilerPoint.OnInteract += NextStep;
            boilerPoint.SetLiquidToWarningLevel();
            pointerTarget = boilerPoint.transform;
            yield return new WaitUntil(() => step > Step.FillBoiler);
            boilerPoint.OnInteract -= NextStep;
            boilerPoint.StopUsingLiquid();

            for (int i = 0; i < railPoints.Length; i++)
            {
                // Pick up pick and break stone
                yield return StartCoroutine(ToolAndTile(pick, Step.Pick, stonePrefab, stonePoint, Step.BreakStone));

                // Pick up stone and place on craft
                craftPoint.OnInteract += NextStep;
                yield return StartCoroutine(StackAndOther(stoneType, Step.Stone, craftPoint.transform, Step.CraftStone));
                craftPoint.OnInteract -= NextStep;

                // Pick up axe and break tree
                yield return StartCoroutine(ToolAndTile(axe, Step.Axe2, treePrefab, treePoint, Step.Tree2));

                // Pick up wood and place on craft
                craftPoint.OnInteract += NextStep;
                yield return StartCoroutine(StackAndOther(woodType, Step.Wood, craftPoint.transform, Step.CraftWood));
                craftPoint.OnInteract -= NextStep;

                // Pick up crafted rail
                holderPoint.OnPickUp += NextStep;
                pointerTarget = holderPoint.transform;
                yield return new WaitUntil(() => step > Step.PickupHolder);
                holderPoint.OnPickUp -= NextStep;

                // Place rail
                pointerTarget = railPoints[i];
                yield return new WaitUntil(() => Physics.CheckBox(pointerTarget.position, 0.4f * Vector3.one, Quaternion.identity, LayerMask.GetMask("Rail")));
                step = Step.Pick;
            }

            step = Step.Complete;
        }

        /// <summary>
        /// Alternates <see cref="step"/> between <paramref name="toolStep"/> and <paramref name="tileStep"/> when you pick up and drop <paramref name="tool"/>.
        /// Completes when <paramref name="tile"/> is interacted with
        /// </summary>
        /// <param name="tile">Tile that needs to be interacted with</param>
        /// <param name="tileParent">Transform <paramref name="tile"/> is parented to if necessary</param>
        private IEnumerator ToolAndTile(Tool tool, Step toolStep, Tile tile, Transform tileParent, Step tileStep)
        {
            tool.OnPickup += CompleteStep;
            tool.OnDropTool += RewindStep;
            while (step == toolStep)
            {
                pointerTarget = tool.transform;
                yield return new WaitUntil(() => step > toolStep);

                if (tile.name.Contains(treeCode))
                {
                    if (treeSpawning == null) treeSpawning = StartCoroutine(ContinuousSpawnResource((BreakableTile)tile, tileParent));
                    pointerTarget = currentTree.transform;
                    currentTree.OnBreak += CompleteStep;
                }
                else if (tile.name.Contains(stoneCode))
                {
                    if (stoneSpawning == null) stoneSpawning = StartCoroutine(ContinuousSpawnResource((BreakableTile)tile, tileParent));
                    pointerTarget = currentStone.transform;
                    currentStone.OnBreak += CompleteStep;
                }
                else if (tile is LiquidTile liquid) pointerTarget = liquid.transform;

                yield return new WaitWhile(() => step == tileStep);
            }
            tool.OnPickup -= CompleteStep;
            tool.OnDropTool -= RewindStep;
        }

        /// <summary>
        /// Alternates <see cref="step"/> between <paramref name="stackStep"/> and <paramref name="otherStep"/> when you pick up and drop stack of <paramref name="stackType"/>.
        /// Completes when stack is placed on <paramref name="otherTransform"/>
        /// </summary>
        /// <param name="stackType"></param>
        /// <param name="stackStep"></param>
        /// <param name="otherTransform"></param>
        /// <param name="otherStep"></param>
        /// <returns></returns>
        private IEnumerator StackAndOther(string stackType, Step stackStep, Transform otherTransform, Step otherStep)
        {
            StackTile stack = null;
            foreach (var s in FindObjectsOfType<StackTile>())
            {
                if (s.name.Contains(stackType) && s.name.Contains("Clone"))
                {
                    stack = s;
                    break;
                }
            }

            stack.OnPickUp += NextStep;
            stack.OnDrop += PreviousStep;
            while (step == stackStep)
            {
                pointerTarget = stack.transform;
                yield return new WaitUntil(() => step > stackStep);

                pointerTarget = otherTransform;
                yield return new WaitWhile(() => step == otherStep);
            }
            stack.OnPickUp -= NextStep;
            stack.OnDrop -= PreviousStep;
        }

        /// <summary>
        /// Spawns <paramref name="resourcePrefab"/> under <paramref name="parent"/> when there are no colliders in the way.
        /// Moves tools out of the way automatically
        /// </summary>
        private IEnumerator ContinuousSpawnResource(BreakableTile resourcePrefab, Transform parent)
        {
            while (this)
            {
                BreakableTile resource = Instantiate(resourcePrefab, parent);
                if (resource.name.Contains(treeCode)) currentTree = resource;
                else currentStone = resource;

                yield return new WaitUntil(() => resource == null);
                yield return new WaitUntil(() =>
                {
                    var colliders = Physics.OverlapBox(parent.position, 0.4f * Vector3.one, Quaternion.identity, obstacleMask);
                    int count = 0;
                    foreach (var col in colliders)
                    {
                        var pickup = col.GetComponent<Tool>();
                        if (pickup != null)
                        {
                            map.MovePickup(pickup);
                            count++;
                        }
                    }

                    return count == colliders.Length;
                });
            }
        }

        /// <summary>
        /// Moves pointer up and down over <see cref="pointerTarget"/> and spins it every <see cref="spinCycleInterval"/> cycle
        /// </summary>
        private async void BobPointer()
        {
            float startTime = Time.time;

            while (pointer && Application.isPlaying && step < Step.Complete)
            {
                Vector3 bobOffset = pointerBobHeight * Mathf.Sin(pointerBobSpeed * 2 * Time.time * Mathf.PI) * Vector3.up;
                float cycleDuration = 1 / pointerBobSpeed;
                float cycleProgress = (Time.time - startTime) / cycleDuration;
                int cycleCount = Mathf.FloorToInt(cycleProgress);
                bool nthCycle = cycleCount % spinCycleInterval == 0;

                pointer.position = pointerTarget.position + pointerOffset + bobOffset;
                pointer.localRotation = nthCycle ? Quaternion.Euler(0, Mathf.Lerp(0, 360, cycleProgress - cycleCount), 0) : Quaternion.identity;

                await Task.Yield();
            }

            if (Application.isPlaying)
            {
                pointer.position = Vector3.zero;
                pointer.gameObject.SetActive(false);
            }
        }

        public void ReachCheckpoint() => SceneManager.LoadScene(titleSceneName);

        private enum Step { Axe, Tree, BridgeWood, Bridge, Bucket, FillBucket, FillBoiler, Pick, BreakStone, Stone, CraftStone, Axe2, Tree2, Wood, CraftWood, PickupHolder, Rail, Complete }
    }
}