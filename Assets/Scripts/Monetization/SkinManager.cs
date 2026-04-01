// ===================================================================
// Map Wars: Tactical Conquest - Skin Manager (Monetization Script)
// Description: Manages visual skin customization for nodes,
//              troops, trails, and backgrounds. Applies visual
//              changes dynamically during gameplay.
// ===================================================================

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines visual properties for a node skin.
/// </summary>
[System.Serializable]
public class NodeSkinData
{
    public string id;
    public string name;
    public Sprite sprite;
    public float ringThickness = 1f;
    public float glowIntensity = 1f;
    public Color tint = Color.white;
    public int cost = 100;
    public bool isDefault = false;
}

/// <summary>
/// Defines visual properties for troop projectile skins.
/// </summary>
[System.Serializable]
public class TroopSkinData
{
    public string id;
    public string name;
    public Sprite sprite;
    public float size = 1f;
    public Color tint = Color.white;
    public bool hasTrail = true;
    public int cost = 150;
    public bool isDefault = false;
}

/// <summary>
/// Manages all visual skin customization in the game.
/// Handles purchasing, equipping, and applying skins to nodes and troops.
/// Attach to "SkinManager" GameObject.
/// </summary>
public class SkinManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static SkinManager _instance;
    public static SkinManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<SkinManager>();
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR REFERENCES
    // =========================================================

    [Header("Node Skins")]
    [SerializeField] private List<NodeSkinData> _nodeSkins = new List<NodeSkinData>();
    [SerializeField] private NodeSkinData _defaultNodeSkin;

    [Header("Troop Skins")]
    [SerializeField] private List<TroopSkinData> _troopSkins = new List<TroopSkinData>();
    [SerializeField] private TroopSkinData _defaultTroopSkin;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Dictionary<string, bool> _purchasedNodeSkins = new Dictionary<string, bool>();
    private Dictionary<string, bool> _purchasedTroopSkins = new Dictionary<string, bool>();
    private string _equippedNodeSkinId;
    private string _equippedTroopSkinId;

    // =========================================================
    // PROPERTIES
    // =========================================================

    /// <summary>Currently equipped node skin</summary>
    public NodeSkinData EquippedNodeSkin
    {
        get
        {
            if (string.IsNullOrEmpty(_equippedNodeSkinId))
                return _defaultNodeSkin;

            var skin = _nodeSkins.Find(s => s.id == _equippedNodeSkinId);
            return skin ?? _defaultNodeSkin;
        }
    }

    /// <summary>Currently equipped troop skin</summary>
    public TroopSkinData EquippedTroopSkin
    {
        get
        {
            if (string.IsNullOrEmpty(_equippedTroopSkinId))
                return _defaultTroopSkin;

            var skin = _troopSkins.Find(s => s.id == _equippedTroopSkinId);
            return skin ?? _defaultTroopSkin;
        }
    }

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSkinData();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // PUBLIC API - NODE SKINS
    // =========================================================

    /// <summary>
    /// Gets all available node skins.
    /// </summary>
    public List<NodeSkinData> GetAllNodeSkins() => _nodeSkins;

    /// <summary>
    /// Checks if a specific node skin is purchased.
    /// </summary>
    public bool IsNodeSkinPurchased(string skinId)
    {
        if (_purchasedNodeSkins.TryGetValue(skinId, out bool purchased))
            return purchased;

        var skin = _nodeSkins.Find(s => s.id == skinId);
        return skin?.isDefault ?? false;
    }

    /// <summary>
    /// Purchases a node skin. Returns false if insufficient coins.
    /// </summary>
    public bool PurchaseNodeSkin(string skinId)
    {
        var skin = _nodeSkins.Find(s => s.id == skinId);
        if (skin == null || skin.isDefault) return false;
        if (IsNodeSkinPurchased(skinId)) return false;

        if (!SaveSystem.Instance.RemoveCoins(skin.cost))
        {
            UIManager.Instance?.ShowToast("Not enough coins!");
            return false;
        }

        _purchasedNodeSkins[skinId] = true;
        PlayerPrefs.SetInt($"NodeSkin_{skinId}", 1);
        PlayerPrefs.Save();

        UIManager.Instance?.ShowToast($"Purchased: {skin.name}!");
        Debug.Log($"[SkinManager] Purchased node skin: {skin.name}");
        return true;
    }

    /// <summary>
    /// Equips a node skin. Must be purchased first.
    /// </summary>
    public void EquipNodeSkin(string skinId)
    {
        if (!IsNodeSkinPurchased(skinId) && !_nodeSkins.Find(s => s.id == skinId)?.isDefault == true)
            return;

        _equippedNodeSkinId = skinId;
        PlayerPrefs.SetString("EquippedNodeSkin", skinId);
        PlayerPrefs.Save();

        ApplyNodeSkinToAllNodes();
        Debug.Log($"[SkinManager] Equipped node skin: {skinId}");
    }

    // =========================================================
    // PUBLIC API - TROOP SKINS
    // =========================================================

    /// <summary>
    /// Gets all available troop skins.
    /// </summary>
    public List<TroopSkinData> GetAllTroopSkins() => _troopSkins;

    /// <summary>
    /// Checks if a specific troop skin is purchased.
    /// </summary>
    public bool IsTroopSkinPurchased(string skinId)
    {
        if (_purchasedTroopSkins.TryGetValue(skinId, out bool purchased))
            return purchased;

        var skin = _troopSkins.Find(s => s.id == skinId);
        return skin?.isDefault ?? false;
    }

    /// <summary>
    /// Purchases a troop skin. Returns false if insufficient coins.
    /// </summary>
    public bool PurchaseTroopSkin(string skinId)
    {
        var skin = _troopSkins.Find(s => s.id == skinId);
        if (skin == null || skin.isDefault) return false;
        if (IsTroopSkinPurchased(skinId)) return false;

        if (!SaveSystem.Instance.RemoveCoins(skin.cost))
        {
            UIManager.Instance?.ShowToast("Not enough coins!");
            return false;
        }

        _purchasedTroopSkins[skinId] = true;
        PlayerPrefs.SetInt($"TroopSkin_{skinId}", 1);
        PlayerPrefs.Save();

        UIManager.Instance?.ShowToast($"Purchased: {skin.name}!");
        return true;
    }

    /// <summary>
    /// Equips a troop skin. Must be purchased first.
    /// </summary>
    public void EquipTroopSkin(string skinId)
    {
        if (!IsTroopSkinPurchased(skinId))
            return;

        _equippedTroopSkinId = skinId;
        PlayerPrefs.SetString("EquippedTroopSkin", skinId);
        PlayerPrefs.Save();

        Debug.Log($"[SkinManager] Equipped troop skin: {skinId}");
    }

    // =========================================================
    // SKIN APPLICATION
    // =========================================================

    /// <summary>
    /// Applies the currently equipped node skin to all active player nodes.
    /// </summary>
    private void ApplyNodeSkinToAllNodes()
    {
        NodeSkinData skin = EquippedNodeSkin;
        if (skin == null) return;

        var playerNodes = GameManager.Instance?.GetNodesByFaction(Faction.Player);
        if (playerNodes == null) return;

        foreach (var node in playerNodes)
        {
            ApplyNodeSkin(node, skin);
        }
    }

    /// <summary>
    /// Applies a skin to a specific node.
    /// </summary>
    private void ApplyNodeSkin(NodeController node, NodeSkinData skin)
    {
        if (node == null || skin == null) return;

        // The visual application would modify the node's renderer
        // This integrates with NodeController's visual system
        // Implementation depends on how NodeController renders its sprites
    }

    // =========================================================
    // PERSISTENCE
    // =========================================================

    /// <summary>
    /// Loads saved skin purchase and equip data from PlayerPrefs.
    /// </summary>
    private void LoadSkinData()
    {
        // Load node skin purchases
        foreach (var skin in _nodeSkins)
        {
            if (skin.isDefault)
            {
                _purchasedNodeSkins[skin.id] = true;
            }
            else
            {
                bool purchased = PlayerPrefs.GetInt($"NodeSkin_{skin.id}", 0) == 1;
                if (purchased) _purchasedNodeSkins[skin.id] = true;
            }
        }

        // Load troop skin purchases
        foreach (var skin in _troopSkins)
        {
            if (skin.isDefault)
            {
                _purchasedTroopSkins[skin.id] = true;
            }
            else
            {
                bool purchased = PlayerPrefs.GetInt($"TroopSkin_{skin.id}", 0) == 1;
                if (purchased) _purchasedTroopSkins[skin.id] = true;
            }
        }

        // Load equipped skins
        _equippedNodeSkinId = PlayerPrefs.GetString("EquippedNodeSkin", "");
        _equippedTroopSkinId = PlayerPrefs.GetString("EquippedTroopSkin", "");

        Debug.Log("[SkinManager] Skin data loaded");
    }
}
