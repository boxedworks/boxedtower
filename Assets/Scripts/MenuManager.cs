using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager
{

  public static GameObject _musicButton, _pauseButton, _waveSign, _coin, _stats, _upgrades;
  public static CMenu _mainMenu, _pauseMenu, _winMenu, _shop, _loseMenu, _betweenWaves;

  // Use this for initialization
  public static void Init()
  {
    _mainMenu = new CMenu("MainMenu");

    _loseMenu = new CMenu("LoseMenu");

    _pauseMenu = new CMenu("PauseMenu");
    _pauseButton = GameObject.Find("PauseButton");

    _upgrades = GameObject.Find("Upgrades");

    _shop = new CMenu("Shop");

    _winMenu = new CMenu("WinMenu");

    _betweenWaves = new CMenu("InBetweenMenu");
    _stats = GameObject.Find("Stats");

    _waveSign = GameObject.Find("WaveSign");

    _coin = GameResources.s_Instance._CameraMain.transform.GetChild(0).GetChild(1).GetChild(0).gameObject;

    _musicButton = GameObject.Find("MusicButton");

    _waveSign.SetActive(false);
    _pauseButton.SetActive(false);
    _musicButton.SetActive(false);

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
      GameScript.s_Instance.StartCoroutine(HideCo(this));
    }

    // Show menu wrapper
    public void Show()
    {
      _Visible = true;
      GameScript.s_Instance.StartCoroutine(ShowCo(this));
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
      Vector3 savePos = menu._menu.transform.localPosition;
      float t = 0f;
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

  static public void MenuBetweenWaves()
  {
    GameScript._state = GameScript.GameState.BETWEEN_MENU;

    ShowMenuAfterTime(MenuManager._betweenWaves, 1.5f);

    MenuManager.ShowStats();
  }

  // Show the stats menu
  public static void ShowStats()
  {
    int lastCoin = PlayerPrefs.GetInt("TotalCoins", 0);
    PlayerPrefs.SetInt("TotalCoins", lastCoin + PlayerScript.WaveStats._coinsGained);

    GameScript.s_Instance.StartCoroutine(ShowStatsCo());
    MenuManager._pauseButton.SetActive(false);
  }
  static IEnumerator ShowStatsCo()
  {
    TextMesh text = _stats.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>();
    text.text = "";
    yield return new WaitForSeconds(2f);
    // Save game before starting waves so can revert if dies
    GameScript.SaveGame();
    // Show coin magic
    int coinsGot = PlayerScript.WaveStats._coinsGained,
        coinsHad = PlayerScript.GetCoins() - coinsGot,
        coinSave = coinsGot;
    //Debug.Log(string.Format("Coins got: {0} Coins had: {1}", coinsGot, coinsHad));
    text.text = string.Format("{0} (+{1})", coinsHad, coinsGot);
    yield return new WaitForSeconds(1.5f);
    while (coinsGot > 0 && GameScript._state == GameScript.GameState.BETWEEN_MENU)
    {
      yield return new WaitForSeconds(0.005f);
      coinsGot -= 5;
      coinsHad += 5;
      text.text = string.Format("{0} (+{1})", coinsHad, coinsGot);

      GameObject newCoin = Object.Instantiate(Resources.Load("Coin") as GameObject);
      GameScript.PlaySound(newCoin.transform.GetChild(0).gameObject, 0.8f, 1.1f);
      newCoin.transform.position = new Vector3(-50f, 0f, 0f);
      newCoin.transform.parent = GameResources.s_Instance._ContainerDead;
    }
    coinsHad = PlayerScript.GetCoins();
    coinsHad = PlayerScript.GetCoins();
    text.text = string.Format("{0} (+ {1})", coinsHad, coinSave);
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
        _pauseMenu.Hide();
        Time.timeScale = 1f;
        break;
      case GameScript.GameState.WIN:
        _winMenu.Hide();
        break;
    }
    _waveSign.SetActive(false);
    GameScript._state = GameScript.GameState.MAIN_MENU;
    _mainMenu.Show();
    _musicButton.SetActive(false);
    // Stop music
    AudioSource s = GameScript.s_Instance.GetComponent<AudioSource>();
    if (s.isPlaying)
    {
      s.Stop();
    }
  }

  public static void SetWaveSign(string text)
  {
    _waveSign.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>().text = text;
  }

  // Handle input other than the player's
  public static void HandleMenuInput(RaycastHit hit)
  {
    if (hit.collider.name.Equals("Play"))
    {
      _mainMenu.Hide();
      GameScript._state = GameScript.GameState.PLAY;
      if (GameScript._mode == GameScript.GameMode.TARGETS)
      {
        Wave.DestroyAll();
        _pauseButton.SetActive(true);
        _waveSign.SetActive(true);
        SetWaveSign("" + 1);
        GameScript.Targets.Init();
      }
      else
      {
        GameScript.LoadGameSettings();
        Shop.Load();
        PlayerScript.WaveStats.Reset();
        ToggleGameUI(true);

        if (Wave.s_MetaWaveIter == 0)
        {
          _pauseButton.SetActive(true);

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
      }
      ButtonNoise();
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
      Shop.Save();
      StartGame();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("ToMenu"))
    {
      ExitToMenu();
      ToggleGameUI(false);
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("Resume"))
    {
      Time.timeScale = 1f;
      _pauseMenu.Hide();
      _pauseButton.SetActive(true);
      MenuManager._musicButton.SetActive(false);
      GameScript._state = GameScript.GameState.PLAY;
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("MusicButton"))
    {
      ToggleMusic();
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("ModeWave"))
    {
      GameScript.SwitchModes(GameScript.GameMode.WAVES);
      ButtonNoise();
    }
    else if (hit.collider.name.Equals("ModeTarget"))
    {
      GameScript.SwitchModes(GameScript.GameMode.TARGETS);
      ButtonNoise();
    }

    // Check shop
    else if (hit.transform.parent.name == "Shop")
    {
      Shop.ShopInput(hit.collider.name);
    }
  }

  public static float s_waveStart;
  static void StartGame()
  {
    s_waveStart = Time.time;

    // Fix inputmanager
    Vector2 pos = Input.mousePosition;
    InputManager._MouseCurrentPos = pos;
    InputManager._MouseDownPos = pos;
    InputManager._MouseUpPos = pos;

    PlayerScript.WaveStats.Reset();
    Shop.ToggleShop(false);
    Wave.LoadWaves(Wave.s_MetaWaveIter);
    Wave.StartWaves();
    GameScript._state = GameScript.GameState.PLAY;
    _pauseButton.SetActive(true);
    // Play music
    AudioSource s = GameScript.s_Instance.GetComponent<AudioSource>();
    if (!s.isPlaying)
    {
      s.Play();
    }
  }

  static void ButtonNoise()
  {
    PlayerScript.ButtonNoise();
  }

  public static void ToggleMusic()
  {
    var s = GameScript.s_Instance.GetComponent<AudioSource>();
    GameScript.s_musicPlaying = !GameScript.s_musicPlaying;
    if (GameScript.s_musicPlaying)
    {
      s.volume = 0.4f;
      _musicButton.GetComponent<MeshRenderer>().material.color = GameObject.Find("Stone").GetComponent<MeshRenderer>().material.color;
    }
    else
    {
      s.volume = 0f;
      _musicButton.GetComponent<MeshRenderer>().material.color = Color.black;
    }

    // Save
    PlayerPrefs.SetInt("Music", GameScript.s_musicPlaying ? 1 : 0);
  }

}
