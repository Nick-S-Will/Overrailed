using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Generation;
using Overrailed.Terrain.Tiles;
using Overrailed.Train;

namespace Overrailed.Terrain
{
    [SelectionBase]
    public class MapManager : MonoBehaviour
    {
        public event System.Action OnFinishAnimateFirstChunk;

        #region Inspector Variables
        [SerializeField] private MonoBehaviour defaultPlayerPrefab;
        [SerializeField] private LayerMask interactMask;
        [SerializeField] [Min(5)] private float initialTrainDelay = 15;
        [Space]
        [SerializeField] private Color highlightColor = Color.white;
        [SerializeField] private bool highlightEnabled = true;
        [SerializeField] private bool visualizeBounds;
        [Header("Animation")]
        [SerializeField] private Vector3 spawnOffset = 10 * Vector3.down;
        [SerializeField] [Min(0)] private float groundSpawnInterval = 0.2f, obstacleSpawnInterval = 0.1f, bounceHeight = 1;
        [SerializeField] [Min(1)] private float slideSpeed = 100;
        [Space]
        [SerializeField] private GameObject[] numbersPrefabs;
        [SerializeField] private float numberFadeSpeed = 0.5f, numberFadeDuration = 1.25f;
        [SerializeField] private AudioClip numberSpawnSound;
        #endregion

        private List<MeshRenderer> newHighlights = new List<MeshRenderer>(), highlights = new List<MeshRenderer>();
        private static List<Locomotive> locomotives = new List<Locomotive>();

        public static Locomotive[] Locomotives => locomotives.ToArray();
        public Vector3Int IntPos => Vector3Int.RoundToInt(transform.position);
        public LayerMask InteractMask => interactMask;
        public int ChunkLength => transform.GetChild(1).GetChild(0).childCount;
        public bool HighlightEnabled => highlightEnabled;

        private void Awake()
        {
            transform.position = IntPos;
        }

        private void Start()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnEndCheckpoint += AnimateNewChunk;
                gm.OnGameEnd += ShowAllChunks;
                OnFinishAnimateFirstChunk += SpawnPlayer;
                OnFinishAnimateFirstChunk += StartTrain;
            }
            else if (Manager.instance is MainMenuManager mm)
            {
                OnFinishAnimateFirstChunk += mm.SlideInMainElements;
                OnFinishAnimateFirstChunk += mm.MoveSelectedSkinToGame;
            }
            else if (Manager.instance is TutorialManager)
            {
                OnFinishAnimateFirstChunk += SpawnPlayer;
            }

            if (GetComponent<MapGenerator>() == null) AnimateFirstChunk();

            if (highlightEnabled) _ = StartCoroutine(UpdateHighlighting());
        }

        public static void AddLocomotive(Locomotive newLocomotive) => locomotives.Add(newLocomotive);

        public void SpawnPlayer()
        {
            MonoBehaviour playerPrefab = Manager.GetSkin();
            if (playerPrefab == null) playerPrefab = defaultPlayerPrefab;

            var obstacles = transform.GetChild(1).GetChild(1);
            var station = obstacles.GetChild(obstacles.childCount - 2);
            var stationPos = Vector3Int.RoundToInt(station.position);
            Instantiate(playerPrefab, stationPos, Quaternion.identity, null);
        }

        #region Train Starting
        public void StartTrain() => StartTrain(initialTrainDelay);
        private async void StartTrain(float delay)
        {
            await Manager.Delay(delay - 5f);
            if (!Application.isPlaying) return;

            var locomotive = GetComponentInChildren<Locomotive>();
            if (locomotive == null) Debug.LogError("Map has no Locomotive");

            for (int countDown = 5; countDown > 0; countDown--)
            {
                if (locomotive == null) return;
                Utils.FadeObject(numbersPrefabs[countDown], locomotive.transform.position + Vector3.up, numberFadeSpeed, numberFadeDuration);

                await Manager.Delay(1);
            }

            locomotive.StartTrain();
        }
        #endregion

        #region Tile highlighting
        public void TryHighlightTile(Transform tile)
        {
            if (tile == null) return;

            foreach (var renderer in tile.GetComponentsInChildren<MeshRenderer>())
            {
                // Standard shader to avoid text mesh pro meshes
                if (!newHighlights.Contains(renderer) && renderer.material.shader == Shader.Find("Standard")) newHighlights.Add(renderer);
            }
        }

        /// <summary>
        /// Removes the highlight on tiles that are no longer selected
        /// </summary>
        private void RemoveOldHighlights()
        {
            foreach (var renderer in highlights.ToArray())
            {
                if (!newHighlights.Contains(renderer)) highlights.Remove(renderer);
            }
        }

        /// <summary>
        /// Highlights the tiles in <see cref="newHighlights"/> that aren't already highlited
        /// </summary>
        private void AddHighlights()
        {
            foreach (var renderer in newHighlights)
            {
                if (!highlights.Contains(renderer))
                {
                    highlights.Add(renderer);
                    _ = StartCoroutine(HightlightMesh(renderer));
                }
            }
        }

        /// <summary>
        /// Tints selected tile's children's meshes by highlightColor until tile is no longer selected
        /// </summary>
        private IEnumerator HightlightMesh(MeshRenderer renderer)
        {
            if (renderer == null) yield break;

            // Tint mesh
            var originalColor = renderer.material.color;
            renderer.material.color = 0.5f * (originalColor + highlightColor);

            yield return new WaitWhile(() => highlights.Contains(renderer));

            // Reset colors
            if (renderer) renderer.material.color = originalColor;
        }

        private IEnumerator UpdateHighlighting()
        {
            while (this)
            {
                RemoveOldHighlights();
                AddHighlights();

                newHighlights.Clear();

                yield return new WaitForEndOfFrame();
            }
        }
        #endregion

        #region Getting Map Points/Tiles
        public static MapManager FindMap(Vector3 point)
        {
            foreach (var mapManager in FindObjectsOfType<MapManager>())
            {
                if (mapManager.PointIsInPlayBounds(point))
                {
                    return mapManager;
                }
            }

            return null;
        }

        private Transform GetParentAt(Vector3Int position)
        {
            try
            {
                var chunk = transform.GetChild(position.x / ChunkLength + 1);
                var section = chunk.GetChild(position.y); // Ground or Obstacles
                var row = section.GetChild(position.x % ChunkLength);
                return row;
            }
            catch (UnityException) { return null; }
        }
        /// <summary>
        /// Gets the tile at the given coords
        /// </summary>
        /// <param name="point">Coords of the searched tile</param>
        /// <returns>The transform of the tile at <paramref name="point"/> if there is one, otherwise null</returns>
        public Transform GetTileAt(Vector3Int point)
        {
            if (!PointIsInPlayBounds(point)) return null;

            // Ground
            if (point.y == transform.position.y)
            {
                point -= IntPos;
                var row = GetParentAt(point);

                try { return row.GetChild(point.z); }
                catch (UnityException)
                {
                    Debug.LogError("Invalid point on map " + point);
                    return null;
                }
            }
            // Obstacles
            else
            {
                _ = Physics.Raycast(new Vector3(point.x, transform.position.y - 0.5f, point.z), Vector3.up, out RaycastHit hitData, 1, interactMask);

                return hitData.transform;
            }
        }

        private Bounds GetPlayBounds()
        {
            var groundCollider = transform.GetChild(0);
            var center = groundCollider.position + Vector3.up;
            var size = groundCollider.GetComponent<BoxCollider>().size;

            return new Bounds(center, new Vector3(size.x, 1, size.z));
        }
        /// <summary>
        /// Checks if <paramref name="point"/> is over the ground collider, up to 3 units above it
        /// </summary>
        public bool PointIsInPlayBounds(Vector3 point) => GetPlayBounds().Contains(point);
        /// <summary>
        /// Checks if <paramref name="point"/> is over the ground collider extended by 30 on the x axis and up to 3 units above it
        /// </summary>
        public bool PointIsInEditBounds(Vector3 point)
        {
            var bounds = GetPlayBounds();
            bounds.size += 30 * Vector3.right;

            return bounds.Contains(point);
        }
        /// <summary>
        /// Checks if <paramref name="point"/> is in bounds based on <see cref="GameManager.CurrentState"/>
        /// </summary>
        public bool PointIsInBounds(Vector3 point)
        {
            if (Manager.Exists) return (Manager.IsPlaying() && PointIsInPlayBounds(point)) || (Manager.IsEditing() && PointIsInEditBounds(point));
            else return PointIsInPlayBounds(point);
        }
        #endregion

        #region Pickup Placing
        /// <summary>
        /// Moves <paramref name="pickup"/> to the nearest coords around <paramref name="pickup"/>'s position
        /// </summary>
        /// <returns>The coords at which the pickup is move to</returns>
        public Vector3Int MovePickup(IPickupable pickup) => PlacePickup(pickup, Vector3Int.RoundToInt((pickup as Tile).transform.position), false);
        /// <summary>
        /// Places given pickup at coords, or nearest avaible, and enables its <see cref="BoxCollider"/>
        /// </summary>
        /// <returns>The coords at which the pickup is placed</returns>
        public Vector3Int PlacePickup(IPickupable pickup, Vector3Int startCoords) => PlacePickup(pickup, startCoords, true);
        private Vector3Int PlacePickup(IPickupable pickup, Vector3Int startCoords, bool includeStart)
        {
            if (!PointIsInPlayBounds(startCoords)) throw new System.Exception($"startCoords aren't in the bounds of the map. Value: {startCoords}");

            LayerMask mask = LayerMask.GetMask("Default", "Water", "Rail", "Entity");
            var flags = new HashSet<Vector3Int>();
            var toCheck = new Queue<Vector3Int>();

            if (includeStart) toCheck.Enqueue(startCoords);
            else
            {
                flags.Add(startCoords);
                foreach (var point in GetAdjacentCoords(startCoords)) if (PointIsInBounds(point)) toCheck.Enqueue(point);
            }

            while (toCheck.Count > 0)
            {
                var coords = toCheck.Dequeue();
                Collider[] colliders = Physics.OverlapBox(coords, 0.45f * Vector3.one, Quaternion.identity, mask);

                if (colliders.Length > 0)
                {
                    // Tries to stack pickup if possible
                    var stack = colliders[0].GetComponent<StackTile>();
                    if (pickup is StackTile toStack && stack != null && toStack.TryStackOn(stack)) return coords;

                    // Prevents from checking same coord twice
                    flags.Add(coords);

                    // Adds adjacent points to list of points to check
                    foreach (var point in GetAdjacentCoords(coords)) if (!flags.Contains(point) && PointIsInBounds(point)) toCheck.Enqueue(point);
                }
                else
                {
                    ForcePlacePickup(pickup, coords);
                    return coords;
                }
            }

            throw new System.Exception($"No viable coord found on map for {((Object)pickup).name}");
        }
        /// <summary>
        /// Places <paramref name="pickup"/> at <paramref name="coords"/> without any collision checks
        /// </summary>
        public void ForcePlacePickup(IPickupable pickup, Vector3Int coords)
        {
            var t = (pickup as MonoBehaviour).transform;
            t.parent = GetParentAt(coords);
            t.position = coords;
            t.localRotation = Quaternion.identity;

            t.GetComponent<BoxCollider>().enabled = true;
            pickup.Drop(coords);
        }

        /// <summary>
        /// Calculates the 4 adjacent coordinates on the XZ plane
        /// </summary>
        private Vector3Int[] GetAdjacentCoords(Vector3Int coord) => new Vector3Int[] { coord + Vector3Int.forward, coord + Vector3Int.right, coord + Vector3Int.back, coord + Vector3Int.left };
        #endregion

        #region Map Toggles
        public void DisableObstacles() => SetHitboxes(false);
        public void EnableObstacles() => SetHitboxes(true);
        private void SetHitboxes(bool enabled)
        {
            // Water
            for (int i = 1; i < transform.childCount; i++)
            {
                foreach (var collider in transform.GetChild(i).GetChild(0).GetComponentsInChildren<BoxCollider>())
                {
                    collider.enabled = enabled;
                }
            }

            // Obstacles
            for (int i = 1; i < transform.childCount; i++)
            {
                foreach (var collider in transform.GetChild(i).GetChild(1).GetComponentsInChildren<BoxCollider>())
                {
                    // Prevents tiles in a stack to all enable their hitboxes
                    if (!enabled)
                    {
                        var stack = collider.GetComponent<StackTile>();
                        if (stack && stack.PrevInStack) continue;
                    }

                    if (collider.gameObject.layer == LayerMask.NameToLayer("Default")) collider.enabled = enabled;
                }
            }
        }

        public void LiftNewChunk() => SetChunkDisplaced(transform.childCount - 2, true);
        public void LowerNewChunk() => SetChunkDisplaced(transform.childCount - 2, false);
        private void SetChunkDisplaced(int index, bool displaced)
        {
            transform.GetChild(index + 1).localPosition = displaced ? 20 * Vector3.up : Vector3.zero;
        }

        public void ShowAllChunks()
        {
            foreach (Transform chunk in transform) chunk.gameObject.SetActive(true);
        }
        #endregion

        #region Spawning animation
        /// <summary>
        /// Slides in the first chunk of the map, and invokes <see cref="OnFinishAnimateFirstChunk"/>. Only meant for the start of the game.
        /// </summary>
        public async void AnimateFirstChunk()
        {
            await Task.Yield();
            await AnimateChunk(0);

            OnFinishAnimateFirstChunk?.Invoke();
        }
        public void AnimateNewChunk()
        {
            LowerNewChunk();
            _ = AnimateChunk(transform.childCount - 2);
        }

        public async Task AnimateChunk(int chunkIndex)
        {
            if (spawnOffset == Vector3.zero || !Application.isPlaying) return;

            var chunk = transform.GetChild(chunkIndex + 1);
            Transform ground = chunk.GetChild(0), obstacles = chunk.GetChild(1);
            List<Task> slideTasks = new List<Task>();

            // Hiding and moving station and checkpoint
            var extras = new List<Transform>();
            var extraStartPos = new List<Vector3>();
            // Adds station to extras
            if (obstacles.childCount - ground.childCount == 2) extras.Add(obstacles.GetChild(obstacles.childCount - 2));
            // Adds checkpoint to extras
            if (obstacles.childCount - ground.childCount >= 1) extras.Add(obstacles.GetChild(obstacles.childCount - 1));
            foreach (var obj in extras)
            {
                extraStartPos.Add(obj.localPosition);
                obj.localPosition += spawnOffset;

                obj.gameObject.SetActive(false);
                foreach (var tile in obj.GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }

            // Moving and hiding each row
            for (int i = 0; i < ground.childCount; i++)
            {
                ground.GetChild(i).localPosition += spawnOffset;
                obstacles.GetChild(i).localPosition += spawnOffset;

                ground.GetChild(i).gameObject.SetActive(false);
                // Hides obstacle graphics without affecting colliders
                foreach (var tile in obstacles.GetChild(i).GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }

            // Turning on and sliding each ground row into place
            System.Func<float, Task> Delay = time => Task.Delay(Mathf.RoundToInt(1000f * time));
            if (Application.isPlaying) Delay = time => Manager.Delay(time);
            foreach (Transform groundRow in ground)
            {
                groundRow.gameObject.SetActive(true);

                slideTasks.Add(AnimateSlideAndBounce(groundRow, Vector3.zero));

                await Manager.Pause;
                await Delay(groundSpawnInterval);
            }

            // Turning on and sliding each obstacle row into place
            for (int i = 0; i < ground.childCount; i++)
            {
                var obstacleRow = obstacles.GetChild(i);
                foreach (var tile in obstacleRow.GetComponentsInChildren<Tile>()) tile.SetVisible(true);

                slideTasks.Add(AnimateSlideAndBounce(obstacleRow, Vector3.zero));

                if (obstacleRow.childCount == 0) continue;

                await Manager.Pause;
                await Delay(obstacleSpawnInterval);
            }

            // Turning on and sliding station and checkpoint into place
            foreach (var transform in extras)
            {
                transform.gameObject.SetActive(true);
                foreach (var tile in transform.GetComponentsInChildren<Tile>()) tile.SetVisible(true);
            }
            if (extras.Count > 0)
            {
                await AnimateSlide(extras[0], extraStartPos[0]);
                if (extras.Count > 1) await AnimateSlide(extras[1], extraStartPos[1]);
            }

            await Task.WhenAll(slideTasks.ToArray());
        }

        private async Task AnimateSlide(Transform t, Vector3 destination)
        {
            while (t.localPosition != destination)
            {
                t.localPosition = Vector3.MoveTowards(t.localPosition, destination, slideSpeed * Time.deltaTime);

                await Manager.Pause;
                await Task.Yield();
            }
        }

        private async Task AnimateSlideAndBounce(Transform t, Vector3 destination)
        {
            await AnimateSlide(t, destination);

            var time = 0f;
            while (time <= 1f)
            {
                t.localPosition = destination + bounceHeight * new Vector3(0, Mathf.Sin(2 * Mathf.PI * (time)), 0);

                await Manager.Pause;
                await Task.Yield();
                time += Time.deltaTime;
            }

            t.localPosition = destination;
        }
        #endregion

        private void OnDrawGizmos()
        {
            if (visualizeBounds)
            {
                var bounds = GetPlayBounds();
                Gizmos.DrawCube(bounds.center, bounds.size);
            }
        }

        void OnDestroy()
        {
            if (!Manager.Exists) return;

            if (Manager.instance is GameManager gm)
            {
                gm.OnEndCheckpoint -= AnimateNewChunk;
                gm.OnGameEnd -= ShowAllChunks;
                OnFinishAnimateFirstChunk -= SpawnPlayer;
                OnFinishAnimateFirstChunk -= StartTrain;
            }
            else if (Manager.instance is MainMenuManager mm)
            {
                OnFinishAnimateFirstChunk -= mm.SlideInMainElements;
                OnFinishAnimateFirstChunk -= mm.MoveSelectedSkinToGame;
            }
            else if (Manager.instance is TutorialManager)
            {
                OnFinishAnimateFirstChunk -= SpawnPlayer;
            }

            var locomotive = GetComponentInChildren<Locomotive>();
            if (locomotive) locomotives.Remove(locomotive);
        }
    }
}