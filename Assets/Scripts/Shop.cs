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

    AMMO_MAX,
    HEALTH,
  }
  struct Upgrade
  {

    // Type
    public UpgradeType _Type;

    // Amount in inv
    public int _Count;
    public UpgradeType _CountUpgrade;
    public bool _Enabled;

    // Shop price


    // Display
    public bool _Active;
    public MeshRenderer[] _Counters;

    // Function
    public System.Action<UpgradeType> _OnUnlock;
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

    // Upgrades
    s_upgrades = new Dictionary<UpgradeType, Upgrade>();
    foreach (UpgradeType upgradeType in System.Enum.GetValues(typeof(UpgradeType)))
    {

      s_upgrades.Add(upgradeType, new Upgrade()
      {
        _Type = upgradeType,
        _Count = PlayerPrefs.GetInt($"{savepre}_{upgradeType}", 0),

        _CountUpgrade = upgradeType
      });
    }

    // Custom
    var upgrade = s_upgrades[UpgradeType.TRI_SHOT];
    {
      upgrade._CountUpgrade = UpgradeType.TRI_SHOT_COUNTER;
      upgrade._OnUnlock = (UpgradeType upgradeType) =>
      {

        // Configure sub upgrades
        var supgrade = s_upgrades[upgradeType];
        {
          supgrade._Count++;
        }
        s_upgrades[upgradeType] = supgrade;

        // First upgrade
        if (supgrade._Count == 1)
        {
          supgrade = s_upgrades[UpgradeType.TRI_SHOT_COUNTER];
          {
            supgrade._Count = 8;
          }
          s_upgrades[UpgradeType.TRI_SHOT_COUNTER] = supgrade;

          s_shopSelections.Add(UpgradeType.TRI_SHOT_COUNTER);
          TryPurchase(UpgradeType.TRI_SHOT_COUNTER);
        }

      };
    }
    s_upgrades[UpgradeType.TRI_SHOT] = upgrade;

    upgrade = s_upgrades[UpgradeType.TRI_SHOT_COUNTER];
    {
      upgrade._Active = true;

      //
      upgrade._OnUnlock += (UpgradeType upgradeType) =>
      {

        var supgrade = s_upgrades[upgradeType];
        {
          if (supgrade._Counters == null) return;
          supgrade._Count = GetUpgradeCountMax(upgradeType) - 1;
          if (supgrade._Count == 3)
            s_shopSelections.Remove(upgradeType);
        }
        s_upgrades[upgradeType] = supgrade;

      };
    }
    s_upgrades[UpgradeType.TRI_SHOT_COUNTER] = upgrade;

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

    if (GetUpgradeCount(UpgradeType.TRI_SHOT) == 0)
      s_shopSelections.Add(UpgradeType.TRI_SHOT);
    else if (GetUpgradeCountMax(UpgradeType.TRI_SHOT_COUNTER) > 3)
      s_shopSelections.Add(UpgradeType.TRI_SHOT_COUNTER);
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
      PlayerPrefs.SetInt($"{savepre}_{upgradeType}", upgrade_data._Active ? upgrade_data._Counters.Length : upgrade_data._Count);
    }
  }

  //
  public static bool TryPurchase(UpgradeType upgrade)
  {
    Debug.Log($"try purchase: {upgrade}");

    var upgrade_data = s_upgrades[upgrade];

    // Check enough money

    // Decrease money

    // Give / save upgrade

    // Check function
    upgrade_data._OnUnlock?.Invoke(upgrade);

    // If active upgrade, place skill button
    UpdateUpgradeUI(upgrade, upgrade_data._Active && upgrade_data._Counters != null);

    // Reload active upgrades / prices
    BufferActiveUpgrades();
    UpdateShopPrices();

    return true;
  }

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
          button_new.transform.localPosition = start_pos + new Vector3(-9.75f * (child_pos - 1), 0f, 0f);
          button_new.SetActive(true);
        }

        // Set counters
        var counter_ = button_.transform.GetChild(1).GetChild(0);
        upgrade_data._Counters = new MeshRenderer[upgrade_data._Count];
        for (var i = 0; i < upgrade_data._Count; i++)
        {
          var counter_new = GameObject.Instantiate(counter_.gameObject, counters);
          counter_new.transform.localPosition += new Vector3(0.09f * i, 0f, 0f);

          upgrade_data._Counters[i] = counter_new.GetComponent<MeshRenderer>();
        }
        upgrade_data._Count = 0;

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

      IncrementUpgrade(upgrade);
    }
  }
  static void IncrementUpgrade(UpgradeType upgrade)
  {

    var upgrade_data = s_upgrades[upgrade];

    // Check sub upgrade
    if (upgrade_data._CountUpgrade != upgrade)
    {
      IncrementUpgrade(upgrade_data._CountUpgrade);
      return;
    }

    // Increment
    upgrade_data._Count++;
    s_upgrades[upgrade] = upgrade_data;

    // Update UI
    upgrade_data._Counters[upgrade_data._Count - 1].material.color = Color.white;
  }

  // Reset a upgrade's UI
  public static void ResetUpgrade(UpgradeType upgrade)
  {

    var upgrade_data = s_upgrades[upgrade];
    if (upgrade_data._CountUpgrade != upgrade)
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
  public static bool UpgradeEnabled(UpgradeType upgrade)
  {
    var upgrade_data = s_upgrades[upgrade];
    if (upgrade_data._CountUpgrade != upgrade)
    {
      return UpgradeEnabled(upgrade_data._CountUpgrade);
    }

    return s_upgrades[upgrade]._Enabled;
  }

  static int GetUpgradeCount(UpgradeType upgrade)
  {
    var upgrade_data = s_upgrades[upgrade];
    return upgrade_data._Count;
  }

  //
  static int GetSupgradeCount(UpgradeType upgrade)
  {
    var upgrade_data = s_upgrades[upgrade];

    if (upgrade_data._CountUpgrade != upgrade)
    {
      return GetSupgradeCount(upgrade_data._CountUpgrade);
    }

    return upgrade_data._Count;
  }
  static int GetUpgradeCountMax(UpgradeType upgrade)
  {
    var upgrade_data = s_upgrades[upgrade];

    if (upgrade_data._CountUpgrade != upgrade)
    {
      return GetUpgradeCountMax(upgrade_data._CountUpgrade);
    }

    return upgrade_data._Counters.Length;
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

    UpgradeType upgrade;
    if (!System.Enum.TryParse(colliderName, true, out upgrade)) { return; }

    var upgrade_data = s_upgrades[upgrade];
    if (!upgrade_data._Enabled && GetSupgradeCount(upgrade) < GetUpgradeCountMax(upgrade)) return;
    upgrade_data._Enabled = !upgrade_data._Enabled;

    s_upgrades[upgrade] = upgrade_data;

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
      button.name = $"empty";

    // Assign buttons
    var upgradesCount = Mathf.Clamp(s_shopSelections.Count, 0, 3);
    Debug.Log(upgradesCount);
    if (upgradesCount == 0)
    {
      Debug.LogWarning("No upgrades!");
      return;
    }

    var upgradesAvailable = new List<UpgradeType>(s_shopSelections);
    while (true)
    {

      var gotUpgrade = upgradesAvailable[Random.Range(0, upgradesAvailable.Count - 1)];
      upgradesAvailable.Remove(gotUpgrade);
      var button = buttons[upgradesCount - 1];
      button.name = $"{gotUpgrade}";

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
    if (price > PlayerScript.GetCoins())
    {
      r.material.color = GameObject.Find("BG").GetComponent<MeshRenderer>().material.color * 1.5f;
    }
    else
    {
      r.material.color = GameObject.Find("Ground").transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
    }
    // Set text
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
      MenuManager._waveSign.transform.localPosition = new Vector3(0f, 25.5f, 10f);
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
