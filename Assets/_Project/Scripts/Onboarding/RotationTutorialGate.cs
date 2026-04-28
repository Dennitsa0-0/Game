using System.Collections;
using ChronoDrop.Core;
using ChronoDrop.Systems;
using UnityEngine;

namespace ChronoDrop.Onboarding
{
    public readonly struct RotationTutorialPromptShownEvent
    {
        public readonly string PromptText;
        public RotationTutorialPromptShownEvent(string promptText) { PromptText = promptText; }
    }

    public readonly struct RotationTutorialCompletedEvent { }

    public sealed class RotationTutorialGate : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private WorldRotator worldRotator;
        [SerializeField] private CanvasGroup promptGroup;

        [Header("Trigger")]
        [SerializeField] private float triggerDepthMeters = 85f;
        [SerializeField] private string promptText = "Rotate the world to open the path!";

        [Header("Slow Motion")]
        [SerializeField, Range(0.01f, 0.25f)] private float slowMotionScale = 0.05f;
        [SerializeField] private float promptFadeSeconds = 0.12f;

        private bool _gameActive;
        private bool _promptActive;
        private bool _completed;
        private float _previousTimeScale = 1f;
        private Coroutine _fadeRoutine;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            RestoreTimeScale();
        }

        private void Update()
        {
            if (!_gameActive || _completed || _promptActive || environmentRoot == null)
                return;

            float depth = Mathf.Max(0f, environmentRoot.position.y);
            if (depth >= triggerDepthMeters)
                ShowPrompt();
        }

        public void OnRotateButtonPressed()
        {
            if (!_promptActive || worldRotator == null)
                return;

            worldRotator.RotationCompleted += CompleteTutorial;
            worldRotator.RotateClockwise();
        }

        private void ShowPrompt()
        {
            _promptActive = true;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            SetPromptVisible(true);
            EventBus.Raise(new RotationTutorialPromptShownEvent(promptText));
        }

        private void CompleteTutorial()
        {
            if (worldRotator != null)
                worldRotator.RotationCompleted -= CompleteTutorial;

            _completed = true;
            _promptActive = false;
            RestoreTimeScale();
            SetPromptVisible(false);
            EventBus.Raise(new RotationTutorialCompletedEvent());
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptGroup == null)
                return;

            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadePrompt(visible ? 1f : 0f));
        }

        private IEnumerator FadePrompt(float target)
        {
            float start = promptGroup.alpha;
            float elapsed = 0f;
            promptGroup.blocksRaycasts = target > 0.5f;
            promptGroup.interactable = target > 0.5f;

            while (elapsed < promptFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                promptGroup.alpha = Mathf.Lerp(start, target, elapsed / promptFadeSeconds);
                yield return null;
            }

            promptGroup.alpha = target;
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        private void OnGameStarted(GameStartedEvent _)
        {
            _gameActive = true;
            _promptActive = false;
            _completed = false;
            _previousTimeScale = 1f;
            SetPromptVisible(false);
        }

        private void OnGameOver(GameOverEvent _)
        {
            _gameActive = false;
            RestoreTimeScale();
        }
    }
}
