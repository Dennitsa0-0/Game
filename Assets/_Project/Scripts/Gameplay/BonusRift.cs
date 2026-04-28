using ChronoDrop.Core;
using UnityEngine;

namespace ChronoDrop.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class BonusRift : MonoBehaviour
    {
        [SerializeField] private bool disableAfterEnter = true;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            EventBus.Raise(new BonusRiftEnteredEvent(transform.position));

            if (disableAfterEnter)
                gameObject.SetActive(false);
        }
    }
}
