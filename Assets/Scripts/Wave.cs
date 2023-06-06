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

    IEnumerator setActive()
    {

      var lastSlide = 0f;
      foreach (var e in _enemies)
      {
        while (e._IsSlide && Time.time - lastSlide < 6f)
        {
          yield return new WaitForSeconds(0.25f);
        }
        if (GameScript._state != GameScript.GameState.PLAY) break;
        e.gameObject.SetActive(true);
        if (e._IsSlide)
          lastSlide = Time.time;

        yield return new WaitForSeconds(0.1f);
      }
    }
    GameScript.s_Instance.StartCoroutine(setActive());
  }

  static public void AddWave(EnemyScript.EnemyType type, int enemyCount, float nextWaveDelay = 0f, bool waveEnd = false)
  {
    // Null check
    if (_Waves == null)
    {
      _Waves = new List<Wave>();
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
    GameScript.s_Instance.StartCoroutine(MoveWaveSign());
  }

  IEnumerator MoveWaveSign()
  {
    var sign = MenuManager._waveSign;
    sign.transform.localPosition = new Vector3(0f, 30.5f, 10f);
    sign.transform.GetChild(0).GetChild(0).GetComponent<TextMesh>().text = "" + (Wave.s_MetaWaveIter);
    sign.SetActive(true);
    GameScript.PlaySound(GameObject.Find("WaveBegin"));
    yield return new WaitForSecondsRealtime(1.5f);
    float t = 0f;
    while (t <= 1f)
    {
      t += 0.05f;
      yield return new WaitForSecondsRealtime(0.01f);
      sign.transform.localPosition = Vector3.Slerp(new Vector3(0f, 30.5f, 10f), new Vector3(0f, 33.52f, 10f), t);
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

    Dictionary<EnemyScript.EnemyType, int> enemyPool,
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
      var pointChunk = i == subWaves - 1 ? pointsAllocated : Mathf.RoundToInt(pointsTotal * (1f / subWaves) * (i == 0 ? Random.Range(0.3f, 0.8f) : Random.Range(0.8f, 1.2f)));
      pointChunk = Mathf.Clamp(pointChunk, 1, pointsAllocated);
      subwaveCounts[i] = pointChunk;
      pointsAllocated -= pointChunk;
    }

    // Calculate enemy rarities based on cost
    var totalCost = 0;
    foreach (var enemyData in enemyPool)
      totalCost += enemyData.Value;
    var enemyPoolTypes = new List<EnemyScript.EnemyType>();
    foreach (var enemyData in enemyPool)
    {
      var ratio = Mathf.Clamp((int)totalCost / enemyData.Value, 1, 1000);
      Debug.LogWarning($"{enemyData.Key}: {ratio}");
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
        var enemyCost = enemyPool[enemyType];

        if (enemyCost > waveChunk + 4)
          continue;

        var isSlide = enemyType.ToString().ToLower().Contains("slide");
        if (isSlide && lastSlide < 8) continue;
        lastSlide++;

        waveChunk -= enemyCost;

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
    DestroyAll();

    // Load waves
    switch (waveNumber)
    {
      case 0:

        // Queue
        QueueWaves(

          // Points
          60, 10,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 }
          },
          true,

          // SubWaves
          3, 0
        );

        break;
      case 1:

        // Queue
        AddWave(EnemyScript.EnemyType.GROUND_ROLL, 3, 5f);
        QueueWaves(

          // Points
          80, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
          },
          true,

          // SubWaves
          3, 0
        );

        break;
      case 2:
        AddWave(EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 1, 15f);
        QueueWaves(

          // Points
          110, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
          },
          true,

          // SubWaves
          3, 1
        );

        break;
      case 3:
        QueueWaves(

          // Points
          130, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
          },
          true,

          // SubWaves
          3, 1
        );

        break;
      case 4:
        QueueWaves(

          // Points
          130, 20,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
          },
          true,

          // SubWaves
          3, 1
        );

        break;
      case 5:
        AddWave(EnemyScript.EnemyType.GROUND_POP, 3, 15f);
        QueueWaves(

          // Points
          140, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
          },
          true,

          // SubWaves
          3, 1
        );

        break;
      case 6:
        QueueWaves(

          // Points
          150, 20,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
          },
          true,

          // SubWaves
          3, 0
        );

        break;
      case 7:
        QueueWaves(

          // Points
          165, 20,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
            { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 25 },
          },
          true,

          // SubWaves
          3, 1
        );

        break;
      case 8:
        QueueWaves(

          // Points
          170, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
            { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 25 },
            { EnemyScript.EnemyType.GROUND_SLIDE, 35 },
          },
          true,

          // SubWaves
          3, 0
        );

        break;
      case 9:
        QueueWaves(

          // Points
          190, 20,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 30 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
            { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 25 },
            { EnemyScript.EnemyType.GROUND_SLIDE, 35 },
          },
          true,

          // SubWaves
          3, 0
        );

        break;
      case 10:
        QueueWaves(

          // Points
          210, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 30 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
            { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 25 },
            { EnemyScript.EnemyType.GROUND_SLIDE, 35 },
          },
          true,

          // SubWaves
          3, 0
        );

        break;
      case 11:
        QueueWaves(

          // Points
          225, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 23 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 30 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
            { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 25 },
            { EnemyScript.EnemyType.GROUND_SLIDE, 35 },
          },
          true,

          // SubWaves
          3, 1
        );

        break;
      case 12:
        AddWave(EnemyScript.EnemyType.GROUND_POP_FLOAT, 2, 15f);

        QueueWaves(

          // Points
          235, 15,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_POP_FLOAT, 13 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 23 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 30 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
            { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 25 },
            { EnemyScript.EnemyType.GROUND_SLIDE, 35 },
          },
          true,

          // SubWaves
          3, 1
        );

        break;
      case 13:
        QueueWaves(

          // Points
          235, 10,

          // Enemy pool
          new Dictionary<EnemyScript.EnemyType, int>(){
            { EnemyScript.EnemyType.GROUND_ROLL, 5 },
            { EnemyScript.EnemyType.GROUND_ROLL_SMALL, 8 },
            { EnemyScript.EnemyType.GROUND_POP, 10 },
            { EnemyScript.EnemyType.GROUND_POP_FLOAT, 13 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_2, 15 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_4, 23 },
            { EnemyScript.EnemyType.GROUND_ROLL_ARMOR_8, 30 },
            { EnemyScript.EnemyType.GROUND_SLIDE_SMALL, 18 },
            { EnemyScript.EnemyType.GROUND_SLIDE_MEDIUM, 25 },
            { EnemyScript.EnemyType.GROUND_SLIDE, 35 },
          },
          true,

          // SubWaves
          2, 1
        );

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

