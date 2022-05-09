using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Overrailed.Player;
using Overrailed.Train;
using Overrailed.UI;

namespace Overrailed.Managers
{
    public enum GameState { Play, Pause, Edit }

    public class GameManager : MonoBehaviour
    {
        public event System.Action<GameState> OnStateChange;
        public event System.Action OnCheckpoint, OnEndCheckpoint, OnGameEnd;

        [SerializeField] private string titleSceneName = "TitleScreenScene";
        [Space]
        [SerializeField] [Min(0)] private float baseTrainSpeed = 0.05f, trainSpeedIncrement = 0.05f, speedUpMultiplier = 2;
        [SerializeField] [Min(5)] private float trainInitialDelay = 10;
        [Space]
        [SerializeField] private GameObject[] numbersPrefabs;
        [SerializeField] private float numberFadeSpeed = 0.5f, numberFadeDuration = 1.25f;
        [SerializeField] private AudioClip numberSpawnSound;

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
        public static float GetBaseTrainSpeed() => instance.baseTrainSpeed + instance.trainSpeedIncrement * instance.checkpointCount;
        public static float GetBoostTrainSpeed() => instance.speedUpMultiplier * (instance.baseTrainSpeed + instance.trainSpeedIncrement * instance.checkpointCount);
        public static bool IsPlaying() => instance && CurrentState == GameState.Play;
        public static bool IsPaused() => instance && CurrentState == GameState.Pause;
        public static bool IsEditing() => instance && CurrentState == GameState.Edit;

        public static GameManager instance;

        void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                Debug.LogError("Multiple GameManagers Exist");
                return;
            }

            instance = this;

            CurrentState = GameState.Play;
        }

        void Start()
        {
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
            AudioManager.instance.PlaySound(instance.numberSpawnSound, position);

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

            OnCheckpoint?.Invoke();
        }

        public void ContinueFromCheckpoint()
        {
            CurrentState = GameState.Play;

            OnEndCheckpoint?.Invoke();
        }

        private async void EndGame()
        {
            OnGameEnd?.Invoke();
            await Task.Delay(2000);

            if (!Application.isPlaying) return;

            var startTime = Time.time;
            await CameraManager.instance.SlideToStart();

            if (Time.time > startTime + 2) await Task.Delay(2000);

            if (instance) SceneManager.LoadScene(titleSceneName);
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
            while (moveTargets.Count > 0)
            {
                currentTarget = moveTargets.Pop();
                currentTarget.gameObject.layer = layer;
                foreach (Transform child in currentTarget) moveTargets.Push(child);
            }
        }

        private void OnDestroy()
        {
            instance = null;
        }
    }
}