using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Managers
{
    public class MobManager : MonoBehaviour
    {
        [SerializeField] private StackTile[] fishPrefabs;

        public static MobManager instance;

        void Awake()
        {
            if (instance == null) instance = this;
            else Debug.LogError("Multiple MobManagers Exist");
        }

        public StackTile RandomFish => Instantiate(fishPrefabs[Random.Range(0, fishPrefabs.Length - 1)], transform);
    }
}