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
        public float monsterWeight = 5f;
        public float rewardWeight = 2f;
        public float voidWeight = 2f;

        [Header("Monsters")]
        public int monsterPowerMin = 3;
        public int monsterPowerMax = 9;

        public GridSettings ToGridSettings() => new GridSettings
        {
            MonsterWeight = monsterWeight,
            RewardWeight = rewardWeight,
            VoidWeight = voidWeight,
            MonsterPowerMin = monsterPowerMin,
            MonsterPowerMax = monsterPowerMax,
        };
    }
}
