using UnityEngine;
using ChronoDrop.Core;

namespace ChronoDrop.Gameplay
{
    /// <summary>
    /// Handles player horizontal movement and death/near-miss detection.
    ///
    /// Movement model: finger (or mouse) X position on screen maps linearly to
    /// the player's world X in [-cylinderRadius, +cylinderRadius].
    /// A Mathf.MoveTowards smooths the transition so it feels physical.
    ///
    /// Death detection: Physics.OverlapCapsuleNonAlloc each frame — no Rigidbody required.
    /// Obstacles must be on the layer specified by <see cref="obstacleLayer"/>.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float cylinderRadius = 2.5f;
        [SerializeField] private float slideSpeed = 14f;

        [Header("Collision")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float colliderRadius = 0.35f;
        [SerializeField] private float colliderHalfHeight = 0.55f;
        [SerializeField] private MonoBehaviour damageGateBehaviour;

        [Header("Near Miss")]
        [SerializeField] private float nearMissRadius = 0.7f;

        private bool _isActive;
        private float _targetX;
        private bool _isTouching;
        private IPlayerDamageGate _damageGate;

        // Pre-allocated collision buffers — zero GC per frame
        private readonly Collider[] _hitBuffer = new Collider[4];
        private readonly Collider[] _nearBuffer = new Collider[4];

        private void Awake()
        {
            _damageGate = damageGateBehaviour as IPlayerDamageGate;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameStopped);
            EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameStopped);
            EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
        }

        private void Update()
        {
            if (!_isActive)
                return;

            ReadInput();
            MoveTowardsTarget();
            CheckDeath();
            CheckNearMiss();
        }

        // ── Input ───────────────────────────────────────────────────────────

        private void ReadInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            // Editor / PC: arrow keys or A/D
            float axis = Input.GetAxis("Horizontal");
            if (Mathf.Abs(axis) > 0.01f)
                _targetX = Mathf.Clamp(transform.position.x + axis * cylinderRadius * Time.deltaTime * 3f,
                                       -cylinderRadius, cylinderRadius);
#else
            // Mobile: map finger X position to cylinder X range
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                float t = touch.position.x / Screen.width;               // 0..1
                _targetX = Mathf.Lerp(-cylinderRadius, cylinderRadius, t);
                _isTouching = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }
            else
            {
                _isTouching = false;
            }
#endif
        }

        private void MoveTowardsTarget()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, _targetX, slideSpeed * Time.deltaTime);
            pos.x = Mathf.Clamp(pos.x, -cylinderRadius, cylinderRadius);
            transform.position = pos;
        }

        // ── Physics detection ───────────────────────────────────────────────

        private void CheckDeath()
        {
            if (obstacleLayer == 0)
                return;

            Vector3 top    = transform.position + Vector3.up * colliderHalfHeight;
            Vector3 bottom = transform.position - Vector3.up * colliderHalfHeight;
            int hitCount = Physics.OverlapCapsuleNonAlloc(top, bottom, colliderRadius, _hitBuffer, obstacleLayer);

            if (hitCount > 0 && !TryBlockDeath(_hitBuffer[0]))
                Die();
        }

        private bool TryBlockDeath(Collider obstacle)
        {
            return _damageGate != null && _damageGate.TryConsumeProtection(obstacle);
        }

        private void CheckNearMiss()
        {
            if (obstacleLayer == 0)
                return;

            Vector3 top    = transform.position + Vector3.up * colliderHalfHeight;
            Vector3 bottom = transform.position - Vector3.up * colliderHalfHeight;
            int nearCount = Physics.OverlapCapsuleNonAlloc(top, bottom, nearMissRadius, _nearBuffer, obstacleLayer);

            // A near-miss = something is close but didn't trigger death yet
            if (nearCount > 0)
            {
                float closest = float.MaxValue;
                for (int i = 0; i < nearCount; i++)
                {
                    float dist = Vector3.Distance(transform.position, _nearBuffer[i].ClosestPoint(transform.position));
                    if (dist < closest)
                        closest = dist;
                }
                EventBus.Raise(new NearMissEvent(closest));
            }
        }

        private void Die()
        {
            if (!_isActive)
                return;

            _isActive = false;
            float depth = Mathf.Max(0f, -transform.position.y);
            EventBus.Raise(new PlayerDiedEvent(depth));
        }

        // ── Event handlers ──────────────────────────────────────────────────

        private void OnGameStarted(GameStartedEvent _)
        {
            transform.position = Vector3.zero;
            _targetX = 0f;
            _isActive = true;
        }

        private void OnGameStopped(GameOverEvent _)
        {
            _isActive = false;
        }

        private void OnGamePaused(GamePausedEvent evt)
        {
            _isActive = !evt.IsPaused;
        }

        // ── Gizmos ──────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, colliderRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, nearMissRadius);

            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireCube(transform.position, new Vector3(cylinderRadius * 2f, 0.05f, 0.1f));
        }
    }
}
