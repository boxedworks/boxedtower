using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{

  public static PlayerScript _Player;

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
    _Player = this;

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
  }

  public static void Init()
  {
    UpdateCoinUI();
    UIUpdateAmmoCounter();
    _Player.UIResetFinger();
  }

  float yAdd;
  // Update is called once per frame
  void Update()
  {
    _crystal.transform.Rotate(new Vector3(0f, 1f, 0f) * 100f * Time.deltaTime * (1 + _gemNumber / 500f));
    _crystal.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.Lerp(_GemColor, Color.white, _gemNumber / 1000);
    yAdd += (_gemNumber - yAdd) * Time.deltaTime;
    float AddAmount = yAdd / 500f;
    _crystal.transform.localPosition = new Vector3(0f, 0f + AddAmount, 0f);

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



  public static void GiveAmmo()
  {
    _Player._ammo++;
    _Player._ammoTimer = _Player._ammoTime;
    UIUpdateAmmoCounter();
  }

  public static int GetAmmo()
  {
    return _Player._ammo;
  }

  public static void GiveCoins(int numCoins)
  {
    _Player._coins += numCoins;
    WaveStats._coinsGained += numCoins;
    UpdateCoinUI();
  }

  static void UpdateCoinUI()
  {
    MenuManager._coin.transform.parent.GetChild(1).GetComponent<TextMesh>().text = "x " + _Player._coins;
  }

  float _gemNumber;

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
    indicatorLength = Mathf.Clamp(indicatorLength, 0f, 8f);

    _indicator.transform.localScale = new Vector3(indicatorLength, 1f, 0.25f);
    _indicator.transform.LookAt(transform.position + new Vector3(dist.x, dist.y, transform.position.z), new Vector3(0f, 0f, 1f));
    _indicator.transform.Rotate(new Vector3(0f, 90f, 0f));
    _indicator.transform.localPosition = -_indicator.transform.right * indicatorLength / 2f;
    _indicator.transform.position = new Vector3(_indicator.transform.position.x, _indicator.transform.position.y, -5f);

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
    GameScript.PlaySound(_Player._sPowerOn);
  }

  public static void SetAmmoForShop()
  {
    _Player._ammo = _Player._maxAmmo;
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

    _gemNumber = 0f;

    // Shoot arrow if ready
    if (_shootReady && _ammo > 0)
    {
      var shots = 1;

      var shotGunEnabled = Shop.UpgradeEnabled(Shop.UpgradeType.TRI_SHOT);
      if (shotGunEnabled)
      {
        shots *= (Mathf.RoundToInt(Random.value * 7f) == 0 ? 5 : 3);
        Shop.ResetUpgrade(Shop.UpgradeType.TRI_SHOT);
      }

      // Shooooot
      Shoot(shots);
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

  static void UIUpdateAmmoCounter()
  {
    // Update UI
    for (var i = 0; i < 5; i++)
    {
      var ui = GameObject.Find("AmmoArrow" + i).GetComponent<MeshRenderer>();
      ui.enabled = (i < _Player._ammo ? true : false);
    }
  }

  IEnumerator LoseHealth()
  {
    GameScript.PlaySound(_sTowerMove);
    float timer = 1f;
    ParticleSystem s = GameObject.Find("TowerSmoke").GetComponent<ParticleSystem>();
    s.Play();
    GameScript.SpawnExplosion(GameObject.Find("Explod").transform.position + new Vector3(-1.5f + Random.value * 2f, -0.5f + Random.value * 1f, 0f));
    int eI = 0;
    while (timer > 0f)
    {
      timer -= 0.05f;
      yield return new WaitForSeconds(0.05f);
      Vector3 movePos = new Vector3(0f, 0.2f, 0f);
      _tower.transform.position -= movePos;
      transform.position -= movePos;

      if (++eI == 5)
      {
        GameScript.SpawnExplosion(GameObject.Find("Explod").transform.position + new Vector3(-4f + Random.value * 5f, -2f + Random.value * 5f, 0f));
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
        addPos.y = 30f;
      }
      else if (i == 2)
      {
        addPos.y = -30f;
      }
      else if (i == 3)
      {
        addPos.y = 15f;
      }
      else if (i == 4)
      {
        addPos.y = -15f;
      }
      var ar = Shoot((InputManager._MouseDownPos - InputManager._MouseUpPos + addPos), _indicator.transform.localScale.x * 200f);
      //if (_arrowRain)
      //  ar.GetComponent<ArrowScript>()._isArrowRain = true;
      arrows.Add(ar);
      WaveStats._arrowsShot++;
    }

    foreach (var arrow in arrows)
      foreach (var otherArrow in arrows)
      {
        if (arrow.GetInstanceID() == otherArrow.GetInstanceID()) continue;
        Physics2D.IgnoreCollision(arrow.transform.GetChild(4).GetComponent<Collider2D>(), otherArrow.transform.GetChild(4).GetComponent<Collider2D>());
      }

    UIUpdateAmmoCounter();
  }

  static public IEnumerator ArrowRainCo(float x)
  {
    for (int i = 0; i < 20; i++)
    {
      GameObject arrow = _Player.SpawnArrow();
      arrow.transform.position = new Vector3(-25f + x + i * 1.25f, 25f, 0f);
      // Fire!
      Rigidbody rb = arrow.GetComponent<Rigidbody>();
      rb.isKinematic = false;
      rb.AddForce(new Vector3(0.75f, -1f, 0f) * 1000f);

      arrow.GetComponent<ArrowScript>().Init();
      arrow.GetComponent<ArrowScript>().Fired();
      yield return new WaitForSeconds(0.1f);
    }
  }

  static public void ArrowRain(float x)
  {
    _Player.StartCoroutine(ArrowRainCo(x));
  }

  GameObject Shoot(Vector2 dir, float force)
  {
    // Check force
    if (force < 800f)
    {
      force = 800f;
    }

    // Spawn arrow
    GameObject arrow = SpawnArrow();
    arrow.transform.position = transform.position;

    // Fire!
    arrow.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
    arrow.GetComponent<ArrowScript>().Init();
    arrow.GetComponent<ArrowScript>().Fired();

    // Check if pierce is enabled
    //if (_pierce)
    //  arrow.GetComponent<ArrowScript>().ActivatePierce();

    // Apply force
    arrow.GetComponent<Rigidbody2D>().AddForce(dir.normalized * force);

    // Update arrow UI
    UIUpdateAmmoCounter();
    return arrow;
  }

  GameObject SpawnArrow()
  {
    // Spawn arrow
    var arrow = Instantiate(_arrow);
    arrow.name = "Arrow";
    var barrier = GameObject.Find("BarrierMod");
    var c = arrow.transform.GetChild(4).GetComponent<Collider2D>();
    for (var i = 0; i < 2; i++)
    {
      Physics2D.IgnoreCollision(c, barrier.transform.GetChild(i).GetComponent<Collider2D>());
    }
    var arrows = GameObject.Find("Arrows");
    for (var i = 0; i < arrows.transform.childCount; i++)
    {
      Physics2D.IgnoreCollision(c, arrows.transform.GetChild(i).GetChild(4).GetComponent<Collider2D>());
    }
    arrow.transform.parent = arrows.transform;
    return arrow;
  }

  public static int GetCoins()
  {
    return _Player._coins;
  }

  public static void SetCoins(int coins)
  {
    _Player._coins = coins;
    UpdateCoinUI();
  }

  public static void ReduceCoins(int number)
  {
    _Player._coins -= number;
    UpdateCoinUI();
  }

  public static void AddHealth()
  {
    _Player._health++;
    _Player.StartCoroutine(_Player.GainHealth());
    if (_Player._health > 1)
    {
      ToggleFire(false);
    }
  }

  static void ToggleFire(bool toggle)
  {
    GameObject fire = GameObject.Find("TowerFire");
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
    _Player._maxAmmo++;
    _Player._ammo = _Player._maxAmmo;
    UIUpdateAmmoCounter();
  }

  public static void RemoveHealth()
  {
    _Player._health--;
    _Player.StartCoroutine(_Player.LoseHealth());
    if (_Player._health < 2)
    {
      ToggleFire(true);
    }
  }

  public static int GetHealth()
  {
    return _Player._health;
  }

  public static int GetAmmoMax()
  {
    return _Player._maxAmmo;
  }

  public static void SetAmmoMax(int ammo)
  {
    _Player._maxAmmo = ammo;

    // Change arrow replenishment speed
    switch (ammo)
    {
      case 3:
        _Player._ammoTime = 1.3f;
        break;
      case 4:
        _Player._ammoTime = 1.1f;
        break;
      case 5:
        _Player._ammoTime = 0.9f;
        break;
    }
  }

  public static void AddTarget()
  {
    _Player._targetNumber++;
    MenuManager.SetWaveSign($"{_Player._targetNumber + 1}");
    switch (_Player._targetNumber)
    {
      case 2:
        GameScript.Targets._Difficulty++;
        break;
      case 3:
        GameScript.Targets._Difficulty++;
        // Play music
        AudioSource s = GameScript._Game.GetComponent<AudioSource>();
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
    _Player._targetNumber = 0;
  }

  public static int GetTarget()
  {
    return _Player._targetNumber;
  }
}
