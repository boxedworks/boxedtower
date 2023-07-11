using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
  public static PlayerScript s_Singleton;

  GameObject _indicator,
      _fingerPos,
      _indicator2,
      _indicatorArrow,
      _arrow,
      _fingerPos2,
      _tower,
      _crystal;

  Transform _laserPointer,
    _tower2;

  int _tower2ClosestEnemyIter;
  (EnemyScript, MeshRenderer) _tower2ClosestEnemy;

  AudioSource _sBowDraw,
      _sNoArrow,
      _sTowerMove,
      _sShoot,
      _sPowerOff,
      _sPowerOn,
      _sNotch,
      _sFireLoop,
      _sTower2Shoot;

  bool _shootReady;

  int _ammo,
    _maxAmmo = 3,
    _health,
    _coins,
    _targetNumber;
  float _ammoTimer,
    _slowMoTimer, _slowMoMax, _slowMoDisabled,
    _ammoTime = 1.3f,
    _invincibilityTimer,
    _combo,

    _tower2Timer, _tower2LastShootTimer;

  public static Color _GemColor;

  UnityEngine.UI.Image _sloMoSliderColor;

  public static float _COMBO_ADD = 0.05f,
      _COMBO_REMOVE = 0.15f;

  public static class WaveStats
  {
    public static int _arrowsShot,
        _coinsGained,
        _enemiesKilled;

    public static void Reset()
    {
      _arrowsShot = 0;
      _coinsGained = 0;
      _enemiesKilled = 0;

      s_Singleton._comboTimer = Time.time;
    }
  }

  // Use this for initialization
  void Start()
  {
    s_Singleton = this;

    _indicator = transform.GetChild(1).gameObject;
    _fingerPos = transform.GetChild(2).gameObject;
    _indicator2 = transform.GetChild(4).gameObject;
    _indicatorArrow = transform.GetChild(5).gameObject;
    _fingerPos2 = transform.GetChild(6).gameObject;
    _arrow = transform.GetChild(3).gameObject;

    _laserPointer = GameObject.Find("LaserPointer").transform;

    _tower = GameObject.Find("Tower");
    _tower2 = GameObject.Find("Tower2").transform;
    _crystal = _tower.transform.GetChild(0).gameObject;

    _sBowDraw = GameObject.Find("BowDraw").GetComponent<AudioSource>();
    _sNoArrow = GameObject.Find("NoArrow").GetComponent<AudioSource>();
    _sTowerMove = _tower.transform.GetChild(1).GetChild(0).GetComponent<AudioSource>();
    _sShoot = GameObject.Find("Shoot").GetComponent<AudioSource>();
    _sPowerOff = GameObject.Find("PowerOff").GetComponent<AudioSource>();
    _sPowerOn = GameObject.Find("PowerOn").GetComponent<AudioSource>();
    _sNotch = GameObject.Find("Notch").GetComponent<AudioSource>();
    _sFireLoop = GameObject.Find("FireLoop").GetComponent<AudioSource>();
    _sTower2Shoot = GameObject.Find("Tower2Shoot").GetComponent<AudioSource>();

    _health = 2;
    _combo = 1f;

    _GemColor = GameObject.Find("Play").transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
    _crystal.transform.parent = _tower.transform.parent;
  }

  public static void Init()
  {
    UpdateCoinComboUI();
    UIUpdateAmmoCounter();
    s_Singleton.UIResetFinger();
  }

  float yAdd;

  // Update is called once per frame
  void Update()
  {
    // Crystal
    _crystal.transform.Rotate(new Vector3(0f, 1f, 0f) * 100f * Time.deltaTime);
    _crystal.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.Lerp(
        _GemColor,
        Color.white,
        0 / 1000
    );
    _crystal.transform.position +=
        ((_tower.transform.position + new Vector3(0f, -0.5f, 0f)) - _crystal.transform.position)
        * Time.deltaTime
        * 2f;

    // Check pause
    if (!GameScript.StateAtPlay())
    {
      // Check audio while pause
      if (_sFireLoop.isPlaying)
        _sFireLoop.pitch = 0f;

      // Hotkey to close store
      if (MenuManager.s_ShopMenu._Visible)
        if (Input.GetKeyDown(KeyCode.S))
        {
          Shop.ToWave();
        }

      // Stats
      if (MenuManager.s_StatsMenu._Visible)
      {
        // Hotkey to close stats
        if (Input.GetKeyDown(KeyCode.I))
        {
          MenuManager.StatsMenuBack();
        }

        // Scroll stats
        var scrollWheel = Input.mouseScrollDelta.y;
        var scrollValue = scrollWheel == 0f ? 0 : (scrollWheel > 0f ? 1 : -1);
        if (scrollValue != 0)
        {
          Shop.IncrementStatsScroll(-scrollValue);
        }
      }

      // Unpause
      if (MenuManager.s_PauseMenu._Visible)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
          MenuManager.Resume();
        }

      return;
    }
    else
    {
      if (_sFireLoop.isPlaying)
        _sFireLoop.pitch = 1f;
    }

    // Tower2
    var tower2 = Shop.GetUpgradeCount(Shop.UpgradeType.TOWER2);
    if (tower2 > 0)
    {

      // Closest enemy
      var closestDistance = _tower2ClosestEnemy.Item1 == null ? 1000f : Vector3.Distance(_tower2.position, _tower2ClosestEnemy.Item2.transform.position);
      var sensorKeys = new List<int>(EnemyScript.s_Sensors.Keys);
      for (var i = 0; i < Mathf.Clamp(5, 0, sensorKeys.Count); i++)
      {
        var enemy_data = EnemyScript.s_Sensors[sensorKeys[_tower2ClosestEnemyIter++ % sensorKeys.Count]];
        var enemy = enemy_data.Item1;
        var sensor = enemy_data.Item2;

        if (!enemy.gameObject.activeSelf) continue;
        if (enemy._EnemyType == EnemyScript.EnemyType.CRATE || enemy._EnemyType == EnemyScript.EnemyType.BALLOON) continue;
        if (sensor.transform.position.x > _tower2.position.x - 2.5f) continue;

        if (_tower2ClosestEnemy.Item1 == null)
        {
          _tower2ClosestEnemy = enemy_data;
          closestDistance = Vector3.Distance(_tower2.position, sensor.transform.position);
          continue;
        }

        var distance = Vector3.Distance(_tower2.position, sensor.transform.position);
        if (distance < closestDistance)
        {
          _tower2ClosestEnemy = enemy_data;
          closestDistance = distance;
        }
      }

      if (_tower2ClosestEnemy.Item1 != null)
      {
        if (_tower2ClosestEnemy.Item1._IsDead)
        {
          _tower2ClosestEnemy.Item1 = null;
        }
        else
        {
          Debug.DrawLine(_tower2.transform.position, _tower2ClosestEnemy.Item2.transform.position, Color.red);
        }
      }

      // Shoot
      _tower2Timer = Mathf.Clamp(_tower2Timer - Time.unscaledDeltaTime, 0f, 10000f);
      _tower2LastShootTimer = Mathf.Clamp(_tower2LastShootTimer - Time.deltaTime, 0f, 10000f);
      var maxTime = _UpgradeFunction_tower2Rate.Invoke(tower2);
      GameResources.s_Instance._SliderTower2UI.value = 1f - ((_tower2Timer) / maxTime);
      if (_tower2Timer <= 0f && _tower2LastShootTimer <= 0f)
      {
        if (_tower2ClosestEnemy.Item1 != null)
        {

          // Closest enemy
          var shootPos = _tower2.GetChild(0).position;
          var targetPos = _tower2ClosestEnemy.Item2.transform.position;

          _tower2Timer += maxTime;
          _tower2LastShootTimer = 0.6f;

          var distance = (shootPos - targetPos);

          targetPos.x -= 1f - 0.04f * distance.x;
          targetPos.y += 0.15f * distance.x;

          // Force / dir
          var arrowForce = 2300f + 43f * distance.x;
          var dir = -(shootPos - targetPos);

          /*var dir = new Vector3(1f, 0f, 0f);

          var gravity = 9.81f;

          var startY = shootPos.y;
          var endY = -8.9f;

          var deltaY = startY - endY;

          var deltaYTime = Mathf.Sqrt(2f * deltaY / gravity);
          Debug.Log(deltaYTime);

          var startX = shootPos.x;
          var endX = targetPos.x;

          var deltaX = startX - endX;
          var xAcceleration = ((2f * -deltaX) / Mathf.Pow(deltaYTime, 2f));

          Debug.Log($"dt: {deltaX} a: {xAcceleration}");*/

          // Spawn arrow
          var arrow = SpawnArrow();
          arrow._GameSpawned = true;
          var arrowPos = shootPos;
          arrowPos.z = 0f;
          arrow.transform.position = arrowPos;

          // Fire!
          arrow.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
          arrow.Init();
          arrow.Fired();
          arrow.GetComponent<Rigidbody2D>().AddForce(dir.normalized * arrowForce);

          // FX
          _tower2.GetChild(0).rotation = Quaternion.LookRotation(dir);
          (_tower2.GetChild(0).GetChild(0).GetComponent<ParticleSystem>()).Play();
          _sTower2Shoot.Play();

          Debug.DrawLine(shootPos, targetPos, Color.green, 4f);
        }
      }
    }

    // Laser
    var laserActive = s_UpgradeEnabled_penetratingArrow && _shootReady;
    if (!laserActive)
    {
      if (_laserPointer.gameObject.activeSelf)
        _laserPointer.gameObject.SetActive(false);
    }
    else
    {
      if (!_laserPointer.gameObject.activeSelf)
        _laserPointer.gameObject.SetActive(true);
    }

    // Hotkeys
    var unlocks = GameResources.s_Instance._UI.GetChild(2);
    foreach (var inputpair in new (KeyCode, int)[]{
      (KeyCode.Alpha1, 1),
      (KeyCode.Alpha2, 2),
      (KeyCode.Alpha3, 3),
      (KeyCode.Alpha4, 4),
    })
    {
      var keycode = inputpair.Item1;
      var index = inputpair.Item2;
      if (Input.GetKeyDown(keycode))
        if (unlocks.childCount > index)
          Shop.UpgradeInput(unlocks.GetChild(index).name);
    }

    // Shop
    if (Wave.ShopVisible())
      if (Input.GetKeyDown(KeyCode.S))
      {
        UIInput("ShopButton");
      }

    // Stats
    //if (Wave.ShopVisible())
    if (Input.GetKeyDown(KeyCode.I))
    {
      UIInput("StatsButton");
    }

    // Pause
    if (!MenuManager.s_PauseMenu._Visible)
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        UIInput("ToPause");
      }

    // Cancel shot
    if (Input.GetMouseButtonDown(1))
    {
      UIResetFinger();
    }

    // Increment slowmo
    if (!GameScript.s_TimeSped)
    {
      var mod = 1f + (Shop.GetUpgradeCount(Shop.UpgradeType.SLOMO_INCREASE) * -0.15f);
      _slowMoTimer = Mathf.Clamp(_slowMoTimer - (Time.unscaledDeltaTime * mod), 0f, _slowMoMax);
      if (_slowMoTimer == 0f || Input.GetKeyDown(KeyCode.Space))
      {
        _slowMoDisabled = Time.unscaledTime;
        GameScript.s_TimeSped = true;
        Time.timeScale = 2.5f;
      }
    }
    else
    {
      _slowMoTimer = Mathf.Clamp(_slowMoTimer + Time.unscaledDeltaTime * 0.5f, 0f, _slowMoMax);
      if (Time.unscaledTime - _slowMoDisabled > 2f && Input.GetKeyDown(KeyCode.Space))
      {
        _slowMoDisabled = Time.unscaledTime;
        GameScript.s_TimeSped = false;
        Time.timeScale = 1f;
      }
    }
    GameResources.s_Instance._SliderSloMo.value = _slowMoTimer / _slowMoMax;
    if (_sloMoSliderColor == null)
    {
      _sloMoSliderColor = GameResources.s_Instance._SliderSloMo.transform.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>();
    }
    _sloMoSliderColor.color = Time.unscaledTime - _slowMoDisabled > 2f ? Color.white : Color.gray;

    // Increment ammo timer
    if (_ammo < _maxAmmo)
    {
      _ammoTimer -= Time.unscaledDeltaTime;
      if (_ammoTimer < 0f)
      {
        _ammoTimer = _ammoTime;
        GiveAmmo();
      }
    }
    // Check sound
    if (!GameScript.StateAtPlay() && _sBowDraw.isPlaying)
    {
      _sBowDraw.Stop();
      UIResetFinger();
    }

    if (_invincibilityTimer > 0f)
    {
      _invincibilityTimer -= Time.deltaTime;
    }

    // Render upgrades / skills

    // Check combo
    var comboDelta = 0f;
    if (Time.time - _comboTimer > 8f)
    {
      _comboTimer = Time.time;
      comboDelta -= PlayerScript._COMBO_REMOVE;
    }
    if (_combo == 1f)
      GameResources.s_Instance._SliderComboDecrease.value = 1f;
    else
      GameResources.s_Instance._SliderComboDecrease.value = 1f - ((Time.time - _comboTimer) / 8f);

    AddCombo(comboDelta);

    // Cheats
#if UNITY_EDITOR

    if (Input.GetKeyDown(KeyCode.C))
    {
      SetCoins(GetCoins() + 10000);
    }
    if (Input.GetKeyDown(KeyCode.V))
    {
      Wave.s_MetaWaveIter++;
    }
    if (Input.GetKeyDown(KeyCode.PageUp))
    {
      AddHealth();
    }
    if (Input.GetKeyDown(KeyCode.PageDown))
    {
      RemoveHealth();
    }
    if (Input.GetKeyDown(KeyCode.B))
    {
      for (var i = GameResources.s_Instance._ContainerAlive.childCount - 1; i >= 0; i--)
      {
        var script = GameResources.s_Instance._ContainerAlive.GetChild(i).GetComponent<EnemyScript>();
        if (script.gameObject.activeSelf)
          script.Die();
      }
    }

#endif
  }

  // Ammo
  public static void GiveAmmo()
  {
    s_Singleton._ammo++;
    s_Singleton._ammoTimer = s_Singleton._ammoTime;
    UIUpdateAmmoCounter();
  }

  public static int GetAmmo()
  {
    return s_Singleton._ammo;
  }

  public static void GiveCoins(int numCoins)
  {
    s_Singleton._coins += numCoins;
    WaveStats._coinsGained += numCoins;
    UpdateCoinComboUI();
  }

  // Increment player combo
  float _comboTimer;
  public static float s_Combo
  {
    get { return s_Singleton._combo; }
  }

  public static void AddCombo(float delta)
  {
    // Sanitize
    if (delta == 0f)
      return;

    // Check positive
    if (delta > 0)
    {
      var c = Shop.GetUpgradeCount(Shop.UpgradeType.BACKUP_ARCHER);
      if (c > 0 && Random.value <= c * 0.20f)
      {
        SpawnRandomArrowAbove();

        // Check proj
        c = Shop.GetUpgradeCount(Shop.UpgradeType.BACKUP_ARCHER_PROJECTILES);
        if (c > 0 && Random.value <= c * 0.10f)
        {
          SpawnRandomArrowAbove();
        }
      }
    }

    // Increment combo
    var saveCombo = s_Singleton._combo;
    s_Singleton._combo = Mathf.Clamp(s_Singleton._combo + delta, 1f, 100000f);

    // Reset timer and update UI
    if (saveCombo != s_Singleton._combo)
    {
      s_Singleton._comboTimer = Time.time;
      UpdateCoinComboUI();
    }
  }

  public static void SetCombo(float combo)
  {
    s_Singleton._combo = combo;
    s_Singleton._comboTimer = Time.time;
    UpdateCoinComboUI();
  }

  static void UpdateCoinComboUI()
  {
    var coins = s_Singleton._coins;
    var combo = s_Singleton._combo;
    var comboColor =
        combo > 1f
            ? combo > 2.5f
                ? "yellow"
                : "green"
            : "black";

    var comboText = string.Format("{0:0.00}", combo);

    GameResources.s_Instance._TextCoins.text =
        @$"x {coins}
<color={comboColor}>x {comboText}</color>";
  }

  // Combine upgrades
  // Upgrades
  public static bool s_UpgradeEnabled_triShot
  {
    get { return Shop.UpgradeEnabled(Shop.UpgradeType.TRI_SHOT); }
  }
  public static bool s_UpgradeEnabled_arrowRain
  {
    get { return Shop.UpgradeEnabled(Shop.UpgradeType.ARROW_RAIN); }
  }
  public static bool s_UpgradeEnabled_penetratingArrow
  {
    get { return Shop.UpgradeEnabled(Shop.UpgradeType.ARROW_PENETRATION); }
  }
  public static System.Func<int, float> _UpgradeFunction_tower2Rate => (int count) =>
  {
    return 9f - (count * 1f);
  };
  Shop.UpgradeType _upgrade0,
      _upgrade1;

  public bool RegisterUpgrade(Shop.UpgradeType upgradeType)
  {
    // Not using....
    if (_upgrade0 != Shop.UpgradeType.NONE)
      return false;

    if (_upgrade0 != Shop.UpgradeType.NONE && _upgrade1 != Shop.UpgradeType.NONE)
      return false;

    if (_upgrade0 == Shop.UpgradeType.NONE)
      _upgrade0 = upgradeType;
    else
      _upgrade1 = upgradeType;

    return true;
  }

  public Shop.UpgradeType GetUpgrade(int index)
  {
    return index == 0 ? _upgrade0 : _upgrade1;
  }

  public void UnregisterUpgrade(int index, bool purposeful = false)
  {
    UnregisterUpgrade(index == 0 ? _upgrade0 : _upgrade1, purposeful);
  }

  public void UnregisterUpgrade(Shop.UpgradeType upgradeType, bool purposeful = false)
  {
    if (_upgrade0 == Shop.UpgradeType.NONE && _upgrade1 == Shop.UpgradeType.NONE)
      return;

    if (_upgrade0 != Shop.UpgradeType.NONE)
    {
      _upgrade0 = Shop.UpgradeType.NONE;
      if (purposeful && _upgrade1 != Shop.UpgradeType.NONE)
      {
        _upgrade0 = _upgrade1;
        _upgrade1 = Shop.UpgradeType.NONE;
      }
    }
    else
      _upgrade1 = Shop.UpgradeType.NONE;
  }

  public void ResetUpgrades()
  {
    if (_upgrade0 != Shop.UpgradeType.NONE)
      Shop.ResetUpgrade(_upgrade0);
    if (_upgrade1 != Shop.UpgradeType.NONE)
      Shop.ResetUpgrade(_upgrade1);

    _upgrade0 = _upgrade1 = Shop.UpgradeType.NONE;
  }

  public void ResetUpgrade(Shop.UpgradeType upgradeType)
  {
    if (_upgrade0 == upgradeType)
      _upgrade0 = Shop.UpgradeType.NONE;
    if (_upgrade1 == upgradeType)
      _upgrade1 = Shop.UpgradeType.NONE;

    Shop.ResetUpgrade(upgradeType);
  }

  public int GetUpgradeOrder(Shop.UpgradeType upgradeType)
  {
    return _upgrade0 == upgradeType
        ? 0
        : _upgrade1 == upgradeType
            ? 1
            : -1;
  }

  // On mouse move
  int _notch;

  public void MouseMove()
  {
    if (Time.time - MenuManager.s_waveStart < 0.25f)
      return;
    if (_fingerPos.transform.localPosition == new Vector3(-50f, 0f, -5f))
      return;

    if (!GameScript.StateAtPlay())
      return;

    // Gameplay
    RaycastHit hit;
    Physics.Raycast(
        GameResources.s_Instance._CameraMain.ScreenPointToRay(
            new Vector3(InputManager._MouseCurrentPos.x, InputManager._MouseCurrentPos.y, 0f)
        ),
        out hit
    );

    // Update finger UI
    _fingerPos2.transform.position = new Vector3(hit.point.x, hit.point.y, -10f);

    var dist = _fingerPos.transform.position - _fingerPos2.transform.position;
    var dir = dist.normalized;

    var indicatorLength = dist.magnitude;
    if (indicatorLength < 2.5f)
    {
      _shootReady = false;
    }
    else
    {
      _shootReady = true;
    }

    if (indicatorLength < 0.1f)
      return;

    _indicator2.transform.localScale = new Vector3(indicatorLength, 0.3f, 0.25f);
    _indicator2.transform.position = _fingerPos2.transform.position + dist * 0.5f;
    _indicator2.transform.LookAt(_fingerPos.transform);
    _indicator2.transform.Rotate(new Vector3(0f, 90f, 0f));
    _indicator2.transform.position = new Vector3(
        _indicator2.transform.position.x,
        _indicator2.transform.position.y,
        -5f
    );

    // Notch
    var notchTime = 2f - (1f * (1f / 3f * Shop.GetUpgradeCount(Shop.UpgradeType.ARROW_NOTCH)));
    if (Time.time - _downTime >= notchTime)
    {
      if (_notch == 0)
      {
        _sNotch.pitch = 1.2f + _notch * 0.2f;
        _notch++;
        _sNotch.Play();
      }
      else if (Time.time - _downTime >= notchTime * 2)
      {
        if (_notch == 1)
        {
          _sNotch.pitch = 1.2f + _notch * 0.2f;
          _notch++;
          _sNotch.Play();
        }
        else if (Time.time - _downTime >= notchTime * 3)
        {
          if (_notch == 2)
          {
            _sNotch.pitch = 1.2f + _notch * 0.2f;
            _notch++;
            _sNotch.Play();
          }
        }
      }
    }

    // Update Tower UI
    var offset = new Vector2(-3.6f, 0.3f);
    indicatorLength = Mathf.Clamp(
        indicatorLength,
        0f,
        ((6.3f + Shop.GetUpgradeCount(Shop.UpgradeType.ARROW_STRENGTH) * 0.45f)
            * (
                _notch > 0
                    ? _notch > 1
                        ? _notch > 2
                            ? 1.6f
                            : 1.4f
                        : 1.2f
                    : 1f
            )
        ) * 1.3f
    );

    _indicator.transform.localScale = new Vector3(indicatorLength, 1f, 0.25f);
    _indicator.transform.LookAt(
        _indicator.transform.position + new Vector3(dist.x, dist.y, transform.position.z),
        new Vector3(0f, 0f, 1f)
    );
    _indicator.transform.Rotate(new Vector3(0f, 90f, 0f));
    _indicator.transform.localPosition = -_indicator.transform.right * indicatorLength / 2f;
    _indicator.transform.position = new Vector3(
        _indicator.transform.position.x + offset.x,
        _indicator.transform.position.y + offset.y,
        -5f
    );

    _indicatorArrow.transform.localPosition =
        _indicator.transform.localPosition
        + -_indicator.transform.right * (indicatorLength / 2f + 1f);
    _indicatorArrow.transform.position = new Vector3(
        _indicatorArrow.transform.position.x,
        _indicatorArrow.transform.position.y,
        -5f
    );
    _indicatorArrow.transform.LookAt(
        _indicatorArrow.transform.position + -new Vector3(dist.x, dist.y, 0f),
        new Vector3(0f, 0f, 1f)
    );
    _indicatorArrow.transform.Rotate(new Vector3(0f, 180f, 0f));
    //var ea = _indicatorArrow.transform.localEulerAngles;
    //ea.x = ea.y = 0f;
    //_indicatorArrow.transform.localEulerAngles = ea;

    // Move cube in Player
    transform.GetChild(0).rotation = _indicatorArrow.transform.rotation;

    // Laser
    var laserLength = 100f;
    _laserPointer.transform.localScale = new Vector3(laserLength, 1f, 0.25f);
    _laserPointer.transform.LookAt(
        _laserPointer.transform.position + new Vector3(dist.x, dist.y, transform.position.z),
        new Vector3(0f, 0f, 1f)
    );
    _laserPointer.transform.Rotate(new Vector3(0f, 90f, 0f));
    _laserPointer.transform.localPosition = -_indicator.transform.right * (laserLength / 2f + indicatorLength + 2f);
    _laserPointer.transform.position = new Vector3(
        _laserPointer.transform.position.x + offset.x,
        _laserPointer.transform.position.y + offset.y,
        -5f
    );

    // Change pitch of bow draw sound
    var deltaDis = Vector2.Distance(_lastmousepos, InputManager._MouseCurrentPos);
    _lastmousepos = InputManager._MouseCurrentPos;
    if (deltaDis == 0f)
      deltaDis = 2.5f;
    _sBowDraw.pitch = deltaDis / 5f;
  }

  Vector2 _lastmousepos;
  float _downTime;

  public void MouseDown(bool secondFinger = false)
  {
    if (Time.time - MenuManager.s_waveStart < 0.25f)
      return;

    var camera = GameResources.s_Instance._CameraMain;

    // Check for menus
    if (!GameScript.StateAtPlay())
    {
      RaycastHit h;
      Physics.Raycast(camera.ScreenPointToRay(InputManager._MouseDownPos), out h);
      MenuManager.HandleMenuInput(h);
      return;
    }

    // Gameplay
    _downTime = Time.time;

    RaycastHit hit;
    Physics.Raycast(
        camera.ScreenPointToRay(
            new Vector3(InputManager._MouseDownPos.x, InputManager._MouseDownPos.y, 0f)
        ),
        out hit
    );
    if (!secondFinger)
    {
      _shootReady = false;

      _fingerPos.transform.position = new Vector3(hit.point.x, hit.point.y, -10f);
      _sBowDraw.Play();
    }

    UIInput(hit.collider.name);
  }

  public static void UIInput(string colliderName)
  {
    // Check UI buttons
    if (colliderName.Equals("ToPause"))
    {
      Time.timeScale = 0f;
      MenuManager.s_PauseMenu.Show();
      MenuManager.s_PauseButton.SetActive(false);
      MenuManager.s_StatsButton.SetActive(false);
      Wave.ToggleShopUI(false);
      s_Singleton._sBowDraw.Stop();
      GameScript._state = GameScript.GameState.PAUSED;

      GameScript.s_NumExits = 0;
      MenuManager.s_PauseMenu._menu.transform.Find("ToMenu").GetChild(1).GetComponent<TMPro.TextMeshPro>().text = $"Exit";
    }

    else if (colliderName.Equals("ShopButton"))
    {
      Time.timeScale = 0f;
      Shop.UpdateShopPriceStatuses();
      MenuManager.s_ShopMenu.Show();
      MenuManager.s_PauseButton.SetActive(false);
      MenuManager.s_StatsButton.SetActive(false);
      Wave.ToggleShopUI(false);
      s_Singleton._sBowDraw.Stop();
      GameScript._state = GameScript.GameState.SHOP;
    }

    else if (colliderName.Equals("StatsButton"))
    {
      Time.timeScale = 0f;
      //Shop.UpdateShopPriceStatuses();
      Shop.GenerateSkillStatsMenu();
      Shop.RenderSkillStatsMenu();
      MenuManager.s_StatsMenu.Show();
      MenuManager.s_PauseButton.SetActive(false);
      MenuManager.s_StatsButton.SetActive(false);
      Wave.ToggleShopUI(false);
      s_Singleton._sBowDraw.Stop();
      GameScript._state = GameScript.GameState.STATS;
    }

    // Check upgrades
    else
    {
      Shop.UpgradeInput(colliderName);
    }
  }

  static public void ButtonNoise()
  {
    if (Time.time < 0.5f) return;
    GameScript.PlaySound(s_Singleton._sPowerOn);
  }

  public static void SetAmmoForShop()
  {
    s_Singleton._ammo = s_Singleton._maxAmmo;
    UIUpdateAmmoCounter();
  }

  public void MouseUp()
  {
    var savenotch = _notch;
    _notch = 0;

    if (Time.time - MenuManager.s_waveStart < 0.25f)
      return;
    if (_fingerPos.transform.localPosition == new Vector3(-50f, 0f, -5f))
      return;

    if (!GameScript.StateAtPlay())
    {
      _sBowDraw.Stop();
      UIResetFinger();
      return;
    }

    // Shoot arrow if ready
    if (_shootReady && _ammo > 0)
    {
      var shots = 1;

      // Check trishot
      if (s_UpgradeEnabled_triShot && GetUpgradeOrder(Shop.UpgradeType.TRI_SHOT_COUNTER) == 0)
      {
        shots *= (Shop.GetUpgradeCount(Shop.UpgradeType.TRI_SHOT) == 1 ? 3 : 5);
        ResetUpgrade(Shop.UpgradeType.TRI_SHOT_COUNTER);
      }

      // Check projectile count
      var projectileCount = 1;
      if (savenotch == 3)
        projectileCount = Mathf.Clamp(Shop.GetUpgradeCount(Shop.UpgradeType.PROJECTILE_AMOUNT) + 1, 1, _ammo);

      // Shooooot
      for (var i = 0; i < projectileCount; i++)
      {
        Shoot(shots, i, projectileCount);

        // Check upgrade
        var tower2Chance = Shop.GetUpgradeCount(Shop.UpgradeType.TOWER2_PLAYERSHOOT);
        if (tower2Chance > 0)
        {
          _tower2Timer -= tower2Chance * 0.5f;
        }
      }

      // Reset upgrades
      if (s_UpgradeEnabled_arrowRain)
      {
        ResetUpgrade(Shop.UpgradeType.ARROW_RAIN_COUNTER);
      }

      // Check pen
      if (s_UpgradeEnabled_penetratingArrow)
      {
        ResetUpgrade(Shop.UpgradeType.ARROW_PENETRATION_COUNTER);
      }
    }
    else
    {
      var dir = (InputManager._MouseDownPos - InputManager._MouseCurrentPos);
      var dist = dir.magnitude;
      if (dist > 99f)
        GameScript.PlaySound(_sNoArrow, 1f, 1f);
    }

    UIResetFinger();

    // Stop noise
    _sBowDraw.Stop();
  }

  // Move Finger UI elements off-screen
  void UIResetFinger()
  {
    _indicator.transform.localScale = new Vector3(1f, 1f, 1f);
    _indicator.transform.localPosition = new Vector3(-50f, 0f, -5f);

    _indicatorArrow.transform.localPosition = new Vector3(-50f, 0f, -5f);

    _indicator2.transform.localScale = new Vector3(1f, 1f, 1f);
    _indicator2.transform.localPosition = new Vector3(-50f, 0f, -5f);

    _fingerPos.transform.localPosition = new Vector3(-50f, 0f, -5f);
    _fingerPos2.transform.localPosition = new Vector3(-50f, 0f, -5f);

    _shootReady = false;

    if (_sBowDraw.isPlaying)
      _sBowDraw.Stop();
  }

  public static void UIUpdateAmmoCounter()
  {
    // Update UI
    for (var i = 0; i < 5; i++)
    {
      var ui = GameObject.Find("AmmoArrow" + i).GetComponent<MeshRenderer>();
      var val = (!GameScript.StateAtMainMenu() && s_Singleton._health > 0 && i < s_Singleton._ammo ? true : false);
      if (ui.enabled != val)
        ui.enabled = val;
    }
  }

  IEnumerator LoseHealth()
  {
    GameScript.PlaySound(_sTowerMove);
    ParticleSystem s = GameObject.Find("TowerSmoke").GetComponent<ParticleSystem>();
    s.Play();

    GameScript.SpawnExplosion(
        GameObject.Find("Explod").transform.position
            + new Vector3(-1.5f + Random.value * 2f, -0.5f + Random.value * 1f, 0f)
    );
    GameScript.ShakeHeavy();

    var eI = 0;
    var timer = 1f;
    while (timer > 0f)
    {
      timer -= 0.05f;
      yield return new WaitForSeconds(0.05f);
      var movePos = new Vector3(0f, 0.2f, 0f);
      _tower.transform.position -= movePos;
      transform.position -= movePos;

      if (++eI == 5)
      {
        GameScript.SpawnExplosion(
            GameObject.Find("Explod").transform.position
                + new Vector3(-4f + Random.value * 5f, -2f + Random.value * 5f, 0f)
        );
        GameScript.ShakeHeavy();

        eI = 0;
      }
    }
    yield return new WaitForSeconds(0.25f);
    s.Stop();
  }

  IEnumerator GainHealth()
  {
    var s = GameObject.Find("TowerSmoke").GetComponent<ParticleSystem>();
    s.Play();
    GameScript.PlaySound(_sTowerMove);

    var timer = 1f;
    while (timer > 0f)
    {
      timer -= 0.05f;
      yield return new WaitForSecondsRealtime(0.05f);
      var movePos = new Vector3(0f, 0.2f, 0f);
      _tower.transform.position += movePos;
      transform.position += movePos;
    }
    yield return new WaitForSecondsRealtime(0.25f);
    s.Stop();
  }

  IEnumerator Tower2GainHealthCo()
  {

    var s = GameObject.Find("TowerSmoke2").GetComponent<ParticleSystem>();
    s.Play();
    GameScript.PlaySound(_sTowerMove);

    var timer = 1f;
    while (timer > 0f)
    {
      timer -= 0.05f;
      yield return new WaitForSecondsRealtime(0.05f);
      var movePos = new Vector3(0f, 0.35f, 0f);
      _tower2.transform.position += movePos;
    }
    yield return new WaitForSecondsRealtime(0.25f);

    _tower2Timer = _UpgradeFunction_tower2Rate.Invoke(Shop.GetUpgradeCount(Shop.UpgradeType.TOWER2));
    GameResources.s_Instance._SliderTower2UI.value = 0f;
    GameResources.s_Instance._SliderTower2UI.gameObject.SetActive(true);

    s.Stop();
  }
  public static void Tower2GainHealth()
  {
    s_Singleton.StartCoroutine(s_Singleton.Tower2GainHealthCo());
  }

  IEnumerator Tower2LoseHealthCo()
  {

    var s = GameObject.Find("TowerSmoke2").GetComponent<ParticleSystem>();
    s.Play();
    GameScript.PlaySound(_sTowerMove);

    GameResources.s_Instance._SliderTower2UI.gameObject.SetActive(false);

    var timer = 1f;
    while (timer > 0f)
    {
      timer -= 0.05f;
      yield return new WaitForSecondsRealtime(0.05f);
      var movePos = new Vector3(0f, 0.35f, 0f);
      _tower2.transform.position -= movePos;
    }
    yield return new WaitForSecondsRealtime(0.25f);

    s.Stop();
  }
  public static void Tower2LoseHealth()
  {
    s_Singleton.StartCoroutine(s_Singleton.Tower2LoseHealthCo());
  }

  public static void IncrementTower2Timer(float by)
  {
    s_Singleton._tower2Timer -= by;
  }

  public void Hit()
  {
    if (!GameScript.StateAtPlay())
      return;
    if (_invincibilityTimer > 0f)
      return;
    if (_health == 0)
      return;
    _invincibilityTimer = 3f;
    RemoveHealth();
    GameScript.ShakeHeavy();
    if (_health == 0)
    {
      GameScript.Lose();
      _sBowDraw.Stop();
      UIResetFinger();
    }
  }

  void Shoot(int shootNum, int projectileIndex = 0, int maxProjectileIndex = 1)
  {
    _ammo--;
    (_sShoot).Play();
    (transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>()).Play();
    for (var i = 0; i < shootNum; i++)
    {
      var addPos = Vector2.zero;
      if (i == 1)
      {
        addPos.y = 40f;
      }
      else if (i == 2)
      {
        addPos.y = -40f;
      }
      else if (i == 3)
      {
        addPos.y = 20f;
      }
      else if (i == 4)
      {
        addPos.y = -20f;
      }

      // Projectile count
      var startPos = 0;
      if (maxProjectileIndex == 2)
      {
        if (projectileIndex == 0)
          startPos = 1;
        else
          startPos = 2;
      }

      // Shoooooot
      var arrowScript = Shoot(
        (InputManager._MouseDownPos - InputManager._MouseUpPos + addPos),
        _indicator.transform.localScale.x * 160f,
        startPos
      );

      // Pen
      if (s_UpgradeEnabled_penetratingArrow)
        arrowScript.ActivatePierce();

      // Check game spawned
      if (i > 0)
        arrowScript._GameSpawned = true;
      WaveStats._arrowsShot++;
    }

    UIUpdateAmmoCounter();
  }

  static public IEnumerator ArrowRainCo(float x, List<Shop.UpgradeType> upgradeModifiers)
  {
    var numArrows = 10 + (Shop.GetUpgradeCount(Shop.UpgradeType.ARROW_RAIN) - 1) * 5;
    for (var i = 0; i < numArrows; i++)
    {
      // Spawn arrow in the air
      var subArrows = 1;
      if (upgradeModifiers.Contains(Shop.UpgradeType.TRI_SHOT))
      {
        subArrows *= 3; //Shop.GetUpgradeCount(Shop.UpgradeType.TRI_SHOT) == 1 ? 3 : 5;
      }
      for (var u = 0; u < subArrows; u++)
      {
        SpawnArrowAbove(x + i * 1.25f);
      }

      yield return new WaitForSeconds(0.1f);
    }
  }

  static public void SpawnArrowAbove(float xPos)
  {
    var arrow = s_Singleton.SpawnArrow();
    arrow._GameSpawned = true;
    arrow.transform.position = new Vector3(-30f + xPos, 30f, 0f);

    // Fire!
    var rb = arrow.GetComponent<Rigidbody2D>();
    rb.isKinematic = false;
    rb.AddForce(new Vector2(1f, 0f) * 1080f);

    arrow.Init();
    arrow.Fired();
  }

  static public void SpawnRandomArrowAbove()
  {
    var xPos = Random.Range(-6f, 48f);
    Debug.DrawRay(new Vector3(xPos, -10f, 0f), new Vector3(0f, 1000f, 0f), Color.red, 3f);
    SpawnArrowAbove(xPos);
  }

  static public void ArrowRain(float x, List<Shop.UpgradeType> upgradeModifiers)
  {
    s_Singleton.StartCoroutine(ArrowRainCo(x, upgradeModifiers));
  }

  ArrowScript Shoot(Vector2 dir, float force, int startPos = 0)
  {
    // Check force
    if (force < 800f)
    {
      force = 800f;
    }

    // Spawn arrow
    var arrow = SpawnArrow();
    var startPosition = transform.GetChild(0).position;
    if (startPos == 1)
      startPosition.y += 0.4f;
    else if (startPos == 2)
      startPosition.y -= 0.4f;
    arrow.transform.position = startPosition;

    // Fire!
    arrow.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
    arrow.Init();
    arrow.Fired();

    // Check if pierce is enabled
    //if (_pierce)
    //  arrow.GetComponent<ArrowScript>().ActivatePierce();

    // Add tri shot
    if (GetUpgradeOrder(Shop.UpgradeType.ARROW_RAIN_COUNTER) != -1)
    {
      Debug.Log("Added arrow rain");
      arrow.RegisterUpgrade(Shop.UpgradeType.ARROW_RAIN);
    }

    // Add tri shot
    if (GetUpgradeOrder(Shop.UpgradeType.TRI_SHOT_COUNTER) == 1)
    {
      arrow.RegisterUpgrade(Shop.UpgradeType.TRI_SHOT);
      ResetUpgrade(Shop.UpgradeType.TRI_SHOT_COUNTER);
    }

    // Apply force
    arrow.GetComponent<Rigidbody2D>().AddForce(dir.normalized * force);

    // Update arrow UI
    UIUpdateAmmoCounter();

    return arrow;
  }

  ArrowScript SpawnArrow()
  {
    // Spawn arrow
    var arrow = Instantiate(_arrow, GameResources.s_Instance._Arrows);
    arrow.name = "Arrow";

    // Check scale
    var scaleMod = 1f + Shop.GetUpgradeCount(Shop.UpgradeType.PROJECTILE_SIZE) * 0.75f;
    if (scaleMod != 1f)
    {
      arrow.transform.localScale *= scaleMod;
    }

    //
    return arrow.GetComponent<ArrowScript>();
  }

  public static int GetCoins()
  {
    return s_Singleton._coins;
  }

  public static void SetCoins(int coins)
  {
    s_Singleton._coins = coins;
    UpdateCoinComboUI();
  }

  public static void ReduceCoins(int number)
  {
    s_Singleton._coins -= number;
    UpdateCoinComboUI();
  }

  public static void AddHealth()
  {
    s_Singleton._health++;
    s_Singleton.StartCoroutine(s_Singleton.GainHealth());
    if (s_Singleton._health > 1)
    {
      ToggleFire(false);
    }
  }

  static void ToggleFire(bool toggle)
  {
    var fire = GameObject.Find("TowerFire");
    if (toggle)
    {
      if (!s_Singleton._sFireLoop.isPlaying)
        s_Singleton._sFireLoop.Play();
      for (int i = 0; i < fire.transform.childCount; i++)
      {
        fire.transform.GetChild(i).GetComponent<ParticleSystem>().Play();
      }
      return;
    }
    if (s_Singleton._sFireLoop.isPlaying)
      s_Singleton._sFireLoop.Stop();
    for (int i = 0; i < fire.transform.childCount; i++)
    {
      fire.transform.GetChild(i).GetComponent<ParticleSystem>().Stop();
    }
  }

  public static void AddAmmo()
  {
    s_Singleton._maxAmmo++;
    s_Singleton._ammo = s_Singleton._maxAmmo;
    UIUpdateAmmoCounter();
  }

  public static void RemoveHealth()
  {
    s_Singleton._health--;

    // Animation
    s_Singleton.StartCoroutine(s_Singleton.LoseHealth());

    // Particles
    if (s_Singleton._health < 2)
    {
      ToggleFire(true);
    }

    // Skills
    var count = Shop.GetUpgradeCount(Shop.UpgradeType.HEALTH_UPGRADES);
    if (count > 0)
    {
      Shop.IncrementUpgradesByPercent(count * 0.5f);
    }

    // Reset combo
    SetCombo(1f);
  }

  public static int GetHealth()
  {
    return s_Singleton._health;
  }

  public static int GetAmmoMax()
  {
    return s_Singleton._maxAmmo;
  }

  public static void SetAmmoMax(int ammo)
  {
    s_Singleton._maxAmmo = s_Singleton._ammo = ammo;
    UIUpdateAmmoCounter();

    // Change arrow replenishment speed
    switch (ammo)
    {
      case 3:
        s_Singleton._ammoTime = 1.05f;
        break;
        /*case 4:
          s_Singleton._ammoTime = 1.1f;
          break;
        case 5:
          s_Singleton._ammoTime = 0.9f;
          break;*/
    }
  }

  public static void Reset()
  {
    SetCombo(1f);

    s_Singleton._slowMoMax = 2f;
    s_Singleton._slowMoTimer = s_Singleton._slowMoMax;
    GameScript.s_TimeSped = true;
    Time.timeScale = 2.5f;

    WaveStats.Reset();
    Wave.ToggleShopUI(false);
  }

  public static int GetTarget()
  {
    return s_Singleton._targetNumber;
  }
}
