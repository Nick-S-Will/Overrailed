using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPickupable
{
    Uncooked.Terrain.Tile TryPickUp(Transform parent, int amount);
}