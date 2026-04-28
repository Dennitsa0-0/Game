using System.Collections.Generic;
using ChronoDrop.Core;
using ChronoDrop.Progression;
using ChronoDrop.Systems;
using UnityEngine;

namespace ChronoDrop.Replay
{
    public enum ReplayTrigger
    {
        Death,
        NearMissStreak
    }

    public readonly struct ReplayFrameSnapshot
    {
        public readonly float TimeSeconds;
        public readonly Vector3 PlayerPosition;
        public readonly float DepthMeters;
        public readonly float Speed;

        public ReplayFrameSnapshot(float timeSeconds, Vector3 playerPosition, float depthMeters, float speed)
        {
            TimeSeconds = timeSeconds;
            PlayerPosition = playerPosition;
            DepthMeters = depthMeters;
            Speed = speed;
        }
    }

    public readonly struct ReplayClipDescriptor
    {
        public readonly ReplayTrigger Trigger;
        public readonly string FilterId;
        public readonly string Watermark;
        public readonly float DurationSeconds;
        public readonly float FinalDepthMeters;
        public readonly int FrameCount;

        public ReplayClipDescriptor(
            ReplayTrigger trigger,
            string filterId,
            string watermark,
            float durationSeconds,
            float finalDepthMeters,
            int frameCount)
        {
            Trigger = trigger;
            FilterId = filterId;
            Watermark = watermark;
            DurationSeconds = durationSeconds;
            FinalDepthMeters = finalDepthMeters;
            FrameCount = frameCount;
        }
    }

    public readonly struct ReplayClipReadyEvent
    {
        public readonly ReplayClipDescriptor Clip;
        public ReplayClipReadyEvent(ReplayClipDescriptor clip) { Clip = clip; }
    }

    public sealed class ReplayMomentRecorder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private DynamicSpeedController speedController;
        [SerializeField] private ChronoNavigator chronoNavigator;

        [Header("Buffer")]
        [SerializeField] private float bufferSeconds = 8f;
        [SerializeField] private int targetFramesPerSecond = 20;

        [Header("Viral Hooks")]
        [SerializeField] private int nearMissesForClip = 3;
        [SerializeField] private float nearMissWindowSeconds = 2f;
        [SerializeField] private string deathFilterId = "glitch";
        [SerializeField] private string nearMissFilterId = "vhs";
        [SerializeField] private string watermark = "Chrono-Drop";

        private readonly Queue<ReplayFrameSnapshot> _frames = new();
        private float _sampleTimer;
        private int _nearMissStreak;
        private float _lastNearMissTime;
        private bool _isRunning;

        public ReplayClipDescriptor LastClip { get; private set; }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<NearMissEvent>(OnNearMiss);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<NearMissEvent>(OnNearMiss);
        }

        private void Update()
        {
            if (!_isRunning || playerTransform == null)
                return;

            float sampleInterval = 1f / Mathf.Max(1, targetFramesPerSecond);
            _sampleTimer += Time.deltaTime;
            if (_sampleTimer < sampleInterval)
                return;

            _sampleTimer = 0f;
            PushFrame();
            TrimBuffer();
        }

        private void PushFrame()
        {
            float depth = chronoNavigator != null ? chronoNavigator.CurrentDepthMeters : Mathf.Max(0f, -playerTransform.position.y);
            float speed = speedController != null ? speedController.CurrentSpeed : 0f;
            _frames.Enqueue(new ReplayFrameSnapshot(Time.time, playerTransform.position, depth, speed));
        }

        private void TrimBuffer()
        {
            float minTime = Time.time - Mathf.Max(1f, bufferSeconds);
            while (_frames.Count > 0 && _frames.Peek().TimeSeconds < minTime)
                _frames.Dequeue();
        }

        private void EmitClip(ReplayTrigger trigger, string filterId, float finalDepth)
        {
            LastClip = new ReplayClipDescriptor(
                trigger,
                filterId,
                watermark,
                Mathf.Min(bufferSeconds, _frames.Count / (float)Mathf.Max(1, targetFramesPerSecond)),
                finalDepth,
                _frames.Count);

            EventBus.Raise(new ReplayClipReadyEvent(LastClip));
        }

        private void OnGameStarted(GameStartedEvent _)
        {
            _frames.Clear();
            _nearMissStreak = 0;
            _sampleTimer = 0f;
            _isRunning = true;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            _isRunning = false;
            EmitClip(ReplayTrigger.Death, deathFilterId, evt.DepthMeters);
        }

        private void OnNearMiss(NearMissEvent _)
        {
            float now = Time.time;
            _nearMissStreak = now - _lastNearMissTime <= nearMissWindowSeconds ? _nearMissStreak + 1 : 1;
            _lastNearMissTime = now;

            if (_nearMissStreak >= nearMissesForClip)
            {
                float depth = chronoNavigator != null ? chronoNavigator.CurrentDepthMeters : 0f;
                EmitClip(ReplayTrigger.NearMissStreak, nearMissFilterId, depth);
                _nearMissStreak = 0;
            }
        }
    }
}
