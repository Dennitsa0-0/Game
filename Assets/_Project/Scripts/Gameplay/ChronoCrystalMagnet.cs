using ChronoDrop.Core;
using ChronoDrop.Loadout;
using UnityEngine;

namespace ChronoDrop.Gameplay
{
    public sealed class ChronoCrystalMagnet : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private LayerMask crystalLayer;
        [SerializeField] private float radius = 5.5f;
        [SerializeField] private float pullSpeed = 18f;
        [SerializeField] private float collectDistance = 0.35f;
        [SerializeField] private int crystalValue = 1;

        private readonly Collider[] _hits = new Collider[32];
        private bool _isEnabled;

        private void OnEnable()
        {
            EventBus.Subscribe<LoadoutSynergyChangedEvent>(OnSynergyChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LoadoutSynergyChangedEvent>(OnSynergyChanged);
        }

        private void Update()
        {
            if (!_isEnabled || playerTransform == null || crystalLayer == 0)
                return;

            int count = Physics.OverlapSphereNonAlloc(playerTransform.position, radius, _hits, crystalLayer);
            for (int i = 0; i < count; i++)
                PullCrystal(_hits[i]);
        }

        private void PullCrystal(Collider crystal)
        {
            if (crystal == null)
                return;

            Transform crystalTransform = crystal.transform;
            crystalTransform.position = Vector3.MoveTowards(
                crystalTransform.position,
                playerTransform.position,
                pullSpeed * Time.deltaTime);

            if (Vector3.Distance(crystalTransform.position, playerTransform.position) <= collectDistance)
            {
                crystal.gameObject.SetActive(false);
                EventBus.Raise(new CrystalCollectedEvent(crystalValue));
            }
        }

        private void OnSynergyChanged(LoadoutSynergyChangedEvent evt)
        {
            _isEnabled = evt.State.IsActive && evt.State.CrystalMagnetEnabled;
        }
    }
}
