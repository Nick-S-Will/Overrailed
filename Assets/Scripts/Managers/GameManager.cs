using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Uncooked.Player;
using Uncooked.Train;
using Uncooked.UI;

namespace Uncooked.Managers
{
    public enum GameState { Play, Pause, Edit }

    public class GameManager : MonoBehaviour
    {
        public event System.Action<GameState> OnStateChange;
        public event System.Action OnCheckpoint, OnEndCheckpoint;

        [SerializeField] private LayerMask interactMask;
        [SerializeField] [Min(0)] private float baseTrainSpeed = 0.05f, trainSpeedIncrement = 0.05f, speedUpMultiplier = 2;
        [SerializeField] [Min(5)] private float trainInitialDelay = 10;
        [Header("UI Buttons")]
        [SerializeField] private TriggerButton checkpointContinueButton;
        [Space]
        public GameObject[] numbersPrefabs;
        [SerializeField] private float numberFadeSpeed = 0.5f, numberFadeDuration = 1.25f;

        private static Locomotive[] locomotives;
        private GameState currentState;
        private int checkpointCount;

        public static Locomotive[] Locomotives => locomotives;
        public static GameState CurrentState 
        {
            get => instance.currentState;
            private set 
            {
                instance.currentState = value;
                instance.OnStateChange?.Invoke(value);
            } 
        }
        public LayerMask InteractMask => interactMask;
        public static float GetBaseTrainSpeed() => instance.baseTrainSpeed + instance.trainSpeedIncrement * instance.checkpointCount;
        public static float GetBoostTrainSpeed() => instance.speedUpMultiplier * (instance.baseTrainSpeed + instance.trainSpeedIncrement * instance.checkpointCount);
        public static bool IsPlaying() => CurrentState == GameState.Play;
        public static bool IsPaused() => CurrentState == GameState.Pause;
        public static bool IsEditing() => CurrentState == GameState.Edit;

        public static GameManager instance;

        void Awake()
        {
            if (instance == null) instance = this;
            else throw new System.Exception("Multiple GameManagers Exist");

            checkpointContinueButton.OnClick += ContinueFromCheckpoint;
            checkpointContinueButton.GetComponent<BoxCollider>().enabled = false;
        }

        void Start()
        {
            currentState = GameState.Play;
            
            StartTrainsWithDelay(trainInitialDelay);
        }

        public void StartTrainsWithDelay(float delayTime) => _ = StartCoroutine(StartTrains(delayTime));

        private IEnumerator StartTrains(float delay)
        {
            yield return new WaitForSeconds(delay - 5);

            locomotives = FindObjectsOfType<Locomotive>();
            foreach (var locomotive in locomotives) locomotive.OnDeath += EndGame;
            
            for (int countDown = 5; countDown > 0; countDown--)
            {
                yield return new WaitUntil(() => IsPlaying());

                foreach (var l in locomotives) FadeNumber(countDown, l.transform.position + Vector3.up, instance.numberFadeDuration);
                yield return new WaitForSeconds(1);
            }

            foreach (var l in locomotives) l.StartTrain();
        }

        /// <summary>
        /// Spawns in a number-shaped mesh at <paramref name="position"/>, and fades it out over <paramref name="duration"/> seconds
        /// </summary>
        /// <param name="number">Number in [0, 9] that gets spawned</param>
        public static async void FadeNumber(int number, Vector3 position, float duration)
        {
            if (number < 0 || number > 9) throw new System.Exception("Given number outside [0, 9] range");

            var numTransform = Instantiate(instance.numbersPrefabs[number], position, Quaternion.identity, instance.transform).transform;
            var meshes = numTransform.GetComponentsInChildren<MeshRenderer>();

            var deltaPos = instance.numberFadeSpeed * Time.deltaTime * Vector3.up;
            var startColor = meshes[0].material.color;
            var endColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            var startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                await Task.Yield();
                if (numTransform == null) break;

                numTransform.position += deltaPos;
                var newColor = Color.Lerp(startColor, endColor, (Time.time - startTime) / duration);
                foreach (var mesh in meshes) mesh.material.color = newColor;
            }

            if (numTransform) Destroy(numTransform.gameObject);
        }

        public void ReachCheckpoint()
        {
            CurrentState = GameState.Edit;
            checkpointCount++;

            // Edit mode UI
            Vector3 pos = checkpointContinueButton.transform.position;
            checkpointContinueButton.GetComponent<BoxCollider>().enabled = true;
            checkpointContinueButton.transform.position = new Vector3(pos.x, pos.y, locomotives[0].transform.position.z - 1);

            OnCheckpoint?.Invoke();
        }

        public void ContinueFromCheckpoint()
        {
            // Managers
            CurrentState = GameState.Play;

            // Edit mode UI
            checkpointContinueButton.GetComponent<BoxCollider>().enabled = false;

            OnEndCheckpoint?.Invoke();
        }

        private async void EndGame()
        {
            await Task.Delay(2000);
            var startTime = Time.time;
            await CameraManager.instance.SlideToStart();
            if (Time.time > startTime + 2) await Task.Delay(2000);
            if (instance) SceneManager.LoadScene("TestScene");
        }

        // Mainly used method for edit cam to see tracks
        /// <summary>
        /// Moves transform and all its children to given layer
        /// </summary>
        /// <param name="root">Starting object for layer transfer</param>
        /// <param name="layer">Selected layer (LayerMask.NameToLayer is recommended)</param>
        public static void MoveToLayer(Transform root, int layer)
        {
            Stack<Transform> moveTargets = new Stack<Transform>();
            moveTargets.Push(root);

            Transform currentTarget;
            while (moveTargets.Count != 0)
            {
                currentTarget = moveTargets.Pop();
                currentTarget.gameObject.layer = layer;
                foreach (Transform child in currentTarget) moveTargets.Push(child);
            }
        }

        private void OnDestroy()
        {
            instance = null;
            
            checkpointContinueButton.OnClick -= ContinueFromCheckpoint;
        }
    }
}