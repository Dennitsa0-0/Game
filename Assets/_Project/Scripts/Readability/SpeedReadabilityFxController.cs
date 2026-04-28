using ChronoDrop.Systems;
using UnityEngine;

namespace ChronoDrop.Readability
{
    public sealed class SpeedReadabilityFxController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DynamicSpeedController speedController;
        [SerializeField] private CanvasGroup vignetteOverlay;
        [SerializeField] private Renderer[] tunnelWallRenderers;

        [Header("Speed Window")]
        [SerializeField] private float effectStartSpeed = 18f;
        [SerializeField] private float effectFullSpeed = 34f;

        [Header("Vignette")]
        [SerializeField, Range(0f, 1f)] private float maxVignetteAlpha = 0.48f;

        [Header("Wall Blur Shader Property")]
        [SerializeField] private string blurPropertyName = "_MotionBlurStrength";
        [SerializeField] private float maxWallBlur = 1f;

        private int _blurPropertyId;
        private readonly MaterialPropertyBlock _block = new();

        private void Awake()
        {
            _blurPropertyId = Shader.PropertyToID(blurPropertyName);
        }

        private void Update()
        {
            if (speedController == null)
                return;

            float t = Mathf.InverseLerp(effectStartSpeed, effectFullSpeed, speedController.CurrentSpeed);
            ApplyVignette(t);
            ApplyWallBlur(t);
        }

        private void ApplyVignette(float t)
        {
            if (vignetteOverlay == null)
                return;

            vignetteOverlay.alpha = Mathf.Lerp(0f, maxVignetteAlpha, t);
        }

        private void ApplyWallBlur(float t)
        {
            if (tunnelWallRenderers == null)
                return;

            float blur = Mathf.Lerp(0f, maxWallBlur, t);
            for (int i = 0; i < tunnelWallRenderers.Length; i++)
            {
                Renderer wall = tunnelWallRenderers[i];
                if (wall == null)
                    continue;

                wall.GetPropertyBlock(_block);
                _block.SetFloat(_blurPropertyId, blur);
                wall.SetPropertyBlock(_block);
            }
        }
    }
}
