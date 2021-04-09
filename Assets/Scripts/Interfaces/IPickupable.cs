using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPickupable
{
    bool IsTwoHanded();

    /// <summary>
    /// Picks up an IPickupable 
    /// </summary>
    /// <param name="parent">Transform the returned IPickupable will be parented to</param>
    /// <param name="amount">Max amount to pick up</param>
    /// <returns>The selected IPickupable from this</returns>
    IPickupable TryPickUp(Transform parent, int amount);

    /// <summary>
    /// Does special action if needed by the class
    /// </summary>
    void OnDrop(Vector3Int position);
}