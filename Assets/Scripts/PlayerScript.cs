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

  AudioSource _sBowDraw,
      _sNoArrow,
      _sTowerMove,
      _sShoot,
      _sPowerOff,
      _sPowerOn,
      _sNotch,
      _sFireLoop;

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
    _combo;

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

    _tower = GameObject.Find("Tower");
    _crystal = _tower.transform.GetChild(0).gameObject;

    _sBowDraw = GameObject.Find("BowDraw").GetComponent<AudioSource>();
    _sNoArrow = GameObject.Find("NoArrow").GetComponent<AudioSource>();
    _sTowerMove = _tower.transform.GetChild(1).GetChild(0).GetComponent<AudioSource>();
    _sShoot = GameObject.Find("Shoot").GetComponent<AudioSource>();
    _sPowerOff = GameObject.Find("PowerOff").GetComponent<AudioSource>();
    _sPowerOn = GameObject.Find("PowerOn").GetComponent<AudioSource>();
    _sNotch = GameObject.Find("Notch").GetComponent<AudioSource>();
    _sFireLoop = GameObject.Find("FireLoop").GetComponent<AudioSource>();

    _health = 2;
    _combo = 1f;

    _GemColor = GameObject.Find("Play").GetComponent<MeshRenderer>().material.color;
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
      if (_sFireLoop.isPlaying)
        _sFireLoop.pitch = 0f;
      return;
    }
    else
    {
      if (_sFireLoop.isPlaying)
        _sFireLoop.pitch = 1f;
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

    // Increment slowmo
    if (!GameScript.s_TimeSped)
    {
      _slowMoTimer = Mathf.Clamp(_slowMoTimer - Time.unscaledDeltaTime, 0f, _slowMoMax);
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
      _ammoTimer -= Time.deltaTime;
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

    AddCombo(comboDelta);

    // Cheats
#if UNITY_EDITOR

    if (Input.GetKeyDown(KeyCode.C))
    {
      SetCoins(GetCoins() + 1000);
    }
    if (Input.GetKeyDown(KeyCode.V))
    {
      Wave.s_MetaWaveIter++;
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
  public bool _upgradeEnabled_triShot
  {
    get { return Shop.UpgradeEnabled(Shop.UpgradeType.TRI_SHOT); }
  }
  public bool _upgradeEnabled_arrowRain
  {
    get { return Shop.UpgradeEnabled(Shop.UpgradeType.ARROW_RAIN); }
  }
  public bool _upgradeEnabled_penetratingArrow
  {
    get { return Shop.UpgradeEnabled(Shop.UpgradeType.ARROW_PENETRATION); }
  }
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

  public Shop.UpgradeType GetUpgrade(int index){
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

    _indicator2.transform.localScale = new Vector3(indicatorLength, 1f, 0.25f);
    _indicator2.transform.position = _fingerPos2.transform.position + dist * 0.5f;
    _indicator2.transform.LookAt(_fingerPos.transform);
    _indicator2.transform.Rotate(new Vector3(0f, 90f, 0f));
    _indicator2.transform.position = new Vector3(
        _indicator2.transform.position.x,
        _indicator2.transform.position.y,
        -5f
    );

    // Notch
    if (Time.time - _downTime >= 2f)
    {
      if (_notch == 0)
      {
        _sNotch.pitch = 1.2f + _notch * 0.2f;
        _notch++;
        _sNotch.Play();
      }
      else if (Time.time - _downTime >= 4f)
      {
        if (_notch == 1)
        {
          _sNotch.pitch = 1.2f + _notch * 0.2f;
          _notch++;
          _sNotch.Play();
        }
        else if (Time.time - _downTime >= 6f)
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
                            ? 1.9f
                            : 1.6f
                        : 1.3f
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

    // Check UI buttons
    if (hit.collider.name.Equals("ToPause"))
    {
      Time.timeScale = 0f;
      MenuManager._pauseMenu.Show();
      MenuManager._pauseButton.SetActive(false);
      Wave.ToggleShopUI(false);
      MenuManager._musicButton.SetActive(true);
      _sBowDraw.Stop();
      GameScript._state = GameScript.GameState.PAUSED;
    }
    else if (hit.collider.name.Equals("ShopButton"))
    {
      Time.timeScale = 0f;
      Shop.UpdateShopPriceStatuses();
      MenuManager._shop.Show();
      MenuManager._pauseButton.SetActive(false);
      Wave.ToggleShopUI(false);
      _sBowDraw.Stop();
      GameScript._state = GameScript.GameState.SHOP;
    }
    // Check upgrades
    else
    {
      Shop.UpgradeInput(hit.collider.name);
    }
  }

  static public void ButtonNoise()
  {
    GameScript.PlaySound(s_Singleton._sPowerOn);
  }

  public static void SetAmmoForShop()
  {
    s_Singleton._ammo = s_Singleton._maxAmmo;
    UIUpdateAmmoCounter();
  }

  public void MouseUp()
  {
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
      if (_upgradeEnabled_triShot && GetUpgradeOrder(Shop.UpgradeType.TRI_SHOT_COUNTER) == 0)
      {
        shots *= (Shop.GetUpgradeCount(Shop.UpgradeType.TRI_SHOT) == 1 ? 3 : 5);
        ResetUpgrade(Shop.UpgradeType.TRI_SHOT_COUNTER);
      }

      // Shooooot
      Shoot(shots);

      // Reset upgrades
      if (_upgradeEnabled_arrowRain)
      {
        ResetUpgrade(Shop.UpgradeType.ARROW_RAIN_COUNTER);
      }

      // Check pen
      if (_upgradeEnabled_penetratingArrow)
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
  }

  public static void UIUpdateAmmoCounter()
  {
    // Update UI
    for (var i = 0; i < 5; i++)
    {
      var ui = GameObject.Find("AmmoArrow" + i).GetComponent<MeshRenderer>();
      var val = (i < s_Singleton._ammo ? true : false);
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

  void Shoot(int shootNum = 1)
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

      // Shoooooot
      var ar = Shoot(
          (InputManager._MouseDownPos - InputManager._MouseUpPos + addPos),
          _indicator.transform.localScale.x * 160f
      );

      // Pen
      if (_upgradeEnabled_penetratingArrow)
        ar.ActivatePierce();

      // Check game spawned
      if (i > 0)
        ar._GameSpawned = true;
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

  ArrowScript Shoot(Vector2 dir, float force)
  {
    // Check force
    if (force < 800f)
    {
      force = 800f;
    }

    // Spawn arrow
    var arrow = SpawnArrow();
    arrow.transform.position = transform.GetChild(0).position;

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
        s_Singleton._ammoTime = 1.3f;
        break;
        /*case 4:
          s_Singleton._ammoTime = 1.1f;
          break;
        case 5:
          s_Singleton._ammoTime = 0.9f;
          break;*/
    }
  }

  public static void AddTarget()
  {
    s_Singleton._targetNumber++;
    MenuManager.SetWaveSign($"{s_Singleton._targetNumber + 1}");
    switch (s_Singleton._targetNumber)
    {
      case 2:
        GameScript.Targets._Difficulty++;
        break;
      case 3:
        GameScript.Targets._Difficulty++;

        // Play music
        var s = GameScript.s_Instance.GetComponent<AudioSource>();
        if (!s.isPlaying)
        {
          s.Play();
        }
        break;
      case 5:
        GameScript.Targets._Difficulty++;
        GameScript.Targets._MinTargets++;
        break;
      case 15:
        GameScript.Targets._Difficulty++;
        break;
      case 25:
        GameScript.Targets._Difficulty++;
        GameScript.Targets._MinTargets++;
        break;
      case 35:
        GameScript.Targets._Difficulty++;
        GameScript.Targets._MinTargets++;
        SetAmmoMax(4);
        break;
      case 45:
        GameScript.Targets._Difficulty++;
        GameScript.Targets._MinTargets++;
        SetAmmoMax(5);
        break;
      case 60:
        GameScript.Targets._Difficulty++;
        GameScript.Targets._MinTargets++;
        break;
      case 100:
        GameScript.Targets._Difficulty++;
        GameScript.Targets._MinTargets++;
        break;
      case 150:
        GameScript.Targets._Difficulty++;
        GameScript.Targets._MinTargets++;
        break;
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
