using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// Tries to use given pickup on this
    /// </summary>
    /// <param name="item">IPickupable used on this</param>
    /// <returns>The type of interaction that occurred</returns>
    Interaction TryInteractUsing(IPickupable item);
}

public enum Interaction { None, Used, Interacted }