using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChronoDrop.Systems
{
    public sealed class DynamicSpeedController : MonoBehaviour
    {
        [Header("Base Formula")]
        [SerializeField] private float alpha = 0.11f;
        [SerializeField] private float baseWorldSpeed = 6f;
        [SerializeField] private float maxSpeed = 42f;

        [Header("Sawtooth Relief")]
        [SerializeField] private bool enableSawtoothRelief = true;
        [SerializeField] private float reliefPeriodSeconds = 34f;
        [SerializeField] [Range(0f, 0.7f)] private float reliefStrength = 0.18f;

        [Header("Boosts")]
        [SerializeField] [Range(0.05f, 1f)] private float parachuteSpeedMultiplier = 0.45f;

        public event Action<float> SpeedChanged;
        public event Action<string, float, float> TemporaryPressureApplied;
        public event Action<string, float, float> TemporarySpeedBoostApplied;

        public float RunTimeSeconds { get; private set; }
        public float CurrentSpeed { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsParachuteActive => _parachuteTimer > 0f;
        public float ExternalMultiplier => _externalMultiplier;

        private float _parachuteTimer;
        private float _externalMultiplier = 1f;
        private readonly List<TimedSpeedPressure> _temporaryPressures = new();

        private void Update()
        {
            if (IsPaused) return;
            RunTimeSeconds += Time.deltaTime;
            if (_parachuteTimer > 0f) _parachuteTimer -= Time.deltaTime;
            TickTemporaryPressures(Time.deltaTime);
            CurrentSpeed = CalculateSpeed(RunTimeSeconds);
            SpeedChanged?.Invoke(CurrentSpeed);
        }

        public void ResetRun()
        {
            RunTimeSeconds = 0f;
            CurrentSpeed = CalculateSpeed(0f);
            _parachuteTimer = 0f;
            _externalMultiplier = 1f;
            _temporaryPressures.Clear();
            SpeedChanged?.Invoke(CurrentSpeed);
        }

        public void SetPaused(bool paused) => IsPaused = paused;
        public void SetExternalMultiplier(float multiplier) => _externalMultiplier = Mathf.Max(0.05f, multiplier);
        public void ActivateParachute(float durationSeconds) => _parachuteTimer = Mathf.Max(_parachuteTimer, durationSeconds);

        public void ApplyTemporaryPressure(string source, float multiplier, float durationSeconds)
        {
            multiplier = Mathf.Clamp(multiplier, 1f, 1.35f);
            durationSeconds = Mathf.Clamp(durationSeconds, 0f, 5f);
            if (durationSeconds <= 0f || multiplier <= 1f) return;
            _temporaryPressures.Add(new TimedSpeedPressure(source, multiplier, durationSeconds));
            TemporaryPressureApplied?.Invoke(source, multiplier, durationSeconds);
        }

        public void ApplyTemporarySpeedBoost(string source, float multiplier, float durationSeconds)
        {
            multiplier = Mathf.Clamp(multiplier, 1f, 4f);
            durationSeconds = Mathf.Clamp(durationSeconds, 0f, 30f);
            if (durationSeconds <= 0f || multiplier <= 1f) return;
            _temporaryPressures.Add(new TimedSpeedPressure(source, multiplier, durationSeconds));
            TemporarySpeedBoostApplied?.Invoke(source, multiplier, durationSeconds);
        }

        public float CalculateSpeed(float timeSeconds)
        {
            float difficulty = Mathf.Sqrt(Mathf.Max(0f, timeSeconds) * alpha) + 1f;
            float speed = baseWorldSpeed * difficulty;
            if (enableSawtoothRelief && reliefPeriodSeconds > 0.01f) speed *= CalculateReliefMultiplier(timeSeconds);
            if (IsParachuteActive) speed *= parachuteSpeedMultiplier;
            speed *= _externalMultiplier * CalculateTemporaryPressureMultiplier();
            return Mathf.Clamp(speed, 0f, maxSpeed);
        }

        private float CalculateReliefMultiplier(float timeSeconds)
        {
            float phase = Mathf.Repeat(timeSeconds, reliefPeriodSeconds) / reliefPeriodSeconds;
            float reliefWindow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, phase));
            return 1f - reliefWindow * reliefStrength;
        }

        private void TickTemporaryPressures(float dt)
        {
            for (int i = _temporaryPressures.Count - 1; i >= 0; i--)
            {
                var p = _temporaryPressures[i];
                p.RemainingSeconds -= dt;
                if (p.RemainingSeconds <= 0f) _temporaryPressures.RemoveAt(i);
                else _temporaryPressures[i] = p;
            }
        }

        private float CalculateTemporaryPressureMultiplier()
        {
            float result = 1f;
            for (int i = 0; i < _temporaryPressures.Count; i++) result *= _temporaryPressures[i].Multiplier;
            return Mathf.Clamp(result, 1f, 4f);
        }

        private struct TimedSpeedPressure
        {
            public readonly string Source;
            public readonly float Multiplier;
            public float RemainingSeconds;
            public TimedSpeedPressure(string source, float multiplier, float remainingSeconds)
            { Source = source; Multiplier = multiplier; RemainingSeconds = remainingSeconds; }
        }
    }
}
