using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Terrain.Generation;
using Uncooked.Terrain.Tools;
using Uncooked.Terrain.Tiles;

public abstract class CharacterControls : MonoBehaviour
{
    #region Inspector Variables
    [Header("Walk")]
    [SerializeField] private float moveSpeed = 5;
    [Tooltip("Radians per second of the player's turn")]
    [SerializeField] private float turnSpeed = 15;
    [SerializeField] private float legSwingCoefficient = 0.1f, legRaiseAngle = 45;

    [Header("Dash")]
    [SerializeField] [Range(1, 10)] private float dashSpeedMultiplier = 2;
    [SerializeField] [Range(0, 1)] private float dashDuration = 1;

    [Header("Arms")]
    [SerializeField] private float armTurnSpeed = 180;
    [SerializeField] private float armSwingSpeed = 360;

    [Header("Interact")]
    [SerializeField] private float interactInterval = 0.5f;

    [Header("Transforms")] [SerializeField] private Transform armL;
    [SerializeField] private Transform armR, legL, legR, calfL, calfR, toolHolder, pickupHolder;
    #endregion

    protected MapManager map;
    private CharacterController controller;
    private List<Coroutine> currentArmTurns = new List<Coroutine>();
    private Coroutine toolSwinging, legSwinging;
    private IPickupable heldItem;
    private Vector3 lastInputDir = Vector3.forward;
    private float lastDashTime, lastInteractTime;
    /// <summary>
    /// True if player was moving in the previous update, used to start leg swinging
    /// </summary>
    private bool wasMoving;
    /// <summary>
    /// True if player is moving in the current update, used to keep legs swinging
    /// </summary>
    private bool isMoving;

    public Vector3Int LookPoint => Vector3Int.RoundToInt(transform.position + Vector3.up + lastInputDir);
    public int Strength { get; private set; } = 2;
    public bool IsHoldingItem => heldItem != null;

    protected virtual void Start()
    {
        try { map = Physics.OverlapBox(transform.position, 0.1f * Vector3.one, Quaternion.identity, LayerMask.GetMask("Ground"))[0].transform.parent.GetComponent<MapManager>(); }
        catch (NullReferenceException) { throw new Exception("Character spawned in without a map beneath it"); }
        controller = GetComponent<CharacterController>();
        lastDashTime = -dashDuration;
    }

    #region Movement
    protected void HandleMovement(bool left, bool right, bool forwards, bool backwards, bool dash)
    {
        // Movement Input
        var hori = (left ? -1 : 0) + (right ? 1 : 0);
        var vert = (forwards ? 1 : 0) + (backwards ? -1 : 0);
        var input = new Vector3(hori, 0, vert).normalized;

        transform.forward = Vector3.RotateTowards(transform.forward, lastInputDir, turnSpeed * Time.deltaTime, 0);
        if (!UpdateMovingStates(input, dash)) return;

        Vector3 deltaPos;
        if (input == Vector3.zero) deltaPos = moveSpeed * lastInputDir * Time.deltaTime;
        else
        {
            deltaPos = moveSpeed * input * Time.deltaTime;
            lastInputDir = input;
        }

        if (dash) lastDashTime = Time.time;
        if (Time.time < lastDashTime + dashDuration) deltaPos *= DashMultiplier();

        // Moves character
        if (map.PointIsInBounds(transform.position + deltaPos)) controller.Move(deltaPos);
    }

    /// <summary>
    /// Updates <see cref="isMoving"/> and <see cref="wasMoving"/>
    /// </summary>
    /// <param name="input">Player XZ plane input</param>
    /// <returns>True if player is moving</returns>
    private bool UpdateMovingStates(Vector3 input, bool dash)
    {
        // Gets current moving state
        isMoving = input != Vector3.zero || dash || Time.time < lastDashTime + dashDuration;
        if (!isMoving) return wasMoving = false;

        // Start swinging legs if player wasn't moving last update
        if (!wasMoving)
        {
            if (legSwinging != null) StopCoroutine(legSwinging);
            legSwinging = StartCoroutine(SwingLegs());
        }
        wasMoving = isMoving;

        return true;
    }

    protected float DashMultiplier()
    {
        /// Parabola that goes from <see cref="dashSpeedMultiplier"/> to 1 over the <see cref="dashDuration"/> seconds
        return (dashSpeedMultiplier - 1) * (-Mathf.Pow(Mathf.InverseLerp(lastDashTime, lastDashTime + dashDuration, Time.time), 4) + 1) + 1;
    }

    protected void StopMovement()
    {
        wasMoving = false;
        isMoving = false;
    }
    #endregion

    #region Interact
    protected void TryInteract(bool single)
    {
        if (Time.time < lastInteractTime + interactInterval) return;
        lastInteractTime = Time.time;

        var collisions = Physics.OverlapBox(transform.position + Vector3.up + lastInputDir, 0.1f * Vector3.one, Quaternion.identity, GameManager.instance.InteractMask);
        if (collisions.Length > 0)
        {
            if (heldItem == null) TryToPickUp(collisions[0].transform.GetComponent<IPickupable>());
            else TryUseHeldItemOn(collisions[0].transform.GetComponent<IInteractable>());
        }
        else
        {
            if (single) TryDropSingle();
            else TryDrop();
        }
    }

    /// <summary>
    /// Tries to pick up given IPickupable
    /// </summary>
    /// <returns>True if given pickup isn't null and is picked up</returns>
    private bool TryToPickUp(IPickupable pickup)
    {
        if (pickup == null) return false;

        bool bothHands = pickup.IsTwoHanded;
        heldItem = pickup.TryPickUp(bothHands ? pickupHolder : toolHolder, Strength);
        if (heldItem != null) RaiseArms(bothHands);

        return heldItem != null;
    }

    /// <summary>
    /// Uses heldItem on the given Tile if not swinging
    /// </summary>
    private void TryUseHeldItemOn(IInteractable interactable)
    {
        if (toolSwinging != null || interactable == null) return;

        var interaction = interactable.TryInteractUsing(heldItem);
        if (interaction == Interaction.Used || interaction == Interaction.Interacted)
        {
            if (heldItem is Tool) toolSwinging = StartCoroutine(SwingTool());
            else
            {
                // Doesn't lower arms if tile wasn't fully used up
                if (interaction == Interaction.Interacted) return;
                
                LowerArms(heldItem.IsTwoHanded);
                heldItem = null;
            }
        }
        else if (!TrySwapHeldWith(interactable)) _ = TryReplaceWithHeld(interactable as Tile);
    }

    /// <summary>
    /// Swaps the <see cref="heldItem"/> and the <paramref name="interactable"/>'s positions
    /// </summary>
    /// <returns>True if they swapped successfully</returns>
    private bool TrySwapHeldWith(IInteractable interactable)
    {
        Tile heldTile = heldItem as Tile, interactTile = interactable as Tile;
        var stackPos = Vector3Int.RoundToInt(interactTile.transform.position);

        if (interactTile.CanPickUp && (!(interactTile is StackTile stackTile) || stackTile.GetStackCount() <= Strength))
        {
            _ = interactTile.TryPickUp(interactTile.IsTwoHanded ? pickupHolder : toolHolder, Strength);
            map.PlacePickup(heldItem, stackPos);
            heldItem = interactTile;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Moves <paramref name="toReplace"/> to the nearest viable position so that <see cref="heldItem"/> can be placed there
    /// </summary>
    /// <returns>True if the objects were moved successfully</returns>
    private bool TryReplaceWithHeld(IPickupable toReplace)
    {
        if (heldItem == null || !(toReplace as Tile).CanPickUp) return false;

        var replacePos = Vector3Int.RoundToInt((toReplace as MonoBehaviour).transform.position);
        _ = map.MovePickup(toReplace);
        map.ForcePlacePickup(heldItem, replacePos);

        LowerArms(heldItem.IsTwoHanded);
        heldItem = null;

        return true;
    }

    /// <summary>
    /// Places heldItem on the ground if it's not null
    /// </summary>
    /// <returns>True if <see cref="heldItem"/> was placed</returns>
    private bool TryDrop()
    {
        Vector3Int coords = LookPoint;
        if (heldItem == null || toolSwinging != null || !map.PointIsInPlayBounds(coords) || !heldItem.OnTryDrop()) return false;

        map.PlacePickup(heldItem, coords);
        LowerArms(heldItem.IsTwoHanded);
        heldItem = null;

        return true;
    }

    /// <summary>
    /// Places heldItem on the ground, or a single tile if it's a stack, if it's not null
    /// </summary>
    /// <returns>True if <see cref="heldItem"/>'s entire stack was placed</returns>
    private bool TryDropSingle()
    {
        Vector3Int coords = LookPoint;
        if (heldItem == null || toolSwinging != null || !map.PointIsInPlayBounds(coords) || !heldItem.OnTryDrop()) return false;

        if (heldItem is StackTile stack && stack.GetStackCount() > 1) map.PlacePickup(stack.TryPickUp(null, 1), coords);
        else return TryDrop();

        return false;
    }

    public void ForceDrop()
    {
        if (heldItem == null) return;

        map.PlacePickup(heldItem, Vector3Int.RoundToInt(transform.position + Vector3.up));
        LowerArms(heldItem.IsTwoHanded);
        heldItem = null;
    }
    #endregion

    #region Limb Movement
    /// <summary>
    /// Points armR to the local forwards
    /// </summary>
    /// <param name="both">Makes armL point forwards too</param>
    private void RaiseArms(bool both)
    {
        _ = StopTurnArmRoutines();
        currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armTurnSpeed)));
        if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, -90, armTurnSpeed)));
    }

    /// <summary>
    /// Points armR or both arms to the local downwards
    /// </summary>
    /// <param name="both">Makes armL point downwards too</param>
    private void LowerArms(bool both)
    {
        _ = StopTurnArmRoutines();
        currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armTurnSpeed)));
        if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, 0, armTurnSpeed)));
    }

    /// <summary>
    /// Rotates armR down, waits 0.1 seconds, then back up
    /// </summary>
    private IEnumerator SwingTool()
    {
        _ = StopTurnArmRoutines();
        currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armSwingSpeed)));
        yield return currentArmTurns[currentArmTurns.Count - 1];
        yield return new WaitForSeconds(0.05f);
        currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armSwingSpeed / 2)));
        yield return currentArmTurns[currentArmTurns.Count - 1];

        toolSwinging = null;
    }

    /// <summary>
    /// Animates arm turning around it's local x
    /// </summary>
    /// <param name="arm">Selected arm pivot</param>
    /// <param name="rotation">Final x value on arm.eulerAngles.x</param>
    /// <param name="turnSpeed">Speed of rotation in degrees per second</param>
    private IEnumerator TurnArm(Transform arm, float rotation, float turnSpeed)
    {
        Quaternion from = arm.localRotation, to = from * Quaternion.Euler(rotation - from.eulerAngles.x, 0, 0);
        float animSpeed = turnSpeed / Quaternion.Angle(from, to);
        float percent = 0;

        while (percent < 1)
        {
            yield return null;

            percent += animSpeed * Time.deltaTime;
            arm.localRotation = Quaternion.Lerp(from, to, percent);
        }

        arm.localRotation = to;
    }

    /// <summary>
    /// Cancels all ongoing TurnArm routines
    /// </summary>
    /// <returns>True if there were any TurnArm routines in progress</returns>
    private bool StopTurnArmRoutines()
    {
        if (currentArmTurns.Count == 0) return false;

        foreach (var c in currentArmTurns) StopCoroutine(c);
        currentArmTurns.Clear();

        return true;
    }

    private IEnumerator SwingLegs()
    {
        float time = 0;

        // TODO: swing arms too
        // Swing legs back and forth
        while (isMoving)
        {
            float angle = legRaiseAngle * Mathf.Sin(time);

            legL.localRotation = Quaternion.Euler(angle, 0, 0);
            legR.localRotation = Quaternion.Euler(-angle, 0, 0);
            if (angle < 0) // Left leg forward
            {
                calfL.localRotation = Quaternion.Inverse(legL.localRotation);
                calfR.localRotation = legR.localRotation;
            }
            else if (angle > 0) // Right leg forward
            {
                calfL.localRotation = legL.localRotation;
                calfR.localRotation = Quaternion.Inverse(legR.localRotation);
            }

            time += legSwingCoefficient * moveSpeed * legRaiseAngle * Time.deltaTime;
            yield return null;
        }

        // Return to base position
        while (legL.localRotation != Quaternion.identity || legR.localRotation != Quaternion.identity)
        {
            float angle = Quaternion.Angle(legL.localRotation, Quaternion.identity);
            float maxRadians = moveSpeed * legRaiseAngle * Mathf.Sqrt(angle) * Time.deltaTime;
            legL.localRotation = Quaternion.RotateTowards(legL.localRotation, Quaternion.identity, maxRadians);
            legR.localRotation = Quaternion.RotateTowards(legR.localRotation, Quaternion.identity, maxRadians);
            calfL.localRotation = Quaternion.RotateTowards(calfL.localRotation, Quaternion.identity, maxRadians);
            calfR.localRotation = Quaternion.RotateTowards(calfR.localRotation, Quaternion.identity, maxRadians);

            yield return null;
        }
    }
    #endregion
}