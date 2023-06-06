using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{

  public static PlayerScript s_Singleton;

  GameObject _indicator, _fingerPos, _indicator2, _indicatorArrow, _arrow, _fingerPos2, _tower, _crystal;

  AudioSource _sBowDraw, _sNoArrow, _sTowerMove, _sShoot, _sPowerOff, _sPowerOn;

  bool _shootReady;

  int _ammo, _maxAmmo = 3, _health, _coins, _targetNumber;
  float _ammoTimer, _ammoTime = 1.3f, _invincibilityTimer;

  public static Color _GemColor;

  public static class WaveStats
  {
    public static int _arrowsShot, _coinsGained, _enemiesKilled;

    public static void Reset()
    {
      _arrowsShot = 0;
      _coinsGained = 0;
      _enemiesKilled = 0;
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

    _health = 2;

    _GemColor = GameObject.Find("Play").GetComponent<MeshRenderer>().material.color;
    _crystal.transform.parent = _tower.transform.parent;
  }

  public static void Init()
  {
    UpdateCoinUI();
    UIUpdateAmmoCounter();
    s_Singleton.UIResetFinger();
  }

  float yAdd;
  // Update is called once per frame
  void Update()
  {
    _crystal.transform.Rotate(new Vector3(0f, 1f, 0f) * 100f * Time.deltaTime);
    _crystal.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.Lerp(_GemColor, Color.white, 0 / 1000);
    _crystal.transform.position += ((_tower.transform.position + new Vector3(0f, -0.5f, 0f)) - _crystal.transform.position) * Time.deltaTime * 2f;

    if (!GameScript.StateAtPlay()) return;

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

  }

  //
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
    UpdateCoinUI();
  }

  static void UpdateCoinUI()
  {
    MenuManager._coin.transform.parent.GetChild(1).GetComponent<TextMesh>().text = "x" + s_Singleton._coins;
  }

  // Combine upgrades
  // Upgrades
  public bool _upgradeEnabled_triShot { get { return Shop.UpgradeEnabled(Shop.UpgradeType.TRI_SHOT); } }
  public bool _upgradeEnabled_arrowRain { get { return Shop.UpgradeEnabled(Shop.UpgradeType.ARROW_RAIN); } }
  Shop.UpgradeType _upgrade0, _upgrade1;
  public bool RegisterUpgrade(Shop.UpgradeType upgradeType)
  {

    if (_upgrade0 != Shop.UpgradeType.NONE && _upgrade1 != Shop.UpgradeType.NONE)
      return false;

    if (_upgrade0 == Shop.UpgradeType.NONE)
      _upgrade0 = upgradeType;
    else
      _upgrade1 = upgradeType;

    return true;
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
    return _upgrade0 == upgradeType ? 0 : _upgrade1 == upgradeType ? 1 : -1;
  }

  // On mouse move
  public void MouseMove()
  {
    if (Time.time - MenuManager.s_waveStart < 0.25f) return;
    if (_fingerPos.transform.localPosition == new Vector3(-50f, 0f, -5f)) return;

    if (!GameScript.StateAtPlay()) return;

    // Gameplay
    RaycastHit hit;
    Physics.Raycast(GameResources.s_Instance._CameraMain.ScreenPointToRay(new Vector3(InputManager._MouseCurrentPos.x, InputManager._MouseCurrentPos.y, 0f)), out hit);

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
    _indicator2.transform.position = new Vector3(_indicator2.transform.position.x, _indicator2.transform.position.y, -5f);

    // Update Tower UI
    var offset = new Vector2(-3.6f, 0.3f);
    indicatorLength = Mathf.Clamp(indicatorLength, 0f, (6.3f + Shop.GetUpgradeCount(Shop.UpgradeType.ARROW_STRENGTH) * 0.45f) * (Time.time - _downTime > 2f ? 1.45f : 1f));

    _indicator.transform.localScale = new Vector3(indicatorLength, 1f, 0.25f);
    _indicator.transform.LookAt(_indicator.transform.position + new Vector3(dist.x, dist.y, transform.position.z), new Vector3(0f, 0f, 1f));
    _indicator.transform.Rotate(new Vector3(0f, 90f, 0f));
    _indicator.transform.localPosition = -_indicator.transform.right * indicatorLength / 2f;
    _indicator.transform.position = new Vector3(_indicator.transform.position.x + offset.x, _indicator.transform.position.y + offset.y, -5f);

    _indicatorArrow.transform.localPosition = _indicator.transform.localPosition + -_indicator.transform.right * (indicatorLength / 2f + 1f);
    _indicatorArrow.transform.position = new Vector3(_indicatorArrow.transform.position.x, _indicatorArrow.transform.position.y, -5f);
    _indicatorArrow.transform.LookAt(_indicatorArrow.transform.position + -new Vector3(dist.x, dist.y, 0f), new Vector3(0f, 0f, 1f));
    _indicatorArrow.transform.Rotate(new Vector3(0f, 180f, 0f));
    //var ea = _indicatorArrow.transform.localEulerAngles;
    //ea.x = ea.y = 0f;
    //_indicatorArrow.transform.localEulerAngles = ea;

    // Move cube in Player
    transform.GetChild(0).rotation = _indicatorArrow.transform.rotation;

    // Change pitch of bow draw sound
    var deltaDis = Vector2.Distance(_lastmousepos, InputManager._MouseCurrentPos);
    _lastmousepos = InputManager._MouseCurrentPos;
    if (deltaDis == 0f) deltaDis = 2.5f;
    _sBowDraw.pitch = deltaDis / 5f;
  }

  Vector2 _lastmousepos;
  float _downTime;
  public void MouseDown(bool secondFinger = false)
  {
    if (Time.time - MenuManager.s_waveStart < 0.25f) return;

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
    Physics.Raycast(camera.ScreenPointToRay(new Vector3(InputManager._MouseDownPos.x, InputManager._MouseDownPos.y, 0f)), out hit);
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
      MenuManager._musicButton.SetActive(true);
      _sBowDraw.Stop();
      GameScript._state = GameScript.GameState.PAUSED;
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
    if (Time.time - MenuManager.s_waveStart < 0.25f) return;
    if (_fingerPos.transform.localPosition == new Vector3(-50f, 0f, -5f)) return;

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

    GameScript.SpawnExplosion(GameObject.Find("Explod").transform.position + new Vector3(-1.5f + Random.value * 2f, -0.5f + Random.value * 1f, 0f));
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
        GameScript.SpawnExplosion(GameObject.Find("Explod").transform.position + new Vector3(-4f + Random.value * 5f, -2f + Random.value * 5f, 0f));
        GameScript.ShakeHeavy();

        eI = 0;
      }
    }
    yield return new WaitForSeconds(0.25f);
    s.Stop();
  }

  IEnumerator GainHealth()
  {
    GameScript.PlaySound(_sTowerMove);
    float timer = 1f;
    ParticleSystem s = GameObject.Find("TowerSmoke").GetComponent<ParticleSystem>();
    s.Play();
    while (timer > 0f)
    {
      timer -= 0.05f;
      yield return new WaitForSeconds(0.05f);
      Vector3 movePos = new Vector3(0f, 0.2f, 0f);
      _tower.transform.position += movePos;
      transform.position += movePos;
    }
    yield return new WaitForSeconds(0.25f);
    s.Stop();
  }

  public void Hit()
  {
    if (!GameScript.StateAtPlay()) return;
    if (_invincibilityTimer > 0f) return;
    if (_health == 0) return;
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
    var arrows = new List<GameObject>();
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
      var ar = Shoot((InputManager._MouseDownPos - InputManager._MouseUpPos + addPos), _indicator.transform.localScale.x * 200f);
      arrows.Add(ar);
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
        subArrows *= 3;//Shop.GetUpgradeCount(Shop.UpgradeType.TRI_SHOT) == 1 ? 3 : 5;
      }
      for (var u = 0; u < subArrows; u++)
      {
        var arrow = s_Singleton.SpawnArrow();
        arrow.transform.position = new Vector3(-31f + x + i * 1.25f, 30f, 0f);

        // Fire!
        var rb = arrow.GetComponent<Rigidbody2D>();
        rb.isKinematic = false;
        rb.AddForce(new Vector3(u == 0 ? 1f : u == 1 ? 0.6f : 1.4f, 0f) * 1150f);

        arrow.Init();
        arrow.Fired();
      }

      yield return new WaitForSeconds(0.1f);
    }
  }

  static public void ArrowRain(float x, List<Shop.UpgradeType> upgradeModifiers)
  {
    Debug.Log("Arrow rain");
    s_Singleton.StartCoroutine(ArrowRainCo(x, upgradeModifiers));
  }

  GameObject Shoot(Vector2 dir, float force)
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

    return arrow.gameObject;
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
    UpdateCoinUI();
  }

  public static void ReduceCoins(int number)
  {
    s_Singleton._coins -= number;
    UpdateCoinUI();
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
      for (int i = 0; i < fire.transform.childCount; i++)
      {
        fire.transform.GetChild(i).GetComponent<ParticleSystem>().Play();
      }
      return;
    }
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
    s_Singleton.StartCoroutine(s_Singleton.LoseHealth());
    if (s_Singleton._health < 2)
    {
      ToggleFire(true);
    }
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
    s_Singleton._maxAmmo = ammo;

    // Change arrow replenishment speed
    switch (ammo)
    {
      case 3:
        s_Singleton._ammoTime = 1.3f;
        break;
      case 4:
        s_Singleton._ammoTime = 1.1f;
        break;
      case 5:
        s_Singleton._ammoTime = 0.9f;
        break;
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
        AudioSource s = GameScript.s_Instance.GetComponent<AudioSource>();
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

  public static void ResetTarget()
  {
    s_Singleton._targetNumber = 0;
  }

  public static int GetTarget()
  {
    return s_Singleton._targetNumber;
  }
}
