using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
  static int s_id;
  int _id;

  public EnemyType _EnemyType;

  Rigidbody2D _rb;

  int _flag;

  bool _hasBalloon;

  public bool _IsDead,
      _CanDropLoot;

  public MeshRenderer[] _Sensors,
      _Armor;

  public float _ForceModifier;

  public AudioSource _EnemyNoise;

  bool _beforeSpawn
  {
    get { return _rb.position.x > GameResources.s_Instance._SpawnLine.GetChild(0).position.x; }
  }

  public static Dictionary<int, (EnemyScript, MeshRenderer)> s_Sensors;

  public enum EnemyType
  {
    GROUND_ROLL,
    GROUND_ROLL_SMALL,
    GROUND_ROLL_ARMOR_2,
    GROUND_ROLL_ARMOR_4,
    GROUND_ROLL_ARMOR_8,

    GROUND_ROLL_STONE_1,

    GROUND_POP,
    GROUND_POP_FLOAT,

    GROUND_SLIDE,
    GROUND_SLIDE_SMALL,
    GROUND_SLIDE_MEDIUM,
    GROUND_SLIDE_TOP,
    GROUND_SLIDE_CASTLE,

    GROUND_TOTEM_5,
    FLYING_HOMING,

    CRATE,

    BOSS,

    BALLOON,
  }

  // Use this for initialization
  float _pitchSave,
      _forceModSave,
      _spawnTime,
      _restTime;

  [System.NonSerialized]
  public bool _IsSlide;

  void Start()
  {
    Init();
  }

  public void Spawn()
  {
    _spawnTime = Time.time;
    gameObject.SetActive(true);
  }

  public void Init()
  {
    if (_rb != null)
      return;
    _rb = GetComponent<Rigidbody2D>();

    _id = s_id++;
    if (s_Sensors == null)
      s_Sensors = new Dictionary<int, (EnemyScript, MeshRenderer)>();
    foreach (var sensor in _Sensors)
    {
      s_Sensors.Add(sensor.GetInstanceID(), (this, sensor));
    }

    _spawnTime = Time.time;
    _CanDropLoot = true;

    if (_pitchSave == 0f)
      _pitchSave = _EnemyNoise.pitch;
    if (_forceModSave == 0f)
      _forceModSave = _ForceModifier;

    // Error
    if (_Sensors?.Length == 0)
    {
      Debug.LogWarning("No sensors on enemy type: " + _EnemyType);
    }

    // Check slide
    if (
        _EnemyType == EnemyType.GROUND_SLIDE_SMALL
        || _EnemyType == EnemyType.GROUND_SLIDE_MEDIUM
        || _EnemyType == EnemyType.GROUND_SLIDE
        || _EnemyType == EnemyType.GROUND_SLIDE_TOP
        || _EnemyType == EnemyType.GROUND_SLIDE_CASTLE
        || _EnemyType == EnemyType.GROUND_TOTEM_5
    )
    {
      _IsSlide = true;
    }

    // Perk
    if (_EnemyType == EnemyType.GROUND_ROLL_SMALL)
    {
      var sizeMod = Shop.GetUpgradeCount(Shop.UpgradeType.ENEMY_SIZED) * 1.3f;
      if (sizeMod > 0)
      {
        transform.localScale *= sizeMod;
        _ForceModifier += 0.06f;
      }
    }

    /*if(_type == EnemyType.BOSS)
    {
        GameObject barrier = GameResources.s_Instance._SpawnLine.gameObject;
        Physics.IgnoreCollision(barrier.transform.GetChild(0).GetComponent<Collider>(), transform.GetChild(0).GetComponent<Collider>());
        Physics.IgnoreCollision(barrier.transform.GetChild(0).GetComponent<Collider>(), transform.GetChild(1).GetComponent<Collider>());
    }*/
  }

  // Update is called once per frame
  void FixedUpdate()
  {
    Move();
  }

  bool _escapeSpawn;
  float _escapeSpawnTime;
  void Update()
  {

    if (_IsDead)
      return;

    /*if (_isSlide)
    {
      _EnemyNoise.pitch = Time.timeScale;
    }*/
    // Make sure not under map
    if (_rb.position.y < -8.5f)
    {
      Die(false);
      Debug.Log($"[{_EnemyType}] died from going under map");
    }

    // Check spawn leave
    if (!_escapeSpawn && !_beforeSpawn)
    {
      _escapeSpawn = true;
      _escapeSpawnTime = Time.time;


      OnLeaveSpawn();
      //if (!_IsSlide && _EnemyType != EnemyType.BALLOON && Random.Range(0, 2) == 0)
      //  SpawnBalloon();
    }

    // Check slide
    if (_IsSlide)
    {
      if (GameScript.StateAtPlay())
      {
        if (_EnemyNoise.isPlaying)
          _EnemyNoise.pitch = 1f;
      }
      else
      {
        if (_EnemyNoise.isPlaying)
          _EnemyNoise.pitch = 0f;
      }
      return;
    }

    // Check balloon
    if (_EnemyType == EnemyType.BALLOON)
    {

      if (_other._IsDead)
      {
        Die(true);
      }
      else
        _lr.SetPositions(new Vector3[] { transform.position, _other.transform.position });
    }

    // Make sure not flying towards player very quickly
    if (_rb.velocity.x < -8f)
    {
      _rb.velocity = Vector3.zero;
    }

    // Make sure not stuck
    if (_rb.angularVelocity < 20f)
    {
      _restTime += Time.deltaTime;
    }
    else
      _restTime = 0f;
    if (_restTime > 5f)
    {
      _ForceModifier = _forceModSave + (_restTime - 5f) * 0.01f;
    }
    else
    {
      _ForceModifier = _forceModSave;
    }

    if (Time.time - _spawnTime > 75f && _beforeSpawn)
    {
      Die(false);
      Debug.Log($"[{_EnemyType}] died from stuck in spawn");
    }

    if (transform.position.y > 80f)
    {
      Die(false);
      Debug.Log($"[{_EnemyType}] died from flying above map");
    }
  }

  // Fired when enemy leaves spawn
  void OnLeaveSpawn()
  {

    switch (_EnemyType)
    {

    }
  }

  float _timer = 1f;

  void Move()
  {
    if (_IsDead)
      return;
    switch (_EnemyType)
    {

      case EnemyType.BALLOON:

        // Check y pos
        if (_flag == 0)
          _flag = Random.Range(1, 9);

        var wantPos = 10.5f + _flag * 0.9f;

        _rb.position += (new Vector2(_rb.position.x, wantPos) - _rb.position) * Time.fixedDeltaTime * 0.5f;

        // X pos
        _rb.position += new Vector2(-1f * Time.fixedDeltaTime, 0f);

        // At tower
        if (_rb.position.x < -11f)
        {
          Die(false);
          break;
        }

        break;

      case (EnemyType.GROUND_ROLL):
        _rb.AddTorque(7.8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_SMALL):
        _rb.AddTorque(2.7f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_2):
        _rb.AddTorque(8.4f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_4):
        _rb.AddTorque(8.7f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_8):
        _rb.AddTorque(20.5f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_STONE_1):
        _rb.AddTorque(22f * _ForceModifier);
        break;

      case (EnemyType.CRATE):
        _rb.AddTorque(8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_POP):

        if (_beforeSpawn)
        {
          _rb.AddTorque(6f * _ForceModifier);
          break;
        }

        _timer -= Time.fixedDeltaTime;
        if (_timer < 0f && _rb.position.y < -3f && Mathf.Abs(_rb.velocity.y) < 0.2f)
        {
          //_rb.velocity = Vector3.zero;
          _rb.AddTorque(100f * _ForceModifier);
          _rb.AddForce(
              new Vector2(
                  -150f * Random.Range(0.7f, 1.1f),
                  775f * Random.Range(0.65f, 1.2f)
              ) * _ForceModifier
          );

          _timer = 4f + Random.value * 6f;

          _EnemyNoise.pitch = _pitchSave + Random.Range(-1f, 1f) * 0.15f;
          _EnemyNoise.Play();
        }
        break;

      case (EnemyType.GROUND_POP_FLOAT):

        // In spawn
        if (_beforeSpawn || (!_hasBalloon && _rb.position.y < -3f && _flag != 0))
        {
          if (_rb.angularDrag != 1.8f)
            _rb.angularDrag = 1.8f;
          _rb.AddTorque(7f * _ForceModifier);
          _timer = 1f + Random.value * 4f;
          break;
        }

        // In between; roll
        _timer -= Time.deltaTime;
        if (_timer > 0f && !_hasBalloon)
        {
          if (_rb.angularDrag != 1.8f && _rb.velocity.y < 0.3f)
            _rb.angularDrag = 1.8f;
          _rb.AddTorque(7f * _ForceModifier);
        }

        // Right before hitting tower, sometimes start floating again to trick
        if (_flag == 0 && !_hasBalloon)
        {
          if (_rb.position.y > 6f && _Sensors[0].isVisible)
          {
            SpawnBalloon();
            _timer = 2f + Random.value * 3f;
            _flag = 1;
            break;
          }

          if (_timer < 0f && _rb.position.y < -3f && Mathf.Abs(_rb.velocity.y) < 0.2f)
          {
            _rb.angularDrag = 0.4f;
            _rb.AddTorque(100f * _ForceModifier);
            _rb.AddForce(
                new Vector2(
                    -150f * Random.Range(0.7f, 1.1f),
                    775f * Random.Range(0.95f, 1.15f)
                ) * _ForceModifier
            );

            _timer = 1f + Random.value * 2f;

            _EnemyNoise.Play();
          }
        }

        break;

      case (EnemyType.GROUND_SLIDE):
        _rb.MovePosition(
            transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime
        );
        if (transform.position.x < -11f)
        {
          PlayerScript.s_Singleton.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_SMALL):
        _rb.MovePosition(
            transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime
        );
        if (transform.position.x < -11f)
        {
          PlayerScript.s_Singleton.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_MEDIUM):
        _rb.MovePosition(
            transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime
        );
        if (transform.position.x < -11f)
        {
          PlayerScript.s_Singleton.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_TOP):
        _rb.MovePosition(
            transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime
        );
        if (transform.position.x < -11f)
        {
          PlayerScript.s_Singleton.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_CASTLE):
        _rb.MovePosition(
            transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime
        );
        if (transform.position.x < -11f)
        {
          PlayerScript.s_Singleton.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_TOTEM_5):
        _rb.MovePosition(
            transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime
        );
        if (transform.position.x < -11f)
        {
          PlayerScript.s_Singleton.Hit();
          Die(false);
        }
        break;
      case (EnemyType.FLYING_HOMING):
        if (
            Vector3.Distance(
                PlayerScript.s_Singleton.transform.position,
                transform.position
            ) > 50f
        )
        {
          _rb.AddForce(new Vector3(-1f, 0f, 0f) * 15);
          break;
        }
        if (_timer == 0f)
        {
          _timer = 3f + Random.value * 3f;
          _rb.velocity = Vector3.zero;
        }
        if (_timer > 0f)
        {
          _timer -= Time.fixedDeltaTime;
          _rb.AddForce(new Vector3(-1f, 0f, 0f) * 10);
        }
        else if (_timer < 0f)
        {
          // Find direction to player
          Vector3 dir = (
              PlayerScript.s_Singleton.transform.position - transform.position
          );
          _rb.AddForce(dir.normalized * 10f);
        }
        break;
    }
  }

  public bool CheckHit(Collider2D c)
  {
    if (_Sensors.Length == 0)
      return false;

    // Check sensors
    switch (c.name)
    {
      case "Sensor":
        foreach (var r in _Sensors)
        {
          var c1 = r.gameObject.GetComponent<Collider2D>();
          if (c1.GetInstanceID() == c.GetInstanceID())
          {
            if (r.material.color == Color.black)
              break;
            r.material.color = Color.black;
            s_Sensors.Remove(r.GetInstanceID());

            // Check towers
            if (_IsSlide)
            {
              // Explosion FX
              var explosion = Instantiate(
                  Resources.Load("ParticleSystems/ExplosionSystem") as GameObject
              );
              explosion.transform.parent = r.transform.parent;
              explosion.transform.position = r.transform.position;
              var mag = r.transform.localScale.x;
              if (mag > 1f)
              {
                mag /= 3f;
              }
              explosion.transform.localScale = new Vector3(mag, mag, mag);

              // Screen shake heavy
              GameScript.ShakeHeavy();

              // Fire FX
              if (_Sensors.Length > 1)
              {
                var fire = Instantiate(
                    Resources.Load("ParticleSystems/FireSystem") as GameObject
                );
                fire.transform.parent = r.transform.parent;
                fire.transform.position = r.transform.position;

                PlayerScript.AddCombo(PlayerScript._COMBO_ADD);
              }
            }
            return CheckSensors();
          }
        }
        break;

      case "Armor":
        CheckArmor(c);
        break;
    }
    return false;
  }

  void CheckArmor(Collider2D c)
  {
    // Check armor
    foreach (var mr in _Armor)
    {
      if (mr == null)
        continue;
      var c1 = mr.transform.parent.GetComponent<Collider2D>();
      if (c1.GetInstanceID() == c.GetInstanceID())
      {
        // Color
        if (mr.material.color == Color.black)
          break;
        mr.material.color = Color.black;

        // Physics
        mr.transform.position += new Vector3(0f, 0f, -5f);
        var rb = mr.transform.parent.gameObject.AddComponent<Rigidbody2D>();
        rb.mass = 0.15f;
        var ran = Random.Range(0.5f, 1f);
        rb.AddTorque((Random.value < 0.5f ? -1f : 1f) * ran * 250f);
        var dir = (mr.transform.position - transform.position);
        rb.AddForce(dir * 40f * Random.Range(0.5f, 1f));
        Physics2D.IgnoreCollision(GameResources.s_Instance._ColliderGround, c1);
        c1.isTrigger = true;
        mr.transform.parent.parent = GameResources.s_Instance._ContainerDead;
        //_rb.mass -= 0.02f;
        return;
      }
    }
  }

  public bool CheckSensors()
  {
    foreach (var r in _Sensors)
    {
      if (r.material.color != Color.black)
        return false;
    }

    Die(_EnemyType == EnemyType.CRATE ? false : true);
    return true;
  }


  public static EnemyScript GetEnemy(int index)
  {
    return GameResources.s_Instance._ContainerAlive.GetChild(index).GetComponent<EnemyScript>();
  }
  public static int GetEnemyAliveCount()
  {
    return GameResources.s_Instance._ContainerAlive.childCount;
  }

  public void Die(bool dropLoot = true)
  {
    if (_IsDead)
      return;

    // If lost, do not die
    if (GameScript._state == GameScript.GameState.LOSE)
      return;

    if (PlayerScript.GetAmmo() == 0)
      PlayerScript.GiveAmmo();

    _IsDead = true;

    // Turn sensors black and destroy particle systems if present
    foreach (var r in _Sensors)
    {
      r.material.color = Color.black;
      s_Sensors.Remove(r.GetInstanceID());
      if (r.transform.childCount == 2)
      {
        Destroy(r.transform.GetChild(1).gameObject);
      }
    }

    // Destroy armor
    foreach (var r in _Armor)
    {
      if (r == null)
        continue;
      CheckArmor(r.transform.parent.GetComponent<Collider2D>());
    }

    // Crate specific
    if (_EnemyType == EnemyType.CRATE)
    {
      transform.GetChild(1).GetComponent<MeshRenderer>().material.color = Color.black;
    }

    transform.parent = GameResources.s_Instance._ContainerDead;

    // Fall
    for (var i = 0; i < transform.childCount; i++)
    {
      var c = transform.GetChild(i).GetComponent<Collider2D>();
      if (c != null)
        c.enabled = false;
    }
    //Destroy(_rb);

    /*var rb = gameObject.AddComponent<Rigidbody>();
    rb.AddForce(new Vector3(0f, 1f, -0.5f) * 1000f, ForceMode.VelocityChange);
    var t = 70f;
    rb.AddTorque(new Vector3(Random.Range(-t, t), Random.Range(-t, t), Random.Range(-t, t)));*/

    // Drop balloon
    if (_EnemyType == EnemyType.BALLOON)
    {
      Destroy(GetComponent<SpringJoint2D>());
      _lr.positionCount = 0;
      transform.GetChild(0).gameObject.SetActive(false);
      _EnemyNoise.Play();
      _other._hasBalloon = false;
    }

    // Check physics
    if (_IsSlide)
    {
      _rb.bodyType = RigidbodyType2D.Dynamic;
    }
    else
    {
      if (!_beforeSpawn)
        GameScript.ShakeLight();
    }

    // Move through air
    transform.position += new Vector3(0f, 0f, -5f);
    _rb.velocity = Vector2.zero;
    _rb.angularVelocity = 0f;
    if (_rb.gravityScale == 0f)
    {
      _rb.gravityScale = 1f;
      _rb.constraints = RigidbodyConstraints2D.None;
    }

    _rb.AddForce(new Vector2(30f, 300f) * _rb.mass);
    if (!_IsSlide)
      _rb.AddTorque(-_rb.mass * 300f);

    // Drop gold
    if (dropLoot)
    {
      DropLoot();

      PlayerScript.AddCombo(PlayerScript._COMBO_ADD);

      OnEnemyKilled(_EnemyType);
    }

    // Check win
    if (_EnemyType == EnemyType.BOSS)
    {
      GameScript.Win();
    }
  }

  static void OnEnemyKilled(EnemyType enemyType)
  {
    PlayerScript.WaveStats._enemiesKilled++;

    // Check skills
    var tower2 = Shop.GetUpgradeCount(Shop.UpgradeType.TOWER2_RATE);
    if (tower2 > 0)
    {
      PlayerScript.IncrementTower2Timer(tower2 * 1f);
    }
  }

  float _enemyNoiseLast;

  void OnCollisionEnter2D(Collision2D c)
  {
    if (_IsDead)
      return;
    if (c.transform.parent.gameObject.name.Equals("Tower"))
    {
      if (_EnemyType == EnemyType.CRATE && GameScript.StateAtPlay())
      {
        Die();

        var incrementAmount = Shop.GetUpgradeCount(Shop.UpgradeType.CRATE_INCREMENT) * 5;
        if (incrementAmount > 0)
          Shop.IncrementUpgrades(incrementAmount);
        return;
      }
      PlayerScript.s_Singleton.Hit();
      if (GameScript.StateAtPlay())
        Die(false);
    }

    if (c.gameObject.name.Equals("Arrow"))
      return;

    if (c.gameObject.name.Equals("Ground"))
    {
      if (_EnemyNoise != null && !_IsSlide && _EnemyType != EnemyType.BALLOON)
      {
        if (Time.time - _enemyNoiseLast > 0.15f)
        {
          _enemyNoiseLast = Time.time;
          _EnemyNoise.Play();
        }
      }
      return;
    }

    _rb.AddForce(new Vector2(0f, 1f) * 100f * _rb.mass);
  }

  public void SpawnBalloon()
  {
    var balloon = SpawnEnemy(EnemyType.BALLOON, true);
    balloon.AttachBalloon(this);
  }

  LineRenderer _lr;
  EnemyScript _other;
  public void AttachBalloon(EnemyScript other)
  {
    _other = other;

    var otherCollider = other._Sensors[0].GetComponent<Collider2D>();
    Physics2D.IgnoreCollision(_Sensors[0].GetComponent<Collider2D>(), otherCollider);
    Physics2D.IgnoreCollision(GameResources.s_Instance._SpawnLine.GetChild(0).GetComponent<Collider2D>(), otherCollider);

    transform.position = other.transform.position;
    other._hasBalloon = true;

    var sj = gameObject.AddComponent<SpringJoint2D>();
    sj.connectedBody = _other._rb;
    sj.distance = Random.Range(5f, 8f);
    sj.autoConfigureDistance = false;

    _lr = GetComponent<LineRenderer>();
  }

  static GameObject _castle;
  // Spawn enemy
  static int s_enemySpawnIter;
  static public EnemyScript SpawnEnemy(EnemyType enemyType, bool canDropLoot = true)
  {
    var enemyName = "Enemy4";
    var spawnPos = Vector3.zero;
    var spawnLine = GameResources.s_Instance._SpawnLine;

    switch (enemyType)
    {
      case (EnemyType.GROUND_ROLL):
        enemyName = "Enemy1";
        break;
      case (EnemyType.GROUND_ROLL_SMALL):
        enemyName = "Enemy2";
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_2):
        enemyName = "Enemy10";
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_4):
        enemyName = "Enemy12";
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_8):
        enemyName = "Enemy13";
        break;
      case (EnemyType.GROUND_ROLL_STONE_1):
        enemyName = "Enemy_stone0";
        break;

      case (EnemyType.GROUND_POP):
        enemyName = "Enemy4";
        break;
      case (EnemyType.GROUND_POP_FLOAT):
        enemyName = "Enemy16";
        break;
      case (EnemyType.CRATE):
        // Check skill
        var c = Shop.GetUpgradeCount(Shop.UpgradeType.CRATE_ARMOR);
        var chance = c * 0.5f;
        if (c > 0 && Random.value <= chance)
          enemyName = "CrateArmored";
        else
          enemyName = "Crate";
        break;
      case (EnemyType.GROUND_SLIDE_SMALL):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy11" : "Enemy11.1");
        spawnPos = spawnLine.position + new Vector3(-5f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE_MEDIUM):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy15" : "Enemy15.1");
        spawnPos = spawnLine.position + new Vector3(-5f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy9" : "Enemy9.1");
        spawnPos = spawnLine.position + new Vector3(-5f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE_TOP):
        enemyName = "Enemy19";
        spawnPos = spawnLine.position + new Vector3(-5f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE_CASTLE):
        enemyName = "Enemy22";
        spawnPos = spawnLine.position + new Vector3(-5f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_TOTEM_5):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy17" : "Enemy18");
        spawnPos = spawnLine.position + new Vector3(-5f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.BOSS):
        enemyName = "Enemy20";
        spawnPos = _castle.transform.position;
        break;

      case (EnemyType.BALLOON):
        enemyName = "Balloon";
        break;
    }
    var enemy = Instantiate(Resources.Load(enemyName) as GameObject);

    if (enemyType == EnemyType.GROUND_SLIDE_CASTLE)
    {
      _castle = enemy;
    }

    enemy.name = enemyName;
    enemy.transform.parent = GameResources.s_Instance._ContainerAlive;
    s_enemySpawnIter++;
    enemy.transform.position = (
        spawnPos == Vector3.zero
            ? new Vector3(
                GameResources.s_Instance._SpawnLine.GetChild(0).position.x + 3f + ((s_enemySpawnIter % 10) / 9f) * 27f,
                15f + Random.Range(0f, 10f),
                0f
            )
            : spawnPos
    );
    var enemyScript = enemy.GetComponent<EnemyScript>();

    // Init enemy script
    enemyScript.Init();
    enemyScript._CanDropLoot = canDropLoot;

    return enemyScript;
  }

  void DropLoot()
  {
    if (!_CanDropLoot)
      return;

    // Decide drop
    var numCoins = 0;
    switch (_EnemyType)
    {
      case (EnemyType.GROUND_ROLL):
        numCoins = 10;
        break;
      case (EnemyType.GROUND_ROLL_SMALL):
        numCoins = 15;
        break;
      case (EnemyType.GROUND_POP):
        numCoins = 20;
        break;
      case (EnemyType.GROUND_POP_FLOAT):
        numCoins = 25;
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_2):
        numCoins = 25;
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_4):
        numCoins = 35;
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_8):
        numCoins = 50;
        break;
      case (EnemyType.GROUND_ROLL_STONE_1):
        numCoins = 65;
        break;
      case (EnemyType.GROUND_SLIDE_SMALL):
        numCoins = 25;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_SLIDE_MEDIUM):
        numCoins = 35;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_SLIDE):
        numCoins = 50;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_SLIDE_TOP):
        numCoins = 50;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_SLIDE_CASTLE):
        numCoins = 150;
        GameScript.Freeze();
        break;
      case (EnemyType.BOSS):
        numCoins = 100;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_TOTEM_5):
        numCoins = 50;
        break;
      case (EnemyType.CRATE):
        numCoins = 50;
        break;

      case (EnemyType.BALLOON):
        numCoins = 15;
        break;
    }

    // Drop
    if (numCoins > 0)
      StartCoroutine(DropCoins(numCoins));
  }

  IEnumerator DropCoins(int coins)
  {
    coins = Mathf.RoundToInt(coins * PlayerScript.s_Combo);
    var timer = 0.14f / ((float)coins / 6f);
    var coinAmount = Mathf.Clamp((int)(0.04f * coins), 1, 3);
    var coinIter = 0;
    var audio = GameResources.s_Instance._AudioCoinDrop;
    var coinRange = 0.75f;
    switch (_EnemyType)
    {
      case EnemyType.GROUND_ROLL_SMALL:
        coinRange = 0.5f;
        break;

      case EnemyType.GROUND_ROLL_ARMOR_8:
        coinRange = 1.25f;
        break;

      case EnemyType.GROUND_SLIDE_SMALL:
      case EnemyType.GROUND_SLIDE:
      case EnemyType.GROUND_SLIDE_MEDIUM:
      case EnemyType.GROUND_SLIDE_TOP:
        coinRange = 2f;
        break;
    }
    while (coins > 0)
    {
      // Give coins
      for (var i = 0; i < coinAmount && coins > 0; i++)
      {
        coins--;

        PlayerScript.GiveCoins(1);

        var particles = GameResources.s_Instance._ParticlesCoins;
        var emitParams = new ParticleSystem.EmitParams();
        if (coinAmount > 1)
        {
          if (i == 1)
            emitParams.position = transform.position + new Vector3(coinRange, 0f, 0f);
          else if (i == 2)
            emitParams.position = transform.position + new Vector3(-coinRange, 0f, 0f);
          else
            emitParams.position = transform.position;
        }
        else
          emitParams.position = transform.position;
        particles.Emit(emitParams, 1);
      }


      if (coinIter++ % 6 == 0)
        audio.PlayOneShot(audio.clip);

      yield return new WaitForSeconds(timer);
    }

    Destroy(this);
  }

  static public void UnpackResource(GameObject r)
  {
    for (int i = r.transform.childCount - 1; i >= 0; i--)
    {
      r.transform.GetChild(i).transform.parent = GameObject.Find("Alive").transform;
    }
  }
}
