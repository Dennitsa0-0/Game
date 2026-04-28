using ChronoDrop.Core;
using UnityEngine;

namespace ChronoDrop.Gameplay
{
    public sealed class BonusRiftSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private Transform playerTransform;

        [Header("Prefab")]
        [SerializeField] private BonusRift riftPrefab;

        [Header("Spawn Rules")]
        [SerializeField] private float firstSpawnDepth = 650f;
        [SerializeField] private float depthInterval = 900f;
        [SerializeField, Range(0f, 1f)] private float spawnChance = 0.35f;
        [SerializeField] private float lookAheadDistance = 34f;
        [SerializeField] private float horizontalRange = 1.8f;

        private BonusRift _activeRift;
        private float _nextDepth;
        private bool _isRunning;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameStopped);
            EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Subscribe<BonusStageStartedEvent>(OnBonusStageStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameStopped);
            EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Unsubscribe<BonusStageStartedEvent>(OnBonusStageStarted);
        }

        private void Update()
        {
            if (!_isRunning || environmentRoot == null || playerTransform == null)
                return;

            float depth = Mathf.Max(0f, environmentRoot.position.y);
            if (depth + lookAheadDistance < _nextDepth)
                return;

            TrySpawnAtDepth(_nextDepth);
            _nextDepth += Mathf.Max(1f, depthInterval);
        }

        private void TrySpawnAtDepth(float depth)
        {
            if (_activeRift != null && _activeRift.gameObject.activeSelf)
                return;

            if (Random.value > spawnChance)
                return;

            BonusRift rift = ResolveRiftInstance();
            if (rift == null)
                return;

            float localY = -depth;
            rift.transform.SetParent(environmentRoot, false);
            rift.transform.localPosition = new Vector3(Random.Range(-horizontalRange, horizontalRange), localY, 0f);
            rift.transform.localRotation = Quaternion.identity;
            rift.gameObject.SetActive(true);
            _activeRift = rift;
            EventBus.Raise(new BonusRiftSpawnedEvent(rift.transform.position));
        }

        private BonusRift ResolveRiftInstance()
        {
            if (_activeRift != null)
                return _activeRift;

            if (riftPrefab != null)
                return Instantiate(riftPrefab, environmentRoot != null ? environmentRoot : transform);

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = "BonusRift_Fallback";
            fallback.transform.localScale = Vector3.one * 0.9f;
            Collider collider = fallback.GetComponent<Collider>();
            collider.isTrigger = true;

            BonusRift rift = fallback.AddComponent<BonusRift>();
            fallback.SetActive(false);
            return rift;
        }

        private void OnGameStarted(GameStartedEvent _)
        {
            _nextDepth = Mathf.Max(0f, firstSpawnDepth);
            _isRunning = true;
            if (_activeRift != null)
                _activeRift.gameObject.SetActive(false);
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
            if (_activeRift != null)
                _activeRift.gameObject.SetActive(false);
        }
    }
}
