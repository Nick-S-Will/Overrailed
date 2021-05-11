using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// Tries to use given pickup on this
    /// </summary>
    /// <param name="item">IPickupable used on this</param>
    /// <param name="hitInfo">Info about the Raycast used to get this</param>
    /// <returns>True if an interaction happened, otherwise false</returns>
    bool TryInteractUsing(IPickupable item, RaycastHit hitInfo);
}