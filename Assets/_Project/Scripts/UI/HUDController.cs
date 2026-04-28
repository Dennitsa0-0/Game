using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChronoDrop.Progression;
using ChronoDrop.Systems;

namespace ChronoDrop.UI
{
    /// <summary>
    /// Drives all in-game HUD elements:
    ///   • Era name + year label (with Speed Blur alpha falloff)
    ///   • Depth in metres (small, technical readout)
    ///   • Era transition banner (fade in/hold/fade out)
    ///   • Combo multiplier pulse
    ///
    /// Wire ChronoNavigator and DynamicSpeedController in the Inspector.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Chrono")]
        [SerializeField] private ChronoNavigator chronoNavigator;
        [SerializeField] private TextMeshProUGUI eraLabel;
        [SerializeField] private TextMeshProUGUI yearLabel;
        [SerializeField] private Slider yearVerticalSlider;

        [Header("Depth")]
        [SerializeField] private TextMeshProUGUI depthLabel;
        [SerializeField] private TextMeshProUGUI recordDepthLabel;

        [Header("Speed Blur")]
        [SerializeField] private DynamicSpeedController speedController;
        [SerializeField] private CanvasGroup yearCanvasGroup;
        [SerializeField] private float blurStartSpeed = 18f;
        [SerializeField] private float blurFullSpeed = 34f;
        [SerializeField] private float minYearAlphaAtHighSpeed = 0.45f;

        [Header("Era Transition Banner")]
        [SerializeField] private CanvasGroup eraBannerGroup;
        [SerializeField] private TextMeshProUGUI eraBannerLabel;
        [SerializeField] private float bannerVisibleDuration = 1.25f;
        [SerializeField] private float bannerFadeDuration = 0.2f;

        [Header("Combo")]
        [SerializeField] private TextMeshProUGUI comboLabel;
        [SerializeField] private RectTransform comboPulseRoot;

        private float _recordDepth;
        private Coroutine _bannerRoutine;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void OnEnable()
        {
            if (chronoNavigator != null)
            {
                chronoNavigator.ProgressChanged += OnProgressChanged;
                chronoNavigator.EraChanged += OnEraChanged;
            }
        }

        private void OnDisable()
        {
            if (chronoNavigator != null)
            {
                chronoNavigator.ProgressChanged -= OnProgressChanged;
                chronoNavigator.EraChanged -= OnEraChanged;
            }
        }

        private void Update()
        {
            UpdateYearReadability();
        }

        // ── Public API (called by GameStateMachine / death screen) ───────────

        public void SetRecordDepth(float recordDepth)
        {
            _recordDepth = Mathf.Max(0f, recordDepth);
            if (recordDepthLabel != null)
                recordDepthLabel.text = $"Рекорд: {_recordDepth:0}м";
        }

        public void SetCombo(float multiplier)
        {
            if (comboLabel == null)
                return;

            comboLabel.text = $"x{multiplier:0.0}";

            if (comboPulseRoot != null)
                comboPulseRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.25f, Mathf.InverseLerp(1f, 5f, multiplier));
        }

        // ── ChronoNavigator callbacks ────────────────────────────────────────

        private void OnProgressChanged(ChronoProgressSnapshot snapshot)
        {
            if (eraLabel != null)
                eraLabel.text = snapshot.Era != null ? snapshot.Era.displayName : string.Empty;

            if (yearLabel != null)
                yearLabel.text = snapshot.YearLabel;

            if (depthLabel != null)
                depthLabel.text = $"{snapshot.DepthMeters:0}м";

            if (yearVerticalSlider != null)
                yearVerticalSlider.value = snapshot.EraProgress01;
        }

        private void OnEraChanged(ChronoEraDefinition previous, ChronoEraDefinition current)
        {
            if (current == null || eraBannerGroup == null || eraBannerLabel == null)
                return;

            eraBannerLabel.text = current.displayName;

            if (_bannerRoutine != null)
                StopCoroutine(_bannerRoutine);

            _bannerRoutine = StartCoroutine(ShowEraBanner());
        }

        // ── Speed blur ───────────────────────────────────────────────────────

        private void UpdateYearReadability()
        {
            if (speedController == null || yearCanvasGroup == null)
                return;

            float t = Mathf.InverseLerp(blurStartSpeed, blurFullSpeed, speedController.CurrentSpeed);
            yearCanvasGroup.alpha = Mathf.Lerp(1f, minYearAlphaAtHighSpeed, t);
        }

        // ── Banner coroutine ─────────────────────────────────────────────────

        private IEnumerator ShowEraBanner()
        {
            yield return FadeCanvas(eraBannerGroup, 1f, bannerFadeDuration);
            yield return new WaitForSeconds(bannerVisibleDuration);
            yield return FadeCanvas(eraBannerGroup, 0f, bannerFadeDuration);
        }

        private static IEnumerator FadeCanvas(CanvasGroup group, float target, float duration)
        {
            float start   = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            group.alpha = target;
        }
    }
}
