using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowScript : MonoBehaviour
{
  Rigidbody2D _rb;

  public bool _GameSpawned;

  List<Shop.UpgradeType> _upgradeModifiers;

  ParticleSystem _ps_trail,
      _ps_trailFire;
  AudioSource _audioPlayer0,
      _audioPlayer1;

  // Use this for initialization
  void Start()
  {
    //Init();
  }

  public void Init()
  {
    if (_rb == null)
    {
      _rb = GetComponent<Rigidbody2D>();
      var audioSources = GetComponents<AudioSource>();
      _audioPlayer0 = audioSources[0];
      _audioPlayer1 = audioSources[1];

      _ps_trail = transform.GetChild(1).GetChild(0).GetComponent<ParticleSystem>();
      _ps_trailFire = transform.GetChild(1).GetChild(1).GetComponent<ParticleSystem>();
    }

    _upgradeModifiers = new List<Shop.UpgradeType>();
  }

  // Update is called once per frame
  void Update()
  {
    if (_rb == null || _rb.velocity == Vector2.zero)
      return;
    var vel = _rb.velocity;
    transform.rotation = Quaternion.LookRotation(vel, new Vector3(0f, 0f, 1f));
    transform.Rotate(new Vector3(0f, 90f, 0f));

    if (Time.timeScale == 0f)
      _audioPlayer0.pitch = 0f;
    else
      _audioPlayer0.pitch = (1f + _rb.velocity.magnitude / 15f); // * Time.timeScale;

    if (
        transform.localPosition.x > 52.2f
        || transform.localPosition.y < -30f
        || (
            (transform.localPosition.x < -22.4f || transform.localPosition.y > 100f)
            && !_GameSpawned
        )
    )
    {
      // Move particles systems
      _ps_trail.transform.parent = GameResources.s_Instance._ContainerDead;
      _ps_trailFire.transform.parent = GameResources.s_Instance._ContainerDead;

      //
      Destroy(gameObject);
    }
  }

  //
  public void RegisterUpgrade(Shop.UpgradeType upgradeType)
  {
    _upgradeModifiers.Add(upgradeType);
  }

  // Pirce
  public void ActivatePierce()
  {
    if (!_ps_trailFire.isPlaying)
    {
      _ps_trailFire.Play();
      _rb.mass = 0.5f;
      _rb.gravityScale = 0f;
      transform.GetChild(0).GetComponent<BoxCollider2D>().isTrigger = true;

      // Check scale
      var scaleMod = 1f + Mathf.Clamp(Shop.GetUpgradeCount(Shop.UpgradeType.ARROW_PENETRATION) - 1, 0, 1000) * 2f;
      if (scaleMod != 1f)
      {
        transform.localScale *= scaleMod;
      }
    }
  }

  void SetAndPlay(
      AudioSource current,
      AudioSource newSource,
      float pitchMin = 0.8f,
      float pitchMax = 1.4f,
      bool oneShot = false
  )
  {
    if (current.isPlaying)
      current.Stop();
    current.clip = newSource.clip;
    current.loop = newSource.loop;
    current.volume = newSource.volume;
    current.pitch = Random.Range(pitchMin, pitchMax);
    if (oneShot)
      current.PlayOneShot(current.clip);
    else
      current.Play();
  }

  void OnCollisionAny()
  {
    //LightningManager.QueueLightning(transform.position.x);
  }

  float _lastWoodSfx,
      _lastStoneSfx;

  void OnTriggerEnter2D(Collider2D c)
  {
    // Check for trigger
    if (c.isTrigger)
      return;

    OnCollisionAny();

    // Check out of bounds
    if (/*c.gameObject.name.Equals("Ground") || */c.gameObject.name.Equals("Barrier"))
    {
      Destroy(_rb);
      transform.position += 1.5f * transform.right + -1f * transform.up;
      CleanUp();
      return;
    }

    // Check enemy
    var enemyScript = c.transform.parent.gameObject.GetComponent<EnemyScript>();
    if (enemyScript != null)
    {
      // Check if collider was a sensor
      if (enemyScript.CheckHit(c) && !_GameSpawned)
      {
        //Shop.IncrementUpgrades();
      }
    }

    // Play sound FX based on material
    var sfx = GameResources.s_Instance._AudioSfxSensor;
    if (
        (enemyScript?._EnemyType ?? EnemyScript.EnemyType.GROUND_ROLL)
        == EnemyScript.EnemyType.CRATE
    )
      sfx = GameResources.s_Instance._AudioSfxCrateBreak;
    else if (c.gameObject.name.Equals("Wood"))
    {
      if (Time.time - _lastWoodSfx < 0.3f)
      {
        sfx = null;
      }
      else
      {
        _lastWoodSfx = Time.time;
        sfx = GameResources.s_Instance._AudioSfxWood;
      }
    }
    else if (c.gameObject.name.Equals("Armor"))
      sfx = GameResources.s_Instance._AudioSfxMetal;
    else if (c.gameObject.name.Equals("Stone"))
    {
      if (Time.time - _lastStoneSfx < 0.3f)
      {
        sfx = null;
      }
      else
      {
        _lastStoneSfx = Time.time;
        sfx = GameResources.s_Instance._AudioSfxStone;
      }
    }
    else if (c.transform.parent.gameObject.name.Equals("Balloon"))
    {
      sfx = null;
    }
    if (sfx != null)
      SetAndPlay(_audioPlayer1, sfx, 0.8f, 1.8f, true);
  }

  static void CheckMaxArrows()
  {
    var maxBodies = 50;
    var bodies = GameResources.s_Instance._ContainerDead.childCount;
    var diff = bodies - maxBodies;

    while (--diff > -1)
    {
      GameObject.Destroy(GameResources.s_Instance._ContainerDead.GetChild(diff).gameObject);
    }
  }

  void OnCollisionEnter2D(Collision2D c)
  {
    CheckCollision(c.collider);
  }

  bool _triggered;

  void CheckCollision(Collider2D c)
  {
    if (c.isTrigger)
      return;
    if (_triggered)
      return;
    _triggered = true;

    OnCollisionAny();

    // Check materials
    var comboDelta = 0f;
    var isBalloon = false;
    AudioSource hitNoise = null;
    if (c.gameObject.name.Equals("Wood"))
    {
      hitNoise = GameResources.s_Instance._AudioSfxWood;
      comboDelta -= PlayerScript._COMBO_REMOVE;
    }
    else if (c.gameObject.name.Equals("Armor"))
    {
      hitNoise = GameResources.s_Instance._AudioSfxMetal;
      comboDelta += PlayerScript._COMBO_ADD;
    }
    else if (c.gameObject.name.Equals("Stone"))
    {
      hitNoise = GameResources.s_Instance._AudioSfxStone;
      comboDelta -= PlayerScript._COMBO_REMOVE;
    }
    else if (c.transform.parent.gameObject.name.Equals("Balloon"))
    {
      isBalloon = true;
    }
    if (hitNoise != null)
    {
      SetAndPlay(_audioPlayer1, hitNoise, 0.8f, 1.6f);
    }
    // Destroy Physics components
    if (transform != null)
    {
      Destroy(_rb);
      transform.GetChild(0).GetComponent<BoxCollider2D>().enabled = false;
    }

    // Move with collider
    if (isBalloon)
    {
      GetComponent<MeshRenderer>().enabled = false;
    }
    else if (c.gameObject.name == "Armor" || c.transform.GetComponent<Rigidbody2D>() != null)
    {
      transform.parent = c.transform;
    }
    else if (c.transform.parent.GetComponent<Rigidbody2D>() != null)
    {
      transform.parent = c.transform.parent;
    }
    else
    {
      transform.parent = GameResources.s_Instance._ContainerDead;
    }

    // Check if ground
    if (c.gameObject.name.Equals("Ground"))
    {
      transform.parent = GameResources.s_Instance._ContainerDead;
      comboDelta -= PlayerScript._COMBO_REMOVE;

      if (!_GameSpawned)
      {
        //PlayerScript.SpawnArrowAbove(transform.position.x);
      }
    }

    // Check if enemy
    var enemyScript = c.transform.parent.gameObject.GetComponent<EnemyScript>();
    if (enemyScript != null)
    {
      // Play hit FX based on surface
      if (hitNoise == null)
      {
        if (enemyScript._EnemyType == EnemyScript.EnemyType.CRATE)
          SetAndPlay(_audioPlayer1, GameResources.s_Instance._AudioSfxCrateBreak, 0.8f, 1.6f);
        else
          SetAndPlay(_audioPlayer1, GameResources.s_Instance._AudioSfxSensor, 0.8f, 1.6f);
      }

      // Check if collider was a sensor
      if (enemyScript.CheckHit(c) && !_GameSpawned)
      {
        if (enemyScript._EnemyType != EnemyScript.EnemyType.CRATE)
          Shop.IncrementUpgrades();
        else
          PlayerScript.AddCombo(PlayerScript._COMBO_ADD);
      }
    }
    else if(hitNoise == null)
    {
      SetAndPlay(_audioPlayer1, GameResources.s_Instance._AudioSfxGround, 0.8f, 1.6f);
    }
    var ps = GameResources.s_Instance._ParticlesArrowHit;
    ps.transform.position = transform.position;
    ps.Play();

    // Move particles systems
    _ps_trail.transform.parent = GameResources.s_Instance._ContainerDead;
    _ps_trailFire.transform.parent = GameResources.s_Instance._ContainerDead;

    // Check powerup
    if (_upgradeModifiers.Contains(Shop.UpgradeType.ARROW_RAIN))
    {
      PlayerScript.ArrowRain(transform.position.x, _upgradeModifiers);
    }

    // Combo
    if (comboDelta != 0f && (!_GameSpawned || (_GameSpawned && comboDelta > 0f)))
      PlayerScript.AddCombo(comboDelta);

    // Destroy script
    CleanUp();
    CheckMaxArrows();
  }

  public void Fired()
  {
    SetAndPlay(_audioPlayer0, GameResources.s_Instance._AudioSfxAir, 0.8f, 1.6f);
  }

  void CleanUp()
  {
    transform.GetChild(0).gameObject.SetActive(false);
    _audioPlayer0.Stop();

    IEnumerator CleanUpCo()
    {
      yield return new WaitForSeconds(5f);

      Destroy(this);
    }
    StartCoroutine(CleanUpCo());
  }


  // Lightning power
  public static class LightningManager
  {

    public static ParticleSystem s_Particles { get { return GameResources.s_Instance._Lightning; } }

    static Queue<float> s_lightningQueue;

    public static void Init()
    {
      s_lightningQueue = new Queue<float>();
    }

    public static void Update()
    {

      if (s_lightningQueue.Count > 0)
      {

        if (s_Particles.isEmitting) return;

        var lightningPos = s_lightningQueue.Dequeue();

        var pos = s_Particles.transform.position;
        pos.x = lightningPos;
        s_Particles.transform.position = pos;

        s_Particles.Play();
      }

    }

    public static void QueueLightning(float xPos)
    {
      s_lightningQueue.Enqueue(xPos);
    }
  }

}


//transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(vel, new Vector3(0f, 0f, 1f)), Time.deltaTime);
