using System.Collections;
using UnityEngine;

namespace ChronoDrop.Haptics
{
    public enum HapticPattern
    {
        NearMissPeek,
        EraTransitionRise,
        GravityDrillRumble,
        ParadigmShiftClunk,
        LootEpicPulse
    }

    public sealed class AdvancedHapticsService : MonoBehaviour
    {
        [Header("Global")]
        [SerializeField] private bool hapticsEnabled = true;
        [SerializeField, Range(0f, 1f)] private float intensityScale = 1f;

        [Header("Rumble")]
        [SerializeField] private float drillPulseInterval = 0.075f;

        private Coroutine _rumbleRoutine;

        public static AdvancedHapticsService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetEnabled(bool enabled)
        {
            hapticsEnabled = enabled;
            if (!enabled)
                StopContinuous(HapticPattern.GravityDrillRumble);
        }

        public void Play(HapticPattern pattern)
        {
            if (!hapticsEnabled || intensityScale <= 0f)
                return;

#if UNITY_IOS || UNITY_ANDROID
            switch (pattern)
            {
                case HapticPattern.NearMissPeek:
                    Handheld.Vibrate();
                    break;
                case HapticPattern.EraTransitionRise:
                    StartCoroutine(RisingPulse(0.45f, 4));
                    break;
                case HapticPattern.ParadigmShiftClunk:
                    StartCoroutine(BurstPulse(2, 0.045f));
                    break;
                case HapticPattern.LootEpicPulse:
                    StartCoroutine(BurstPulse(3, 0.06f));
                    break;
                case HapticPattern.GravityDrillRumble:
                    StartContinuous(pattern);
                    break;
            }
#endif
        }

        public void StartContinuous(HapticPattern pattern)
        {
            if (!hapticsEnabled || pattern != HapticPattern.GravityDrillRumble || _rumbleRoutine != null)
                return;

            _rumbleRoutine = StartCoroutine(GravityDrillRumbleLoop());
        }

        public void StopContinuous(HapticPattern pattern)
        {
            if (pattern != HapticPattern.GravityDrillRumble || _rumbleRoutine == null)
                return;

            StopCoroutine(_rumbleRoutine);
            _rumbleRoutine = null;
        }

        private IEnumerator GravityDrillRumbleLoop()
        {
            while (true)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
                yield return new WaitForSecondsRealtime(drillPulseInterval);
            }
        }

        private IEnumerator BurstPulse(int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private IEnumerator RisingPulse(float duration, int pulses)
        {
            float elapsed = 0f;
            for (int i = 0; i < pulses; i++)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
                float t = (float)(i + 1) / pulses;
                float wait = Mathf.Lerp(0.16f, 0.055f, t) * Mathf.Max(0.1f, intensityScale);
                elapsed += wait;
                yield return new WaitForSecondsRealtime(wait);
                if (elapsed >= duration)
                    break;
            }
        }
    }
}
