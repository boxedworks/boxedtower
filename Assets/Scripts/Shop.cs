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

    BACKUP_ARCHER,
    BACKUP_ARCHER_PROJECTILES,

    ARROW_STRENGTH,
    AMMO_MAX,
    HEALTH,

    CRATE_ARMOR,
    CRATE_INCREMENT,
    ENEMY_SIZED,
  }

  struct Upgrade
  {
    // Type
    public UpgradeType _Type;

    // Amount in inv
    public int _Count;
    public UpgradeType _CountUpgrade;
    public bool _Enabled;

    // Display
    public bool _Active;
    public MeshRenderer[] _Counters;
    public string _Desc
    {
      get { return GetUpgradeDesc(_Type, _Count); }
    }

    // Function
    public System.Action<UpgradeType> _OnPurchase;
    public System.Func<Upgrade, int> _GetRealCostIter;

    // Functions
    public int GetCurrentCost()
    {
      var costs = GetUpgradeCosts(_Type);
      if (costs == null)
      {
        Debug.LogWarning($"No costs for {_Type}");
        return 0;
      }

      var index = GetRealCount();
      if (index >= costs.Length || index < 0)
      {
        Debug.LogWarning(
            $"Invalid index ({index} / {costs.Length}) for {_Type}, realcostiter? {_GetRealCostIter != null}"
        );
        return 0;
      }
      return costs[index];
    }

    public int GetRealCount()
    {
      return _GetRealCostIter?.Invoke(this) ?? _Count;
    }
  }

  // Definition of an upgrade
  struct UpgradeDefinition
  {
    public UpgradeType _UpgradeType;
    public int[] _Costs;
    public System.Func<int, string> _DescriptionFunction;
    public int _MaxPurchases;
    public int _StartCount;
    public (UpgradeType, int)[] _UpgradeUnlocks;
  }

  // Containers
  static Dictionary<UpgradeType, Upgrade> s_upgrades;
  static UpgradeType[] s_upgradesActive;
  static Dictionary<UpgradeType, UpgradeDefinition> s_upgradeDefinitions;
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

    // Reset
    if (s_upgrades != null)
    {

      if (s_upgradesActive != null)
        foreach (var up in s_upgradesActive)
        {
          var upgrade = s_upgrades[up];
          if (upgrade._Counters != null)
          {
            GameObject.Destroy(upgrade._Counters[0].transform.parent.parent.gameObject);
          }
        }

    }

    /// Upgrades
    // Load
    s_upgrades = new Dictionary<UpgradeType, Upgrade>();
    foreach (UpgradeType upgradeType in System.Enum.GetValues(typeof(UpgradeType)))
    {
      var count = 0; //PlayerPrefs.GetInt($"{savepre}_{upgradeType}", 0);
      s_upgrades.Add(
          upgradeType,
          new Upgrade()
          {
            _Type = upgradeType,
            _Count = count,
            _CountUpgrade = UpgradeType.NONE
          }
      );
    }

    // Custom
    void UpdateUpgrade(UpgradeType upgradeType, System.Func<Upgrade, Upgrade> upgradeUpdate)
    {
      var upgrade = s_upgrades[upgradeType];
      upgrade = upgradeUpdate.Invoke(upgrade);
      s_upgrades[upgradeType] = upgrade;
    }

    System.Action<UpgradeType> PurchaseFunction_CountPlus = null;
    System.Func<Upgrade, int> RealCountIndexFunction_Basic = null;
    System.Func<Upgrade, Upgrade> UpdateFunction_CountPlus = null,
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
      if (u._Counters == null)
        return u;
      u._Count = GetUpgradeCountMax(u._Type) - 1;
      return u;
    };
    UpdateFunction_RemoveMaxPurchases = (Upgrade u) =>
    {
      var count = u.GetRealCount();
      var countStart = GetCountStart(u._Type);
      if (countStart == -1)
        countStart = 0;
      var maxPurchases = GetMaxPurchases(u._Type);
      if (maxPurchases != 0 && count >= maxPurchases)
      {
        s_shopSelections.Remove(u._Type);
        Debug.Log($"Removed {u._Type} from shop count ({count}) start ({countStart}) maxPurchases ({maxPurchases})");
      }
      return u;
    };

    PurchaseFunction_CountPlus = (UpgradeType upgradeType) =>
    {
      UpdateUpgrade(upgradeType, UpdateFunction_CountPlus);
    };

    RealCountIndexFunction_Basic = (Upgrade u) =>
    {
      var count = GetUpgradeCountMax(u._Type);
      var countStart = GetCountStart(u._Type);
      if (count == 0)
        return 0;
      return countStart - count + 1;
    };

    void LinkActiveUpgrade(UpgradeType upgradeType, UpgradeType counterUpgrade)
    {
      UpdateUpgrade(
          upgradeType,
          (Upgrade u) =>
          {
            u._CountUpgrade = counterUpgrade;

            u._OnPurchase += (UpgradeType upgradeType) =>
            {
              UpdateUpgrade(
                upgradeType,
                (Upgrade u_) =>
              {
                // If first purchase; give counter as well
                if (u_._Count == 1)
                {
                  // Assign counter
                  UpdateUpgrade(
                    counterUpgrade,
                    (Upgrade su) =>
                    {
                      var countStart = GetCountStart(counterUpgrade);
                      su._Count = countStart;
                      return su;
                    }
                  );
                  TryPurchase(counterUpgrade);

                  // Add to shop to purchase
                  s_shopSelections.Add(counterUpgrade);
                }

                return u_;
              });
            };

            return u;
          }
      );

      UpdateUpgrade(
        counterUpgrade,
        (Upgrade u) =>
      {
        u._Active = true;

        //
        u._OnPurchase += (UpgradeType upgradeType) =>
        {
          // Deincrement counter on purchase
          UpdateUpgrade(upgradeType, UpdateFunction_DeincrementActive);
        };

        u._GetRealCostIter = RealCountIndexFunction_Basic;

        return u;
      });
    }

    // Descs
    string GetActiveSkillDesc(string skillName, string desc, int count)
    {
      return @$"<b><color=yellow>Unlock: [{skillName}]</color></b>
====================

<color=grey>Desc:</color> {desc}

<color=grey>Type:</color> Active
<color=grey>Cooldown:</color> {count}";
    }

    string GetPassiveSkillDesc(string skillName, string desc)
    {
      return @$"<b><color=yellow>Unlock: [{skillName}]</color></b>
====================

<color=grey>Desc:</color> {desc}

<color=grey>Type:</color> Passive";
    }

    string GetUpgradeDesc(string name, string desc, string c0, string c1, string subName = "")
    {
      return @$"<b><color=green>Upgrade: [{name}] {subName}</color></b>
====================

{desc}:

{c0} -> {c1}";
    }

    // Super function
    s_upgradeDefinitions = new Dictionary<UpgradeType, UpgradeDefinition>();
    void RegisterUpgradeDefinition(
        UpgradeType upgradeType,
        int[] costs,
        System.Func<int, string> descriptionFunction,
        int start_count = -1,
        (UpgradeType, int)[] unlocks = null,
        int max_purchases = -1,

        bool increment_on_purchase = true,
        bool remove_on_max_purchase = true
    )
    {
      // Append to dict
      s_upgradeDefinitions.Add(
          upgradeType,
          new UpgradeDefinition()
          {
            _UpgradeType = upgradeType,
            _Costs = costs,
            _DescriptionFunction = descriptionFunction,
            _MaxPurchases = max_purchases == -1 ? costs.Length : max_purchases,
            _StartCount = start_count,
            _UpgradeUnlocks = unlocks
          }
      );

      // Increment on purchase
      if (increment_on_purchase)
      {
        var upgrade = s_upgrades[upgradeType];
        var purchaseFunctions = upgrade._OnPurchase;
        upgrade._OnPurchase = null;
        s_upgrades[upgradeType] = upgrade;

        UpdateUpgrade(upgradeType, UpdateFunction_CountPlusOnPurchase);

        upgrade = s_upgrades[upgradeType];
        upgrade._OnPurchase += purchaseFunctions;
        s_upgrades[upgradeType] = upgrade;
      }

      // Remove on max purchase
      if (remove_on_max_purchase)
      {
        UpdateUpgrade(
          upgradeType,
          (Upgrade u) =>
          {
            //
            u._OnPurchase += (UpgradeType upgradeType) =>
            {
              // Remove from shop selections if reached max purchases
              UpdateUpgrade(upgradeType, UpdateFunction_RemoveMaxPurchases);
            };

            return u;
          });
      }

      // Pair upgrades per unlock
      if (unlocks != null)
      {
        foreach (var unlock in unlocks)
        {

          var unlock_skill = unlock.Item1;
          var unlock_level = unlock.Item2;
          UpdateUpgrade(
            upgradeType,
            (Upgrade u) =>
            {
              //
              u._OnPurchase += (UpgradeType upgradeType) =>
              {
                // Add unlock to shop
                var count = s_upgrades[upgradeType]._Count;
                if (count == unlock_level)
                {
                  s_shopSelections.Add(unlock_skill);
                }
              };

              return u;
            });
        }
      }
    }

    // Active upgrades
    LinkActiveUpgrade(UpgradeType.TRI_SHOT, UpgradeType.TRI_SHOT_COUNTER);
    RegisterUpgradeDefinition(
        UpgradeType.TRI_SHOT,
        new int[] { 250, 2000 },
        (int upgradeLevel) =>
        {
          return upgradeLevel == 0
            ? GetActiveSkillDesc(
                "Split Shot",
                "Fire multiple (3) arrows at once",
                GetCountStart(UpgradeType.TRI_SHOT_COUNTER)
            )
            : GetUpgradeDesc(
                "Split Shot",
                "Increased number of arrows",
                $"{3 + (upgradeLevel - 1) * 2}",
                $"{3 + (upgradeLevel) * 2}",
                "projectiles"
            );
        }
    );

    RegisterUpgradeDefinition(
        UpgradeType.TRI_SHOT_COUNTER,
        new int[] { 0, 250, 500, 1000, 2000, 3000 },
        (int upgradeLevel) =>
        {
          return GetUpgradeDesc(
            "Split Shot",
            "Decrease cooldown",
            $"{GetCountStart(UpgradeType.TRI_SHOT_COUNTER) - s_upgrades[UpgradeType.TRI_SHOT_COUNTER].GetRealCount()}",
            $"{GetCountStart(UpgradeType.TRI_SHOT_COUNTER) - s_upgrades[UpgradeType.TRI_SHOT_COUNTER].GetRealCount() - 1}",
            "cooldown"
          );
        },
        15
    );

    LinkActiveUpgrade(UpgradeType.ARROW_RAIN, UpgradeType.ARROW_RAIN_COUNTER);
    RegisterUpgradeDefinition(
      UpgradeType.ARROW_RAIN,
      new int[] { 400, 1000, 2500 },
        (int upgradeLevel) =>
        {
          return upgradeLevel == 0
            ? GetActiveSkillDesc(
                "Arrow Rain",
                "Arrows (10) rain from the sky at a marked position",
                GetCountStart(UpgradeType.ARROW_RAIN_COUNTER)
            )
            : GetUpgradeDesc(
                "Arrow Rain",
                "Increased number of arrows",
                $"{10 + (upgradeLevel - 1) * 5}",
                $"{10 + (upgradeLevel) * 5}",
                "projectiles"
            );
        }
    );

    RegisterUpgradeDefinition(
      UpgradeType.ARROW_RAIN_COUNTER,
      new int[] { 0, 450, 800, 1250, 2000 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Arrow Rain",
          "Decrease cooldown",
            $"{GetCountStart(UpgradeType.ARROW_RAIN_COUNTER) - s_upgrades[UpgradeType.ARROW_RAIN_COUNTER].GetRealCount()}",
            $"{GetCountStart(UpgradeType.ARROW_RAIN_COUNTER) - s_upgrades[UpgradeType.ARROW_RAIN_COUNTER].GetRealCount() - 1}",
          "cooldown"
        );
      },
      30
    );

    LinkActiveUpgrade(UpgradeType.ARROW_PENETRATION, UpgradeType.ARROW_PENETRATION_COUNTER);
    RegisterUpgradeDefinition(
      UpgradeType.ARROW_PENETRATION,
      new int[] { 350, 700, 1250 },
      (int upgradeLevel) =>
      {
        return upgradeLevel == 0
          ? GetActiveSkillDesc(
            "Pierce Shot",
            "Fire a piercing arrow",
            GetCountStart(UpgradeType.ARROW_PENETRATION_COUNTER)
          )
          : GetUpgradeDesc(
            "Pierce Shot",
            "Increased projectile size",
            $"{(upgradeLevel - 1) * 100}%",
            $"{(upgradeLevel) * 100}%",
            "size"
          );
      }
    );

    RegisterUpgradeDefinition(
      UpgradeType.ARROW_PENETRATION_COUNTER,
      new int[] { 0, 500, 850 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Piece Shot",
          "Decrease cooldown",
            $"{GetCountStart(UpgradeType.ARROW_PENETRATION_COUNTER) - s_upgrades[UpgradeType.ARROW_PENETRATION_COUNTER].GetRealCount()}",
            $"{GetCountStart(UpgradeType.ARROW_PENETRATION_COUNTER) - s_upgrades[UpgradeType.ARROW_PENETRATION_COUNTER].GetRealCount() - 1}",
          "cooldown"
        );
      },
      15
    );

    // Backup archer
    RegisterUpgradeDefinition(
      UpgradeType.BACKUP_ARCHER,
      new int[] { 300, 650, 1000, 1500, 2500 },
      (int upgradeLevel) =>
      {
        return upgradeLevel == 0
          ? GetPassiveSkillDesc(
            "Backup Archer",
            "Increasing the combo meter has a chance (20%) to randomly fire an arrow"
          )
          : GetUpgradeDesc(
            "Backup Archer",
            "Arrow chance",
            $"{upgradeLevel * 20}%",
            $"{(upgradeLevel + 1) * 20}%",
            "chance"
          );
      },
      -1,
      new (UpgradeType, int)[] { (UpgradeType.BACKUP_ARCHER_PROJECTILES, 1) }
    );

    RegisterUpgradeDefinition(
      UpgradeType.BACKUP_ARCHER_PROJECTILES,
      new int[] { 500, 1100, 1500 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Backup Archer",
          "On fire, chance to spawn a 2nd arrow",
          $"{upgradeLevel * 10}%",
          $"{(upgradeLevel + 1) * 10}%",
          "projectiles"
        );
      }
    );

    // Shoot strength
    RegisterUpgradeDefinition(
      UpgradeType.ARROW_STRENGTH,
      new int[] { 250, 500, 1250, 2000, 2500 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Arrow Strength",
          "Arrow max shoot strength",
          $"+{upgradeLevel * 10}%",
          $"+{(upgradeLevel + 1) * 10}%"
        );
      }
    );

    // Enemy size
    RegisterUpgradeDefinition(
      UpgradeType.ENEMY_SIZED,
      new int[] { 1500 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Growth Ray",
          "Smallest enemies are larger",
          $"+{upgradeLevel * 30}%",
          $"+{(upgradeLevel + 1) * 30}%"
        );
      }
    );

    // Health
    UpdateUpgrade(
        UpgradeType.HEALTH,
        (Upgrade u) =>
        {
          // Give player health, then if max health, remove from shop
          u._OnPurchase += (UpgradeType upgradeType) =>
          {
            PlayerScript.AddHealth();
            if (PlayerScript.GetHealth() == 3)
              s_shopSelections.Remove(upgradeType);

            GameScript.s_NumHealthBuys++;

            // Increase costs
            for (var i = 0; i < s_upgradeDefinitions[u._Type]._Costs.Length; i++)
            {
              s_upgradeDefinitions[u._Type]._Costs[i] += 500;
            }
          };

          u._GetRealCostIter = (Upgrade u_) =>
          {
            return PlayerScript.GetHealth() - 1;
          };

          return u;
        }
    );

    RegisterUpgradeDefinition(
      UpgradeType.HEALTH,
      new int[] { 250, 500 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Health",
          "Health",
          $"{PlayerScript.GetHealth()}",
          $"{PlayerScript.GetHealth() + 1}"
        );
      },

      -1,
      null,
      -1,
      false,
      false
    );

    // Ammo count
    UpdateUpgrade(
        UpgradeType.AMMO_MAX,
        (Upgrade u) =>
        {
          // Give player health, then if max health, remove from shop
          u._OnPurchase += (UpgradeType upgradeType) =>
          {
            u._Count++;
            PlayerScript.SetAmmoMax(u._Count + 3);
            PlayerScript.UIUpdateAmmoCounter();
          };

          u._GetRealCostIter = (Upgrade u_) =>
          {
            return PlayerScript.GetAmmoMax() - 3;
          };

          PlayerScript.SetAmmoMax(u._Count + 3);

          return u;
        }
    );

    RegisterUpgradeDefinition(
      UpgradeType.AMMO_MAX,
      new int[] { 750, 2500 },
      (int upgradeLevel) =>
      {
        var playerAmmo = PlayerScript.GetAmmoMax();
        return GetUpgradeDesc(
          "Max Ammo",
          "Max ammo count",
          $"{playerAmmo}",
          $"{playerAmmo + 1}"
        );
      },

      -1,
      null,
      -1,
      false
    );

    // Crate armor
    RegisterUpgradeDefinition(
      UpgradeType.CRATE_ARMOR,
      new int[] { 750, 2000 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Crate Armor",
          "Chance for supply crates to spawn with armor",
          $"{upgradeLevel * 50}%",
          $"{(upgradeLevel + 1) * 50}%"
        );
      }
    );

    // Increment crate
    RegisterUpgradeDefinition(
      UpgradeType.CRATE_INCREMENT,
      new int[] { 750, 2000 },
      (int upgradeLevel) =>
      {
        return GetUpgradeDesc(
          "Supply Crate",
          "Receiving a crate decreases all upgrade cooldowns",
          $"-{upgradeLevel * 5}",
          $"-{(upgradeLevel + 1) * 5}"
        );
      }
    );

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
    foreach (
        var upgrade in new UpgradeType[]
        {
          UpgradeType.TRI_SHOT,
          UpgradeType.ARROW_RAIN,
          UpgradeType.ARROW_PENETRATION,

          UpgradeType.ARROW_STRENGTH,
          UpgradeType.AMMO_MAX,
          UpgradeType.BACKUP_ARCHER,
          UpgradeType.CRATE_ARMOR,
          UpgradeType.CRATE_INCREMENT,
        }
    )
    {
      if (GetUpgradeCount(upgrade) < GetMaxPurchases(upgrade))
        s_shopSelections.Add(upgrade);
    }
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
      PlayerPrefs.SetInt(
          $"{savepre}_{upgradeType}",
          upgrade_data._Active ? upgrade_data._Counters?.Length ?? 0 : upgrade_data._Count
      );
    }
  }

  static int[] GetUpgradeCosts(UpgradeType upgradeType)
  {
    return s_upgradeDefinitions[upgradeType]._Costs;
  }

  //
  static int GetCountStart(UpgradeType upgradeType)
  {
    return s_upgradeDefinitions[upgradeType]._StartCount;
  }

  //
  static int GetMaxPurchases(UpgradeType upgradeType)
  {
    return s_upgradeDefinitions[upgradeType]._MaxPurchases;
  }

  // Desc
  static string GetUpgradeDesc(UpgradeType upgradeType, int count)
  {
    return s_upgradeDefinitions[upgradeType]._DescriptionFunction.Invoke(count);
  }

  //
  public static bool TryPurchase(UpgradeType upgrade)
  {
    Debug.Log($"Try purchase: {upgrade}");
    var upgrade_data = s_upgrades[upgrade];

    // Check enough money
    var cost = upgrade_data.GetCurrentCost();
    var playerCoins = PlayerScript.GetCoins();
    if (cost > playerCoins)
      return false;

    Debug.Log($"Purchased: {upgrade}");

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
      var counters =
          upgrade_data._Counters != null ? upgrade_data._Counters[0].transform.parent : null;

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
        var button_ = GameResources.s_Instance._UI.GetChild(2).GetChild(0);
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

          // Icon
          switch (upgrade)
          {
            case UpgradeType.TRI_SHOT_COUNTER:
              button_new.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
              break;
            case UpgradeType.ARROW_RAIN_COUNTER:
              button_new.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
              break;
            case UpgradeType.ARROW_PENETRATION_COUNTER:
              button_new.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
              break;

            default:
              Debug.Log($"Unhandled upgradetype: {upgrade}");
              break;
          }

          // Set index
          button_new.transform.GetChild(2).GetComponent<TMPro.TextMeshPro>().text = $"{button_.parent.childCount - 1}";
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
          else if (upgrade_data._Count < 20)
          {
            cSpacing = 0.055f;
            cSize = 0.035f;
          }
          else if (upgrade_data._Count < 26)
          {
            cSpacing = 0.035f;
            cSize = 0.025f;
          }
          else // if (upgrade_data._Count < 26)
          {
            cSpacing = 0.028f;
            cSize = 0.023f;
          }

          if (cSize != 0.05f)
          {
            counter_new.transform.localScale = new Vector3(
                cSize,
                counter_new.transform.localScale.y,
                counter_new.transform.localScale.z
            );
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
            counter.material.color =
                s_saveCount >= upgrade_data._Counters.Length
                    ? Color.yellow
                    : countIter < s_saveCount
                        ? Color.white
                        : counter.transform.parent.parent.parent
                            .GetChild(0)
                            .GetChild(1)
                            .GetChild(0)
                            .GetComponent<MeshRenderer>()
                            .material.color;
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
      if (upgrade_data._Counters == null)
        continue;
      if (upgrade_data._Count >= upgrade_data._Counters.Length)
        continue;

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
      for (var i = 0; i < by; i++)
        upgrade_data._Counters[upgrade_data._Count - i - 1].material.color = Color.white;
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
        counter.material.color = counter.transform.parent.parent.parent
          .GetChild(0)
          .GetChild(1)
          .GetChild(0)
          .GetComponent<MeshRenderer>()
          .material.color;
      }
      var icon = upgrade_data._Counters[0].transform.parent.parent.GetComponent<MeshRenderer>();
      icon.material.color = upgrade_data._Counters[0].transform.parent.parent.parent
        .GetChild(0)
        .GetChild(1)
        .GetChild(0)
        .GetComponent<MeshRenderer>()
        .material.color;
    }

    s_upgrades[upgrade] = upgrade_data;
  }

  //
  public static bool UpgradeEnabled(UpgradeType upgradeType)
  {
    if (upgradeType == UpgradeType.NONE)
      return false;

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
    if (!System.Enum.TryParse(colliderName, true, out upgrade))
    {
      return;
    }

    TryPurchase(upgrade);
  }

  // Check upgrade UI buttons
  public static void UpgradeInput(string colliderName)
  {
    // Parse upgrade type
    UpgradeType upgradeType;
    if (!System.Enum.TryParse(colliderName, true, out upgradeType))
      return;
    var upgrade_data = s_upgrades[upgradeType];

    // Check count
    if (
        !upgrade_data._Enabled
        && GetSupgradeCount(upgradeType) < GetUpgradeCountMax(upgradeType)
    )
      return;

    // Check player combination
    if (!upgrade_data._Enabled && !PlayerScript.s_Singleton.RegisterUpgrade(upgradeType))
    {
      UpgradeInput(PlayerScript.s_Singleton.GetUpgrade(0) + "");
      PlayerScript.s_Singleton.RegisterUpgrade(upgradeType);
    }

    // Check unregister
    if (upgrade_data._Enabled)
      PlayerScript.s_Singleton.UnregisterUpgrade(upgradeType, true);

    // Toggle upgrade
    upgrade_data._Enabled = !upgrade_data._Enabled;

    // Save upgrade data
    s_upgrades[upgradeType] = upgrade_data;

    // UI
    var icon = upgrade_data._Counters[0].transform.parent.parent.GetComponent<MeshRenderer>();
    icon.material.color = upgrade_data._Enabled
        ? Color.green
        : upgrade_data._Counters[0].transform.parent.parent.parent
            .GetChild(0)
            .GetChild(1)
            .GetChild(0)
            .GetComponent<MeshRenderer>()
            .material.color;
  }

  // Set all of the shop's buttons' prices
  public static void UpdateShopPrices()
  {
    var buttons = new GameObject[]
    {
                        MenuManager._shop._menu.transform.GetChild(1).gameObject,
                        MenuManager._shop._menu.transform.GetChild(2).gameObject,
                        MenuManager._shop._menu.transform.GetChild(3).gameObject
    };
    foreach (var button in buttons)
    {
      button.name = $"empty";
      UpdateShopButton(button, -1);
      button.transform.GetChild(2).GetComponent<TMPro.TextMeshPro>().text = "";
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
      button.transform.GetChild(2).GetComponent<TMPro.TextMeshPro>().text =
          $"{upgrade._Desc}";
      if (upgrade._Desc.Length == 0)
      {
        Debug.LogWarning($"No desc set for {upgrade._Type}");
      }

      // Cost
      UpdateShopButton(button, upgrade.GetCurrentCost());

      // Check if any more upgrades to assign
      if (--upgradesCount <= 0)
        break;
    }
  }

  // Update shop prices
  public static void UpdateShopPriceStatuses()
  {
    var buttons = new GameObject[]
    {
      MenuManager._shop._menu.transform.GetChild(1).gameObject,
      MenuManager._shop._menu.transform.GetChild(2).gameObject,
      MenuManager._shop._menu.transform.GetChild(3).gameObject
    };
    foreach (var button in buttons)
    {
      UpgradeType upgradeType;
      if (!System.Enum.TryParse(button.name, true, out upgradeType))
      {
        continue;
      }

      if (upgradeType == UpgradeType.NONE)
        continue;

      var price = s_upgrades[upgradeType].GetCurrentCost();

      UpdateShopButton(button, price);
    }

    // Reroll button
    var button0 = MenuManager._shop._menu.transform.GetChild(4).gameObject;
    var rerollCost = GameScript.s_NumRerolls * 100;
    UpdateShopButton(button0, rerollCost);
  }

  // Turn off the shop button
  static void OffShopButton(GameObject button)
  {
    var t = button.transform.GetChild(1).GetComponent<TextMesh>();
    t.text = "x-";
    button.GetComponent<MeshRenderer>().material.color =
        GameObject.Find("BG").GetComponent<MeshRenderer>().material.color * 1.5f;
    button.GetComponent<BoxCollider>().enabled = false;
  }

  // Update a shop button UI using price
  static void UpdateShopButton(GameObject button, int price)
  {
    button.GetComponent<BoxCollider>().enabled = true;
    var t = button.transform.GetChild(1).GetComponent<TextMesh>();
    button.transform.GetChild(1).name = $"{price}";

    /*/ Set text size
    if (price > 999)
    {
    t.characterSize = 0.2f;
    }
    else if (price > 99)
    {
    t.characterSize = 0.25f;
    }*/

    // Set color
    foreach (
        var r in new MeshRenderer[]
        {
          button.transform.GetChild(3).GetComponent<MeshRenderer>(),
          button.transform.GetChild(4).GetComponent<MeshRenderer>()
        }
    )
    {
      if (price == -1 || price > PlayerScript.GetCoins())
      {
        r.material.color =
            GameObject.Find("BG").GetComponent<MeshRenderer>().material.color * 1.5f;
      }
      else
      {
        r.material.color = GameObject
            .Find("Ground")
            .transform.GetChild(0)
            .GetComponent<MeshRenderer>()
            .material.color;
      }
    }

    // Set text
    if (price == -1)
      t.text = "-";
    else
      t.text = "x" + price.ToString("#,##0");
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
      MenuManager._waveSign.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>().text =
          "" + (Wave.s_MetaWaveIter + 1);
    }
    PlayerScript.SetAmmoForShop();
  }

  // Make a sound when a purchase is completed
  static public void SoundPurchase()
  {
    GameScript.PlaySound(MenuManager._shop._menu.transform.GetChild(0).GetChild(0).gameObject);
  }
}
