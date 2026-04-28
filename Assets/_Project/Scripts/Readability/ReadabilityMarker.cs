using UnityEngine;

namespace ChronoDrop.Readability
{
    public enum ReadabilityRole
    {
        Hazard,
        Loot,
        Boost,
        Portal
    }

    [DisallowMultipleComponent]
    public sealed class ReadabilityMarker : MonoBehaviour
    {
        [SerializeField] private ReadabilityRole role = ReadabilityRole.Hazard;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private bool applyOnEnable = true;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private readonly MaterialPropertyBlock _block = new();

        private void Reset()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void OnEnable()
        {
            if (applyOnEnable)
                Apply();
        }

        public void Apply()
        {
            Color color = ResolveColor(role);
            Color emission = color * ResolveEmission(role);

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null)
                    continue;

                target.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(ColorId, color);
                _block.SetColor(EmissionColorId, emission);
                target.SetPropertyBlock(_block);
            }
        }

        private static Color ResolveColor(ReadabilityRole role)
        {
            switch (role)
            {
                case ReadabilityRole.Loot:
                    return new Color(0.10f, 0.95f, 1f, 1f);
                case ReadabilityRole.Boost:
                    return new Color(0.18f, 1f, 0.38f, 1f);
                case ReadabilityRole.Portal:
                    return new Color(1f, 0.84f, 0.16f, 1f);
                default:
                    return new Color(1f, 0.24f, 0.08f, 1f);
            }
        }

        private static float ResolveEmission(ReadabilityRole role)
        {
            return role == ReadabilityRole.Hazard ? 1.25f : 1.8f;
        }
    }
}
