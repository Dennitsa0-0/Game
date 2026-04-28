using ChronoDrop.Core;
using ChronoDrop.Gameplay;
using UnityEngine;

namespace ChronoDrop.Loadout
{
    public readonly struct BarrierPiercedEvent
    {
        public readonly Collider Obstacle;
        public readonly int RemainingCharges;

        public BarrierPiercedEvent(Collider obstacle, int remainingCharges)
        {
            Obstacle = obstacle;
            RemainingCharges = remainingCharges;
        }
    }

    public sealed class LegionnaireBarrierPierceGate : MonoBehaviour, IPlayerDamageGate
    {
        [SerializeField] private bool disablePiercedObstacle = true;

        private LoadoutSynergyState _synergyState = LoadoutSynergyState.None;
        private int _remainingCharges;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<LoadoutSynergyChangedEvent>(OnSynergyChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<LoadoutSynergyChangedEvent>(OnSynergyChanged);
        }

        public bool TryConsumeProtection(Collider obstacle)
        {
            if (!_synergyState.IsActive || !_synergyState.BarrierPierceEnabled || _remainingCharges <= 0)
                return false;

            _remainingCharges--;

            if (disablePiercedObstacle && obstacle != null)
                obstacle.gameObject.SetActive(false);

            EventBus.Raise(new BarrierPiercedEvent(obstacle, _remainingCharges));
            return true;
        }

        private void OnGameStarted(GameStartedEvent _)
        {
            _remainingCharges = _synergyState.IsActive ? _synergyState.BarrierPierceCharges : 0;
        }

        private void OnSynergyChanged(LoadoutSynergyChangedEvent evt)
        {
            _synergyState = evt.State;
            _remainingCharges = _synergyState.IsActive ? _synergyState.BarrierPierceCharges : 0;
        }
    }
}
