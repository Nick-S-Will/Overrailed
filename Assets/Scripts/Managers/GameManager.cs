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
        public event System.Action OnCheckpoint, OnEndCheckpoint;
        public event System.Action<string> OnSpeedChange;

        [SerializeField] private LayerMask interactMask;
        [SerializeField] [Min(0)] private float baseTrainSpeed = 0.05f, trainSpeedIncrement = 0.05f, speedUpMultiplier = 2;
        [SerializeField] [Min(5)] private float trainInitialDelay = 10;
        [Header("UI Buttons")]
        [SerializeField] private TriggerButton checkpointContinueButton;
        [Space]
        public GameObject[] numbersPrefabs;
        [SerializeField] private float numberFadeSpeed = 0.5f, numberFadeDuration = 1.25f;

        private Locomotive[] locomotives;
        private float trainSpeed;
        private int checkpointCount;

        public Locomotive[] Locomotives => locomotives;
        public GameState CurrentState { get; private set; }
        public LayerMask InteractMask => interactMask;
        public float TrainSpeed 
        {
            get { return trainSpeed; }
            private set {
                trainSpeed = value;
                OnSpeedChange?.Invoke(trainSpeed.ToString());
            } 
        }
        public bool TrainIsSpeeding => TrainSpeed - (baseTrainSpeed + trainSpeedIncrement * checkpointCount) > 0.0001f; // > operator is inconsistent
        public bool IsPlaying() => CurrentState == GameState.Play;
        public bool IsPaused() => CurrentState == GameState.Pause;
        public bool IsEditing() => CurrentState == GameState.Edit;

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
            TrainSpeed = baseTrainSpeed;
            CurrentState = GameState.Play;
            
            StartTrainWithDelay(trainInitialDelay);
        }

        // For beta
        private async void Reset()
        {
            await Task.Delay(5000);
            if (Application.isPlaying) SceneManager.LoadScene("TestScene");
        }

        public void StartTrainWithDelay(float delayTime) => _ = StartCoroutine(StartTrain(delayTime));

        private IEnumerator StartTrain(float delay)
        {
            yield return new WaitForSeconds(delay - 5);

            locomotives = FindObjectsOfType<Locomotive>();
            foreach (var l in locomotives)
            {
                l.OnDeath += SpeedUp;
                // For beta
                l.OnDeath += Reset;
            }

            for (int countDown = 5; countDown > 0; countDown--)
            {
                yield return new WaitUntil(() => IsPlaying());

                foreach (var l in locomotives) FadeNumber(countDown, l.transform.position + Vector3.up, instance.numberFadeDuration);
                yield return new WaitForSeconds(1);
            }

            foreach (var l in locomotives) l.OnStartTrain?.Invoke();
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

            Destroy(numTransform.gameObject);
        }

        /// <summary>
        /// Gives train temporary speed buff until it reaches the next checkpoint
        /// </summary>
        public void SpeedUp() => trainSpeed = speedUpMultiplier * (baseTrainSpeed + trainSpeedIncrement * checkpointCount);

        public void ReachCheckpoint()
        {
            foreach (var player in FindObjectsOfType<PlayerController>()) player.ForceDrop();

            // Managers
            CurrentState = GameState.Edit;
            checkpointCount++;
            CameraManager.instance.TransitionEditMode();

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
            TrainSpeed = baseTrainSpeed + trainSpeedIncrement * checkpointCount;
            CameraManager.instance.TransitionGameMode();

            // Edit mode UI
            checkpointContinueButton.GetComponent<BoxCollider>().enabled = false;

            OnEndCheckpoint?.Invoke();
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

            if (locomotives == null) return;
            foreach (var l in locomotives)
            {
                if (l == null) continue;

                l.OnDeath -= SpeedUp;
                // For beta
                l.OnDeath -= Reset;
            }
        }
    }
}