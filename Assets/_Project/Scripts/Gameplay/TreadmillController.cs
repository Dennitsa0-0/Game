using UnityEngine;
using ChronoDrop.Core;
using ChronoDrop.Systems;
using ChronoDrop.Progression;

namespace ChronoDrop.Gameplay
{
    /// <summary>
    /// Moves EnvironmentRoot upward to simulate the player falling.
    /// The player Transform stays fixed; the world comes up to meet it.
    /// Also feeds real depth (environmentRoot.position.y) to ChronoNavigator each frame.
    /// </summary>
    public sealed class TreadmillController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private DynamicSpeedController speedController;
        [SerializeField] private ChronoNavigator chronoNavigator;

        private bool _isRunning;
        private float _currentSpeed;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<GamePausedEvent>(OnGamePaused);

            if (speedController != null)
                speedController.SpeedChanged += OnSpeedChanged;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);

            if (speedController != null)
                speedController.SpeedChanged -= OnSpeedChanged;
        }

        private void Update()
        {
            if (!_isRunning || environmentRoot == null)
                return;

            environmentRoot.position += Vector3.up * (_currentSpeed * Time.deltaTime);

            // Feed real depth to ChronoNavigator (player is fixed at Y=0,
            // so depth = how far EnvironmentRoot has travelled upward)
            chronoNavigator?.SetExternalDepth(environmentRoot.position.y);
        }

        // ── Event handlers ──────────────────────────────────────────────────

        private void OnGameStarted(GameStartedEvent _)
        {
            if (environmentRoot != null)
                environmentRoot.position = Vector3.zero;

            speedController?.ResetRun();
            speedController?.SetPaused(false);
            _currentSpeed = speedController != null ? speedController.CurrentSpeed : 0f;
            _isRunning = true;
        }

        private void OnGameOver(GameOverEvent _)
        {
            _isRunning = false;
            speedController?.SetPaused(true);
        }

        private void OnGamePaused(GamePausedEvent evt)
        {
            _isRunning = !evt.IsPaused;
            speedController?.SetPaused(evt.IsPaused);
        }

        private void OnSpeedChanged(float speed)
        {
            _currentSpeed = speed;
        }
    }
}
