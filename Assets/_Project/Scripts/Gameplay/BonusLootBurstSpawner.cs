using System.Collections.Generic;
using ChronoDrop.Core;
using UnityEngine;

namespace ChronoDrop.Gameplay
{
    public sealed class BonusLootBurstSpawner : MonoBehaviour
    {
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private GameObject crystalPrefab;
        [SerializeField] private int poolSize = 160;
        [SerializeField] private int rowsPerBurst = 12;
        [SerializeField] private int crystalsPerRow = 8;
        [SerializeField] private float rowSpacing = 1.15f;
        [SerializeField] private float horizontalRange = 2.1f;

        private readonly Queue<GameObject> _pool = new();
        private readonly List<GameObject> _active = new();

        private void Awake()
        {
            BuildPool();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BonusStageStartedEvent>(OnBonusStageStarted);
            EventBus.Subscribe<BonusStageEndedEvent>(OnBonusStageEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BonusStageStartedEvent>(OnBonusStageStarted);
            EventBus.Unsubscribe<BonusStageEndedEvent>(OnBonusStageEnded);
        }

        private void BuildPool()
        {
            GameObject template = crystalPrefab != null ? crystalPrefab : CreateFallbackCrystal();
            Transform parent = environmentRoot != null ? environmentRoot : transform;

            for (int i = 0; i < poolSize; i++)
            {
                GameObject crystal = Instantiate(template, parent);
                crystal.SetActive(false);
                _pool.Enqueue(crystal);
            }

            if (crystalPrefab == null)
                Destroy(template);
        }

        private void OnBonusStageStarted(BonusStageStartedEvent _)
        {
            SpawnBurst();
        }

        private void SpawnBurst()
        {
            if (environmentRoot == null)
                return;

            for (int row = 0; row < rowsPerBurst && _pool.Count > 0; row++)
            {
                float y = -(row + 2) * rowSpacing;
                for (int col = 0; col < crystalsPerRow && _pool.Count > 0; col++)
                {
                    float t = crystalsPerRow <= 1 ? 0.5f : (float)col / (crystalsPerRow - 1);
                    GameObject crystal = _pool.Dequeue();
                    crystal.transform.localPosition = new Vector3(Mathf.Lerp(-horizontalRange, horizontalRange, t), y, 0f);
                    crystal.SetActive(true);
                    _active.Add(crystal);
                }
            }
        }

        private void OnBonusStageEnded(BonusStageEndedEvent _)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                GameObject crystal = _active[i];
                if (crystal != null)
                {
                    crystal.SetActive(false);
                    _pool.Enqueue(crystal);
                }
            }

            _active.Clear();
        }

        private static GameObject CreateFallbackCrystal()
        {
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.name = "ChronoCrystal_Fallback";
            crystal.transform.localScale = Vector3.one * 0.22f;
            return crystal;
        }
    }
}
