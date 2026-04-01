// ===================================================================
// Map Wars: Tactical Conquest - Level Data Configuration
// Description: ScriptableObject-based level data storage.
//              Defines all hand-crafted level layouts and parameters.
// ===================================================================

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject for storing level data in the Unity Editor.
/// Create new instances via: Right-click > Create > Map Wars > Level Data
/// Each instance defines one level's complete configuration.
/// </summary>
[CreateAssetMenu(fileName = "LevelData_", menuName = "Map Wars/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Info")]
    [Tooltip("Display name of the level")]
    public string levelName = "New Level";

    [Tooltip("Level number (determines difficulty scaling)")]
    public int levelNumber = 1;

    [Header("Node Configuration")]
    [Tooltip("All nodes in this level with their properties")]
    public List<NodeData> nodes = new List<NodeData>();

    [Header("AI Settings")]
    [Tooltip("AI difficulty for this level")]
    [Range(0f, 1f)] public float aiDifficulty = 0.5f;

    [Header("Rewards")]
    [Tooltip("Base coins awarded for completion")]
    public int coinReward = 50;

    [Tooltip("Time in seconds for 3-star rating")]
    public float starTime3 = 30f;

    [Tooltip("Time in seconds for 2-star rating")]
    public float starTime2 = 60f;

    [Tooltip("Bonus coins for completing under the 3-star time")]
    public int speedBonus = 100;
}

/// <summary>
/// Data for a single node within a level.
/// </summary>
[System.Serializable]
public class NodeData
{
    [Tooltip("Position on screen (0-1 normalized coordinates)")]
    public Vector2 position = Vector2.zero;

    [Tooltip("Initial owner of this node")]
    public Faction owner = Faction.Neutral;

    [Tooltip("Type of node (affects size and production rate)")]
    public NodeType type = NodeType.Small;

    [Tooltip("Starting number of soldiers")]
    public int initialSoldiers = 10;

    [Tooltip("Custom production rate multiplier (0 = use default from type)")]
    [Range(0f, 5f)] public float productionMultiplier = 0f;
}

// ===================================================================
// BUILT-IN LEVEL DEFINITIONS
// These can be converted to ScriptableObjects for editor usage.
// ===================================================================
public static class BuiltInLevels
{
    /// <summary>
    /// Tutorial level with 3 nodes. Introduces basic mechanics.
    /// Player base (left) - Neutral (center) - Enemy base (right)
    /// </summary>
    public static LevelDataSO GetLevel1()
    {
        LevelDataSO level = CreateInstance<LevelDataSO>();
        level.levelName = "First Contact";
        level.levelNumber = 1;
        level.aiDifficulty = 0.2f;
        level.coinReward = 50;
        level.starTime3 = 20f;
        level.starTime2 = 40f;
        level.speedBonus = 50;

        level.nodes = new List<NodeData>
        {
            new NodeData { position = new Vector2(0.25f, 0.5f), owner = Faction.Player, type = NodeType.Medium, initialSoldiers = 15 },
            new NodeData { position = new Vector2(0.5f, 0.5f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 5 },
            new NodeData { position = new Vector2(0.75f, 0.5f), owner = Faction.Enemy, type = NodeType.Medium, initialSoldiers = 10 }
        };

        return level;
    }

    /// <summary>
    /// Expansion level with 5 nodes. Introduces multiple neutral targets.
    /// </summary>
    public static LevelDataSO GetLevel2()
    {
        LevelDataSO level = CreateInstance<LevelDataSO>();
        level.levelName = "Expansion";
        level.levelNumber = 2;
        level.aiDifficulty = 0.3f;
        level.coinReward = 65;
        level.starTime3 = 30f;
        level.starTime2 = 50f;
        level.speedBonus = 60;

        level.nodes = new List<NodeData>
        {
            new NodeData { position = new Vector2(0.2f, 0.7f), owner = Faction.Player, type = NodeType.Medium, initialSoldiers = 12 },
            new NodeData { position = new Vector2(0.35f, 0.3f), owner = Faction.Player, type = NodeType.Small, initialSoldiers = 8 },
            new NodeData { position = new Vector2(0.5f, 0.6f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 5 },
            new NodeData { position = new Vector2(0.65f, 0.4f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 4 },
            new NodeData { position = new Vector2(0.8f, 0.65f), owner = Faction.Enemy, type = NodeType.Medium, initialSoldiers = 12 }
        };

        return level;
    }

    /// <summary>
    /// Skirmish level with 6 nodes. First real tactical challenge.
    /// </summary>
    public static LevelDataSO GetLevel3()
    {
        LevelDataSO level = CreateInstance<LevelDataSO>();
        level.levelName = "Skirmish";
        level.levelNumber = 3;
        level.aiDifficulty = 0.4f;
        level.coinReward = 80;
        level.starTime3 = 35f;
        level.starTime2 = 60f;
        level.speedBonus = 70;

        level.nodes = new List<NodeData>
        {
            new NodeData { position = new Vector2(0.15f, 0.5f), owner = Faction.Player, type = NodeType.Medium, initialSoldiers = 15 },
            new NodeData { position = new Vector2(0.3f, 0.25f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 5 },
            new NodeData { position = new Vector2(0.3f, 0.75f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 6 },
            new NodeData { position = new Vector2(0.55f, 0.35f), owner = Faction.Enemy, type = NodeType.Small, initialSoldiers = 8 },
            new NodeData { position = new Vector2(0.55f, 0.7f), owner = Faction.Enemy, type = NodeType.Small, initialSoldiers = 8 },
            new NodeData { position = new Vector2(0.75f, 0.5f), owner = Faction.Neutral, type = NodeType.Large, initialSoldiers = 10 }
        };

        return level;
    }

    /// <summary>
    /// Fortification level with 7 nodes. Introduces large/fortress nodes.
    /// </summary>
    public static LevelDataSO GetLevel4()
    {
        LevelDataSO level = CreateInstance<LevelDataSO>();
        level.levelName = "Fortification";
        level.levelNumber = 4;
        level.aiDifficulty = 0.45f;
        level.coinReward = 100;
        level.starTime3 = 40f;
        level.starTime2 = 70f;
        level.speedBonus = 80;

        level.nodes = new List<NodeData>
        {
            new NodeData { position = new Vector2(0.2f, 0.5f), owner = Faction.Player, type = NodeType.Medium, initialSoldiers = 14 },
            new NodeData { position = new Vector2(0.35f, 0.3f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 5 },
            new NodeData { position = new Vector2(0.35f, 0.7f), owner = Faction.Player, type = NodeType.Small, initialSoldiers = 8 },
            new NodeData { position = new Vector2(0.5f, 0.5f), owner = Faction.Neutral, type = NodeType.Fortress, initialSoldiers = 20, productionMultiplier = 3f },
            new NodeData { position = new Vector2(0.65f, 0.3f), owner = Faction.Enemy, type = NodeType.Small, initialSoldiers = 10 },
            new NodeData { position = new Vector2(0.65f, 0.7f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 5 },
            new NodeData { position = new Vector2(0.8f, 0.5f), owner = Faction.Enemy, type = NodeType.Medium, initialSoldiers = 14 }
        };

        return level;
    }

    /// <summary>
    /// Full Assault level with 8 nodes. Multiple front lines.
    /// </summary>
    public static LevelDataSO GetLevel5()
    {
        LevelDataSO level = CreateInstance<LevelDataSO>();
        level.levelName = "Full Assault";
        level.levelNumber = 5;
        level.aiDifficulty = 0.5f;
        level.coinReward = 120;
        level.starTime3 = 45f;
        level.starTime2 = 75f;
        level.speedBonus = 100;

        level.nodes = new List<NodeData>
        {
            new NodeData { position = new Vector2(0.12f, 0.3f), owner = Faction.Player, type = NodeType.Medium, initialSoldiers = 15 },
            new NodeData { position = new Vector2(0.12f, 0.7f), owner = Faction.Player, type = NodeType.Medium, initialSoldiers = 10 },
            new NodeData { position = new Vector2(0.32f, 0.5f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 6 },
            new NodeData { position = new Vector2(0.5f, 0.25f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 5 },
            new NodeData { position = new Vector2(0.5f, 0.75f), owner = Faction.Neutral, type = NodeType.Small, initialSoldiers = 5 },
            new NodeData { position = new Vector2(0.68f, 0.5f), owner = Faction.Neutral, type = NodeType.Large, initialSoldiers = 12 },
            new NodeData { position = new Vector2(0.88f, 0.3f), owner = Faction.Enemy, type = NodeType.Medium, initialSoldiers = 12 },
            new NodeData { position = new Vector2(0.88f, 0.7f), owner = Faction.Enemy, type = NodeType.Medium, initialSoldiers = 10 }
        };

        return level;
    }
}
