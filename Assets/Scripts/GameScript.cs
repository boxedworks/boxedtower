using System.Collections;
using System;
using UnityEngine;

public class GameScript : MonoBehaviour
{

  public static GameScript s_Instance;

  public float _dayAmount;

  static float _timeCycle;

  public static GameState _state;
  public static GameMode _mode;

  public static bool s_musicPlaying, s_HasWon;

  static Color _SkyColor;

  public enum GameState
  {
    MAIN_MENU,
    PLAY,
    PAUSED,
    BETWEEN_MENU,
    SHOP,
    LOSE,
    WIN
  }

  public enum GameMode
  {
    WAVES,
    TARGETS
  }

  // Use this for initialization
  void Start()
  {
    s_Instance = this;

    // Init helpers
    new GameResources();

    MenuManager.Init();
    PlayerScript.Init();

    //
    UpdateResolution();


    s_musicPlaying = true;

    _state = GameState.MAIN_MENU;

    _SkyColor = GameObject.Find("Sky").transform.GetChild(0).GetComponent<MeshRenderer>().material.color;

    //ResetGame();
    LoadGameSettings();

    //PlayerScript.SetCoins(0);
    //PlayerScript.SetAmmoPierce(2);
    //PlayerScript.SetAmmoRain(1);
    //PlayerScript.SetAmmoMax(5);

    //Wave._RealWaveIter = 10;

    _timeCycle = 60f + UnityEngine.Random.value * 600f;

    _mode = GameMode.TARGETS;
    SwitchModes(GameMode.WAVES);
  }

  public void UpdateResolution()
  {

    var width = 640;
    var height = 360;
    var fullscreen = true;

    /*#if UNITY_EDITOR
          return;
    #endif*/
    Screen.SetResolution((int)width, (int)height, fullscreen);
    // set the desired aspect ratio (the values in this example are
    // hard-coded for 16:9, but you could make them into public
    // variables instead so you can set them at design time)
    var targetaspect = 16.0f / 9.0f;

    // determine the game window's current aspect ratio
    var windowaspect = (float)width / (float)height;

    // current viewport height should be scaled by this amount
    var scaleheight = windowaspect / targetaspect;

    // obtain camera component so we can modify its viewport
    foreach (var camera in new Camera[] { GameResources.s_Instance._CameraMain })
    {

      // if scaled height is less than current height, add letterbox
      if (scaleheight < 1.0f)
      {
        var rect = camera.rect;

        rect.width = 1.0f;
        rect.height = scaleheight;
        rect.x = 0;
        rect.y = (1.0f - scaleheight) / 2.0f;

        camera.rect = rect;
      }
      else // add pillarbox
      {
        var scalewidth = 1.0f / scaleheight;
        var rect = camera.rect;

        rect.width = scalewidth;
        rect.height = 1.0f;
        rect.x = (1.0f - scalewidth) / 2.0f;
        rect.y = 0;

        camera.rect = rect;
      }
    }
  }

  // Update is called once per frame
  void Update()
  {
    InputManager.HandleInput();

    MenuManager.Update();

    UpdateScreenShake();

    if (_state == GameState.PLAY)
    {
      switch (_mode)
      {
        case GameMode.WAVES:
          Wave.Update();
          break;
        case GameMode.TARGETS:
          Targets.Update();
          break;
      }
    }

    _timeCycle -= Time.deltaTime;
    if (_timeCycle < 0f)
    {
      if (_dayAmount == 0f)
      {
        TurnDay();
        _timeCycle = 300f;
      }
      else
      {
        TurnNight();
        _timeCycle = 60f;
      }
    }

    if (_state == GameState.MAIN_MENU || _state == GameState.SHOP)
    {
      if (Mathf.RoundToInt(UnityEngine.Random.value * 500f) == 0f)
      {
        if (GameObject.Find("Alive").transform.childCount > 20) return;
        EnemyScript.SpawnEnemy(s_HasWon ? EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8 : EnemyScript.EnemyType.GROUND_ROLL, false);
      }
    }
  }

  public static class Targets
  {
    static public int _Difficulty, _MinTargets;

    static public void Init()
    {
      _Difficulty = 0;
      _MinTargets = 0;
      SetHealth(2);
      PlayerScript.SetAmmoMax(3);
      PlayerScript.ResetTarget();
    }

    static bool IsTargets()
    {
      if (GameObject.Find("Alive").transform.childCount > _MinTargets) return true;
      return false;
    }

    public static void Update()
    {
      if (IsTargets()) return;
      SpawnTarget();
    }

    static void SpawnTarget()
    {
      float tSize, forceMod = 1f;
      TargetScript.TargetType type = TargetScript.TargetType.FLY;
      switch (_Difficulty)
      {
        case 0:
          tSize = 1f;
          break;
        case 1:
          tSize = 0.75f;
          break;
        case 2:
          tSize = 0.5f;
          break;
        case 3:
          tSize = (Mathf.RoundToInt(UnityEngine.Random.value) == 0 ? 0.75f : 0.6f);
          forceMod = (Mathf.RoundToInt(UnityEngine.Random.value) == 0 ? 1f : 2f);
          break;
        case 4:
          tSize = (Mathf.RoundToInt(UnityEngine.Random.value) == 0 ? 0.6f : 0.5f);
          forceMod = (Mathf.RoundToInt(UnityEngine.Random.value) == 0 ? 1.6f : 2f);
          int r = Mathf.RoundToInt(UnityEngine.Random.value * 2f);
          if (r == 0)
          {
            type = TargetScript.TargetType.ROLL;
          }
          else
          {
            type = TargetScript.TargetType.FLY;
          }
          break;
        default:
          tSize = (Mathf.RoundToInt(UnityEngine.Random.value) == 0 ? 0.6f : 0.5f);
          forceMod = (Mathf.RoundToInt(UnityEngine.Random.value) == 0 ? 1.6f : 2f);
          r = Mathf.RoundToInt(UnityEngine.Random.value * 3f);
          if (r == 0)
          {
            type = TargetScript.TargetType.ROLL;
          }
          else if (r == 1)
          {
            type = TargetScript.TargetType.BOMB;
          }
          else
          {
            type = TargetScript.TargetType.FLY;
          }
          break;
      }

      TargetScript s = GameScript.SpawnTarget(GameObject.Find("ShootEffect").transform.parent.position, tSize, type);//new Vector2(4f + UnityEngine.Random.value * 34f, -5f + UnityEngine.Random.value * 15f), tSize);
      s._forceModifier = forceMod;

      DestroyPS();
    }

    static void DestroyPS()
    {
      var dead = GameResources.s_Instance._ContainerDead;
      for (var i = dead.childCount - 1; i >= 0; i--)
      {
        var t = dead.transform.GetChild(i);
        if ((t.gameObject.name.Length > 9 && t.gameObject.name.Substring(0, 9).Equals("Explosion")) ||
            t.gameObject.name.Equals("Trail") || t.gameObject.name.Equals("FireTrail")) Destroy(t.gameObject);
      }
    }
  }

  // Day / Night cycles
  static void SetTimeStuff()
  {
    var sun = GameResources.s_Instance._Sun;
    var sky = GameResources.s_Instance._Sky;

    sun.transform.position = Vector3.Lerp(new Vector3(35.9f, 0f, 3f), new Vector3(47.2f, 18.4f, 3f), GameScript.s_Instance._dayAmount);
    sun.transform.GetChild(0).GetComponent<Light>().intensity = Mathf.Lerp(0.1f, 1f, GameScript.s_Instance._dayAmount);

    var c = Color.Lerp(_SkyColor / 1.4f, _SkyColor, GameScript.s_Instance._dayAmount);
    for (var i = 0; i < sky.transform.childCount; i++)
    {
      sky.transform.GetChild(i).GetComponent<MeshRenderer>().material.color = c;
    }
  }

  static IEnumerator TurnDayCo(float time)
  {

    var saveTime = time;
    while (time > 0f)
    {
      time -= 0.01f;
      yield return new WaitForSeconds(0.01f);
      s_Instance._dayAmount = 1f - time / saveTime;

      SetTimeStuff();
    }
    s_Instance._dayAmount = 1f;
    SetTimeStuff();
  }

  static IEnumerator TurnNightCo(float time)
  {
    float saveTime = time;
    while (time > 0f)
    {
      time -= 0.01f;
      yield return new WaitForSeconds(0.01f);
      s_Instance._dayAmount = time / saveTime;

      SetTimeStuff();
    }
    s_Instance._dayAmount = 0f;
    SetTimeStuff();
  }

  public static void TurnDay()
  {
    if (s_Instance._dayAmount != 0f) return;
    s_Instance.StartCoroutine(TurnDayCo(20f));
  }

  public static void TurnNight()
  {
    if (s_Instance._dayAmount != 1f) return;
    s_Instance.StartCoroutine(TurnNightCo(20f));
  }

  static float _shake, _shakeAmount, _decreaseFactor;
  static public void ShakeLight()
  {
    _shake = 0.2f;
    _shakeAmount = 0.2f;
    _decreaseFactor = 1f;
  }
  static public void ShakeHeavy()
  {
    _shake = 0.5f;
    _shakeAmount = 0.7f;
    _decreaseFactor = 1f;
  }
  static void UpdateScreenShake()
  {
    var camera = GameResources.s_Instance._CameraMain;
    camera.transform.localPosition = new Vector3(15.5f, 7.2f, -50f);
    if (_shake > 0f && _state == GameState.PLAY)
    {
      camera.transform.localPosition += UnityEngine.Random.insideUnitSphere * _shakeAmount;
      _shake -= Time.deltaTime * _decreaseFactor;
    }
    else
    {
      _shake = 0f;
    }
  }

  static public void Freeze()
  {
    s_Instance.StartCoroutine(TurnTimeFor(Time.timeScale == 2.5f ? 1f : 0f, 0.2f));
  }

  static IEnumerator TurnTimeFor(float newTime, float amount)
  {
    float save = Time.timeScale;
    Time.timeScale = newTime;
    yield return new WaitForSecondsRealtime(amount);
    Time.timeScale = save;
  }

  // Save player stats
  public static void SaveGame()
  {
    PlayerPrefs.SetInt("Wave", Wave.GetWaveCurrentWaveIter());
    PlayerPrefs.SetInt("Coins", PlayerScript.GetCoins());
    PlayerPrefs.SetInt("Health", PlayerScript.GetHealth());
    PlayerPrefs.SetInt("Ammo", PlayerScript.GetAmmoMax());

    // Max wave
    int wave = PlayerPrefs.GetInt("MaxWave", 0),
        currentWave = Wave.s_MetaWaveIter;
    if (currentWave > wave)
    {
      PlayerPrefs.SetInt("MaxWave", currentWave);
    }
  }

  // Load overall game settings
  public static void LoadGameSettings()
  {
    // Load music toggle
    var musicToggle = PlayerPrefs.GetInt("Music", 1) == 1;
    if (musicToggle)
    {
      MenuManager.ToggleMusic();
    }
  }

  // Reset player save / game over
  public static void ResetGame()
  {
    Debug.LogWarning("ResetGame() => Not defined");
  }

  public static void SetHealth(int health)
  {
    int plHealth = PlayerScript.GetHealth();
    if (plHealth == 1)
    {
      if (health == 2)
      {
        PlayerScript.AddHealth();
      }
      else if (health == 3)
      {
        PlayerScript.AddHealth();
        PlayerScript.AddHealth();
      }
    }
    else if (plHealth == 2)
    {
      if (health == 3)
      {
        PlayerScript.AddHealth();
      }
      else if (health == 1)
      {
        PlayerScript.RemoveHealth();
      }
    }
    else if (plHealth == 3)
    {
      if (health == 2)
      {
        PlayerScript.RemoveHealth();
      }
      else if (health == 1)
      {
        PlayerScript.RemoveHealth();
        PlayerScript.RemoveHealth();
      }
    }
    else if (plHealth == 0)
    {
      if (health == 1)
      {
        PlayerScript.AddHealth();
      }
      else if (health == 2)
      {
        PlayerScript.AddHealth();
        PlayerScript.AddHealth();
      }
      else if (health == 3)
      {
        PlayerScript.AddHealth();
        PlayerScript.AddHealth();
        PlayerScript.AddHealth();
      }
    }
  }

  static IEnumerator CoinFall()
  {
    ParticleSystem s = GameObject.Find("CoinParticleSystem").GetComponent<ParticleSystem>();
    s.Play();
    while (_state == GameState.WIN)
    {
      yield return new WaitForSeconds(0.07f);
      GameObject n = Instantiate(s.transform.GetChild(0).gameObject);
      n.transform.parent = GameResources.s_Instance._ContainerDead;
      PlaySound(n.GetComponent<AudioSource>(), 0.85f, 1.1f);
    }
    s.Stop();
  }

  static IEnumerator WinCo()
  {
    GameObject alive = GameObject.Find("Alive");
    for (int i = alive.transform.childCount - 1; i >= 0; i--)
    {
      try
      {
        SpawnExplosion(alive.transform.GetChild(i).position);
        alive.transform.GetChild(i).GetComponent<EnemyScript>().Die(true);
      }
      catch (Exception e)
      {

      }
      yield return new WaitForSeconds(0.2f);
    }
  }

  static public void PlaySound(GameObject g, float minPitch = 1f, float maxPitch = 1f)
  {
    AudioSource s = g.GetComponent<AudioSource>();
    PlaySound(s, minPitch, maxPitch);
  }

  static public void PlaySound(AudioSource s, float minPitch = 1f, float maxPitch = 1f)
  {
    s.pitch = minPitch + UnityEngine.Random.value * (maxPitch - minPitch);
    s.Play();
  }

  public static bool StateAtMainMenu()
  {
    return _state == GameState.MAIN_MENU;
  }

  public static bool StateAtPlay()
  {
    return _state == GameState.PLAY;
  }

  public static void Lose()
  {
    if (_mode == GameMode.WAVES)
    {
      // Erase game data
      ResetGame();
    }

    // Stop music
    GameScript.s_Instance.GetComponent<AudioSource>().Stop();

    // Set state
    _state = GameState.LOSE;

    // Show lose menu
    MenuManager.ShowMenuAfterTime(MenuManager._loseMenu, 2f);

    // Hide pause button
    MenuManager._pauseButton.SetActive(false);

    // Lose noise
    GameScript.PlaySound(GameObject.Find("Lose"));
  }

  static public void Win()
  {
    if (!StateAtPlay()) return;
    _state = GameState.WIN;

    // Win noise
    GameScript.PlaySound(GameObject.Find("Lose"));
    s_Instance.StartCoroutine(WinCo());
    s_Instance.StartCoroutine(CoinFall());
    MenuManager.ShowMenuAfterTime(MenuManager._winMenu, 5f);
    MenuManager.ToggleGameUI(false);
    MenuManager._pauseButton.SetActive(false);

    // Save the win
    PlayerPrefs.SetInt("Win0", 1);

    // Reset local game
    ResetGame();
    s_Instance.StartCoroutine(MenuManager.ShowWinStats());
  }

  static public TargetScript SpawnTarget(Vector2 position, float size = 1f, TargetScript.TargetType type = TargetScript.TargetType.ROLL)
  {
    var t = Instantiate(GameObject.Find("Target"));
    t.name = "Target";
    t.transform.localScale = new Vector3(size, size, 1f);
    t.transform.position = new Vector3(50f + UnityEngine.Random.value * 10f, -3.5f + UnityEngine.Random.value * 18f, 0f);
    t.transform.parent = GameObject.Find("Alive").transform;
    var s = t.GetComponent<TargetScript>();
    s.Init(position, type);
    return s;
  }

  static public ParticleSystem SpawnExplosion(Vector3 position)
  {
    // Spawn explosion
    var explosion = Instantiate(Resources.Load("ParticleSystems/ExplosionSystem") as GameObject);
    explosion.transform.parent = GameResources.s_Instance._ContainerDead;
    explosion.transform.position = position;
    return explosion.GetComponent<ParticleSystem>();
  }

  static public void SwitchModes(GameMode newMode)
  {
    if (_mode == newMode) return;
    GameObject barrier = GameObject.Find("BarrierMod"),
        spawnLine = GameResources.s_Instance._SpawnLine.gameObject,
        waveButton = GameObject.Find("MainMenu").transform.GetChild(1).gameObject,
        targetButton = GameObject.Find("MainMenu").transform.GetChild(2).gameObject;
    waveButton.GetComponent<MeshRenderer>().material.color = GameObject.Find("Stone").GetComponent<MeshRenderer>().material.color * 1.5f;
    targetButton.GetComponent<MeshRenderer>().material.color = GameObject.Find("Stone").GetComponent<MeshRenderer>().material.color * 1.5f;
    switch (newMode)
    {
      case GameMode.TARGETS:
        barrier.transform.position = new Vector3(250f, 0f, 0f);
        //spawnLine.transform.position = new Vector3(250f, 0f, 0f);
        targetButton.GetComponent<MeshRenderer>().material.color = GameObject.Find("Play").GetComponent<MeshRenderer>().material.color;
        break;
      case GameMode.WAVES:
        barrier.transform.position = new Vector3(26.8f, -5.5f, 0f);
        //spawnLine.transform.position = new Vector3(62.1f, -3.1f, 0f);
        waveButton.GetComponent<MeshRenderer>().material.color = GameObject.Find("Play").GetComponent<MeshRenderer>().material.color;
        break;
    }
    _mode = newMode;
  }

}
