using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPickupable
{
    /// <summary>
    /// Chosen by classes that implement this, used to determine how the object will be held when picked up
    /// </summary>
    bool IsTwoHanded { get; }

    /// <summary>
    /// Picks up an IPickupable 
    /// </summary>
    /// <param name="parent">Transform the returned IPickupable will be parented to</param>
    /// <param name="amount">Max amount to pick up</param>
    /// <returns>The selected IPickupable from this</returns>
    IPickupable TryPickUp(Transform parent, int amount);

    /// <summary>
    /// Used to do a custom action and choose if the IPickupable is held or dropped
    /// </summary>
    /// <returns>True if implementation wants IPickupable to be dropped, otherwise false</returns>
    bool OnTryDrop();

    /// <summary>
    /// Does special action if needed by the class
    /// </summary>
    void Drop(Vector3Int position);
}