using System;
using System.Collections;
using ChronoDrop.Core;
using ChronoDrop.Haptics;
using UnityEngine;

namespace ChronoDrop.Systems
{
    public interface IComboResetter { void ResetCombo(string reason); }

    public sealed class WorldRotator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform levelContainer;
        [SerializeField] private Collider playerCollider;
        [SerializeField] private MonoBehaviour comboResetterBehaviour;

        [Header("Rotation")]
        [SerializeField] private float rotationStepDegrees = 90f;
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool resetComboOnRotate = true;

        [Header("Safety")]
        [SerializeField] private bool disablePlayerColliderDuringRotation = true;
        [SerializeField] private float colliderRestoreDelay = 0.02f;

        public event Action RotationStarted;
        public event Action RotationCompleted;
        public event Action<int> RotationIndexChanged;
        public event Action<RotationStateSnapshot> RotationStateChanged;

        public bool IsRotating { get; private set; }
        public int RotationIndex => NormalizeRotationIndex(_rotationIndex);
        public float RotationYDegrees => RotationIndex * rotationStepDegrees;

        private Coroutine _rotationCoroutine;
        private IComboResetter _comboResetter;
        private int _rotationIndex;
        private bool _gameActive;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (levelContainer == null)
                levelContainer = transform;

            _comboResetter = comboResetterBehaviour as IComboResetter;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
        }

        private void OnDestroy()
        {
            if (_rotationCoroutine != null)
                StopCoroutine(_rotationCoroutine);
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void RotateClockwise()        => Rotate(1);
        public void RotateCounterClockwise() => Rotate(-1);

        public void Rotate(int direction)
        {
            if (!_gameActive || IsRotating || direction == 0 || levelContainer == null)
                return;

            IsRotating = true;
            RotationStarted?.Invoke();

            AdvancedHapticsService.Instance?.Play(HapticPattern.ParadigmShiftClunk);

            if (resetComboOnRotate)
                _comboResetter?.ResetCombo("ParadigmShift");

            if (disablePlayerColliderDuringRotation && playerCollider != null)
                playerCollider.enabled = false;

            _rotationIndex = NormalizeRotationIndex(_rotationIndex + Math.Sign(direction));
            RotationIndexChanged?.Invoke(RotationIndex);
            RotationStateChanged?.Invoke(new RotationStateSnapshot(RotationIndex, RotationYDegrees, Time.time));

            if (_rotationCoroutine != null)
                StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = StartCoroutine(RotateCoroutine(RotationYDegrees));
        }

        public void ApplyRotationIndexVisualOnly(int rotationIndex)
        {
            _rotationIndex = NormalizeRotationIndex(rotationIndex);
            if (levelContainer == null) return;

            Vector3 euler = levelContainer.eulerAngles;
            euler.y = RotationYDegrees;
            levelContainer.eulerAngles = euler;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private IEnumerator RotateCoroutine(float targetY)
        {
            Quaternion startRot = levelContainer.rotation;
            Vector3 endEuler = levelContainer.eulerAngles;
            endEuler.y = targetY;
            Quaternion endRot = Quaternion.Euler(endEuler);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = easeCurve != null ? easeCurve.Evaluate(t) : t;
                levelContainer.rotation = Quaternion.Slerp(startRot, endRot, easedT);
                yield return null;
            }

            // Snap to exact target to avoid float drift
            levelContainer.eulerAngles = endEuler;
            CompleteRotation();
        }

        private void CompleteRotation()
        {
            if (disablePlayerColliderDuringRotation && playerCollider != null)
                StartCoroutine(RestoreColliderDelayed());

            IsRotating = false;
            RotationCompleted?.Invoke();
        }

        private IEnumerator RestoreColliderDelayed()
        {
            yield return new WaitForSeconds(colliderRestoreDelay);
            if (playerCollider != null)
                playerCollider.enabled = true;
        }

        private static int NormalizeRotationIndex(int value)
        {
            int result = value % 4;
            return result < 0 ? result + 4 : result;
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnGameStarted(GameStartedEvent _)
        {
            _gameActive = true;
            _rotationIndex = 0;
            if (levelContainer != null)
            {
                Vector3 euler = levelContainer.eulerAngles;
                euler.y = 0f;
                levelContainer.eulerAngles = euler;
            }
        }

        private void OnGameOver(GameOverEvent _)      => _gameActive = false;
        private void OnGamePaused(GamePausedEvent e)  => _gameActive = !e.IsPaused;
    }

    [Serializable]
    public readonly struct RotationStateSnapshot
    {
        public readonly int RotationIndex;
        public readonly float RotationYDegrees;
        public readonly float LocalTimestamp;

        public RotationStateSnapshot(int rotationIndex, float rotationYDegrees, float localTimestamp)
        {
            RotationIndex = rotationIndex;
            RotationYDegrees = rotationYDegrees;
            LocalTimestamp = localTimestamp;
        }
    }
}
