using System.Collections;
using ChronoDrop.Core;
using ChronoDrop.Systems;
using UnityEngine;

namespace ChronoDrop.Gameplay
{
    public readonly struct BonusRiftSpawnedEvent
    {
        public readonly Vector3 WorldPosition;
        public BonusRiftSpawnedEvent(Vector3 worldPosition) { WorldPosition = worldPosition; }
    }

    public readonly struct BonusRiftEnteredEvent
    {
        public readonly Vector3 WorldPosition;
        public BonusRiftEnteredEvent(Vector3 worldPosition) { WorldPosition = worldPosition; }
    }

    public readonly struct BonusStageStartedEvent
    {
        public readonly float DurationSeconds;
        public BonusStageStartedEvent(float durationSeconds) { DurationSeconds = durationSeconds; }
    }

    public readonly struct BonusStageEndedEvent { }

    public sealed class BonusStageController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DynamicSpeedController speedController;

        [Header("Bonus Stage")]
        [SerializeField] private float durationSeconds = 10f;
        [SerializeField] private float speedMultiplier = 3f;

        private Coroutine _routine;
        private bool _gameActive;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<BonusRiftEnteredEvent>(OnBonusRiftEntered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<BonusRiftEnteredEvent>(OnBonusRiftEntered);
        }

        private void OnBonusRiftEntered(BonusRiftEnteredEvent _)
        {
            if (!_gameActive)
                return;

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(RunBonusStage());
        }

        private IEnumerator RunBonusStage()
        {
            float duration = Mathf.Max(0.1f, durationSeconds);
            EventBus.Raise(new BonusStageStartedEvent(duration));
            speedController?.ApplyTemporarySpeedBoost("BonusStage", speedMultiplier, duration);

            yield return new WaitForSeconds(duration);

            EventBus.Raise(new BonusStageEndedEvent());
            _routine = null;
        }

        private void OnGameStarted(GameStartedEvent _)
        {
            _gameActive = true;
        }

        private void OnGameOver(GameOverEvent _)
        {
            _gameActive = false;
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
                EventBus.Raise(new BonusStageEndedEvent());
            }
        }
    }
}
