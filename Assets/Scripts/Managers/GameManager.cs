using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
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

        public async void EndGame()
        {
            OnGameEnd?.Invoke();
            await Task.Delay(2000);

            if (!Application.isPlaying) return;

            var startTime = Time.time;
            await CameraManager.SlideToStart();

            if (Time.time > startTime + 2) await Task.Delay(2000);

            if (instance) SceneManager.LoadScene(titleSceneName);
        }

        protected override void OnDestroy() => base.OnDestroy();
    }
}