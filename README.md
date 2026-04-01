# Map Wars: Tactical Conquest

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3_LTS-blue?logo=unity" alt="Unity Version">
  <img src="https://img.shields.io/badge/C%23-.NET_Standard_2.1-purple?logo=csharp" alt="C#">
  <img src="https://img.shields.io/badge/Platform-Android_7.0%2B-green?logo=android" alt="Android">
  <img src="https://img.shields.io/badge/Type-Hyper--Casual-orange" alt="Genre">
  <img src="https://img.shields.io/badge/License-MIT-brightgreen" alt="License">
</p>

## Map Wars - Tactical Conquest

**🎮 لعبة استراتيجية إدمانية للاعبين الأندرويد**

لعبة موبايل من نوع Hyper-Casual تعتمد على السيطرة على قواعد (دوائر) عبر إرسال الجنود لاحتلال قواعد الخصم.

---

## Screenshots

> *(أضف لقطات الشاشة هنا)*

---

## Features

### Core Gameplay
- **Node System**: 4 node types (Small, Medium, Large, Fortress) with different production rates
- **Auto-Production**: Soldiers auto-generate at 1 soldier/second (scaled by node type)
- **Drag-to-Attack**: Intuitive drag gesture to send 50% of soldiers to target nodes
- **Real-time Combat**: Troops travel as projectiles with visual trails

### AI Opponent
- **4 Difficulty Levels**: Easy, Normal, Hard, Nightmare
- **Strategic AI**: Prioritizes defense → expansion → offense → consolidation
- **Adaptive Behavior**: AI reacts to node captures and adjusts strategy

### Visual Effects
- **Minimalist Neon Style**: Dark background with glowing neon colors
- **Particle System**: Object-pooled particles for captures, impacts, and boosts
- **Haptic Feedback**: Android Vibrator API with event-specific patterns
- **Audio System**: Pooled audio sources with SFX variation

### Monetization (Balanced)
- **Rewarded Ads**: "Military Support" (+20 soldiers) and Energy Refill (+5)
- **Interstitial Ads**: Only after every 3 levels (non-intrusive)
- **Skin Shop**: Gold coins for node and troop cosmetic skins
- **IAP Ready**: Coin pack purchases (optional)

### Progression
- **5 Tutorial Levels**: Hand-crafted progressive difficulty
- **Procedural Generation**: Infinite levels after level 5
- **Daily Login Rewards**: Consecutive day bonuses
- **3-Star Rating**: Speed-based performance scoring
- **Energy System**: 10 max energy, regenerates every 5 minutes

---

## Project Structure

```
Assets/Scripts/
├── Core/                    # Core game systems
│   ├── GameManager.cs       # Central game logic & state management
│   ├── LevelManager.cs      # Level configs & procedural generation
│   └── InputHandler.cs      # Touch/mouse input handling
├── Gameplay/                # Game mechanics
│   ├── NodeController.cs    # Node behavior & soldier production
│   └── TroopProjectile.cs   # Projectile movement & collision
├── AI/
│   └── AIController.cs      # AI decision-making system
├── UI/
│   └── UIManager.cs         # All UI screens & HUD
├── Monetization/
│   ├── MonetizationManager.cs  # Ads & IAP management
│   └── SkinManager.cs        # Cosmetic skin system
├── Effects/
│   ├── EffectsManager.cs    # Particle effects (object pooled)
│   ├── HapticFeedbackManager.cs  # Vibration patterns
│   ├── AudioManager.cs      # Music & SFX management
│   └── GridBackground.cs    # Tactical grid background
├── Data/
│   └── SaveSystem.cs        # PlayerPrefs persistence
├── Config/
│   ├── LevelDataSO.cs       # ScriptableObject level data
│   └── ScreenAdapter.cs     # Screen size compatibility
└── Editor/
    ├── MapWarsSetup.cs      # One-click project setup
    └── NodeCreator.cs       # Node prefab creation tool
```

---

## Getting Started

### Prerequisites
- **Unity 2022.3 LTS** or newer
- **Android SDK** with API Level 24+
- **TextMeshPro** (install via Package Manager)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/MapWars-TacticalConquest.git
   cd MapWars-TacticalConquest
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Add project from existing folder
   - Select the cloned folder

3. **Run Auto Setup**
   - In Unity, go to **Map Wars > Setup Game**
   - Click **Full Setup (Recommended)**
   - This creates all required GameObjects, layers, and tags

4. **Create Prefabs**
   - Go to **Map Wars > Create Node Prefab**
   - Click **Create All Node Prefabs**
   - Create a troop projectile prefab manually

5. **Assign References**
   - Select GameManager in hierarchy
   - Assign Node Prefab and Troop Prefab in Inspector
   - Assign references for all managers

6. **Build**
   - File > Build Settings > Android
   - Switch platform if needed
   - Build & Run

---

## Technical Specifications

| Setting | Value |
|---------|-------|
| Engine | Unity 2022.3 LTS |
| Language | C# (.NET Standard 2.1) |
| Scripting Backend | IL2CPP |
| Target API | Android 7.0 (API 24) |
| Min SDK | 24 |
| Target SDK | 34 |
| Architecture | ARM64 |
| Code Stripping | R8 (Release) |
| Target FPS | 60 |
| Target Size | < 50 MB |

---

## Ad Integration

The project includes placeholder implementations for:

- **Unity Ads** (recommended)
- **Google AdMob**

To enable real ads:
1. Install the ads SDK via Package Manager
2. Replace placeholder methods in `MonetizationManager.cs` with actual SDK calls
3. Set your Game ID and Placement IDs
4. Disable test mode in production

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Credits

- Developed with Unity Engine
- Architecture by Z.ai
