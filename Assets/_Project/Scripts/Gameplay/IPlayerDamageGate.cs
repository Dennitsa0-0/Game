using UnityEngine;

namespace ChronoDrop.Gameplay
{
    public interface IPlayerDamageGate
    {
        bool TryConsumeProtection(Collider obstacle);
    }
}
