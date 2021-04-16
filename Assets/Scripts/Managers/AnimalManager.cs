using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Managers
{
    public class AnimalManager : MonoBehaviour
    {
        [SerializeField] private StackTile[] fishPrefabs;

        public static AnimalManager instance;

        void Awake()
        {
            instance = this;
        }

        public StackTile RandomFish => Instantiate(fishPrefabs[Random.Range(0, fishPrefabs.Length - 1)], transform);
    }
}