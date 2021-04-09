using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private float trainSpeed = 0.1f;
        [SerializeField] private bool isEditing;

        public float TrainSpeed => trainSpeed;
        public bool IsEditing => isEditing;

        public static GameManager instance;

        void Start()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple Game Managers Exist");
        }

        private void OnDestroy()
        {
            instance = null;
        }
    }
}