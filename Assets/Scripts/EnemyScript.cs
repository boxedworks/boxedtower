using System.Collections;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{

  public EnemyType _EnemyType;

  Rigidbody2D _rb;


  public bool _IsDead, _CanDropLoot;

  public MeshRenderer[] _Sensors, _Armor;

  public float _ForceModifier;

  public AudioSource _EnemyNoise;

  public enum EnemyType
  {
    GROUND_ROLL,
    GROUND_ROLL_SMALL,
    GROUND_ROLL_ARMOR_2,
    GROUND_ROLL_ARMOR_4,
    GROUND_ROLL_ARMOR_8,
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
  void Start()
  {
    Init();
  }

  public void Init()
  {
    _rb = GetComponent<Rigidbody2D>();


    _CanDropLoot = true;

    // Error
    if (_Sensors?.Length == 0)
    {
      Debug.LogWarning("No sensors on enemy type: " + _EnemyType);
    }

    /*if(_type == EnemyType.BOSS)
    {
        GameObject barrier = GameObject.Find("SpawnLine");
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
    if (_EnemyType == EnemyType.GROUND_SLIDE || _EnemyType == EnemyType.GROUND_SLIDE_MEDIUM || _EnemyType == EnemyType.GROUND_SLIDE_SMALL || _EnemyType == EnemyType.GROUND_SLIDE_TOP)
    {
      _EnemyNoise.pitch = Time.timeScale;
    }
    // Make sure not under map
    if (_rb.position.y < -8.5f)
    {
      Die(false);
    }

    if (_rb.velocity.x < -8f)
    {
      _rb.velocity = Vector3.zero;
    }
  }

  float _timer = 1f;

  void Move()
  {
    if (_IsDead) return;
    switch (_EnemyType)
    {
      case (EnemyType.GROUND_ROLL):
        _rb.AddTorque(8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_SMALL):
        _rb.AddTorque(3f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_2):
        _rb.AddTorque(8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_4):
        _rb.AddTorque(8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_8):
        _rb.AddTorque(8f * _ForceModifier);
        break;
      case (EnemyType.CRATE):
        _rb.AddTorque(8f * _ForceModifier);
        break;
      case (EnemyType.GROUND_POP):
        if (_rb.position.x > GameObject.Find("Bar").transform.position.x)
        {
          _rb.AddTorque(8f * _ForceModifier);
          break;
        }
        _timer -= Time.fixedDeltaTime;
        if (_timer < 0f && _rb.position.y < -3f)
        {
          _rb.velocity = Vector3.zero;
          _rb.AddTorque(2000f * _ForceModifier);
          _rb.AddForce(new Vector2(-200f, 1000f) * _ForceModifier);

          _timer = 3f + Random.value * 3f;

          _EnemyNoise.Play();
        }
        break;
      case (EnemyType.GROUND_POP_FLOAT):
        if (_rb.position.x > GameObject.Find("Bar").transform.position.x)
        {
          _rb.AddTorque(8f * _ForceModifier);
          _timer = 1f + Random.value * 4f;
          break;
        }
        if (_rb.position.x < -2f)
        {
          _rb.gravityScale = 1f;
          break;
        }
        _timer -= Time.fixedDeltaTime;
        if (_timer > 0f && _rb.gravityScale == 1f)
        {
          _rb.AddTorque(8f * _ForceModifier);
        }
        if (_rb.gravityScale == 0f)
        {
          if (_timer < 0f || _rb.position.x < -3f)
          {
            _timer = 3f + Random.value * 3f;
            _rb.gravityScale = 1f;
          }
          _rb.AddForce(new Vector3(-1f, 0f, 0f) * 10f);
        }
        if (_rb.gravityScale == 1f && ((_rb.position.y > 4f && _timer < 0f) || _rb.position.y > 6f))
        {
          _rb.velocity = new Vector3(_rb.velocity.x, 0f, 0f);
          _rb.gravityScale = 0f;
          _timer = 2f + Random.value * 3f;
          break;
        }
        if (_timer < 0f && _rb.position.y < -5f && _rb.gravityScale == 1f)
        {
          _rb.velocity = Vector3.zero;
          _rb.AddTorque(2000f * _ForceModifier);
          _rb.AddForce(new Vector2(-200f, 1100f) * _ForceModifier);

          _timer = 1f + Random.value * 2f;

          _EnemyNoise.Play();
        }
        break;
      case (EnemyType.GROUND_SLIDE):
        _rb.MovePosition(transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime);
        if (transform.position.x < -6f)
        {
          PlayerScript._Player.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_SMALL):
        _rb.MovePosition(transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime);
        if (transform.position.x < -6f)
        {
          PlayerScript._Player.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_MEDIUM):
        _rb.MovePosition(transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime);
        if (transform.position.x < -6f)
        {
          PlayerScript._Player.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_TOP):
        _rb.MovePosition(transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime);
        if (transform.position.x < -6f)
        {
          PlayerScript._Player.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_SLIDE_CASTLE):
        _rb.MovePosition(transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime);
        if (transform.position.x < -6f)
        {
          PlayerScript._Player.Hit();
          Die(false);
        }
        break;
      case (EnemyType.GROUND_TOTEM_5):
        _rb.MovePosition(transform.position + new Vector3(-1f, 0f, 0f) * Time.fixedDeltaTime);
        if (transform.position.x < -6f)
        {
          PlayerScript._Player.Hit();
          Die(false);
        }
        break;
      case (EnemyType.FLYING_HOMING):
        if (Vector3.Distance(PlayerScript._Player.transform.position, transform.position) > 50f)
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
          Vector3 dir = (PlayerScript._Player.transform.position - transform.position);
          _rb.AddForce(dir.normalized * 10f);
        }
        break;
    }
  }

  public void CheckHit(Collider2D c)
  {
    if (_Sensors.Length == 0) return;

    // Check sensors
    foreach (var r in _Sensors)
    {
      var c1 = r.gameObject.GetComponent<Collider2D>();
      if (c1.GetInstanceID() == c.GetInstanceID())
      {
        if (r.material.color == Color.black) break;
        r.material.color = Color.black;

        // Check towers
        if (_EnemyType == EnemyType.GROUND_SLIDE || _EnemyType == EnemyType.GROUND_SLIDE_SMALL || _EnemyType == EnemyType.GROUND_SLIDE_MEDIUM || _EnemyType == EnemyType.GROUND_SLIDE_TOP || _EnemyType == EnemyType.GROUND_SLIDE_CASTLE)
        {

          // Explosion FX
          var explosion = Instantiate(Resources.Load("ParticleSystems/ExplosionSystem") as GameObject);
          explosion.transform.parent = r.transform.parent;
          explosion.transform.position = r.transform.position;
          float mag = r.transform.localScale.x;
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
            var fire = Instantiate(Resources.Load("ParticleSystems/FireSystem") as GameObject);
            fire.transform.parent = r.transform.parent;
            fire.transform.position = r.transform.position;
          }
        }

        CheckSensors();
        return;
      }
    }
    CheckArmor(c);
  }

  void CheckArmor(Collider2D c)
  {
    // Check armor
    foreach (var r in _Armor)
    {
      if (r == null) continue;
      var c1 = r.gameObject.GetComponent<Collider>();
      if (c1.GetInstanceID() == c.GetInstanceID())
      {
        if (r.material.color == Color.black) break;
        r.material.color = Color.black;
        r.gameObject.transform.position += new Vector3(0f, 0f, -4f);
        r.gameObject.AddComponent<Rigidbody>();
        r.gameObject.transform.parent = GameResources.s_Instance._ContainerDead;
        return;
      }
    }
  }

  public void CheckSensors()
  {
    foreach (var r in _Sensors)
    {
      if (r.material.color != Color.black) return;
    }

    Die(_EnemyType == EnemyType.CRATE ? false : true);
  }

  public void Die(bool dropLoot = true)
  {
    if (_IsDead) return;

    // If lost, do not die
    if (GameScript._state == GameScript.GameState.LOSE) return;

    if (PlayerScript.GetAmmo() == 0) PlayerScript.GiveAmmo();

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
      if (r == null) continue;
      CheckArmor(r.transform.GetComponent<Collider2D>());
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

    var istower = false;
    if (_EnemyType == EnemyType.GROUND_SLIDE || _EnemyType == EnemyType.GROUND_SLIDE_SMALL || _EnemyType == EnemyType.GROUND_SLIDE_MEDIUM || _EnemyType == EnemyType.GROUND_SLIDE_TOP || _EnemyType == EnemyType.GROUND_SLIDE_CASTLE)
    {
      istower = true;
      _rb.bodyType = RigidbodyType2D.Dynamic;
    }
    else
    {
      GameScript.ShakeLight();
    }

    // Move through air
    transform.position += new Vector3(0f, 0f, -5f);
    _rb.velocity = Vector2.zero;
    _rb.angularVelocity = 0f;

    _rb.AddForce(new Vector2(30f, 300f) * _rb.mass);
    if (!istower)
      _rb.AddTorque(-_rb.mass * 300f);

    // Drop gold
    if (dropLoot)
    {
      DropLoot();

      Shop.IncrementUpgrades();
    }

    // Check win
    if (_EnemyType == EnemyType.BOSS)
    {
      GameScript.Win();
    }

    PlayerScript.WaveStats._enemiesKilled++;
  }

  void OnCollisionEnter2D(Collision2D c)
  {
    if (_IsDead) return;
    if (c.transform.parent.gameObject.name.Equals("Tower"))
    {
      if (_EnemyType == EnemyType.CRATE && GameScript.StateAtPlay())
      {
        Die();
        return;
      }
      PlayerScript._Player.Hit();
      if (GameScript.StateAtPlay()) Die(false);
    }
    if (c.gameObject.name.Equals("Arrow")) return;
    if (c.gameObject.name.Equals("Ground"))
    {
      if (_EnemyNoise != null && _EnemyType != EnemyType.GROUND_POP && _EnemyType != EnemyType.GROUND_POP_FLOAT && _EnemyType != EnemyType.GROUND_SLIDE && _EnemyType != EnemyType.GROUND_SLIDE_MEDIUM && _EnemyType != EnemyType.GROUND_SLIDE_SMALL && _EnemyType != EnemyType.GROUND_TOTEM_5)
        _EnemyNoise.Play();
      return;
    }
    _rb.AddForce(new Vector3(0f, 1f, 0f) * 100f * _rb.mass);
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
      case (EnemyType.GROUND_POP):
        enemyName = "Enemy4";
        break;
      case (EnemyType.GROUND_POP_FLOAT):
        enemyName = "Enemy16";
        break;
      case (EnemyType.CRATE):
        enemyName = "Crate";
        break;
      case (EnemyType.GROUND_SLIDE_SMALL):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy11" : "Enemy11.1");
        spawnPos = spawnLine.position + new Vector3(-10f + Random.value * 6f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE_MEDIUM):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy15" : "Enemy15.1");
        spawnPos = spawnLine.position + new Vector3(-10f + Random.value * 6f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy9" : "Enemy9.1");
        spawnPos = spawnLine.position + new Vector3(-10f + Random.value * 6f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE_TOP):
        enemyName = "Enemy19";
        spawnPos = spawnLine.position + new Vector3(-10f + Random.value * 6f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_SLIDE_CASTLE):
        enemyName = "Enemy22";
        spawnPos = spawnLine.position + new Vector3(-10f + Random.value * 6f, 0f, 0f);
        spawnPos.y = -7.4f;
        break;
      case (EnemyType.GROUND_TOTEM_5):
        enemyName = (Mathf.RoundToInt(Random.value) == 0 ? "Enemy17" : "Enemy18");
        spawnPos = spawnLine.position + new Vector3(-10f + Random.value * 6f, 0f, 0f);
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
    enemy.transform.position = (spawnPos == Vector3.zero ? new Vector3(58f, 17f, 0f) : spawnPos);
    var script = enemy.GetComponent<EnemyScript>();

    // Init enemy script
    script.Init();
    script._CanDropLoot = canDropLoot;

    return script;
  }

  void DropLoot()
  {
    if (!_CanDropLoot) return;

    // Decide drop
    var numCoins = 0;
    switch (_EnemyType)
    {
      case (EnemyType.GROUND_ROLL):
        numCoins = 5;
        break;
      case (EnemyType.GROUND_POP):
        numCoins = 20;
        break;
      case (EnemyType.GROUND_POP_FLOAT):
        numCoins = 35;
        break;
      case (EnemyType.GROUND_ROLL_SMALL):
        numCoins = 15;
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_2):
        numCoins = 25;
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_4):
        numCoins = 50;
        break;
      case (EnemyType.GROUND_ROLL_ARMOR_8):
        numCoins = 75;
        break;
      case (EnemyType.GROUND_SLIDE_SMALL):
        numCoins = 25;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_SLIDE_MEDIUM):
        numCoins = 50;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_SLIDE):
        numCoins = 100;
        GameScript.Freeze();
        break;
      case (EnemyType.GROUND_SLIDE_TOP):
        numCoins = 75;
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
        numCoins = 100;
        break;
    }

    // Drop
    if (numCoins > 0)
      StartCoroutine(DropCoins(numCoins));
  }

  IEnumerator DropCoins(int coins)
  {
    float timer = 0.2f / ((float)coins / 5f);
    while (coins-- > 0)
    {
      // Give coins
      PlayerScript.GiveCoins(1);

      var particles = GameResources.s_Instance._ParticlesCoins;
      var emitParams = new ParticleSystem.EmitParams();
      emitParams.position = transform.position;
      particles.Emit(emitParams, 1);

      var audio = GameResources.s_Instance._AudioCoinDrop;
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
