using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public class RailTile : StackTile
    {
        [SerializeField] private Transform straightMesh, bentMesh;
        [Tooltip("Gameobject that enable/disable based on the rail's isPowered")]
        [SerializeField] private GameObject straightPower, bentPower;
        [SerializeField] private bool startsPowered;

        private Vector3Int inDirection = Vector3Int.zero, outDirection = Vector3Int.zero;
        private static LayerMask mask;

        public bool IsPowered => inDirection != Vector3Int.zero;

        void Awake()
        {
            if (mask == 0) mask = LayerMask.GetMask("Default");
        }

        protected override void Start()
        {
            if (startsPowered)
            {
                straightPower.SetActive(true);
                inDirection = Vector3Int.RoundToInt(straightMesh.forward);
                outDirection = Vector3Int.RoundToInt(straightMesh.forward);
            }

            base.Start();
        }

        public override Tile TryPickUp(Transform parent, int amount)
        {
            if (startsPowered) return null;
            if (IsPowered) SetState(Vector3Int.zero, Vector3Int.zero);

            return base.TryPickUp(parent, amount);
        }

        public override bool TryStackOn(StackTile stackBase)
        {
            if (stackBase is RailTile rail && rail.IsPowered) return false;
            return base.TryStackOn(stackBase);
        }

        public override void OnDrop(Vector3Int coords)
        {
            List<RailTile> poweredRails = new List<RailTile>();
            Vector3 dir = Vector3.forward;

            for (int i = 0; i < 4; i++)
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(coords, dir, out hitInfo, 1, mask))
                {
                    var rail = hitInfo.transform.GetComponent<RailTile>();
                    if (rail != null && rail.IsPowered && Vector3.Dot(rail.inDirection, coords - rail.transform.position) >= 0)
                        poweredRails.Add(rail);
                }

                dir = Quaternion.AngleAxis(90, Vector3.up) * dir;
            }

            if (poweredRails.Count == 1)
            {
                var dirToThis = coords - Vector3Int.FloorToInt(poweredRails[0].transform.position);

                if (poweredRails[0].outDirection != dirToThis) poweredRails[0].SetState(poweredRails[0].inDirection, dirToThis);
                SetState(dirToThis, dirToThis);
            }
        }

        private void SetState(Vector3Int inDir, Vector3Int outDir)
        {
            if (inDir != Vector3Int.zero)
            {
                bool isStraight = inDir - outDir == Vector3Int.zero;

                straightMesh.gameObject.SetActive(isStraight);
                bentMesh.gameObject.SetActive(!isStraight);
                straightPower.SetActive(isStraight);
                bentPower.SetActive(!isStraight);

                if (isStraight) straightMesh.forward = outDir;
                else
                {
                    // Sus code, don't look
                    Vector3Int deltaPos = inDir + outDir;
                    print(deltaPos);

                    if (Mathf.Abs(deltaPos.x + deltaPos.z) == 0)
                    {
                        if (inDir.x == 1 || inDir.y == 1) bentMesh.forward = Vector3.back;
                        else bentMesh.forward = Vector3.forward;
                    }
                    else
                    {
                        if (inDir.x == 1 || inDir.z == -1) bentMesh.forward = Vector3.left;
                        else bentMesh.forward = Vector3.right;
                    }
                }
            }
            else
            {
                straightMesh.gameObject.SetActive(true);
                bentMesh.gameObject.SetActive(false);
                straightPower.SetActive(false);
                bentPower.SetActive(false);

                straightMesh.forward = Vector3.forward;

                RaycastHit hitInfo;
                if (Physics.Raycast(transform.position, outDirection, out hitInfo, 1, mask))
                {
                    var rail = hitInfo.transform.GetComponent<RailTile>();
                    if (rail != null && rail.IsPowered) rail.SetState(Vector3Int.zero, rail.outDirection);
                }
            }

            inDirection = inDir;
            outDirection = outDir;
        }
    }
}