using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    bool TryInteractUsing(IPickupable item, RaycastHit hitInfo);
}