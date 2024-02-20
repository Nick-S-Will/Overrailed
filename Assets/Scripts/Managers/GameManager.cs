using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using Overrailed.Managers.Cameras;

namespace Overrailed.Managers
{
    public class GameManager : Manager
    {
        public event System.Action OnCheckpoint, OnEndCheckpoint, OnGameEnd;

        private int checkpointCount;

        public int CheckpointCount => checkpointCount;

        protected override void Awake() => base.Awake();

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

        private IEnumerator EndGameRoutine()
        {
            OnGameEnd?.Invoke();
            yield return new WaitForSeconds(1f);

            if (!Application.isPlaying) yield break;

            yield return CameraManager.SlideToStart();
            yield return new WaitForSeconds(1.5f);

            if (instance) SceneManager.LoadScene(titleSceneName);
        }
        public void EndGame() => _ = StartCoroutine(EndGameRoutine());

        protected override void OnDestroy() => base.OnDestroy();
    }
}