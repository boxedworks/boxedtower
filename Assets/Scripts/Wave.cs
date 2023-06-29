using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class Wave
{
  static int s_waveIter;
  public static int s_MetaWaveIter;

  static List<Wave> s_waves;

  static bool s_shopVisible;
  static float s_timeShopVisible;

  public Wave()
  {
    _enemies = new List<EnemyScript>();
  }

  List<EnemyScript> _enemies;
  float _waveTimer;
  bool _setNextWave;

  void Start()
  {
    IEnumerator setActive()
    {
      var lastSlide = 0f;
      foreach (var e in _enemies)
      {
        if (e._IsSlide)
          Debug.Log($"Spawning slide [{e._EnemyType}]");
        while (e._IsSlide && Time.time - lastSlide < 0f)
        {
          yield return new WaitForSeconds(0.25f);
        }
        if (GameScript._state != GameScript.GameState.PLAY)
          break;

        e.Spawn();

        if (e._IsSlide)
        {
          Debug.Log($"Spawned slide [{e._EnemyType}]: {Time.time}");
          var slideWait = 7.5f;
          switch (e._EnemyType)
          {
            case EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM:
              slideWait = 19f;
              break;

            case EnemyScript.EnemyType.GROUND_SLIDE:
            case EnemyScript.EnemyType.GROUND_SLIDE_TOP:
              slideWait = 15f;
              break;
          }
          lastSlide = Time.time + slideWait;
        }
        yield return new WaitForSeconds(0.1f);
      }
    }
    GameScript.s_Instance.StartCoroutine(setActive());
  }

  static public bool ShopVisible()
  {
    return s_shopVisible;
  }

  static public void ShowShop()
  {
    s_shopVisible = true;
    s_timeShopVisible = Time.time;
    ToggleShopUI(true);

    GameResources.s_Instance._SliderShop.value = 1f;
  }
  static public void HideShop()
  {
    s_shopVisible = false;
    ToggleShopUI(false);
  }

  static public void ToggleShopUI(bool toggle)
  {
    MenuManager.s_ShopButton.SetActive(toggle);

    var slider = GameResources.s_Instance._SliderShop;
    slider.transform.parent.gameObject.SetActive(toggle);
  }

  static public void AddWave(
      EnemyScript.EnemyType type,
      int enemyCount,
      float nextWaveDelay = 0f,
      bool waveEnd = false
  )
  {
    // Null check
    if (s_waves == null)
    {
      s_waves = new List<Wave>();
    }

    // Create wave
    var wave = new Wave();
    wave._waveTimer = nextWaveDelay;
    wave._setNextWave = waveEnd;

    // Add enemies to wave
    for (var i = 0; i < enemyCount; i++)
    {
      var e = EnemyScript.SpawnEnemy(type);
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
    s_waves.Add(wave);
  }

  static public void SendNextWave()
  {
    // Do not send wave if lost
    if (GameScript._state == GameScript.GameState.LOSE)
      return;

    // Check if wave is over
    if (s_waveIter + 1 == s_waves.Count)
    {

      // Shop shop
      ShowShop();

      // Next wave audio
      GameScript.PlaySound(GameObject.Find("WaveEnd"));
      //MenuManager.MenuBetweenWaves();

      // Clean up
      Resources.UnloadUnusedAssets();
      System.GC.Collect();

      // Next wave
      MenuManager.StartGame();

      return;
    }

    // Send next wave
    s_waves[++s_waveIter].Start();
  }

  static public bool AllEnemiesDead()
  {
    GameObject alive = GameObject.Find("Alive");
    if (alive.transform.childCount == 0)
      return true;
    for (int i = 0; i < alive.transform.childCount; i++)
    {
      if (alive.transform.GetChild(i).gameObject.activeSelf)
        return false;
    }
    return true;
  }

  static public Wave GetCurrentWave()
  {
    return s_waves[s_waveIter];
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

    // Shop timer
    if (s_shopVisible)
    {
      var maxTime = 35f;
      GameResources.s_Instance._SliderShop.value = (Time.time - s_timeShopVisible) / 15f;
      if (Time.time - s_timeShopVisible > maxTime)
      {
        s_shopVisible = false;
        ToggleShopUI(false);
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
    s_waves = null;
    s_waveIter = 0;
  }

  static public int GetWaveCurrentWaveIter()
  {
    return s_MetaWaveIter;
  }

  void WaveSign()
  {
    GameScript.s_Instance.StartCoroutine(MoveWaveSign());
  }

  IEnumerator MoveWaveSign()
  {
    var sign = MenuManager._waveSign;
    sign.transform.localPosition = new Vector3(0f, 30.5f, 10f);
    sign.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>().text =
        "" + (Wave.s_MetaWaveIter);
    sign.SetActive(true);
    GameScript.PlaySound(GameObject.Find("WaveBegin"));
    yield return new WaitForSecondsRealtime(1.5f);
    float t = 0f;
    while (t <= 1f)
    {
      t += 0.05f;
      yield return new WaitForSecondsRealtime(0.01f);
      sign.transform.localPosition = Vector3.Slerp(
          new Vector3(0f, 30.5f, 10f),
          new Vector3(0f, 33.52f, 10f),
          t
      );
    }
  }

  // Destroy enemies in alive/dead
  public static void DestroyAll()
  {
    Transform dead = GameResources.s_Instance._ContainerDead,
        alive = GameObject.Find("Alive").transform,
        arrows = GameObject.Find("Arrows").transform;

    for (var i = dead.childCount - 1; i >= 0; i--)
      Object.Destroy(dead.GetChild(i).gameObject);
    for (var i = alive.childCount - 1; i >= 0; i--)
      Object.Destroy(alive.GetChild(i).gameObject);
    for (var i = arrows.childCount - 1; i >= 0; i--)
      Object.Destroy(arrows.GetChild(i).gameObject);
  }

  // Random load waves
  static void QueueWaves(
      int pointsTotal,
      int pointsAddRandom,
      Dictionary<EnemyScript.EnemyType, (int, float)> enemyPool,
      bool setLastWave,
      int subWaves,
      int subWavesAddRandom
  )
  {
    // Set points allocated
    pointsTotal += Random.Range(0, pointsAddRandom);

    // Create subwaves
    subWaves += Random.Range(0, subWavesAddRandom);
    var subwaveCounts = new int[subWaves];
    var pointsAllocated = pointsTotal;
    var lastSlide = 0;
    for (var i = 0; i < subWaves; i++)
    {
      var pointChunk =
          i == subWaves - 1
              ? pointsAllocated
              : Mathf.RoundToInt(
                  pointsTotal
                      * (1f / subWaves)
                      * (i == 0 ? Random.Range(0.3f, 0.8f) : Random.Range(0.8f, 1.2f))
              );
      pointChunk = Mathf.Clamp(pointChunk, 1, pointsAllocated);
      subwaveCounts[i] = pointChunk;
      pointsAllocated -= pointChunk;
    }

    // Calculate enemy rarities based on cost
    var totalCost = 0;
    var minCost = 1000;
    foreach (var enemyData in enemyPool)
    {
      var pointCost = enemyData.Value.Item1;
      if (pointCost < minCost)
        minCost = pointCost;
      totalCost += pointCost;
    }
    var enemyPoolTypes = new List<EnemyScript.EnemyType>();
    foreach (var enemyData in enemyPool)
    {
      var pointCost = enemyData.Value.Item1;
      var multiplier = enemyData.Value.Item2;
      pointCost = Mathf.Clamp(Mathf.RoundToInt(pointCost / multiplier), 1, 10000);
      var ratio = Mathf.Clamp((int)totalCost / pointCost, 1, 1000);
      //Debug.LogWarning($"{enemyData.Key}: {ratio}");
      for (var i = 0; i < ratio; i++)
        enemyPoolTypes.Add(enemyData.Key);
    }

    // Populate waves
    for (var i = 0; i < subWaves; i++)
    {
      var waveChunk = subwaveCounts[i];

      while (waveChunk > 0)
      {
        var enemyType = enemyPoolTypes[Random.Range(0, enemyPoolTypes.Count)];

        var enemyData = enemyPool[enemyType];
        var pointCost = enemyData.Item1;

        if (pointCost > waveChunk + minCost + 1)
          continue;

        var isSlide = enemyType.ToString().ToLower().Contains("slide");
        if (isSlide && lastSlide < 8)
          continue;
        lastSlide++;

        waveChunk -= pointCost;

        var lastSpawn = waveChunk <= 0;
        var lastSubwave = setLastWave && lastSpawn && i == subWaves - 1;

        // Default time between spawns
        var waitTime = 0.1f;

        // If last spawn in last subwave, wait for all dead
        if (lastSubwave)
        {
          waitTime = 0f;
        }
        // If last spawn in subwave, either wait for all dead or wait a period between waves
        else if (lastSpawn)
        {
          if (Random.Range(0, 6) == 0 && Wave.s_MetaWaveIter > 2)
            waitTime = 0f;
          else
            waitTime = 15f;
        }
        // Towers need a wait time between spawns
        else if (isSlide)
        {
          waitTime = 6f + Random.Range(0f, 2f);
        }

        AddWave(enemyType, 1, waitTime, lastSubwave);
      }
    }
  }

  // Hardcoded waves
  static public void LoadWaves(int waveNumber)
  {
    // Reset
    ClearWaves();
    //DestroyAll();

    // Load waves
    switch (waveNumber)
    {
      // Unlimited waves
      default:

        //var waveNumberModified = waveNumber - 16 + 1;

        var wavePoints =
            80 + (int)(waveNumber * (40f * Mathf.Clamp(waveNumber * 0.07f, 0f, 10000)));
        var wavePointsRandom = (int)Random.Range(5, 10 + waveNumber * 1.5f);

        var subwaveCount = (int)Random.Range(1, 3 + waveNumber * 0.08f);
        var subwaveCountRandom = (int)Random.Range(0, 1 + waveNumber * 0.09f);

        // Enemy pool
        var enemyPool = new Dictionary<EnemyScript.EnemyType, (int, float)>();

        var enemyPoolTotal = new List<EnemyScript.EnemyType>();

        var enemyPoolDef = new Dictionary<EnemyScript.EnemyType, (int, int, float)>()
        {
          { EnemyScript.EnemyType.GROUND_ROLL, (5, 0, 1f) },
          { EnemyScript.EnemyType.GROUND_ROLL_SMALL, (8, 0, 0.5f) },
          { EnemyScript.EnemyType.GROUND_POP, (10, 6, 1f) },
          { EnemyScript.EnemyType.GROUND_POP_FLOAT, (13, 12, 1f) },
          { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, (15, 4, 0.95f) },
          { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, (23, 10, 0.95f) },
          { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, (30, 12, 0.95f) },
          { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, (18, 2, 0.85f) },
          { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, (25, 8, 0.85f) },
          { EnemyScript.EnemyType.GROUND_SLIDE, (35, 14, 0.85f) },
          { EnemyScript.EnemyType.GROUND_SLIDE_TOP, (30, 16, 0.85f) },
        };

        // Create enemy pool for wave selection based on wave progression
        foreach (var enemyType in enemyPoolDef.Keys)
        {
          var enemyData = enemyPoolDef[enemyType];

          var pointCost = enemyData.Item1;
          var waveUnlock = enemyData.Item2;

          if (waveNumber >= waveUnlock)
          {
            enemyPoolTotal.Add(enemyType);
          }
        }

        // Add enemy to pool for waves
        var minPoolCount = 5;
        var r = Random.value;
        if (r < 0.05f)
        {
          minPoolCount = 2;
        }
        else if (r < 0.2f)
        {
          minPoolCount = 3;
        }

        var enemyPoolCount = Random.Range(minPoolCount, enemyPoolTotal.Count);
        enemyPoolCount = Mathf.Clamp(enemyPoolCount, 1, enemyPoolTotal.Count);
        var enemyPoolTotalSave = enemyPoolTotal.Count;
        for (var i = 0; i < enemyPoolCount; i++)
        {
          var randomEnemy = enemyPoolTotal[Random.Range(0, enemyPoolTotal.Count)];
          enemyPoolTotal.Remove(randomEnemy);
          enemyPool.Add(
              randomEnemy,
              (enemyPoolDef[randomEnemy].Item1, enemyPoolDef[randomEnemy].Item3)
          );
        }

        // Check for some small
        if (
            enemyPool.Keys.Contains(EnemyScript.EnemyType.GROUND_ROLL_SMALL)
            && (
                !enemyPool.Keys.Contains(EnemyScript.EnemyType.GROUND_ROLL)
                && !enemyPool.Keys.Contains(EnemyScript.EnemyType.GROUND_POP)
                && !enemyPool.Keys.Contains(EnemyScript.EnemyType.GROUND_POP_FLOAT)
            )
        )
        {
          enemyPool.Add(
              EnemyScript.EnemyType.GROUND_ROLL,
              (
                  enemyPoolDef[EnemyScript.EnemyType.GROUND_ROLL].Item1,
                  enemyPoolDef[EnemyScript.EnemyType.GROUND_ROLL].Item3
              )
          );
        }

        Debug.Log(
            $"Custom wave... Points ({wavePoints}, {wavePointsRandom}) . Subwaves ({subwaveCount}, {subwaveCountRandom}) . Pool count ({enemyPoolCount} / {enemyPoolTotalSave})"
        );

        // Queue
        QueueWaves(
            // Points
            wavePoints,
            wavePointsRandom,
            // Enemy pool
            enemyPool,
            true,
            // SubWaves
            subwaveCount,
            subwaveCountRandom
        );

        break;
    }
  }
}
