using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Plant Pollution Game/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Dimensions")]
        public int width = 20;
        public int height = 20;

        [Header("Pollution Sources")]
        public List<PollutionSourceSetup> pollutionSources = new List<PollutionSourceSetup>();

        [System.Serializable]
        public class PollutionSourceSetup
        {
            [Tooltip("Position on the grid")]
            public Vector2Int position;

            [Tooltip("Type of pollution this source emits")]
            public PollutionType pollutionType = PollutionType.Toxic;

            [Tooltip("Tier of the source (affects HP and emission)")]
            public SourceTier tier = SourceTier.Weak;

            [Tooltip("Maximum health points")]
            public float maxHP = 50f;

            [Tooltip("Amount of pollution emitted per tick")]
            public float emissionRate = 5f;

            [Tooltip("How often this source emits (in seconds)")]
            public float tickInterval = 7f;

            [Tooltip("How long before this source becomes active (0 = immediate)")]
            public float dormantDuration = 0f;
        }

        [ContextMenu("Setup Example - Easy")]
        public void SetupExampleEasy()
        {
            pollutionSources.Clear();
            width = 15;
            height = 15;

            // 2 weak sources
            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(3, 3),
                pollutionType = PollutionType.Toxic,
                tier = SourceTier.Weak,
                maxHP = 50f,
                emissionRate = 5f,
                tickInterval = 7f,
                dormantDuration = 0f
            });

            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(11, 11),
                pollutionType = PollutionType.Sludge,
                tier = SourceTier.Weak,
                maxHP = 50f,
                emissionRate = 5f,
                tickInterval = 7f,
                dormantDuration = 0f
            });
        }

        [ContextMenu("Setup Example - Medium")]
        public void SetupExampleMedium()
        {
            pollutionSources.Clear();
            width = 20;
            height = 20;

            // 2 weak sources (active immediately)
            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(4, 4),
                pollutionType = PollutionType.Toxic,
                tier = SourceTier.Weak,
                maxHP = 50f,
                emissionRate = 5f,
                tickInterval = 7f,
                dormantDuration = 0f
            });

            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(15, 15),
                pollutionType = PollutionType.Sludge,
                tier = SourceTier.Weak,
                maxHP = 50f,
                emissionRate = 5f,
                tickInterval = 7f,
                dormantDuration = 0f
            });

            // 2 medium sources (awaken at 2 minutes)
            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(4, 15),
                pollutionType = PollutionType.Acidic,
                tier = SourceTier.Medium,
                maxHP = 150f,
                emissionRate = 15f,
                tickInterval = 7f,
                dormantDuration = 120f // 2 minutes
            });

            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(15, 4),
                pollutionType = PollutionType.Toxic,
                tier = SourceTier.Medium,
                maxHP = 150f,
                emissionRate = 15f,
                tickInterval = 7f,
                dormantDuration = 120f // 2 minutes
            });
        }

        [ContextMenu("Setup Example - Hard")]
        public void SetupExampleHard()
        {
            pollutionSources.Clear();
            width = 25;
            height = 25;

            // 2 weak sources
            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(5, 5),
                pollutionType = PollutionType.Toxic,
                tier = SourceTier.Weak,
                maxHP = 50f,
                emissionRate = 5f,
                tickInterval = 7f,
                dormantDuration = 0f
            });

            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(19, 19),
                pollutionType = PollutionType.Sludge,
                tier = SourceTier.Weak,
                maxHP = 50f,
                emissionRate = 5f,
                tickInterval = 7f,
                dormantDuration = 0f
            });

            // 2 medium sources (awaken at 2 minutes)
            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(5, 19),
                pollutionType = PollutionType.Acidic,
                tier = SourceTier.Medium,
                maxHP = 150f,
                emissionRate = 15f,
                tickInterval = 7f,
                dormantDuration = 120f
            });

            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(19, 5),
                pollutionType = PollutionType.Toxic,
                tier = SourceTier.Medium,
                maxHP = 150f,
                emissionRate = 15f,
                tickInterval = 7f,
                dormantDuration = 120f
            });

            // 1 strong source (awakens at 5 minutes)
            pollutionSources.Add(new PollutionSourceSetup
            {
                position = new Vector2Int(12, 12),
                pollutionType = PollutionType.Acidic,
                tier = SourceTier.Strong,
                maxHP = 300f,
                emissionRate = 30f,
                tickInterval = 7f,
                dormantDuration = 300f // 5 minutes
            });
        }
    }
}

