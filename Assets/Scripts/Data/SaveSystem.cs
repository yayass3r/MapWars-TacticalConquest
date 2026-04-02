// ===================================================================
// Map Wars: Tactical Conquest - Save System (Data Script)
// Description: Handles persistent data storage using PlayerPrefs.
//              Manages coins, energy, level progress, settings,
//              and daily reward tracking.
// ===================================================================

using UnityEngine;
using System;

/// <summary>
/// Manages all persistent game data using PlayerPrefs.
/// Handles encryption of sensitive values and automatic saving.
/// Attach to "SaveSystem" GameObject.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static SaveSystem _instance;
    public static SaveSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveSystem");
                    _instance = go.AddComponent<SaveSystem>();
                }
            }
            return _instance;
        }
    }

    // =========================================================
    // CONFIGURATION
    // =========================================================

    [Header("Energy Settings")]
    [SerializeField] private int _maxEnergy = 10;
    [SerializeField] private int _energyRegenSeconds = 300; // 5 minutes per energy

    [Header("Daily Rewards")]
    [SerializeField] private int _dailyLoginReward = 50;
    [SerializeField] private int _consecutiveDayBonus = 25;

    // =========================================================
    // PROPERTIES
    // =========================================================

    /// <summary>Current number of coins (gold currency)</summary>
    public int Coins
    {
        get => GetInt("Coins", 0);
        private set => SetInt("Coins", value);
    }

    /// <summary>Current energy level</summary>
    public int CurrentEnergy
    {
        get => CalculateCurrentEnergy();
        set => SetInt("Energy", Mathf.Clamp(value, 0, _maxEnergy));
    }

    /// <summary>Maximum energy capacity</summary>
    public int MaxEnergy => _maxEnergy;

    /// <summary>Timestamp of last energy consumption (Unix time)</summary>
    public long LastEnergyTime
    {
        get => GetLong("LastEnergyTime", 0);
        private set => SetLong("LastEnergyTime", value);
    }

    /// <summary>Highest level unlocked by the player</summary>
    public int HighestUnlockedLevel
    {
        get => GetInt("HighestLevel", 1);
        private set => SetInt("HighestLevel", value);
    }

    /// <summary>Total number of games played</summary>
    public int TotalGamesPlayed
    {
        get => GetInt("TotalGames", 0);
        private set => SetInt("TotalGames", value);
    }

    /// <summary>Total number of victories</summary>
    public int TotalVictories
    {
        get => GetInt("TotalVictories", 0);
        private set => SetInt("TotalVictories", value);
    }

    /// <summary>Music volume (0.0 to 1.0)</summary>
    public float MusicVolume
    {
        get => GetFloat("MusicVolume", 0.7f);
        set => SetFloat("MusicVolume", value);
    }

    /// <summary>SFX volume (0.0 to 1.0)</summary>
    public float SFXVolume
    {
        get => GetFloat("SFXVolume", 0.8f);
        set => SetFloat("SFXVolume", value);
    }

    /// <summary>Whether haptic feedback is enabled</summary>
    public bool HapticsEnabled
    {
        get => GetInt("HapticsEnabled", 1) == 1;
        set => SetInt("HapticsEnabled", value ? 1 : 0);
    }

    /// <summary>Last login date (YYYY-MM-DD format)</summary>
    public string LastLoginDate
    {
        get => GetString("LastLoginDate", "");
        private set => SetString("LastLoginDate", value);
    }

    /// <summary>Consecutive login days</summary>
    public int ConsecutiveLoginDays
    {
        get => GetInt("ConsecutiveDays", 0);
        private set => SetInt("ConsecutiveDays", value);
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

        CheckDailyLogin();
        RegenerateEnergy();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            // App resumed - check energy regeneration
            RegenerateEnergy();
            CheckDailyLogin();
        }
    }

    // =========================================================
    // ENERGY SYSTEM
    // =========================================================

    /// <summary>
    /// Calculates the current energy based on time elapsed
    /// since the last energy event.
    /// </summary>
    private int CalculateCurrentEnergy()
    {
        int savedEnergy = GetInt("Energy", _maxEnergy);
        long lastTime = LastEnergyTime;

        if (lastTime == 0 || savedEnergy >= _maxEnergy) return savedEnergy;

        long currentTime = GetCurrentUnixTime();
        long elapsedSeconds = currentTime - lastTime;

        int energyToRegen = (int)(elapsedSeconds / _energyRegenSeconds);
        int newEnergy = Mathf.Min(savedEnergy + energyToRegen, _maxEnergy);

        // Update the last energy time to account for regenerated energy
        if (energyToRegen > 0)
        {
            LastEnergyTime = lastTime + (energyToRegen * _energyRegenSeconds);
            SetInt("Energy", newEnergy);
        }

        return newEnergy;
    }

    /// <summary>
    /// Forces energy regeneration check and updates UI.
    /// </summary>
    public void RegenerateEnergy()
    {
        int currentEnergy = CalculateCurrentEnergy();
        UIManager.Instance?.UpdateEnergyDisplay();
    }

    /// <summary>
    /// Gets the remaining time until the next energy point regenerates.
    /// </summary>
    /// <returns>Time in seconds until next energy regen</returns>
    public int GetEnergyRegenTimeRemaining()
    {
        if (CurrentEnergy >= _maxEnergy) return 0;

        long lastTime = LastEnergyTime;
        if (lastTime == 0) return _energyRegenSeconds;

        long currentTime = GetCurrentUnixTime();
        long elapsed = currentTime - lastTime;
        long remainder = elapsed % _energyRegenSeconds;

        return (int)(_energyRegenSeconds - remainder);
    }

    /// <summary>
    /// Checks if the player has enough energy to start a game.
    /// </summary>
    public bool CanConsumeEnergy() => CurrentEnergy > 0;

    /// <summary>
    /// Consumes one energy point when starting a level.
    /// </summary>
    public void ConsumeEnergy()
    {
        int current = CalculateCurrentEnergy();
        if (current > 0)
        {
            SetInt("Energy", current - 1);
            LastEnergyTime = GetCurrentUnixTime();
            TotalGamesPlayed = TotalGamesPlayed + 1;
            SaveAll();
            Debug.Log($"[SaveSystem] Energy consumed: {current - 1}/{_maxEnergy}");
        }
    }

    /// <summary>
    /// Adds energy points (from rewards or ads).
    /// </summary>
    public void AddEnergy(int amount)
    {
        int current = CalculateCurrentEnergy();
        SetInt("Energy", Mathf.Min(current + amount, _maxEnergy));
        SaveAll();
    }

    // =========================================================
    // COINS SYSTEM
    // =========================================================

    /// <summary>
    /// Adds coins to the player's balance.
    /// </summary>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        Coins = Coins + amount;
        UIManager.Instance?.UpdateCoinsDisplay();
        SaveAll();
        Debug.Log($"[SaveSystem] Coins added: +{amount} (Total: {Coins})");
    }

    /// <summary>
    /// Removes coins from the player's balance.
    /// </summary>
    /// <returns>True if successful, false if insufficient funds</returns>
    public bool RemoveCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins = Coins - amount;
        UIManager.Instance?.UpdateCoinsDisplay();
        SaveAll();
        Debug.Log($"[SaveSystem] Coins removed: -{amount} (Total: {Coins})");
        return true;
    }

    // =========================================================
    // LEVEL PROGRESSION
    // =========================================================

    /// <summary>
    /// Unlocks a level and updates the highest unlocked level.
    /// </summary>
    public void UnlockLevel(int levelNumber)
    {
        int nextLevel = levelNumber + 1;
        if (nextLevel > HighestUnlockedLevel)
        {
            HighestUnlockedLevel = nextLevel;
            LevelManager.Instance?.SetHighestUnlockedLevel(nextLevel);
        }
        TotalVictories = TotalVictories + 1;
        SaveAll();
    }

    /// <summary>
    /// Gets the highest level the player has unlocked.
    /// </summary>
    public int GetHighestUnlockedLevel() => HighestUnlockedLevel;

    // =========================================================
    // DAILY LOGIN SYSTEM
    // =========================================================

    /// <summary>
    /// Checks if the player has logged in today and awards daily rewards.
    /// Tracks consecutive login days for bonus rewards.
    /// </summary>
    public void CheckDailyLogin()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string lastLogin = LastLoginDate;

        if (lastLogin == today) return; // Already logged in today

        if (lastLogin == "")
        {
            // First time playing
            ConsecutiveLoginDays = 1;
        }
        else
        {
            // Check if it's the next day
            DateTime lastDate = DateTime.Parse(lastLogin);
            DateTime todayDate = DateTime.Parse(today);
            int dayDiff = (todayDate - lastDate).Days;

            if (dayDiff == 1)
            {
                // Consecutive day
                ConsecutiveLoginDays = ConsecutiveLoginDays + 1;
            }
            else if (dayDiff > 1)
            {
                // Streak broken
                ConsecutiveLoginDays = 1;
            }
        }

        // Award daily login reward
        int reward = _dailyLoginReward + (ConsecutiveLoginDays - 1) * _consecutiveDayBonus;
        // Cap the reward at a reasonable maximum
        reward = Mathf.Min(reward, 500);

        AddCoins(reward);
        LastLoginDate = today;
        SaveAll();

        Debug.Log($"[SaveSystem] Daily login reward: Day {ConsecutiveLoginDays}, +{reward} coins");
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    /// <summary>
    /// Saves music volume setting.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        SaveAll();
    }

    /// <summary>
    /// Saves SFX volume setting.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        SaveAll();
    }

    // =========================================================
    // DATA PERSISTENCE
    // =========================================================

    /// <summary>
    /// Gets an integer from PlayerPrefs with a default value.
    /// Uses simple XOR obfuscation for basic data protection.
    /// </summary>
    private int GetInt(string key, int defaultValue)
    {
        int obfuscated = PlayerPrefs.GetInt($"MW_{key}", defaultValue ^ 0x5A);
        return obfuscated ^ 0x5A;
    }

    /// <summary>
    /// Saves an integer to PlayerPrefs with XOR obfuscation.
    /// </summary>
    private void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt($"MW_{key}", value ^ 0x5A);
    }

    /// <summary>
    /// Gets a float from PlayerPrefs with a default value.
    /// </summary>
    private float GetFloat(string key, float defaultValue)
    {
        return PlayerPrefs.GetFloat($"MW_{key}", defaultValue);
    }

    /// <summary>
    /// Saves a float to PlayerPrefs.
    /// </summary>
    private void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat($"MW_{key}", value);
    }

    /// <summary>
    /// Gets a string from PlayerPrefs with a default value.
    /// </summary>
    private string GetString(string key, string defaultValue)
    {
        return PlayerPrefs.GetString($"MW_{key}", defaultValue);
    }

    /// <summary>
    /// Saves a string to PlayerPrefs.
    /// </summary>
    private void SetString(string key, string value)
    {
        PlayerPrefs.SetString($"MW_{key}", value);
    }

    /// <summary>
    /// Gets a long value from PlayerPrefs.
    /// </summary>
    private long GetLong(string key, long defaultValue)
    {
        return long.Parse(PlayerPrefs.GetString($"MW_{key}", defaultValue.ToString()));
    }

    /// <summary>
    /// Saves a long value to PlayerPrefs.
    /// </summary>
    private void SetLong(string key, long value)
    {
        PlayerPrefs.SetString($"MW_{key}", value.ToString());
    }

    /// <summary>
    /// Saves all data to PlayerPrefs immediately.
    /// </summary>
    public void SaveAll()
    {
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Clears all saved data. Used for debug/reset purposes.
    /// </summary>
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("[SaveSystem] All data reset");
    }

    // =========================================================
    // UTILITY
    // =========================================================

    /// <summary>
    /// Gets the current Unix timestamp.
    /// </summary>
    private long GetCurrentUnixTime()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }
}
