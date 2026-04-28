using System;
using System.Collections.Generic;
using ChronoDrop.Core;
using ChronoDrop.Systems;
using UnityEngine;

namespace ChronoDrop.Loadout
{
    public enum EquipmentSlot
    {
        Head,
        Body,
        Hands,
        Legs
    }

    public enum EquipmentSetId
    {
        None,
        Legionnaire,
        Steampunk,
        CyberHacker
    }

    public enum LoadoutSynergyId
    {
        None,
        LegionnaireBulwark,
        SteampunkOverdrive,
        CyberHackerPhase
    }

    [Serializable]
    public sealed class EquippedItemDefinition
    {
        public string itemId;
        public EquipmentSlot slot;
        public EquipmentSetId setId;
    }

    [Serializable]
    public sealed class SetBonusDefinition
    {
        public EquipmentSetId setId;
        public LoadoutSynergyId synergyId;
        [Range(1, 4)] public int requiredPieces = 3;
        public string displayName;
        [TextArea] public string rulesText;
        public float speedMultiplier = 1f;
        public bool enablesCrystalMagnet;
        public bool grantsBarrierPierce;
        public int barrierPierceCharges = 1;
    }

    public readonly struct LoadoutSynergyState
    {
        public readonly EquipmentSetId SetId;
        public readonly LoadoutSynergyId SynergyId;
        public readonly int EquippedPieces;
        public readonly int RequiredPieces;
        public readonly float SpeedMultiplier;
        public readonly bool CrystalMagnetEnabled;
        public readonly bool BarrierPierceEnabled;
        public readonly int BarrierPierceCharges;

        public bool IsActive => SynergyId != LoadoutSynergyId.None && EquippedPieces >= RequiredPieces;

        public LoadoutSynergyState(
            EquipmentSetId setId,
            LoadoutSynergyId synergyId,
            int equippedPieces,
            int requiredPieces,
            float speedMultiplier,
            bool crystalMagnetEnabled,
            bool barrierPierceEnabled,
            int barrierPierceCharges)
        {
            SetId = setId;
            SynergyId = synergyId;
            EquippedPieces = equippedPieces;
            RequiredPieces = requiredPieces;
            SpeedMultiplier = speedMultiplier;
            CrystalMagnetEnabled = crystalMagnetEnabled;
            BarrierPierceEnabled = barrierPierceEnabled;
            BarrierPierceCharges = barrierPierceCharges;
        }

        public static LoadoutSynergyState None => new(
            EquipmentSetId.None,
            LoadoutSynergyId.None,
            0,
            3,
            1f,
            false,
            false,
            0);
    }

    public readonly struct LoadoutSynergyChangedEvent
    {
        public readonly LoadoutSynergyState State;
        public LoadoutSynergyChangedEvent(LoadoutSynergyState state) { State = state; }
    }

    public sealed class LoadoutSynergyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DynamicSpeedController speedController;

        [Header("Equipped Items")]
        [SerializeField] private List<EquippedItemDefinition> equippedItems = new();

        [Header("Set Bonuses")]
        [SerializeField] private List<SetBonusDefinition> setBonuses = new()
        {
            new SetBonusDefinition
            {
                setId = EquipmentSetId.Legionnaire,
                synergyId = LoadoutSynergyId.LegionnaireBulwark,
                displayName = "Legionnaire Bulwark",
                rulesText = "3/4 pieces: once per run, pierce one lethal barrier.",
                grantsBarrierPierce = true,
                barrierPierceCharges = 1
            },
            new SetBonusDefinition
            {
                setId = EquipmentSetId.Steampunk,
                synergyId = LoadoutSynergyId.SteampunkOverdrive,
                displayName = "Steampunk Overdrive",
                rulesText = "3/4 pieces: attract chrono crystals, but fall faster.",
                speedMultiplier = 1.12f,
                enablesCrystalMagnet = true
            },
            new SetBonusDefinition
            {
                setId = EquipmentSetId.CyberHacker,
                synergyId = LoadoutSynergyId.CyberHackerPhase,
                displayName = "Cyber-Hacker Phase",
                rulesText = "3/4 pieces: reserves a future phase-dodge hook.",
            }
        };

        public LoadoutSynergyState CurrentState { get; private set; } = LoadoutSynergyState.None;

        private void Start()
        {
            Recalculate();
        }

        public void SetEquippedItems(IEnumerable<EquippedItemDefinition> items)
        {
            equippedItems.Clear();
            if (items != null)
                equippedItems.AddRange(items);

            Recalculate();
        }

        public void Recalculate()
        {
            CurrentState = ResolveBestActiveSynergy();
            speedController?.SetExternalMultiplier(CurrentState.IsActive ? CurrentState.SpeedMultiplier : 1f);
            EventBus.Raise(new LoadoutSynergyChangedEvent(CurrentState));
        }

        private LoadoutSynergyState ResolveBestActiveSynergy()
        {
            SetBonusDefinition bestBonus = null;
            int bestPieces = 0;

            for (int i = 0; i < setBonuses.Count; i++)
            {
                SetBonusDefinition bonus = setBonuses[i];
                if (bonus == null || bonus.setId == EquipmentSetId.None)
                    continue;

                int pieces = CountUniquePieces(bonus.setId);
                if (pieces < bonus.requiredPieces || pieces <= bestPieces)
                    continue;

                bestBonus = bonus;
                bestPieces = pieces;
            }

            if (bestBonus == null)
                return LoadoutSynergyState.None;

            return new LoadoutSynergyState(
                bestBonus.setId,
                bestBonus.synergyId,
                bestPieces,
                bestBonus.requiredPieces,
                Mathf.Max(0.05f, bestBonus.speedMultiplier),
                bestBonus.enablesCrystalMagnet,
                bestBonus.grantsBarrierPierce,
                Mathf.Max(0, bestBonus.barrierPierceCharges));
        }

        private int CountUniquePieces(EquipmentSetId setId)
        {
            bool head = false;
            bool body = false;
            bool hands = false;
            bool legs = false;

            for (int i = 0; i < equippedItems.Count; i++)
            {
                EquippedItemDefinition item = equippedItems[i];
                if (item == null || item.setId != setId)
                    continue;

                switch (item.slot)
                {
                    case EquipmentSlot.Head: head = true; break;
                    case EquipmentSlot.Body: body = true; break;
                    case EquipmentSlot.Hands: hands = true; break;
                    case EquipmentSlot.Legs: legs = true; break;
                }
            }

            int count = 0;
            if (head) count++;
            if (body) count++;
            if (hands) count++;
            if (legs) count++;
            return count;
        }
    }
}
