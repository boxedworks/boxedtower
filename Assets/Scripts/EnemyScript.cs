using System.Collections;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
  public EnemyType _EnemyType;

  Rigidbody2D _rb;

  int _flag;

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

  public enum EnemyType
  {
    GROUND_ROLL,
    GROUND_ROLL_SMALL,
    GROUND_ROLL_ARMOR_2,
    GROUND_ROLL_ARMOR_4,
    GROUND_ROLL_ARMOR_8,

    GROUND_ROLL_STONE_2,

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

    BOSS
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

    if (_pitchSave == 0f)
      _pitchSave = _EnemyNoise.pitch;
    if (_forceModSave == 0f)
      _forceModSave = _ForceModifier;
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

    _spawnTime = Time.time;
    _CanDropLoot = true;

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

    // Check floating
    if (_EnemyType == EnemyType.GROUND_POP_FLOAT)
    {
      _flag = Random.Range(0, 10);
    }

    // Perk
    var sizeMod = Shop.GetUpgradeCount(Shop.UpgradeType.ENEMY_SIZED) * 1.3f;
    if (sizeMod > 0)
    {
      transform.localScale *= sizeMod;
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

  void Update()
  {
    /*if (_isSlide)
    {
      _EnemyNoise.pitch = Time.timeScale;
    }*/
    // Make sure not under map
    if (_rb.position.y < -8.5f)
    {
      Die(false);
    }

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

    if (Time.time - _spawnTime > 25f && _beforeSpawn)
    {
      Die(false);
    }

    if (transform.position.y > 80f)
    {
      Die(false);
    }
  }

  float _timer = 1f;

  void Move()
  {
    if (_IsDead)
      return;
    switch (_EnemyType)
    {
      case (EnemyType.GROUND_ROLL):
        _rb.AddTorque(8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_SMALL):
        _rb.AddTorque(2.8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_2):
        _rb.AddTorque(9.6f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_4):
        _rb.AddTorque(11f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_8):
        _rb.AddTorque(21f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_STONE_2):
        _rb.AddTorque(16f * _ForceModifier);
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

          _timer = 3f + Random.value * 3f;

          _EnemyNoise.pitch = _pitchSave + Random.Range(-1f, 1f) * 0.15f;
          _EnemyNoise.Play();
        }
        break;

      case (EnemyType.GROUND_POP_FLOAT):

        // In spawn
        if (_beforeSpawn)
        {
          if (_rb.angularDrag != 1.8f)
            _rb.angularDrag = 1.8f;
          _rb.AddTorque(7f * _ForceModifier);
          _timer = 1f + Random.value * 4f;
          break;
        }

        // At tower
        if (_rb.position.x < -11f)
        {
          _rb.gravityScale = 1f;
          _rb.constraints = RigidbodyConstraints2D.None;
          break;
        }

        // In between; roll
        _timer -= Time.deltaTime;
        if (_timer > 0f && _rb.gravityScale == 1f)
        {
          if (_rb.angularDrag != 1.8f && _rb.velocity.y < 0.3f)
            _rb.angularDrag = 1.8f;
          _rb.AddTorque(7f * _ForceModifier);
        }

        // Right before hitting tower, sometimes start floating again to trick
        if (_rb.gravityScale == 0f)
        {
          if (_timer < 0f || _rb.position.x < -3f)
          {
            _timer = 3f + Random.value * 3f;
            _rb.gravityScale = 1f;
            _rb.constraints = RigidbodyConstraints2D.None;
          }

          var vel = _rb.velocity;
          vel.x += (-4f - vel.x) * Time.deltaTime * 5f;
          _rb.velocity = vel;
        }
        // Else, when in air, stop movement and float towards player
        else
        {
          if (_rb.position.y > 5.5f + _flag * 0.25f)
          {
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector3(_rb.velocity.x, 0f, 0f);
            _rb.drag = 0.01f;
            _rb.constraints = RigidbodyConstraints2D.FreezePositionY;
            _timer = 2f + Random.value * 3f;
            break;
          }
        }

        if (_timer < 0f && _rb.position.y < -5f && _rb.gravityScale == 1f)
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
    }

    // Check win
    if (_EnemyType == EnemyType.BOSS)
    {
      GameScript.Win();
    }

    PlayerScript.WaveStats._enemiesKilled++;
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
      if (_EnemyNoise != null && !_IsSlide)
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

  static GameObject _castle;

  // Spawn enemy
  static public EnemyScript SpawnEnemy(EnemyType type, bool canDropLoot = true)
  {
    var enemyName = "Enemy4";
    var spawnPos = Vector3.zero;
    var spawnLine = GameResources.s_Instance._SpawnLine;

    switch (type)
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
      case (EnemyType.GROUND_ROLL_STONE_2):
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
    }
    var enemy = Instantiate(Resources.Load(enemyName) as GameObject);

    if (type == EnemyType.GROUND_SLIDE_CASTLE)
    {
      _castle = enemy;
    }

    enemy.name = enemyName;
    enemy.transform.parent = GameObject.Find("Alive").transform;
    enemy.transform.position = (
        spawnPos == Vector3.zero
            ? new Vector3(
                GameResources.s_Instance._SpawnLine.position.x - 8f + Random.Range(-1f, 1f),
                17f + Random.Range(0f, 2f),
                0f
            )
            : spawnPos
    );
    var script = enemy.GetComponent<EnemyScript>();

    // Init enemy script
    script.Init();
    script._CanDropLoot = canDropLoot;

    return script;
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
      case (EnemyType.GROUND_ROLL_STONE_2):
        numCoins = 50;
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
        numCoins = 75;
        break;
    }

    // Drop
    if (numCoins > 0)
      StartCoroutine(DropCoins(numCoins));
  }

  IEnumerator DropCoins(int coins)
  {
    coins = Mathf.RoundToInt(coins * PlayerScript.s_Combo);
    var timer = 0.15f / ((float)coins / 8f);
    var coinAmount = 1;// Mathf.Clamp((int)(0.15f * coins), 1, 10000);
    var coinIter = 0;
    var audio = GameResources.s_Instance._AudioCoinDrop;
    var coinRange = 0.5f;
    switch (_EnemyType)
    {
      case EnemyType.GROUND_ROLL_SMALL:
        coinRange = 0.25f;
        break;

      case EnemyType.GROUND_ROLL_ARMOR_8:
        coinRange = 1f;
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
          emitParams.position = transform.position + new Vector3(Random.Range(-coinRange, coinRange), 0f, 0f);
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
