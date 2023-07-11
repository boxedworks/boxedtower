using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager
{

  public static GameObject s_ShopButton, s_PauseButton, s_StatsButton, _waveSign, _coin, _stats, _upgrades;
  public static CMenu _mainMenu, s_PauseMenu, _winMenu, s_ShopMenu, s_StatsMenu, _loseMenu, _betweenWaves, _guideMenu, _optionsMenu;

  // Use this for initialization
  public static void Init()
  {
    _mainMenu = new CMenu("MainMenu");

    _loseMenu = new CMenu("LoseMenu");

    s_PauseMenu = new CMenu("PauseMenu");
    s_PauseButton = GameObject.Find("PauseButton");
    s_StatsButton = GameObject.Find("StatsButton");

    _guideMenu = new CMenu("GuideMenu");
    _optionsMenu = new CMenu("OptionsMenu");

    _upgrades = GameObject.Find("Upgrades");

    s_ShopMenu = new CMenu("Shop");
    s_StatsMenu = new CMenu("StatsMenu");

    _winMenu = new CMenu("WinMenu");

    _betweenWaves = new CMenu("InBetweenMenu");
    _stats = GameObject.Find("Stats");

    _waveSign = GameObject.Find("WaveSign");

    _coin = GameResources.s_Instance._CameraMain.transform.GetChild(0).GetChild(1).GetChild(0).gameObject;

    s_ShopButton = GameObject.Find("ShopButton");

    _waveSign.SetActive(false);
    s_PauseButton.SetActive(false);
    s_StatsButton.SetActive(false);
    Wave.ToggleShopUI(false);

    GameResources.s_Instance._SliderTower2UI.gameObject.SetActive(false);

    ToggleGameUI(false);
  }

  public class CMenu
  {
    // Desired transform.y values for hiden and !hiden
    static Vector2 YPOS = new Vector2(0f, 70f);

    // Local
    public GameObject _menu;
    public bool _Visible;

    // Constructors
    public CMenu(string menuName)
    {
      Init(GameObject.Find(menuName));
    }
    public CMenu(GameObject menu)
    {
      Init(menu);
    }
    public void Init(GameObject menu)
    {
      _menu = menu;
      // Determine status
      if (_menu.transform.localPosition.y == YPOS.x)
      {
        _Visible = true;
      }
      else if (_menu.transform.localPosition.y == YPOS.y)
      {
        _Visible = false;
      }
      // If in wrong state, print to log and hide
      else
      {
        Debug.LogError(string.Format("{0} CMenu not in either YPOS {1} or {2}; Hiding", _menu.name, YPOS.x, YPOS.y));
        Hide();
      }
    }

    // Hide menu wrapper
    public void Hide()
    {
      _Visible = false;
      _menu.transform.localPosition = new Vector3(_menu.transform.localPosition.x, 70f, _menu.transform.localPosition.z); ;
      //GameScript.s_Instance.StartCoroutine(HideCo(this));
    }

    // Show menu wrapper
    public void Show()
    {
      _Visible = true;
      _menu.transform.localPosition = new Vector3(_menu.transform.localPosition.x, 0f, _menu.transform.localPosition.z); ;
      //GameScript.s_Instance.StartCoroutine(ShowCo(this));
    }

    // Toggle
    public void Toggle()
    {
      if (_Visible)
      {
        Hide();
      }
      else
      {
        Show();
      }
    }
    public void Toggle(bool show)
    {
      if (_Visible == show) return;
      if (show) Show();
      else Hide();
    }
    // Coroutines for lerping hide/showing
    static IEnumerator HideCo(CMenu menu)
    {
      Vector3 savePos = menu._menu.transform.localPosition;
      float t = 0f;
      while (t <= 1f)
      {
        menu._menu.transform.localPosition = Vector3.Slerp(savePos, new Vector3(savePos.x, YPOS.y, savePos.z), t);
        t += 0.03f;
        yield return new WaitForSecondsRealtime(0.01f);
      }
    }
    static IEnumerator ShowCo(CMenu menu)
    {
      var savePos = menu._menu.transform.localPosition;
      var t = 0f;
      while (t <= 1f)
      {
        menu._menu.transform.localPosition = Vector3.Slerp(savePos, new Vector3(savePos.x, YPOS.x, savePos.z), t);
        t += 0.03f;
        yield return new WaitForSecondsRealtime(0.01f);
      }
    }
  }

  // Update is called once per frame
  public static void Update()
  {
    _coin.transform.Rotate(new Vector3(0f, 1f, 0f) * Time.deltaTime * 50f);
  }

  public static void ToggleGameUI(bool toggle)
  {
    _coin.transform.parent.gameObject.SetActive(toggle);
    _upgrades.SetActive(toggle);
    GameResources.s_Instance._SliderSloMo.gameObject.SetActive(toggle);
    GameResources.s_Instance._SliderComboDecrease.gameObject.SetActive(toggle);

    if (!toggle || (toggle && Shop.GetUpgradeCount(Shop.UpgradeType.TOWER2) > 0))
      GameResources.s_Instance._SliderTower2UI.gameObject.SetActive(toggle);
  }

  // Show a menu after a given time
  static public void ShowMenuAfterTime(CMenu menu, float time)
  {
    GameScript.s_Instance.StartCoroutine(MenuDelay(menu, time));
  }
  static IEnumerator MenuDelay(CMenu menu, float time)
  {
    yield return new WaitForSeconds(time);
    menu.Show();
  }

  static public IEnumerator ShowWinStats()
  {
    TextMesh text = _winMenu._menu.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>();
    int coins = PlayerPrefs.GetInt("TotalCoins", 0);
    coins += PlayerScript.WaveStats._coinsGained;
    int haveCoins = 0;
    while (haveCoins < coins && GameScript._state == GameScript.GameState.WIN)
    {
      haveCoins += 5;
      GameObject newCoin = Object.Instantiate(Resources.Load("Coin") as GameObject);
      GameScript.PlaySound(newCoin.transform.GetChild(0).gameObject, 0.8f, 1.1f);
      newCoin.transform.position = new Vector3(-50f, 0f, 0f);
      newCoin.transform.parent = GameResources.s_Instance._ContainerDead;
      text.text = "" + haveCoins;
      yield return new WaitForSeconds(0.01f);
    }
    text.text = "" + coins;
  }

  // Exit to menu
  public static void ExitToMenu()
  {
    switch (GameScript._state)
    {
      case GameScript.GameState.BETWEEN_MENU:
        _betweenWaves.Hide();
        break;
      case GameScript.GameState.LOSE:
        _loseMenu.Hide();
        break;
      case GameScript.GameState.PAUSED:
        s_PauseMenu.Hide();
        Time.timeScale = 2.5f;
        break;
      case GameScript.GameState.WIN:
        _winMenu.Hide();
        break;
    }
    _waveSign.SetActive(false);
    GameScript._state = GameScript.GameState.MAIN_MENU;
    _mainMenu.Show();
    // Stop music
    AudioSource s = GameScript.s_Instance.GetComponent<AudioSource>();
    if (s.isPlaying)
    {
      s.Stop();
    }
  }

  // Handle input other than the player's
  public static void HandleMenuInput(RaycastHit hit)
  {
    if (hit.collider.name.Equals("Play"))
    {
      _mainMenu.Hide();
      GameScript._state = GameScript.GameState.PLAY;
      GameScript.LoadGameSettings();
      Shop.Load();
      PlayerScript.Reset();
      ToggleGameUI(true);

      Shop.UpdateShopPrices();

      GameScript.s_NumExitsMainMenu = 0;

      if (Wave.s_MetaWaveIter == 0)
      {
        s_PauseButton.SetActive(true);
        s_StatsButton.SetActive(true);
        Wave.ToggleShopUI(false);

        // Play music
        var s = GameScript.s_Instance.GetComponent<AudioSource>();
        if (!s.isPlaying)
        {
          s.Play();
        }

        StartGame();

      }
      else
      {
        Shop.UpdateShopPrices();
        Shop.ToggleShop(true);
      }

      ButtonNoise();
    }
    else if (hit.collider.name.Equals("ToGuide"))
    {
      if (!GameScript.StateAtMainMenu())
        MenuManager.s_PauseMenu.Hide();
      else
        MenuManager._mainMenu.Hide();
      MenuManager._guideMenu.Show();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("gBack"))
    {
      if (!GameScript.StateAtMainMenu())
      {
        MenuManager.s_PauseMenu.Show();

        GameScript.s_NumExits = 0;
        GameScript.s_NumExitsMainMenu = 0;
        MenuManager.s_PauseMenu._menu.transform.Find("ToMenu").GetChild(1).GetComponent<TMPro.TextMeshPro>().text = $"Exit";
      }
      else
        MenuManager._mainMenu.Show();
      MenuManager._guideMenu.Hide();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("statBack"))
    {
      StatsMenuBack();
    }
    else if (hit.collider.name.Equals("ToOptions"))
    {
      if (!GameScript.StateAtMainMenu())
        MenuManager.s_PauseMenu.Hide();
      else
        MenuManager._mainMenu.Hide();
      MenuManager._optionsMenu.Show();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("exitOptions"))
    {
      if (!GameScript.StateAtMainMenu())
      {
        MenuManager.s_PauseMenu.Show();

        GameScript.s_NumExits = 0;
        GameScript.s_NumExitsMainMenu = 0;
        MenuManager.s_PauseMenu._menu.transform.Find("ToMenu").GetChild(1).GetComponent<TMPro.TextMeshPro>().text = $"Exit";
      }
      else
        MenuManager._mainMenu.Show();
      MenuManager._optionsMenu.Hide();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("oMusicDown"))
    {
      var volume = GameScript.s_MusicVolume;
      if (volume == 0)
        return;

      volume--;

      GameScript.s_MusicVolume = volume;
      PlayerPrefs.SetInt("MusicVolume", GameScript.s_MusicVolume);
      UpdateMusicFX();
    }
    else if (hit.collider.name.Equals("oMusicUp"))
    {
      var volume = GameScript.s_MusicVolume;
      if (volume == 5)
        return;

      volume++;

      var inputGroup = GameObject.Find("VolumeGroup").transform;
      inputGroup.Find("Value").GetComponent<TMPro.TextMeshPro>().text = $"{volume}/5";

      GameScript.s_MusicVolume = volume;
      PlayerPrefs.SetInt("MusicVolume", GameScript.s_MusicVolume);
      UpdateMusicFX();
    }
    else if (hit.collider.name.Equals("ToShop"))
    {
      Shop.UpdateShopPrices();
      Shop.ToggleShop(true);
      _betweenWaves.Hide();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("ToWave"))
    {
      Shop.ToWave();
    }
    else if (hit.collider.name.Equals("RerollButton"))
    {
      var rerollCost = GameScript.s_NumRerolls * 100;
      var playerMoney = PlayerScript.GetCoins();
      if (playerMoney >= rerollCost)
      {
        GameScript.s_NumRerolls++;
        Shop.SoundPurchase();
        PlayerScript.SetCoins(playerMoney - rerollCost);
        Shop.UpdateShopPrices(true);
        Shop.UpdateShopPriceStatuses();
      }
    }
    else if (hit.collider.name.Equals("ToMenu"))
    {
      // Check confirm
      if (hit.transform.parent.name == "PauseMenu")
      {
        if (GameScript.s_NumExits == 3) { }
        else
        {
          GameScript.s_NumExits++;
          hit.transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().text = $"Exit? {GameScript.s_NumExits + 1}/3";
          ButtonNoise();
          return;
        }
      }

      Wave.DestroyAll();
      ExitToMenu();
      ToggleGameUI(false);
      PlayerScript.UIUpdateAmmoCounter();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("Resume"))
    {
      Resume();
    }
    else if (hit.collider.name.Equals("ModeWave"))
    {
      GameScript.SwitchModes(GameScript.GameMode.WAVES);
      ButtonNoise();
    }

    else if (hit.collider.name.Equals("ToExitGame"))
    {
      if (GameScript.s_NumExitsMainMenu == 3) { }
      else
      {
        GameScript.s_NumExitsMainMenu++;
        hit.transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().text = $"Exit? {GameScript.s_NumExits + 1}/3";
        ButtonNoise();
        return;
      }

#if !UNITY_EDITOR
      Application.Quit();
#endif
    }

    // Check shop
    else if (hit.transform.parent.name == "Shop")
    {
      Shop.ShopInput(hit.collider.name);
    }

    // Check guide
    else if (hit.transform.parent.name == "gSelections")
    {
      GameScript.Guide.SetGuideText(hit.transform.name);
    }
  }

  public static float s_waveStart;
  public static void StartGame()
  {
    s_waveStart = Time.time;

    /*/ Fix inputmanager
    Vector2 pos = Input.mousePosition;
    InputManager._MouseCurrentPos = pos;
    InputManager._MouseDownPos = pos;
    InputManager._MouseUpPos = pos;*/

    PlayerScript.WaveStats.Reset();

    //Shop.ToggleShop(false);
    Wave.LoadWaves(Wave.s_MetaWaveIter);
    Wave.StartWaves();
    GameScript._state = GameScript.GameState.PLAY;
    s_PauseButton.SetActive(true);
    s_StatsButton.SetActive(true);

    // Play music
    var s = GameScript.s_Instance.GetComponent<AudioSource>();
    if (!s.isPlaying)
    {
      s.Play();
    }
  }

  public static void StatsMenuBack()
  {
    Resume();
    MenuManager.s_StatsMenu.Hide();
    ButtonNoise();
  }

  public static void Resume()
  {
    Time.timeScale = GameScript.s_TimeSped ? 2.5f : 1f;
    s_PauseMenu.Hide();
    s_PauseButton.SetActive(true);
    s_StatsButton.SetActive(true);
    if (Wave.ShopVisible())
      Wave.ToggleShopUI(true);
    GameScript._state = GameScript.GameState.PLAY;
    ButtonNoise();
  }

  static void ButtonNoise()
  {
    PlayerScript.ButtonNoise();
  }

  public static void UpdateMusicFX()
  {
    var volume = GameScript.s_MusicVolume;
    var musicSource = GameScript.s_Instance.GetComponent<AudioSource>();

    musicSource.volume = GameScript.s_MusicVolumeSave * (volume / 5f);

    // Buttons
    var inputGroup = GameObject.Find("VolumeGroup").transform;
    inputGroup.Find("Value").GetComponent<TMPro.TextMeshPro>().text = $"{volume}/5";

    var buttonDown = inputGroup.Find("oMusicDown");
    var buttonUp = inputGroup.Find("oMusicUp");

    var colorDown = GameScript.Guide.s_buttonSaveColor;
    var colorUp = GameScript.Guide.s_buttonSaveColor;
    if (volume == 0)
    {
      colorDown = Color.gray;
    }
    else if (volume == 5)
    {
      colorUp = Color.gray;
    }

    buttonDown.GetChild(0).GetComponent<MeshRenderer>().material.color = colorDown;
    buttonUp.GetChild(0).GetComponent<MeshRenderer>().material.color = colorUp;
  }

}
