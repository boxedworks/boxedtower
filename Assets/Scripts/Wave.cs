using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wave
{
  static int _WaveIter;
  public static int s_MetaWaveIter;

  static List<Wave> _Waves;

  public Wave()
  {
    _enemies = new List<EnemyScript>();
  }

  List<EnemyScript> _enemies;
  float _waveTimer;
  bool _setNextWave;

  void Start()
  {
    foreach (EnemyScript e in _enemies)
    {
      e.gameObject.SetActive(true);
    }
  }

  static public void AddWave(EnemyScript.EnemyType type, int amount, float time = 0f, bool nextWave = false)
  {
    // Null check
    if (_Waves == null)
    {
      _Waves = new List<Wave>();
    }
    // Create wave
    Wave wave = new Wave();
    wave._waveTimer = time;
    wave._setNextWave = nextWave;

    // Add enemies to wave
    for (int i = 0; i < amount; i++)
    {
      EnemyScript e = EnemyScript.SpawnEnemy(type);
      wave._enemies.Add(e);
      e.gameObject.SetActive(false);
      // Random chance to add crate
      if (Mathf.RoundToInt(Random.value * 15f) == 0f)
      {
        e = EnemyScript.SpawnEnemy(EnemyScript.EnemyType.CRATE);
        wave._enemies.Add(e);
        e.gameObject.SetActive(false);
      }
      // Make harder if beaten game
      if (GameScript.s_HasWon && Mathf.RoundToInt(Random.value * 20f) == 0f)
      {
        switch (Mathf.RoundToInt(Random.value * 3f))
        {
          case 0:
            e = EnemyScript.SpawnEnemy(EnemyScript.EnemyType.GROUND_ROLL);
            break;
          case 1:
            e = EnemyScript.SpawnEnemy(EnemyScript.EnemyType.GROUND_ROLL);
            break;
          case 2:
            e = EnemyScript.SpawnEnemy(EnemyScript.EnemyType.GROUND_POP_FLOAT);
            break;
          case 3:
            e = EnemyScript.SpawnEnemy(EnemyScript.EnemyType.GROUND_POP);
            break;
        }
        wave._enemies.Add(e);
        e.gameObject.SetActive(false);
      }
    }
    // Add wave to list
    _Waves.Add(wave);
  }

  static public void SendNextWave()
  {
    // Do not send wave if lost
    if (GameScript._state == GameScript.GameState.LOSE) return;

    // Check if wave is over
    if (_WaveIter + 1 == _Waves.Count)
    {
      GameScript.PlaySound(GameObject.Find("WaveEnd"));
      MenuManager.MenuBetweenWaves();

      Resources.UnloadUnusedAssets();
      System.GC.Collect();

      return;
    }

    // Send next wave
    _Waves[++_WaveIter].Start();
  }

  static public bool AllEnemiesDead()
  {
    GameObject alive = GameObject.Find("Alive");
    if (alive.transform.childCount == 0) return true;
    for (int i = 0; i < alive.transform.childCount; i++)
    {
      if (alive.transform.GetChild(i).gameObject.activeSelf) return false;
    }
    return true;
  }

  static public Wave GetCurrentWave()
  {
    return _Waves[_WaveIter];
  }

  static float waveTimer;

  static public void Update()
  {
    var w = GetCurrentWave();
    if (AllEnemiesDead())
    {
      if (w._setNextWave)
      {
        s_MetaWaveIter++;
      }
      SendNextWave();
      waveTimer = 0f;
    }

    // Check timed waves
    else if (w._waveTimer > 0f)
    {
      waveTimer += Time.deltaTime;
      if (waveTimer > w._waveTimer)
      {
        if (w._setNextWave)
        {
          s_MetaWaveIter++;
        }
        SendNextWave();
        waveTimer = 0f;
      }
    }
  }

  static public void StartWaves()
  {
    // Begin first wave
    GetCurrentWave().Start();

    // Move wave sign
    GetCurrentWave().WaveSign();
  }

  static public void ClearWaves()
  {
    _Waves = null;
    _WaveIter = 0;
  }

  static public int GetWaveCurrentWaveIter()
  {
    return s_MetaWaveIter;
  }

  void WaveSign()
  {
    GameScript._Game.StartCoroutine(MoveWaveSign());
  }

  IEnumerator MoveWaveSign()
  {
    var sign = MenuManager._waveSign;
    sign.transform.localPosition = new Vector3(0f, 0f, 10f);
    sign.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>().text = "" + (Wave.s_MetaWaveIter);
    sign.SetActive(true);
    GameScript.PlaySound(GameObject.Find("WaveBegin"));
    yield return new WaitForSecondsRealtime(1.5f);
    float t = 0f;
    while (t <= 1f)
    {
      t += 0.05f;
      yield return new WaitForSecondsRealtime(0.01f);
      sign.transform.localPosition = Vector3.Slerp(new Vector3(0f, 0f, 10f), new Vector3(0f, 29.23f, 10f), t);
    }
  }

  // Destroy enemies in alive/dead
  public static void DestroyAll()
  {
    GameObject dead = GameObject.Find("Dead"),
        alive = GameObject.Find("Alive"),
        arrows = GameObject.Find("Arrows");
    for (int i = dead.transform.childCount - 1; i >= 0; i--)
    {
      Object.Destroy(dead.transform.GetChild(i).gameObject);
    }
    for (int i = alive.transform.childCount - 1; i >= 0; i--)
    {
      Object.Destroy(alive.transform.GetChild(i).gameObject);
    }
    for (int i = arrows.transform.childCount - 1; i >= 0; i--)
    {
      Object.Destroy(arrows.transform.GetChild(i).gameObject);
    }
  }

  // Hardcoded waves
  static public void LoadWaves(int waveNumber)
  {
    ClearWaves();
    DestroyAll();
    switch (waveNumber)
    {
      case 0:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 1, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 10f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 13f);

        /*AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 1, 10f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 2f);*/
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 2, 0f, true);
        break;
      case 1:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 1, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 6, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 4, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 0f, true);
        break;
      case 2:
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 2, 15f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 4, 0f, true);
        break;
      case 3:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 6, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 5, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 20f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 5, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 0f, true);
        break;
      case 4:
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 6, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 20f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 4, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 8, 0f, true);
        break;
      case 5:
        AddWave(EnemyScript.EnemyType.GROUND_POP, 3, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 6, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 20f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 0f, true);
        break;
      case 6:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 4, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 7, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 6, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 8, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 1, 0f, true);
        break;
      case 7:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 5, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 3, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 6, 9f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 5, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 6, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 0f, true);
        break;
      case 8:
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 20f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 6, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 2, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 25f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 4, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 0f, true);
        break;
      case 9:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 25f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 2, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 0f, true);
        break;
      case 10:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 7, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 12f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 7, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 6, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 12f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 0f, true);
        break;
      case 11:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 4, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 9f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 4, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 2, 20f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 2, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 2, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 6, 9f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 2, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 6, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 9f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 0f, true);
        break;
      case 12:
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 2, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 7, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 2, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 0f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 0f);

        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 3, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 0f, true);
        break;
      case 13:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 8, 15f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 2, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 3, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 2, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 30f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 0f, true);
        break;
      case 14:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 15, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 5, 10f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 10, 9f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 12f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 0f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 15, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 5, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 0.1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 4f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 8, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 0f, true);
        break;
      case 15:
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 10, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_TOP, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 23f);

        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_TOP, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 1, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_TOP, 1, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 4, 20f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 10, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_TOP, 1, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 7, 6f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 2f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_TOP, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 3f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 2, 0f, true);
        break;
      case 16:
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 8f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 15, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 10, 25f);

        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_TOP, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 10, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 12f);
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_POP, 4, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 12f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_TOP, 1, 10f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 5, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 1, 7f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 2, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 10, 5f);
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_CASTLE, 1, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 10, 1f);
        AddWave(EnemyScript.EnemyType.GROUND_ROLL_SMALL, 10, 1f);
        AddWave(EnemyScript.EnemyType.BOSS, 1, 0f);
        break;
      default:
        Wave.AddWave(EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 10, 0f, false);
        break;
    }
  }
}

