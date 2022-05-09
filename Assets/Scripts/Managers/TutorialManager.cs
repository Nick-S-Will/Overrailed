using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

using Overrailed.Terrain.Tools;
using Overrailed.Terrain.Tiles;
using Overrailed.Train;
using Overrailed.Terrain.Generation;

namespace Overrailed.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        public event System.Action OnShowInfo, OnCloseInfo;

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
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private TextMeshProUGUI infoTitle, info;
        [Header("Pointer Values")]
        [SerializeField] private Transform pointer;
        [SerializeField] private Vector3 pointerOffset = Vector3.up;
        [SerializeField] private float pointerBobHeight = 0.25f, pointerBobSpeed = 1;
        [SerializeField] private int spinCycleInterval = 5;
        [Space]
        [SerializeField] private List<StepInfo> steps;
        #endregion

        private MapManager map;
        private Coroutine treeSpawning, stoneSpawning;
        private BreakableTile currentTree, currentStone;
        private Transform pointerTarget;
        private string treeBreakCode = "Tree", stoneBreakCode = "Stone", woodStackType = "Wood", stoneStackType = "Stone";
        private int stepIndex;

        public static bool Exists { get; private set; }

        private void Awake()
        {
            Exists = true;
        }

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

        public void CloseInfo() => infoPanel.SetActive(false);
        private void CompleteStep(Tool unused) => NextStep();
        private void CompleteStep(Tile unused) => NextStep();
        private void NextStep()
        {
            stepIndex++;
        }
        private void RewindStep(Tool unused) => PreviousStep();
        private void RewindStep(Tile unused) => PreviousStep();
        private void PreviousStep()
        {
            stepIndex--;
        }

        private void StartTutorial() => _ = StartCoroutine(TutorialRoutine());
        private IEnumerator TutorialRoutine()
        {
            pointerTarget = axe.transform;
            BobPointer();

            // Pick up axe and break tree
            yield return StartCoroutine(ToolAndTile(axe, treePrefab, treePoint, true));

            // Pick up wood and make bridge
            bridgeWaterPoint.OnBridge += NextStep;
            yield return StartCoroutine(StackAndOther(woodStackType, bridgeWaterPoint.transform, true));

            // Pick up bucket and fill it
            foreach (var water in FindObjectsOfType<LiquidTile>()) water.OnRefill += NextStep;
            yield return StartCoroutine(ToolAndTile(bucket, bucketWaterPoint, null, true));
            foreach (var water in FindObjectsOfType<LiquidTile>()) water.OnRefill -= NextStep;

            // Refill boiler
            ShowCurrentStepInfo();
            boilerPoint.OnInteract += NextStep;
            boilerPoint.SetLiquidToWarningLevel();
            pointerTarget = boilerPoint.transform;
            yield return new WaitUntil(() => stepIndex > 6);
            boilerPoint.OnInteract -= NextStep;

            bool firstLoop = true;
            for (int i = 0; i < railPoints.Length; i++)
            {
                // Pick up pick and break stone
                yield return StartCoroutine(ToolAndTile(pick, stonePrefab, stonePoint, firstLoop));

                // Pick up stone and place on craft
                craftPoint.OnInteract += NextStep;
                yield return StartCoroutine(StackAndOther(stoneStackType, craftPoint.transform, firstLoop));
                craftPoint.OnInteract -= NextStep;

                // Pick up axe and break tree
                yield return StartCoroutine(ToolAndTile(axe, treePrefab, treePoint, firstLoop));

                // Pick up wood and place on craft
                craftPoint.OnInteract += NextStep;
                yield return StartCoroutine(StackAndOther(woodStackType, craftPoint.transform, firstLoop));
                craftPoint.OnInteract -= NextStep;

                // Pick up crafted rail
                if (firstLoop) ShowCurrentStepInfo();
                holderPoint.OnPickUp += NextStep;
                pointerTarget = holderPoint.transform;
                yield return new WaitUntil(() => stepIndex > 15);
                holderPoint.OnPickUp -= NextStep;

                // Place rail
                if (firstLoop) ShowCurrentStepInfo();
                pointerTarget = railPoints[i];
                yield return new WaitUntil(() => Physics.CheckBox(pointerTarget.position, 0.4f * Vector3.one, Quaternion.identity, LayerMask.GetMask("Rail")));

                // Set values if there's another loop
                if (railPoints.Length - i > 1)
                {
                    stepIndex = 7;
                    firstLoop = false;
                }
            }

            pointer.gameObject.SetActive(false);
        }

        private void ShowCurrentStepInfo(float delay = 0) => _ = StartCoroutine(ShowInfoRoutine(steps[stepIndex], delay));
        private IEnumerator ShowInfoRoutine(StepInfo stepInfo, float delay = 0)
        {
            if (stepInfo.Info == string.Empty) yield break;

            if (delay > 0) yield return new WaitForSeconds(delay);

            infoPanel.SetActive(true);
            infoTitle.text = stepInfo.Title;
            info.text = stepInfo.Info;

            OnShowInfo?.Invoke();
            yield return new WaitWhile(() => infoPanel.activeSelf);
            OnCloseInfo?.Invoke();
        }

        /// <summary>
        /// Alternates <see cref="stepIndex"/> between <paramref name="toolStepIndex"/> and <paramref name="tileStepIndex"/> when you pick up and drop <paramref name="tool"/>.
        /// Completes when <paramref name="tile"/> is interacted with
        /// </summary>
        /// <param name="tile">Tile that needs to be interacted with</param>
        /// <param name="tileParent">Transform <paramref name="tile"/> is parented to if necessary</param>
        private IEnumerator ToolAndTile(Tool tool, Tile tile, Transform tileParent, bool showInfo)
        {
            if (showInfo) ShowCurrentStepInfo();
            int startIndex = stepIndex;
            bool firstLoop = true;

            tool.OnPickup += CompleteStep;
            tool.OnDropTool += RewindStep;
            while (stepIndex == startIndex)
            {
                pointerTarget = tool.transform;
                yield return new WaitUntil(() => stepIndex > startIndex);

                if (firstLoop && showInfo) ShowCurrentStepInfo();

                if (tile.name.Contains(treeBreakCode))
                {
                    if (treeSpawning == null) treeSpawning = StartCoroutine(ContinuousSpawnResource((BreakableTile)tile, tileParent));
                    if (firstLoop) currentTree.OnBreak += CompleteStep;
                    pointerTarget = currentTree.transform;
                }
                else if (tile.name.Contains(stoneBreakCode))
                {
                    if (stoneSpawning == null) stoneSpawning = StartCoroutine(ContinuousSpawnResource((BreakableTile)tile, tileParent));
                    if (firstLoop) currentStone.OnBreak += CompleteStep;
                    pointerTarget = currentStone.transform;
                }
                else if (tile is LiquidTile liquid) pointerTarget = liquid.transform;

                yield return new WaitWhile(() => stepIndex == startIndex + 1);
                firstLoop = false;
            }
            tool.OnPickup -= CompleteStep;
            tool.OnDropTool -= RewindStep;
        }

        /// <summary>
        /// Alternates <see cref="stepIndex"/> between <paramref name="stackStepIndex"/> and <paramref name="otherStepIndex"/> when you pick up and drop stack of <paramref name="stackType"/>.
        /// Completes when stack is placed on <paramref name="otherTransform"/>
        /// </summary>
        /// <param name="stackType"></param>
        /// <param name="stackStepIndex"></param>
        /// <param name="otherTransform"></param>
        /// <param name="otherStepIndex"></param>
        /// <returns></returns>
        private IEnumerator StackAndOther(string stackType, Transform otherTransform, bool showInfo)
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

            if (showInfo) ShowCurrentStepInfo();
            int startIndex = stepIndex;
            bool firstLoop = true;

            stack.OnPickUp += NextStep;
            stack.OnDrop += PreviousStep;
            while (stepIndex == startIndex)
            {
                pointerTarget = stack.transform;
                yield return new WaitUntil(() => stepIndex > startIndex);

                if (firstLoop)
                {
                    if (showInfo) ShowCurrentStepInfo();
                    firstLoop = false;
                }

                pointerTarget = otherTransform;
                yield return new WaitWhile(() => stepIndex == startIndex + 1);
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
                if (resource.name.Contains(treeBreakCode)) currentTree = resource;
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
        /// Moves <see cref="pointer"/> up and down over <see cref="pointerTarget"/> and spins it every <see cref="spinCycleInterval"/> cycle
        /// </summary>
        private async void BobPointer()
        {
            float startTime = Time.time;

            pointer.gameObject.SetActive(true);
            while (pointer && Application.isPlaying && stepIndex < steps.Count - 1)
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

            if (pointer && Application.isPlaying)
            {
                pointer.position = Vector3.zero;
                pointer.localRotation = Quaternion.identity;
                pointer.gameObject.SetActive(false);
            }
        }

        public void ReachCheckpoint() => _ = StartCoroutine(EndTutorial());
        private IEnumerator EndTutorial()
        {
            stepIndex = steps.Count - 1;
            yield return ShowInfoRoutine(steps[stepIndex]);
            SceneManager.LoadScene(titleSceneName);
        }

        private void OnDestroy()
        {
            Exists = false;
        }

        [System.Serializable]
        private struct StepInfo
        {
            [SerializeField] private string title;
            [TextArea(1, 10)]
            [SerializeField] private string info;

            public string Title => title;
            public string Info => info;
        }
    }
}