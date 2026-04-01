// ===================================================================
// Map Wars: Tactical Conquest - Monetization Manager (Monetization Script)
// Description: Handles all monetization systems including rewarded ads,
//              interstitial ads, in-app purchases, and the skin shop.
// Uses Unity Ads / Google AdMob integration pattern.
// ===================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Defines a purchasable skin item in the shop.
/// </summary>
[System.Serializable]
public class SkinItem
{
    public string skinId;
    public string skinName;
    public string description;
    public int cost;               // Cost in gold coins
    public bool isPurchased;
    public bool isEquipped;
    public Sprite previewImage;
    public string nodeColorHex;    // Hex color code for nodes
    public string troopColorHex;   // Hex color code for troops
    public SkinCategory category;
}

/// <summary>
/// Category of cosmetic skin items.
/// </summary>
public enum SkinCategory
{
    NodeShape,
    TroopStyle,
    TrailEffect,
    BackgroundTheme
}

/// <summary>
/// Manages all monetization aspects of the game:
/// - Rewarded video ads (military support, energy refill)
/// - Interstitial ads (every 3 levels)
/// - Coin shop and skin purchases
/// - In-app purchases for premium currency
/// 
/// This script uses a placeholder pattern for ad SDK integration.
/// Replace placeholder methods with actual SDK calls when integrating
/// Unity Ads, AdMob, or ironSource.
/// Attach to "MonetizationManager" GameObject.
/// </summary>
public class MonetizationManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static MonetizationManager _instance;
    public static MonetizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MonetizationManager>();
            }
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("Ad Settings")]
    [SerializeField] private string _gameId = "YOUR_GAME_ID";
    [SerializeField] private string _rewardedAdPlacementId = "rewarded_video";
    [SerializeField] private string _interstitialAdPlacementId = "interstitial";
    [SerializeField] private bool _testMode = true;

    [Header("Shop Settings")]
    [SerializeField] private List<SkinItem> _availableSkins = new List<SkinItem>();
    [SerializeField] private int _militarySupportSoldiers = 20;
    [SerializeField] private int _energyRefillAmount = 5;

    [Header("IAP Products")]
    [SerializeField] private int _smallCoinPack = 500;
    [SerializeField] private float _smallCoinPrice = 0.99f;
    [SerializeField] private int _mediumCoinPack = 2000;
    [SerializeField] private float _mediumCoinPrice = 3.99f;
    [SerializeField] private int _largeCoinPack = 10000;
    [SerializeField] private float _largeCoinPrice = 9.99f;

    // =========================================================
    // EVENTS
    // =========================================================

    /// <summary>Fired when a rewarded ad completes successfully</summary>
    public event System.Action OnRewardedAdCompleted;

    /// <summary>Fired when a rewarded ad fails or is skipped</summary>
    public event System.Action OnRewardedAdFailed;

    /// <summary>Fired when a skin is purchased</summary>
    public event System.Action<SkinItem> OnSkinPurchased;

    /// <summary>Fired when a skin is equipped</summary>
    public event System.Action<SkinItem> OnSkinEquipped;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private bool _isAdReady = false;
    private bool _isShowingAd = false;
    private System.Action _currentRewardCallback;
    private System.Action _currentFailureCallback;
    private Dictionary<string, bool> _purchasedSkins = new Dictionary<string, bool>();
    private string _equippedSkinId = "default";

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

        LoadPurchasedSkins();
        InitializeAdSDK();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // AD SDK INITIALIZATION
    // =========================================================

    /// <summary>
    /// Initializes the ad SDK. Replace with actual SDK initialization.
    /// Currently uses placeholder logic for testing.
    /// </summary>
    private void InitializeAdSDK()
    {
        Debug.Log("[MonetizationManager] Initializing ad SDK...");
        
        // === UNITY ADS INTEGRATION ===
        // Uncomment and configure when using Unity Ads:
        /*
        if (Advertisement.isInitialized)
        {
            Advertisement.AddListener(this);
            LoadRewardedAd();
            LoadInterstitialAd();
        }
        else
        {
            Advertisement.Initialize(_gameId, _testMode, () =>
            {
                Advertisement.AddListener(this);
                LoadRewardedAd();
                LoadInterstitialAd();
            });
        }
        */

        // === ADMOB INTEGRATION ===
        // Uncomment when using Google AdMob:
        /*
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[MonetizationManager] AdMob initialized");
            LoadRewardedAd();
            LoadInterstitialAd();
        });
        */

        // Placeholder: Ads are "ready" after 2 seconds for testing
        StartCoroutine(PlaceholderAdReady());
    }

    private IEnumerator PlaceholderAdReady()
    {
        yield return new WaitForSeconds(2f);
        _isAdReady = true;
        Debug.Log("[MonetizationManager] Ads ready (placeholder)");
    }

    // =========================================================
    // REWARDED ADS
    // =========================================================

    /// <summary>
    /// Loads a rewarded video ad. Replace with actual SDK call.
    /// </summary>
    public void LoadRewardedAd()
    {
        // === UNITY ADS ===
        // Advertisement.Load(_rewardedAdPlacementId, new LoadOptions());

        // === ADMOB ===
        // _rewardedAd = new RewardedAd(_rewardedAdUnitId);
        // AdRequest request = new AdRequest.Builder().Build();
        // _rewardedAd.LoadAd(request);

        Debug.Log("[MonetizationManager] Loading rewarded ad...");
    }

    /// <summary>
    /// Shows a rewarded ad with callbacks for success and failure.
    /// </summary>
    /// <param name="onReward">Called when the user watches the full ad</param>
    /// <param name="onFailure">Called when the ad fails or is skipped</param>
    public void ShowRewardedAd(System.Action onReward, System.Action onFailure = null)
    {
        if (_isShowingAd)
        {
            onFailure?.Invoke();
            return;
        }

        _currentRewardCallback = onReward;
        _currentFailureCallback = onFailure;

        // === UNITY ADS ===
        /*
        if (Advertisement.IsReady(_rewardedAdPlacementId))
        {
            _isShowingAd = true;
            Advertisement.Show(_rewardedAdPlacementId, new ShowOptions
            {
                resultCallback = HandleRewardedAdResult
            });
        }
        else
        {
            onFailure?.Invoke();
        }
        */

        // === ADMOB ===
        /*
        if (_rewardedAd.IsLoaded())
        {
            _isShowingAd = true;
            _rewardedAd.Show((Reward reward) =>
            {
                HandleRewardedAdResult(ShowResult.Finished);
            });
        }
        else
        {
            onFailure?.Invoke();
        }
        */

        // PLACEHOLDER: Simulate successful ad watch
        StartCoroutine(SimulateRewardedAd());
    }

    /// <summary>
    /// Placeholder coroutine that simulates watching a rewarded ad.
    /// </summary>
    private IEnumerator SimulateRewardedAd()
    {
        _isShowingAd = true;
        Debug.Log("[MonetizationManager] Showing rewarded ad (placeholder)...");

        // Simulate ad loading time
        yield return new WaitForSeconds(1f);

        // Simulate successful ad completion
        _isShowingAd = false;
        _currentRewardCallback?.Invoke();
        OnRewardedAdCompleted?.Invoke();

        // Reload ad
        LoadRewardedAd();
    }

    /// <summary>
    /// Handles the result of a rewarded ad showing.
    /// </summary>
    private void HandleRewardedAdResult(ShowResult result)
    {
        _isShowingAd = false;

        switch (result)
        {
            case ShowResult.Finished:
                Debug.Log("[MonetizationManager] Rewarded ad completed");
                _currentRewardCallback?.Invoke();
                OnRewardedAdCompleted?.Invoke();
                break;

            case ShowResult.Skipped:
                Debug.Log("[MonetizationManager] Rewarded ad skipped");
                _currentFailureCallback?.Invoke();
                OnRewardedAdFailed?.Invoke();
                break;

            case ShowResult.Error:
                Debug.LogError("[MonetizationManager] Rewarded ad error");
                _currentFailureCallback?.Invoke();
                OnRewardedAdFailed?.Invoke();
                break;
        }

        // Reload ad for next use
        LoadRewardedAd();
    }

    // =========================================================
    // INTERSTITIAL ADS
    // =========================================================

    /// <summary>
    /// Loads an interstitial ad. Replace with actual SDK call.
    /// </summary>
    public void LoadInterstitialAd()
    {
        // === UNITY ADS ===
        // Advertisement.Load(_interstitialAdPlacementId, new LoadOptions());

        // === ADMOB ===
        // _interstitialAd = new InterstitialAd(_interstitialAdUnitId);
        // AdRequest request = new AdRequest.Builder().Build();
        // _interstitialAd.LoadAd(request);

        Debug.Log("[MonetizationManager] Loading interstitial ad...");
    }

    /// <summary>
    /// Shows an interstitial ad. Only shows if an ad is ready
    /// and the player hasn't just started (cooldown prevention).
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (_isShowingAd) return;

        // Don't show interstitial in first 30 seconds of a session
        if (Time.realtimeSinceStartup < 30f) return;

        // === UNITY ADS ===
        /*
        if (Advertisement.IsReady(_interstitialAdPlacementId))
        {
            _isShowingAd = true;
            Advertisement.Show(_interstitialAdPlacementId, new ShowOptions
            {
                resultCallback = (result) => _isShowingAd = false
            });
        }
        */

        // PLACEHOLDER: Simulate interstitial
        Debug.Log("[MonetizationManager] Interstitial ad shown (placeholder)");
        LoadInterstitialAd();
    }

    // =========================================================
    // MILITARY SUPPORT (REWARDED AD FEATURE)
    // =========================================================

    /// <summary>
    /// Shows a rewarded ad to grant the player bonus soldiers.
    /// Adds soldiers to all player-owned nodes upon successful completion.
    /// </summary>
    public void RequestMilitarySupport()
    {
        ShowRewardedAd(
            onReward: () =>
            {
                var playerNodes = GameManager.Instance.GetNodesByFaction(Faction.Player);
                foreach (var node in playerNodes)
                {
                    node.AddSoldiers(_militarySupportSoldiers);
                }

                // Spawn visual effect at player nodes
                if (EffectsManager.Instance != null)
                {
                    foreach (var node in playerNodes)
                    {
                        EffectsManager.Instance.SpawnBoostEffect(node.transform.position);
                    }
                }

                Debug.Log($"[MonetizationManager] Military support granted: +{_militarySupportSoldiers} soldiers");
            },
            onFailure: () =>
            {
                UIManager.Instance?.ShowToast("Ad not available. Try again later.");
            }
        );
    }

    // =========================================================
    // ENERGY REFILL (REWARDED AD FEATURE)
    // =========================================================

    /// <summary>
    /// Shows a rewarded ad to refill energy.
    /// </summary>
    public void RequestEnergyRefill()
    {
        ShowRewardedAd(
            onReward: () =>
            {
                SaveSystem.Instance.AddEnergy(_energyRefillAmount);
                UIManager.Instance?.UpdateEnergyDisplay();
                UIManager.Instance?.ShowToast($"+{_energyRefillAmount} Energy!");
                Debug.Log("[MonetizationManager] Energy refilled via ad");
            },
            onFailure: () =>
            {
                UIManager.Instance?.ShowToast("Ad not available. Try again later.");
            }
        );
    }

    // =========================================================
    // SKIN SHOP SYSTEM
    // =========================================================

    /// <summary>
    /// Gets all available skin items.
    /// </summary>
    public List<SkinItem> GetAllSkins() => _availableSkins;

    /// <summary>
    /// Gets skins filtered by category.
    /// </summary>
    public List<SkinItem> GetSkinsByCategory(SkinCategory category)
    {
        return _availableSkins.FindAll(s => s.category == category);
    }

    /// <summary>
    /// Gets only the skins that are purchased and not currently equipped.
    /// </summary>
    public List<SkinItem> GetPurchasedSkins()
    {
        return _availableSkins.FindAll(s => s.isPurchased);
    }

    /// <summary>
    /// Attempts to purchase a skin with gold coins.
    /// </summary>
    /// <param name="skinId">The skin to purchase</param>
    /// <returns>True if the purchase was successful</returns>
    public bool PurchaseSkin(string skinId)
    {
        SkinItem skin = _availableSkins.Find(s => s.skinId == skinId);
        if (skin == null)
        {
            Debug.LogError($"[MonetizationManager] Skin not found: {skinId}");
            return false;
        }

        if (skin.isPurchased)
        {
            Debug.LogWarning($"[MonetizationManager] Skin already purchased: {skinId}");
            return false;
        }

        if (SaveSystem.Instance.Coins < skin.cost)
        {
            UIManager.Instance?.ShowToast("Not enough coins!");
            return false;
        }

        // Deduct coins and mark as purchased
        SaveSystem.Instance.RemoveCoins(skin.cost);
        skin.isPurchased = true;
        _purchasedSkins[skinId] = true;

        SavePurchasedSkins();
        OnSkinPurchased?.Invoke(skin);

        Debug.Log($"[MonetizationManager] Skin purchased: {skin.skinName} for {skin.cost} coins");
        return true;
    }

    /// <summary>
    /// Equips a purchased skin for use in-game.
    /// </summary>
    /// <param name="skinId">The skin to equip</param>
    public void EquipSkin(string skinId)
    {
        SkinItem skin = _availableSkins.Find(s => s.skinId == skinId);
        if (skin == null || !skin.isPurchased)
        {
            Debug.LogWarning($"[MonetizationManager] Cannot equip skin: {skinId}");
            return;
        }

        // Unequip current skin
        foreach (var s in _availableSkins)
        {
            s.isEquipped = false;
        }

        // Equip new skin
        skin.isEquipped = true;
        _equippedSkinId = skinId;

        SaveEquippedSkin();
        OnSkinEquipped?.Invoke(skin);

        // Apply visual changes
        ApplySkinVisuals(skin);

        Debug.Log($"[MonetizationManager] Skin equipped: {skin.skinName}");
    }

    /// <summary>
    /// Applies the visual changes from a skin to the game.
    /// Updates node colors, troop colors, and effects.
    /// </summary>
    private void ApplySkinVisuals(SkinItem skin)
    {
        Color nodeColor = Color.white;
        Color troopColor = Color.white;

        if (ColorUtility.TryParseHtmlString(skin.nodeColorHex, out nodeColor))
        {
            // Apply to all player nodes
            var playerNodes = GameManager.Instance.GetNodesByFaction(Faction.Player);
            foreach (var node in playerNodes)
            {
                // Update node visual through the node's color system
                // This would integrate with a customizable color system
            }
        }

        if (ColorUtility.TryParseHtmlString(skin.troopColorHex, out troopColor))
        {
            // Apply to troop projectile system
        }
    }

    // =========================================================
    // IN-APP PURCHASES
    // =========================================================

    /// <summary>
    /// Initiates purchase of a coin pack.
    /// Replace with actual IAP SDK calls (Unity IAP).
    /// </summary>
    /// <param name="packSize">Identifier for the coin pack</param>
    public void PurchaseCoinPack(string packSize)
    {
        int coins = packSize switch
        {
            "small" => _smallCoinPack,
            "medium" => _mediumCoinPack,
            "large" => _largeCoinPack,
            _ => 0
        };

        if (coins == 0) return;

        // === UNITY IAP INTEGRATION ===
        /*
        string productId = $"com.yourcompany.mapwars.coins_{packSize}";
        Debug.Log($"[MonetizationManager] Initiating IAP: {productId}");
        // Product product = _storeController.products.WithID(productId);
        // _storeController.InitiatePurchase(product);
        */

        // PLACEHOLDER
        SaveSystem.Instance.AddCoins(coins);
        UIManager.Instance?.ShowToast($"+{coins} Coins!");
        Debug.Log($"[MonetizationManager] Coin pack purchased (placeholder): {coins} coins");
    }

    // =========================================================
    // PERSISTENCE
    // =========================================================

    /// <summary>
    /// Saves purchased skins state to PlayerPrefs.
    /// </summary>
    private void SavePurchasedSkins()
    {
        foreach (var skin in _availableSkins)
        {
            PlayerPrefs.SetInt($"Skin_Purchased_{skin.skinId}", skin.isPurchased ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads purchased skins state from PlayerPrefs.
    /// </summary>
    private void LoadPurchasedSkins()
    {
        foreach (var skin in _availableSkins)
        {
            skin.isPurchased = PlayerPrefs.GetInt($"Skin_Purchased_{skin.skinId}", 0) == 1;
            if (skin.isPurchased)
                _purchasedSkins[skin.skinId] = true;
        }

        _equippedSkinId = PlayerPrefs.GetString("EquippedSkin", "default");
    }

    /// <summary>
    /// Saves the currently equipped skin to PlayerPrefs.
    /// </summary>
    private void SaveEquippedSkin()
    {
        PlayerPrefs.SetString("EquippedSkin", _equippedSkinId);
        PlayerPrefs.Save();
    }
}

// Enum for ShowResult (matches Unity Ads)
public enum ShowResult
{
    Finished,
    Skipped,
    Error
}
