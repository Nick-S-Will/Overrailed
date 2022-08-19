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
            await Task.Delay(1000);

            if (!Application.isPlaying) return;

            await CameraManager.SlideToStart();
            await Task.Delay(1500);

            if (instance) SceneManager.LoadScene(titleScene.name);
        }

        protected override void OnDestroy() => base.OnDestroy();
    }
}