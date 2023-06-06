using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static public class Shop
{

  public enum UpgradeType
  {
    NONE,

    TRI_SHOT,
    TRI_SHOT_COUNTER,

    ARROW_RAIN,
    ARROW_RAIN_COUNTER,

    ARROW_PENETRATION,
    ARROW_PENETRATION_COUNTER,

    ARROW_STRENGTH,
    AMMO_MAX,
    HEALTH,
  }
  struct Upgrade
  {

    // Type
    public UpgradeType _Type;

    // Amount in inv
    public int _Count, _CountStart,
      _MaxPurchases;
    public UpgradeType _CountUpgrade;
    public bool _Enabled;

    // Shop prices
    public int[] _Costs;

    // Display
    public bool _Active;
    public MeshRenderer[] _Counters;
    public string _Desc { get { return GetUpgradeDesc(_Type, _Count); } }

    // Function
    public System.Action<UpgradeType> _OnPurchase;
    public System.Func<Upgrade, int> _GetRealCostIter;

    // Functions
    public int GetCurrentCost()
    {
      if (_Costs == null)
      {
        Debug.LogWarning($"No costs for {_Type}");
        return 0;
      }
      return _GetRealCostIter != null ? _Costs[_GetRealCostIter.Invoke(this)] : _Costs[_Count];
    }
  }
  static Dictionary<UpgradeType, Upgrade> s_upgrades;
  static UpgradeType[] s_upgradesActive;
  static List<UpgradeType> s_shopSelections;

  // Load upgrades for a run / save
  public static void Load()
  {
    var savepre = "save0";

    // Money
    PlayerScript.SetCoins(PlayerPrefs.GetInt($"{savepre}_coins", 0));

    // Has won the game before
    GameScript.s_HasWon = PlayerPrefs.GetInt($"{savepre}_won0", 0) == 1 ? true : false;

    // Health
    GameScript.SetHealth(PlayerPrefs.GetInt($"{savepre}_health", 2));

    // Wave
    Wave.s_MetaWaveIter = PlayerPrefs.GetInt($"{savepre}_wave", 0);

    /// Upgrades

    // Cost
    int[] GetUpgradeCosts(UpgradeType upgradeType)
    {

      switch (upgradeType)
      {

        //case UpgradeType.TRI_SHOT:
        //  return new int[] { 0 };
        case UpgradeType.TRI_SHOT_COUNTER:
        case UpgradeType.ARROW_RAIN_COUNTER:
          return new int[] { 0, 100, 200, 350, 500, 750, 900, 1200 };
        case UpgradeType.HEALTH:
          return new int[] { 150, 300 };
        case UpgradeType.ARROW_STRENGTH:
          return new int[] { 150, 250, 350, 500, 750 };

        default:
          return null;

      }

    }

    //
    int GetCountStart(UpgradeType upgradeType)
    {
      switch (upgradeType)
      {

        case UpgradeType.TRI_SHOT_COUNTER:
          return 10;

        case UpgradeType.ARROW_RAIN_COUNTER:
          return 25;

        default:
          return 0;

      }
    }

    //
    int GetMaxPurchases(UpgradeType upgradeType)
    {
      switch (upgradeType)
      {

        case UpgradeType.TRI_SHOT:
          return 2;
        case UpgradeType.TRI_SHOT_COUNTER:
          return 7;

        case UpgradeType.ARROW_RAIN:
          return 3;
        case UpgradeType.ARROW_RAIN_COUNTER:
          return 5;

        case UpgradeType.ARROW_STRENGTH:
          return 5;
        case UpgradeType.AMMO_MAX:
          return 2;

        default:
          return 0;

      }
    }

    // Load
    s_upgrades = new Dictionary<UpgradeType, Upgrade>();
    foreach (UpgradeType upgradeType in System.Enum.GetValues(typeof(UpgradeType)))
    {

      var count = PlayerPrefs.GetInt($"{savepre}_{upgradeType}", 0);
      s_upgrades.Add(upgradeType, new Upgrade()
      {
        _Type = upgradeType,
        _Count = count,
        _CountStart = GetCountStart(upgradeType),
        _MaxPurchases = GetMaxPurchases(upgradeType),

        _CountUpgrade = UpgradeType.NONE,

        _Costs = GetUpgradeCosts(upgradeType)
      });

      //Debug.Log($"Loaded {upgradeType}: {count}");
    }

    // Custom
    void UpdateUpgrade(UpgradeType upgradeType, System.Func<Upgrade, Upgrade> upgradeUpdate)
    {
      var upgrade = s_upgrades[upgradeType];
      upgrade = upgradeUpdate.Invoke(upgrade);
      s_upgrades[upgradeType] = upgrade;
    }

    System.Action<UpgradeType>
      PurchaseFunction_CountPlus = null;
    System.Func<Upgrade, int> RealCountIndexFunction_Basic = null;
    System.Func<Upgrade, Upgrade>
      UpdateFunction_CountPlus = null,
      UpdateFunction_CountPlusOnPurchase = null,
      UpdateFunction_DeincrementActive = null,
      UpdateFunction_RemoveMaxPurchases = null;

    UpdateFunction_CountPlus = (Upgrade u) =>
    {
      u._Count++;
      return u;
    };
    UpdateFunction_CountPlusOnPurchase = (Upgrade u) =>
    {
      u._OnPurchase += PurchaseFunction_CountPlus;
      return u;
    };

    UpdateFunction_DeincrementActive = (Upgrade u) =>
    {
      if (u._Counters == null) return u;
      u._Count = GetUpgradeCountMax(u._Type) - 1;
      return u;
    };
    UpdateFunction_RemoveMaxPurchases = (Upgrade u) =>
    {
      if (u._MaxPurchases != 0 && Mathf.Abs(u._CountStart - u._Count) >= u._MaxPurchases)
        s_shopSelections.Remove(u._Type);
      return u;
    };

    PurchaseFunction_CountPlus = (UpgradeType upgradeType) =>
    {
      UpdateUpgrade(upgradeType, UpdateFunction_CountPlus);
    };

    RealCountIndexFunction_Basic = (Upgrade u) =>
    {
      var count = GetUpgradeCountMax(u._Type);
      if (count == 0)
        return 0;
      return u._CountStart - count + 1;
    };

    void UpdateNormalUpgrade(UpgradeType upgradeType, UpgradeType counterUpgrade)
    {
      UpdateUpgrade(upgradeType, (Upgrade u) =>
      {
        u._CountUpgrade = counterUpgrade;

        u._OnPurchase = PurchaseFunction_CountPlus;
        u._OnPurchase += (UpgradeType upgradeType) =>
        {

          UpdateUpgrade(upgradeType, (Upgrade u_) =>
          {

            // If first purchase; give counter as well
            if (u_._Count == 1)
            {

              // Assign counter
              UpdateUpgrade(counterUpgrade, (Upgrade su) =>
              {
                su._Count = su._CountStart;
                return su;
              });
              TryPurchase(counterUpgrade);

              // Add to shop to purchase
              s_shopSelections.Add(counterUpgrade);
            }

            else if (u_._Count >= GetMaxPurchases(upgradeType))
            {
              s_shopSelections.Remove(upgradeType);
            }

            return u_;
          });

        };

        return u;
      });

      UpdateUpgrade(counterUpgrade, (Upgrade u) =>
      {
        u._Active = true;

        //
        u._OnPurchase += (UpgradeType upgradeType) =>
        {

          // Deincrement counter and remove from shop selections if reached max purchases
          UpdateUpgrade(upgradeType, UpdateFunction_DeincrementActive);
          UpdateUpgrade(upgradeType, UpdateFunction_RemoveMaxPurchases);
        };

        u._GetRealCostIter = RealCountIndexFunction_Basic;

        return u;
      });
    }

    // Active upgrades
    UpdateNormalUpgrade(UpgradeType.TRI_SHOT, UpgradeType.TRI_SHOT_COUNTER);
    UpdateNormalUpgrade(UpgradeType.ARROW_RAIN, UpgradeType.ARROW_RAIN_COUNTER);

    // Shoot strength
    UpdateUpgrade(UpgradeType.ARROW_STRENGTH, UpdateFunction_CountPlusOnPurchase);
    UpdateUpgrade(UpgradeType.ARROW_STRENGTH, (Upgrade u) =>
    {
      //
      u._OnPurchase += (UpgradeType upgradeType) =>
      {

        // Remove from shop selections if reached max purchases
        UpdateUpgrade(upgradeType, UpdateFunction_RemoveMaxPurchases);
      };

      return u;
    });

    // Health
    UpdateUpgrade(UpgradeType.HEALTH, (Upgrade u) =>
    {

      // Give player health, then if max health, remove from shop
      u._OnPurchase += (UpgradeType upgradeType) =>
      {
        PlayerScript.AddHealth();
        if (PlayerScript.GetHealth() == 3)
          s_shopSelections.Remove(upgradeType);
      };

      u._GetRealCostIter = (Upgrade u_) =>
      {
        return PlayerScript.GetHealth() - 1;
      };

      return u;
    });

    // Ammo count
    UpdateUpgrade(UpgradeType.AMMO_MAX, (Upgrade u) =>
    {

      // Give player health, then if max health, remove from shop
      u._OnPurchase += (UpgradeType upgradeType) =>
      {
        u._Count++;
        PlayerScript.SetAmmoMax(u._Count + 3);
        PlayerScript.UIUpdateAmmoCounter();

        if (u._Count >= GetMaxPurchases(upgradeType))
        {
          s_shopSelections.Remove(upgradeType);
        }
      };

      u._GetRealCostIter = (Upgrade u_) =>
      {
        return PlayerScript.GetAmmoMax() - 3;
      };

      PlayerScript.SetAmmoMax(u._Count + 3);

      return u;
    });

    //
    BufferActiveUpgrades();

    // Check UI
    foreach (var upgradeType in s_upgradesActive)
    {
      var upgrade_data = s_upgrades[upgradeType];
      if (upgrade_data._Count > 0)
      {
        UpdateUpgradeUI(upgradeType);
      }
    }

    // Add shop selections
    s_shopSelections = new List<UpgradeType>();

    // General
    s_shopSelections.Add(UpgradeType.ARROW_STRENGTH);

    // Active
    if (GetUpgradeCount(UpgradeType.TRI_SHOT) < GetMaxPurchases(UpgradeType.TRI_SHOT))
      s_shopSelections.Add(UpgradeType.TRI_SHOT);
    if (GetUpgradeCount(UpgradeType.TRI_SHOT) > 0)
      if (s_upgrades[UpgradeType.TRI_SHOT_COUNTER]._GetRealCostIter.Invoke(s_upgrades[UpgradeType.TRI_SHOT_COUNTER]) < GetMaxPurchases(UpgradeType.TRI_SHOT_COUNTER))
        s_shopSelections.Add(UpgradeType.TRI_SHOT_COUNTER);

    if (GetUpgradeCount(UpgradeType.ARROW_RAIN) < GetMaxPurchases(UpgradeType.ARROW_RAIN))
      s_shopSelections.Add(UpgradeType.ARROW_RAIN);
    if (GetUpgradeCount(UpgradeType.ARROW_RAIN) > 0)
      if (s_upgrades[UpgradeType.ARROW_RAIN_COUNTER]._GetRealCostIter.Invoke(s_upgrades[UpgradeType.ARROW_RAIN_COUNTER]) < GetMaxPurchases(UpgradeType.ARROW_RAIN_COUNTER))
        s_shopSelections.Add(UpgradeType.ARROW_RAIN_COUNTER);

    if (GetUpgradeCount(UpgradeType.AMMO_MAX) < GetMaxPurchases(UpgradeType.AMMO_MAX))
      s_shopSelections.Add(UpgradeType.AMMO_MAX);
  }

  // Gather all active upgrades
  static void BufferActiveUpgrades()
  {
    var upgradesActive = new List<UpgradeType>();
    foreach (UpgradeType upgradeType in System.Enum.GetValues(typeof(UpgradeType)))
    {
      if (s_upgrades[upgradeType]._Active)
        upgradesActive.Add(upgradeType);
    }
    s_upgradesActive = upgradesActive.ToArray();
  }

  // Save upgrades for a run / save
  public static void Save()
  {
    var savepre = "save0";

    // Money
    PlayerPrefs.SetInt($"{savepre}_coins", PlayerScript.GetCoins());

    // Health
    PlayerPrefs.SetInt($"{savepre}_health", PlayerScript.GetHealth());

    // Wave
    PlayerPrefs.SetInt($"{savepre}_wave", Wave.s_MetaWaveIter);

    // Upgrades
    foreach (UpgradeType upgradeType in System.Enum.GetValues(typeof(UpgradeType)))
    {
      var upgrade_data = s_upgrades[upgradeType];
      PlayerPrefs.SetInt($"{savepre}_{upgradeType}", upgrade_data._Active ? upgrade_data._Counters?.Length ?? 0 : upgrade_data._Count);
    }
  }

  // Desc
  static string GetUpgradeDesc(UpgradeType upgradeType, int count)
  {
    switch (upgradeType)
    {

      case UpgradeType.TRI_SHOT:
        return count == 0 ? "<b>Unlock:</b>\n<size=45>Tri-Shot</size>" : "<b>Upgrade:</b>\n<size=45>+2 Tri-Shot arrows</size>";

      case UpgradeType.TRI_SHOT_COUNTER:
        return "<b>Upgrade:</b>\n<size=45>Tri-Shot cooldown</size>";

      case UpgradeType.ARROW_RAIN:
        return count == 0 ? "<b>Unlock:</b>\n<size=45>Arrow Rain</size>" : "<b>Upgrade:</b>\n<size=45>+5 Arrow Rain arrows</size>";

      case UpgradeType.ARROW_RAIN_COUNTER:
        return "<b>Upgrade:</b>\n<size=45>Arrow Rain cooldown</size>";

      case UpgradeType.ARROW_STRENGTH:
        return "<b>Upgrade:</b>\n<size=45>+Arrow strength</size>";

      case UpgradeType.HEALTH:
        return "<b>Upgrade:</b>\n<size=45>+1 health</size>";

      case UpgradeType.AMMO_MAX:
        return "<b>Upgrade:</b>\n<size=45>+1 max ammo</size>";

      default:
        return "";
    }
  }

  //
  public static bool TryPurchase(UpgradeType upgrade)
  {
    Debug.Log($"try purchase: {upgrade}");

    var upgrade_data = s_upgrades[upgrade];

    // Check enough money
    var cost = upgrade_data.GetCurrentCost();
    var playerCoins = PlayerScript.GetCoins();
    if (cost > playerCoins)
      return false;

    // Decrease money
    PlayerScript.SetCoins(playerCoins - cost);
    SoundPurchase();

    // Check function
    s_saveCount = upgrade_data._Count;
    upgrade_data._OnPurchase?.Invoke(upgrade);

    // If active upgrade, place skill button
    UpdateUpgradeUI(upgrade, upgrade_data._Active && upgrade_data._Counters != null);

    // Reload active upgrades / prices
    BufferActiveUpgrades();
    UpdateShopPrices();

    return true;
  }

  static int s_saveCount;
  static void UpdateUpgradeUI(UpgradeType upgrade, bool reset = false)
  {
    var upgrade_data = s_upgrades[upgrade];

    if (upgrade_data._Active)
    {
      var counters = upgrade_data._Counters != null ? upgrade_data._Counters[0].transform.parent : null;

      // Check reset
      if (reset)
      {
        for (var i = upgrade_data._Counters.Length - 1; i >= 0; i--)
        {
          GameObject.DestroyImmediate(upgrade_data._Counters[i].gameObject);
        }
        upgrade_data._Counters = null;
      }

      // New UI
      if (upgrade_data._Counters == null)
      {

        // Item icon
        var button_ = GameObject.Find("UI").transform.GetChild(2).GetChild(0);
        if (!reset)
        {

          var button_new = GameObject.Instantiate(button_.gameObject, button_.parent);
          GameObject.Destroy(button_new.transform.GetChild(1).GetChild(0).gameObject);
          counters = button_new.transform.GetChild(1);
          var child_pos = button_new.transform.parent.childCount - 1;
          var start_pos = button_.localPosition;

          button_new.name = $"{upgrade}";
          button_new.transform.localPosition = start_pos + new Vector3(-12f * (child_pos - 1), 0f, 0f);
          button_new.SetActive(true);
        }

        // Set counters
        var counter_ = button_.transform.GetChild(1).GetChild(0);
        upgrade_data._Counters = new MeshRenderer[upgrade_data._Count];
        for (var i = 0; i < upgrade_data._Count; i++)
        {
          var counter_new = GameObject.Instantiate(counter_.gameObject, counters);

          // Size
          var cSpacing = 0.08f;
          var cSize = 0.05f;
          /*
          var cSpacing = Mathf.LerpUnclamped(0.124f, 0.04f, (upgrade_data._Count - 8f) / 17f);
          var cSize = Mathf.LerpUnclamped(0.115f, 0.035f, (upgrade_data._Count - 8f) / 17f);
          */
          if (upgrade_data._Count < 12) { }
          else // if (upgrade_data._Count < 26)
          {
            cSpacing = 0.035f;
            cSize = 0.025f;
          }

          if (cSize != 0.05f)
          {
            counter_new.transform.localScale = new Vector3(cSize, counter_new.transform.localScale.y, counter_new.transform.localScale.z);
          }

          counter_new.transform.localPosition += new Vector3(cSpacing * i, 0f, 0f);

          upgrade_data._Counters[i] = counter_new.GetComponent<MeshRenderer>();
        }

        // Reset count
        upgrade_data._Count = reset ? s_saveCount : 0;

        // Set UI color if count saved
        if (reset && s_saveCount > 0)
        {
          var countIter = 0;
          foreach (var counter in upgrade_data._Counters)
          {
            counter.material.color = s_saveCount >= upgrade_data._Counters.Length ? Color.yellow : countIter < s_saveCount ? Color.white : counter.transform.parent.parent.parent.GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
            countIter++;
          }
        }
      }

      // Save upgrade data
      s_upgrades[upgrade] = upgrade_data;
    }
  }

  //
  public static void IncrementUpgrades(int by = 1)
  {

    // Check active upgrades
    foreach (var upgrade in s_upgradesActive)
    {
      var upgrade_data = s_upgrades[upgrade];
      if (upgrade_data._Counters == null) continue;
      if (upgrade_data._Count >= upgrade_data._Counters.Length) continue;

      IncrementUpgrade(upgrade, by);
    }
  }
  static void IncrementUpgrade(UpgradeType upgrade, int by = 1)
  {

    var upgrade_data = s_upgrades[upgrade];

    // Check sub upgrade
    if (upgrade_data._CountUpgrade != UpgradeType.NONE)
    {
      IncrementUpgrade(upgrade_data._CountUpgrade);
      return;
    }

    // Increment
    upgrade_data._Count += by;
    s_upgrades[upgrade] = upgrade_data;

    // Update UI
    if (upgrade_data._Count >= upgrade_data._Counters.Length)
    {
      foreach (var counter in upgrade_data._Counters)
      {
        counter.material.color = Color.yellow;
      }
    }
    else
      upgrade_data._Counters[upgrade_data._Count - 1].material.color = Color.white;
  }

  // Reset a upgrade's UI
  public static void ResetUpgrade(UpgradeType upgrade)
  {

    var upgrade_data = s_upgrades[upgrade];
    if (upgrade_data._CountUpgrade != UpgradeType.NONE)
    {
      ResetUpgrade(upgrade_data._CountUpgrade);
      return;
    }

    upgrade_data._Count = 0;
    upgrade_data._Enabled = false;
    if (upgrade_data._Active)
    {
      foreach (var counter in upgrade_data._Counters)
      {
        counter.material.color = counter.transform.parent.parent.parent.GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
      }
      var icon = upgrade_data._Counters[0].transform.parent.parent.GetComponent<MeshRenderer>();
      icon.material.color = upgrade_data._Counters[0].transform.parent.parent.parent.GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
    }

    s_upgrades[upgrade] = upgrade_data;
  }

  //
  public static bool UpgradeEnabled(UpgradeType upgradeType)
  {
    if (upgradeType == UpgradeType.NONE) return false;

    var upgrade_data = s_upgrades[upgradeType];
    if (upgrade_data._CountUpgrade != UpgradeType.NONE)
    {
      return UpgradeEnabled(upgrade_data._CountUpgrade);
    }

    return s_upgrades[upgradeType]._Enabled;
  }

  public static int GetUpgradeCount(UpgradeType upgradeType)
  {
    var upgrade_data = s_upgrades[upgradeType];
    return upgrade_data._Count;
  }

  //
  public static int GetSupgradeCount(UpgradeType upgrade)
  {
    var upgrade_data = s_upgrades[upgrade];

    if (upgrade_data._CountUpgrade != UpgradeType.NONE)
    {
      return GetSupgradeCount(upgrade_data._CountUpgrade);
    }

    return upgrade_data._Count;
  }
  static int GetUpgradeCountMax(UpgradeType upgrade)
  {
    var upgrade_data = s_upgrades[upgrade];

    if (upgrade_data._CountUpgrade != UpgradeType.NONE)
    {
      return GetUpgradeCountMax(upgrade_data._CountUpgrade);
    }

    return upgrade_data._Counters?.Length ?? 0;
  }

  // Check shop UI buttons
  public static void ShopInput(string colliderName)
  {

    UpgradeType upgrade;
    if (!System.Enum.TryParse(colliderName, true, out upgrade)) { return; }

    TryPurchase(upgrade);

  }

  // Check upgrade UI buttons
  public static void UpgradeInput(string colliderName)
  {

    // Parse upgrade type
    UpgradeType upgradeType;
    if (!System.Enum.TryParse(colliderName, true, out upgradeType)) return;
    var upgrade_data = s_upgrades[upgradeType];

    // Check count
    if (!upgrade_data._Enabled && GetSupgradeCount(upgradeType) < GetUpgradeCountMax(upgradeType)) return;

    // Check player combination
    if (!upgrade_data._Enabled && !PlayerScript.s_Singleton.RegisterUpgrade(upgradeType)) return;

    // Check unregister
    if (upgrade_data._Enabled)
      PlayerScript.s_Singleton.UnregisterUpgrade(upgradeType, true);

    // Toggle upgrade
    upgrade_data._Enabled = !upgrade_data._Enabled;

    // Save upgrade data
    s_upgrades[upgradeType] = upgrade_data;

    // UI
    var icon = upgrade_data._Counters[0].transform.parent.parent.GetComponent<MeshRenderer>();
    icon.material.color = upgrade_data._Enabled ? Color.green : upgrade_data._Counters[0].transform.parent.parent.parent.GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
  }

  // Set all of the shop's buttons' prices
  public static void UpdateShopPrices()
  {
    var buttons = new GameObject[]{
      MenuManager._shop._menu.transform.GetChild(1).gameObject,
      MenuManager._shop._menu.transform.GetChild(2).gameObject,
      MenuManager._shop._menu.transform.GetChild(3).gameObject
    };
    foreach (var button in buttons)
    {
      button.name = $"empty";
      UpdateShopButton(button, -1);
      button.transform.GetChild(2).GetComponent<TextMesh>().text = "";
    }

    // Custom
    {
      // Check health upgrade
      if (PlayerScript.GetHealth() < 3 && !s_shopSelections.Contains(UpgradeType.HEALTH))
      {
        s_shopSelections.Add(UpgradeType.HEALTH);
      }
    }

    // Assign buttons
    var upgradesCount = Mathf.Clamp(s_shopSelections.Count, 0, 3);
    if (upgradesCount == 0)
    {
      Debug.LogWarning("No upgrades!");
      return;
    }

    // Get random upgrades
    var upgradesAvailable = new List<UpgradeType>(s_shopSelections);
    while (true)
    {

      var upgradeType = upgradesAvailable[Random.Range(0, upgradesAvailable.Count)];
      var upgrade = s_upgrades[upgradeType];
      upgradesAvailable.Remove(upgradeType);
      var button = buttons[upgradesCount - 1];

      // Set button name
      button.name = $"{upgradeType}";

      // Button desc
      button.transform.GetChild(2).GetComponent<TextMesh>().text = $"{upgrade._Desc}";
      if (upgrade._Desc.Length == 0)
      {
        Debug.LogWarning($"No desc set for {upgrade._Type}");
      }

      // Cost
      UpdateShopButton(button, upgrade.GetCurrentCost());

      // Check if any more upgrades to assign
      if (--upgradesCount <= 0) break;
    }
  }

  // Turn off the shop button
  static void OffShopButton(GameObject button)
  {
    var t = button.transform.GetChild(1).GetComponent<TextMesh>();
    t.text = "x-";
    button.GetComponent<MeshRenderer>().material.color = GameObject.Find("BG").GetComponent<MeshRenderer>().material.color * 1.5f;
    button.GetComponent<BoxCollider>().enabled = false;
  }

  // Update a shop button UI using price
  static void UpdateShopButton(GameObject button, int price)
  {

    button.GetComponent<BoxCollider>().enabled = true;
    var t = button.transform.GetChild(1).GetComponent<TextMesh>();
    button.transform.GetChild(1).name = $"{price}";

    // Set text size
    if (price > 999)
    {
      t.characterSize = 0.2f;
    }
    else if (price > 99)
    {
      t.characterSize = 0.25f;
    }

    // Set color
    var r = button.GetComponent<MeshRenderer>();
    if (price == -1 || price > PlayerScript.GetCoins())
    {
      r.material.color = GameObject.Find("BG").GetComponent<MeshRenderer>().material.color * 1.5f;
    }
    else
    {
      r.material.color = GameObject.Find("Ground").transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
    }

    // Set text
    if (price == -1)
      t.text = "-";
    else
      t.text = "x" + price;
  }

  // Show / hide shop menu
  static public void ToggleShop(bool toggle)
  {
    MenuManager._shop.Toggle(toggle);
    if (toggle)
    {
      GameScript._state = GameScript.GameState.SHOP;
      MenuManager._waveSign.SetActive(true);
      MenuManager._waveSign.transform.localPosition = new Vector3(0f, 30.5f, 10f);
      MenuManager._waveSign.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>().text = "" + (Wave.s_MetaWaveIter);
    }
    PlayerScript.SetAmmoForShop();
  }

  // Make a sound when a purchase is completed
  static public void SoundPurchase()
  {
    GameScript.PlaySound(MenuManager._shop._menu.transform.GetChild(0).GetChild(0).gameObject);
  }
}
