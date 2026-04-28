using System.Collections.Generic;
using UnityEngine;
using ChronoDrop.Core;

namespace ChronoDrop.Gameplay
{
    /// <summary>
    /// Pool-based obstacle spawner.
    ///
    /// Treadmill geometry recap:
    ///   • Player sits at world Y = 0 (fixed).
    ///   • EnvironmentRoot drifts upward; its world Y increases each frame.
    ///   • Obstacles are children of EnvironmentRoot.
    ///   • An obstacle's world Y = environmentRoot.position.y + obstacle.localPosition.y
    ///   • To appear BELOW the player, an obstacle needs localY < -environmentRoot.position.y
    ///
    /// Spawn head: we track the next LOCAL Y to spawn at and decrement it by
    /// <see cref="spawnInterval"/> each time we place a new obstacle.
    /// </summary>
    public sealed class ObstacleGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private Transform playerTransform;

        [Header("Prefab")]
        [SerializeField] private GameObject obstaclePrefab;

        [Header("Pool")]
        [SerializeField] private int poolSize = 24;

        [Header("Spawn Rules")]
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private float lookAheadDistance = 40f;  // units ahead of player to keep filled
        [SerializeField] private float recycleAboveOffset = 6f;  // recycle when this many units above player
        [SerializeField] private bool clearObstaclesDuringBonusStage = true;

        [Header("Obstacle Placement")]
        [SerializeField] private float maxHorizontalOffset = 1.8f;

        private readonly Queue<GameObject> _pool = new();
        private readonly List<GameObject> _active = new();
        private float _nextSpawnLocalY;
        private bool _isRunning;
        private bool _bonusStageActive;

        // ── Unity lifecycle ─────────────────────────────────────────────────

        private void Awake()
        {
            BuildPool();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameStopped);
            EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Subscribe<BonusStageStartedEvent>(OnBonusStageStarted);
            EventBus.Subscribe<BonusStageEndedEvent>(OnBonusStageEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameStopped);
            EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Unsubscribe<BonusStageStartedEvent>(OnBonusStageStarted);
            EventBus.Unsubscribe<BonusStageEndedEvent>(OnBonusStageEnded);
        }

        private void Update()
        {
            if (!_isRunning || _bonusStageActive)
                return;

            RecyclePassedObstacles();
            FillLookAhead();
        }

        // ── Pool ────────────────────────────────────────────────────────────

        private void BuildPool()
        {
            GameObject template = obstaclePrefab != null ? obstaclePrefab : CreateFallbackObstacle();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(template, environmentRoot != null ? environmentRoot : transform);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }

            // Clean up the temporary fallback if we created one
            if (obstaclePrefab == null)
                Destroy(template);
        }

        private static GameObject CreateFallbackObstacle()
        {
            // A wide, thin bar — visually obvious during testing
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "Obstacle_Fallback";
            obj.transform.localScale = new Vector3(3f, 0.35f, 0.5f);
            return obj;
        }

        // ── Spawn / Recycle ─────────────────────────────────────────────────

        private void FillLookAhead()
        {
            if (environmentRoot == null || playerTransform == null)
                return;

            // Player in EnvironmentRoot local space
            // (works even when root has only Y translation, which is our case)
            float playerLocalY = playerTransform.position.y - environmentRoot.position.y;
            float bottomLocalY = playerLocalY - lookAheadDistance;

            while (_nextSpawnLocalY > bottomLocalY && _pool.Count > 0)
            {
                PlaceObstacle(_nextSpawnLocalY);
                _nextSpawnLocalY -= spawnInterval;
            }
        }

        private void PlaceObstacle(float localY)
        {
            if (_pool.Count == 0)
                return;

            GameObject obj = _pool.Dequeue();
            obj.transform.localPosition = new Vector3(
                Random.Range(-maxHorizontalOffset, maxHorizontalOffset),
                localY,
                0f);
            obj.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            obj.SetActive(true);
            _active.Add(obj);
        }

        private void RecyclePassedObstacles()
        {
            if (playerTransform == null)
                return;

            float recycleWorldY = playerTransform.position.y + recycleAboveOffset;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                GameObject obj = _active[i];
                if (obj == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                if (obj.transform.position.y >= recycleWorldY)
                {
                    obj.SetActive(false);
                    _active.RemoveAt(i);
                    _pool.Enqueue(obj);
                }
            }
        }

        // ── Reset ───────────────────────────────────────────────────────────

        private void ResetGenerator()
        {
            ClearActiveObstacles();

            // First obstacle spawns one interval below player (local Y 0)
            _nextSpawnLocalY = -spawnInterval;
            _bonusStageActive = false;
        }

        private void ClearActiveObstacles()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                GameObject obj = _active[i];
                if (obj != null)
                {
                    obj.SetActive(false);
                    _pool.Enqueue(obj);
                }
            }
            _active.Clear();
        }

        // ── Event handlers ──────────────────────────────────────────────────

        private void OnGameStarted(GameStartedEvent _)
        {
            ResetGenerator();
            _isRunning = true;
        }

        private void OnGameStopped(GameOverEvent _)
        {
            _isRunning = false;
        }

        private void OnGamePaused(GamePausedEvent evt)
        {
            _isRunning = !evt.IsPaused;
        }

        private void OnBonusStageStarted(BonusStageStartedEvent _)
        {
            if (clearObstaclesDuringBonusStage)
                ClearActiveObstacles();

            _bonusStageActive = true;
        }

        private void OnBonusStageEnded(BonusStageEndedEvent _)
        {
            _bonusStageActive = false;
        }
    }
}
