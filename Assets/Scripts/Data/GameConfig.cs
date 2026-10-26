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
        public float rewardWeight = 2f;
        public float voidWeight = 2f;

        [Header("Monsters")]
        public int monsterPowerMin = 3;
        public int monsterPowerMax = 9;

        [Header("Player")]
        public int playerMaxHp = 30;
        public int playerAttackMin = 4;
        public int playerAttackMax = 8;

        [Header("Round")]
        [Tooltip("How many blocks the player may place in one round.")]
        public int maxPlacements = 6;

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
            RewardWeight = rewardWeight,
            VoidWeight = voidWeight,
            MonsterPowerMin = monsterPowerMin,
            MonsterPowerMax = monsterPowerMax,
        };

        public PlayerStats CreatePlayer() => new PlayerStats(playerMaxHp, playerAttackMin, playerAttackMax);

        public BlockSettings ToBlockSettings() => new BlockSettings
        {
            Size2Weight = blockWeight2,
            Size3Weight = blockWeight3,
            Size4Weight = blockWeight4,
        };
    }
}
