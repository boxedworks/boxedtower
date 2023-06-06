using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowScript : MonoBehaviour
{

  AudioSource _sHitGround, _sHitSensor, _sHitWood, _sHitMetal, _sAirNoise, _sHitStone;

  Rigidbody2D _rb;

  List<Shop.UpgradeType> _upgradeModifiers;

  // Use this for initialization
  public Transform[] _children;
  void Start()
  {
    //Init();
  }

  public void Init()
  {
    _sHitGround = transform.GetChild(3).GetChild(0).GetComponent<AudioSource>();
    _sHitSensor = transform.GetChild(3).GetChild(1).GetComponent<AudioSource>();
    _sHitWood = transform.GetChild(3).GetChild(2).GetComponent<AudioSource>();
    _sHitMetal = transform.GetChild(3).GetChild(3).GetComponent<AudioSource>();
    _sHitStone = transform.GetChild(3).GetChild(5).GetComponent<AudioSource>();

    _sAirNoise = transform.GetChild(3).GetChild(4).GetComponent<AudioSource>();

    _rb = GetComponent<Rigidbody2D>();

    _upgradeModifiers = new List<Shop.UpgradeType>();
  }

  // Update is called once per frame
  void Update()
  {
    if (_rb == null || _rb.velocity == Vector2.zero) return;
    var vel = _rb.velocity;
    transform.rotation = Quaternion.LookRotation(vel, new Vector3(0f, 0f, 1f));
    transform.Rotate(new Vector3(0f, 90f, 0f));

    _sAirNoise.pitch = (1f + _rb.velocity.magnitude / 15f);// * Time.timeScale;

    if (transform.localPosition.x > 52.2f || transform.localPosition.x < -22.4f || transform.localPosition.y > 100f || transform.localPosition.y < -30f)
    {
      _children[0].parent = GameResources.s_Instance._ContainerDead;
      _children[1].parent = GameResources.s_Instance._ContainerDead;
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
    var s = transform.GetChild(2).GetComponent<ParticleSystem>();
    if (!s.isPlaying)
    {
      s.Play();
      _rb.mass = 0.5f;
      _rb.gravityScale = 0f;
      GetComponent<BoxCollider2D>().isTrigger = true;
    }
  }

  void OnTriggerEnter2D(Collider2D c)
  {
    // Check for trigger
    if (c.isTrigger) return;

    // Check for stone
    if (c.gameObject.name.Equals("Stone")) GameScript.PlaySound(_sHitStone, 0.8f, 2.2f);

    // Check out of bounds
    if (c.gameObject.name.Equals("Ground") || c.gameObject.name.Equals("Barrier"))
    {
      Destroy(_rb);
      transform.position += 1.5f * transform.right;
      _sAirNoise.Stop();
      CleanUp();
      return;
    }
    // Check enemy
    var enemyScript = c.transform.parent.gameObject.GetComponent<EnemyScript>();
    if (enemyScript != null)
    {
      // Play sound FX based on material
      var sfx = _sHitSensor;
      if (c.gameObject.name.Equals("Wood")) sfx = _sHitWood;
      if (c.gameObject.name.Equals("Armor")) sfx = _sHitMetal;
      if (!c.gameObject.name.Equals("Stone"))
        GameScript.PlaySound(sfx, 0.8f, 2.2f);

      // Check if collider was a sensor
      enemyScript.CheckHit(c);
    }
  }

  void OnCollisionEnter2D(Collision2D c)
  {
    CheckCollision(c.collider);
  }

  bool _triggered;
  void CheckCollision(Collider2D c)
  {
    if (c.isTrigger) return;
    if (_triggered) return;
    _triggered = true;

    // Check materials
    AudioSource hitNoise = null;
    if (c.gameObject.name.Equals("Wood")) hitNoise = _sHitWood;
    if (c.gameObject.name.Equals("Armor")) hitNoise = _sHitMetal;
    if (c.gameObject.name.Equals("Stone")) hitNoise = _sHitStone;
    if (hitNoise != null)
      GameScript.PlaySound(hitNoise, 0.8f, 1.2f);
    _sAirNoise.Stop();

    // Destroy Physics components
    Destroy(_rb);
    Destroy(transform.GetChild(4).GetComponent<BoxCollider2D>());

    // Move with collider
    if (c.transform.GetComponent<Rigidbody2D>() != null)
    {
      transform.parent = c.transform;
    }
    else if (c.transform.parent.GetComponent<Rigidbody2D>() != null)
      transform.parent = c.transform.parent;
    else
      transform.parent = GameResources.s_Instance._ContainerDead;

    // Check if ground
    if (c.gameObject.name.Equals("Ground"))
    {
      transform.parent = GameResources.s_Instance._ContainerDead;
    }
    if (TargetScript.IsTarget(c.gameObject))
    {
      var ts = c.transform.parent.GetComponent<TargetScript>();
      GameScript.PlaySound(_sHitSensor, 0.8f, 2.2f);
      ts.Die();
    }

    // Check if enemy
    var enemyScript = c.transform.parent.gameObject.GetComponent<EnemyScript>();
    if (enemyScript != null)
    {

      // Play hit FX based on surface
      if (hitNoise == null)
        GameScript.PlaySound(_sHitSensor, 0.8f, 2.2f);

      // Check if collider was a sensor
      enemyScript.CheckHit(c);
    }
    else
    {
      GameScript.PlaySound(_sHitGround, 1.8f, 2.2f);
    }
    transform.GetChild(1).GetComponent<ParticleSystem>().Play();

    // Move particles systems
    _children[0].parent = GameResources.s_Instance._ContainerDead;
    _children[1].parent = GameResources.s_Instance._ContainerDead;

    // Check powerup
    Debug.Log("Mods: ");
    foreach (var u in _upgradeModifiers)
      Debug.Log(u);
    if (_upgradeModifiers.Contains(Shop.UpgradeType.ARROW_RAIN))
    {
      PlayerScript.ArrowRain(transform.position.x, _upgradeModifiers);
    }

    // Destroy script
    CleanUp();
  }

  public void Fired()
  {
    _sAirNoise.Play();
  }

  void CleanUp()
  {
    IEnumerator CleanUpCo()
    {
      yield return new WaitForSeconds(5f);

      Destroy(_children[0].gameObject);
      Destroy(_children[1].gameObject);
      Destroy(_children[2].gameObject);
      Destroy(_children[3].gameObject);
      Destroy(this);
    }
    StartCoroutine(CleanUpCo());
  }

}


//transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(vel, new Vector3(0f, 0f, 1f)), Time.deltaTime);
