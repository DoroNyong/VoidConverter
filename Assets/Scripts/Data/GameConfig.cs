using UnityEngine;
using VoidConverter.Core;

namespace VoidConverter.Data
{
    /// <summary>Tunable numbers live here so they can be changed without touching code.</summary>
    [CreateAssetMenu(menuName = "VoidConverter/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        public int gridWidth = 5;
        public int gridHeight = 5;
        public float cellSize = 1f;
        public float cellGap = 0.06f;
        public float monsterWeight = 5f;
        public float goldWeight = 1f;
        public float healWeight = 1f;
        public float voidWeight = 2f;

        [Header("Monsters")]
        public int monsterPowerMin = 3;
        public int monsterPowerMax = 9;

        [Header("Player")]
        public int playerMaxHp = 30;
        public int playerAttackMin = 4;
        public int playerAttackMax = 8;

        [Header("Void")]
        [Tooltip("Corruption cap. Monster chance of a void cell = corruption / this.")]
        public int maxCorruption = 10;
        [Tooltip("Base range of the monster power a void cell would spawn.")]
        public int voidPowerMin = 3;
        public int voidPowerMax = 9;
        [Tooltip("The void power range shifts up by floor(corruption * this).")]
        public float voidPowerPerCorruption = 0.5f;

        [Header("Rewards")]
        [Tooltip("Gold gained for beating monsters = their summed power * this (only when the block's contest is won).")]
        public float goldPerMonsterPower = 1f;
        [Tooltip("Gold from a gold cell, and from a void cell that turned out to be a reward.")]
        public int goldCellReward = 2;
        [Tooltip("HP restored by a heal cell.")]
        public int healCellAmount = 2;

        [Header("Round")]
        [Tooltip("How many blocks the player may place in one round.")]
        public int maxPlacements = 6;
        [Tooltip("The run is cleared after this many rounds.")]
        public int maxRounds = 5;
        [Tooltip("The round can be ended by hand once this many blocks were placed.")]
        public int minPlacementsToEnd = 3;
        [Tooltip("Corruption lost when a round ends.")]
        public int corruptionDecayPerRound = 2;

        [Header("Blocks")]
        public int handSize = 3;
        [Tooltip("Probability weight of 2-cell blocks (split evenly among that size's shapes).")]
        public float blockWeight2 = 4f;
        [Tooltip("Probability weight of 3-cell blocks.")]
        public float blockWeight3 = 4f;
        [Tooltip("Probability weight of 4-cell blocks.")]
        public float blockWeight4 = 2f;

        [Header("Hand Layout")]
        public float handSlotSize = 2.4f;
        public float handSlotGap = 0.2f;
        public float handMiniCellSize = 0.55f;

        public GridSettings ToGridSettings() => new GridSettings
        {
            MonsterWeight = monsterWeight,
            GoldWeight = goldWeight,
            HealWeight = healWeight,
            VoidWeight = voidWeight,
            MonsterPowerMin = monsterPowerMin,
            MonsterPowerMax = monsterPowerMax,
            VoidPowerMin = voidPowerMin,
            VoidPowerMax = voidPowerMax,
        };

        public VoidSettings ToVoidSettings() => new VoidSettings
        {
            PowerMin = voidPowerMin,
            PowerMax = voidPowerMax,
            PowerPerCorruption = voidPowerPerCorruption,
        };

        public PlayerStats CreatePlayer() => new PlayerStats(playerMaxHp, playerAttackMin, playerAttackMax, maxCorruption);

        public BlockSettings ToBlockSettings() => new BlockSettings
        {
            Size2Weight = blockWeight2,
            Size3Weight = blockWeight3,
            Size4Weight = blockWeight4,
        };
    }
}
