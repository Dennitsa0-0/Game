using System;
using System.Collections.Generic;
using UnityEngine;
using ChronoDrop.Core;

namespace ChronoDrop.Progression
{
    public enum ChronoEraId
    {
        Modern,
        Industrial,
        Medieval,
        Antiquity,
        Primitive,
        IceAge,
        Triassic,
        Jurassic,
        Cretaceous,
        Core
    }

    [Serializable]
    public sealed class ChronoEraDefinition
    {
        [Header("Identity")]
        public ChronoEraId eraId;
        public string displayName;
        [TextArea] public string description;

        [Header("Depth Range")]
        public float startDepthMeters;
        public float endDepthMeters;

        [Header("Chronology")]
        [Tooltip("Calendar year at the top of this layer. BC years are negative.")]
        public double startYear;
        [Tooltip("Calendar year at the bottom of this layer. BC years are negative.")]
        public double endYear;
        [Tooltip("Use Mya/BCE labels instead of plain years.")]
        public bool geologicalScale;

        [Header("Difficulty & Presentation")]
        public float difficultyMultiplier = 1f;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.015f;
        public AudioClip transitionStinger;

        public bool ContainsDepth(float depth)
        {
            if (endDepthMeters <= startDepthMeters)
                return depth >= startDepthMeters;
            return depth >= startDepthMeters && depth < endDepthMeters;
        }

        public float NormalizedDepth(float depth)
        {
            if (endDepthMeters <= startDepthMeters)
                return 1f;
            return Mathf.InverseLerp(startDepthMeters, endDepthMeters, depth);
        }

        public double YearAtDepth(float depth)
        {
            float t = NormalizedDepth(depth);
            return LerpDouble(startYear, endYear, t);
        }

        private static double LerpDouble(double a, double b, double t)
        {
            return a + (b - a) * Math.Clamp(t, 0d, 1d);
        }
    }

    public readonly struct ChronoProgressSnapshot
    {
        public readonly ChronoEraDefinition Era;
        public readonly float DepthMeters;
        public readonly double HistoricalYear;
        public readonly float EraProgress01;
        public readonly string YearLabel;

        public ChronoProgressSnapshot(
            ChronoEraDefinition era,
            float depthMeters,
            double historicalYear,
            float eraProgress01,
            string yearLabel)
        {
            Era = era;
            DepthMeters = depthMeters;
            HistoricalYear = historicalYear;
            EraProgress01 = eraProgress01;
            YearLabel = yearLabel;
        }
    }

    public sealed class ChronoNavigator : MonoBehaviour
    {
        [Header("Depth Source")]
        [Tooltip("Enable when using Treadmill pattern — depth is fed via SetExternalDepth().")]
        [SerializeField] private bool useExternalDepth = true;
        [SerializeField] private Transform playerReference;   // fallback if not using external depth
        [SerializeField] private float externalDepthMeters;

        [Header("Era Table")]
        [SerializeField] private List<ChronoEraDefinition> eras = new();

        [Header("Fog")]
        [SerializeField] private bool driveUnityFog = true;
        [SerializeField] private float fogLerpSpeed = 4f;

        // ── C# events (subscribed by HUDController directly) ────────────────
        public event Action<ChronoProgressSnapshot> ProgressChanged;
        public event Action<ChronoEraDefinition, ChronoEraDefinition> EraChanged;

        public ChronoEraDefinition CurrentEra { get; private set; }
        public float CurrentDepthMeters { get; private set; }
        public double CurrentHistoricalYear { get; private set; }
        public string CurrentYearLabel { get; private set; }

        private int _currentEraIndex = -1;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Reset()
        {
            eras = BuildDefaultEraTable();
        }

        private void Awake()
        {
            if (eras == null || eras.Count == 0)
                eras = BuildDefaultEraTable();

            eras.Sort((a, b) => a.startDepthMeters.CompareTo(b.startDepthMeters));
            ForceRefresh();
        }

        private void Update()
        {
            CurrentDepthMeters = useExternalDepth ? externalDepthMeters : ResolveDepthFromPlayer();
            UpdateEra(CurrentDepthMeters);
            UpdateFog();
            PublishSnapshot();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Called by TreadmillController every frame with environmentRoot.position.y.
        /// </summary>
        public void SetExternalDepth(float depthMeters)
        {
            useExternalDepth = true;
            externalDepthMeters = Mathf.Max(0f, depthMeters);
        }

        public ChronoProgressSnapshot GetSnapshot()
        {
            if (CurrentEra == null)
                ForceRefresh();

            return new ChronoProgressSnapshot(
                CurrentEra,
                CurrentDepthMeters,
                CurrentHistoricalYear,
                CurrentEra != null ? CurrentEra.NormalizedDepth(CurrentDepthMeters) : 0f,
                CurrentYearLabel
            );
        }

        public void ForceRefresh()
        {
            CurrentDepthMeters = useExternalDepth ? externalDepthMeters : ResolveDepthFromPlayer();
            _currentEraIndex = -1;
            UpdateEra(CurrentDepthMeters);
            UpdateFog(instant: true);
            PublishSnapshot();
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private float ResolveDepthFromPlayer()
        {
            if (playerReference == null)
                return CurrentDepthMeters;
            return Mathf.Max(0f, -playerReference.position.y);
        }

        private void UpdateEra(float depthMeters)
        {
            int nextIndex = FindEraIndex(depthMeters);
            if (nextIndex < 0)
                return;

            ChronoEraDefinition previous = CurrentEra;

            if (nextIndex != _currentEraIndex)
            {
                _currentEraIndex = nextIndex;
                CurrentEra = eras[nextIndex];

                // Tight-coupled C# event (for HUDController)
                EraChanged?.Invoke(previous, CurrentEra);

                // EventBus broadcast (for music, haptics, obstacle pool, etc.)
                EventBus.Raise(new EraTransitionEvent(CurrentEra.displayName));
            }

            CurrentHistoricalYear = CurrentEra.YearAtDepth(depthMeters);
            CurrentYearLabel = FormatYear(CurrentHistoricalYear, CurrentEra.geologicalScale);
        }

        private int FindEraIndex(float depthMeters)
        {
            for (int i = 0; i < eras.Count; i++)
            {
                if (eras[i].ContainsDepth(depthMeters))
                    return i;
            }
            return eras.Count > 0 ? eras.Count - 1 : -1;
        }

        private void UpdateFog(bool instant = false)
        {
            if (!driveUnityFog || CurrentEra == null)
                return;

            RenderSettings.fog = true;

            if (instant)
            {
                RenderSettings.fogColor = CurrentEra.fogColor;
                RenderSettings.fogDensity = CurrentEra.fogDensity;
                return;
            }

            float k = 1f - Mathf.Exp(-fogLerpSpeed * Time.deltaTime);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, CurrentEra.fogColor, k);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, CurrentEra.fogDensity, k);
        }

        private void PublishSnapshot()
        {
            ProgressChanged?.Invoke(GetSnapshot());
        }

        // ── Year formatting ──────────────────────────────────────────────────

        public static string FormatYear(double year, bool geological)
        {
            if (geological)
            {
                double yearsBeforePresent = Math.Abs(year - 2026d);

                if (yearsBeforePresent >= 1_000_000d)
                    return $"{yearsBeforePresent / 1_000_000d:0.#} млн лет назад";

                if (yearsBeforePresent >= 10_000d)
                    return $"{yearsBeforePresent / 1000d:0} тыс. лет назад";
            }

            if (year < 0)
                return $"{Math.Abs(Math.Round(year)):0} до н.э.";

            return $"{Math.Round(year):0}";
        }

        // ── Default era table ────────────────────────────────────────────────

        public static List<ChronoEraDefinition> BuildDefaultEraTable()
        {
            return new List<ChronoEraDefinition>
            {
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Modern,
                    displayName = "Современность",
                    startDepthMeters = 0f,   endDepthMeters = 500f,
                    startYear = 2026,         endYear = 1900,
                    difficultyMultiplier = 1.0f,
                    fogColor = new Color(0.48f, 0.52f, 0.55f),
                    fogDensity = 0.010f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Industrial,
                    displayName = "Индустриализация",
                    startDepthMeters = 500f,  endDepthMeters = 1500f,
                    startYear = 1900,          endYear = 1800,
                    difficultyMultiplier = 1.18f,
                    fogColor = new Color(0.45f, 0.38f, 0.30f),
                    fogDensity = 0.018f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Medieval,
                    displayName = "Средневековье",
                    startDepthMeters = 1500f, endDepthMeters = 3000f,
                    startYear = 1500,          endYear = 500,
                    difficultyMultiplier = 1.34f,
                    fogColor = new Color(0.30f, 0.34f, 0.28f),
                    fogDensity = 0.020f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Antiquity,
                    displayName = "Античность",
                    startDepthMeters = 3000f, endDepthMeters = 4200f,
                    startYear = -100,          endYear = -1200,
                    difficultyMultiplier = 1.48f,
                    fogColor = new Color(0.52f, 0.47f, 0.38f),
                    fogDensity = 0.018f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Primitive,
                    displayName = "Первобытность",
                    startDepthMeters = 4200f, endDepthMeters = 6000f,
                    startYear = -12000,        endYear = -9000,
                    geologicalScale = true,
                    difficultyMultiplier = 1.62f,
                    fogColor = new Color(0.35f, 0.30f, 0.25f),
                    fogDensity = 0.024f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.IceAge,
                    displayName = "Ледяной плен",
                    startDepthMeters = 6000f, endDepthMeters = 8000f,
                    startYear = -30000,        endYear = -12000,
                    geologicalScale = true,
                    difficultyMultiplier = 1.78f,
                    fogColor = new Color(0.55f, 0.68f, 0.78f),
                    fogDensity = 0.030f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Triassic,
                    displayName = "Триас",
                    startDepthMeters = 8000f,  endDepthMeters = 10000f,
                    startYear = -252_000_000,   endYear = -201_000_000,
                    geologicalScale = true,
                    difficultyMultiplier = 1.92f,
                    fogColor = new Color(0.40f, 0.32f, 0.22f),
                    fogDensity = 0.026f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Jurassic,
                    displayName = "Юра",
                    startDepthMeters = 10000f, endDepthMeters = 12500f,
                    startYear = -201_000_000,   endYear = -145_000_000,
                    geologicalScale = true,
                    difficultyMultiplier = 2.10f,
                    fogColor = new Color(0.24f, 0.38f, 0.22f),
                    fogDensity = 0.030f
                },
                new ChronoEraDefinition
                {
                    eraId = ChronoEraId.Cretaceous,
                    displayName = "Мел",
                    startDepthMeters = 12500f, endDepthMeters = 999999f,
                    startYear = -145_000_000,   endYear = -66_000_000,
                    geologicalScale = true,
                    difficultyMultiplier = 2.35f,
                    fogColor = new Color(0.38f, 0.26f, 0.18f),
                    fogDensity = 0.034f
                }
            };
        }
    }
}
