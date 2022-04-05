using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Terrain.Tiles;

namespace Overrailed.Managers
{
    public class MobManager : MonoBehaviour
    {
        [SerializeField] private StackTile[] fishPrefabs;

        public static MobManager instance;

        void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                Debug.LogError("Multiple MobManagers Found");
            }
            else instance = this; 
        }

        public StackTile RandomFish => Instantiate(fishPrefabs[Random.Range(0, fishPrefabs.Length - 1)], transform);

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}