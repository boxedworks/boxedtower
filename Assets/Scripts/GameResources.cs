using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResources
{
  //
  public Camera _CameraMain;

  public Transform _Sun,
      _Sky,
      _SpawnLine,
      _ContainerDead,
      _Arrows,
      _UI;

  public TextMesh _TextCoins;
  public UnityEngine.UI.Slider _SliderSloMo, _SliderShop;

  public ParticleSystem _ParticlesCoins,
      _ParticlesArrowHit;

  public AudioSource _AudioCoinDrop,
      _AudioSfxGround,
      _AudioSfxSensor,
      _AudioSfxWood,
      _AudioSfxMetal,
      _AudioSfxAir,
      _AudioSfxStone,
      _AudioSfxCrateBreak;

  public Collider2D _ColliderGround;

  // Constructor
  public static GameResources s_Instance;

  public GameResources()
  {
    s_Instance = this;

    // Find objects
    _CameraMain = Camera.main;

    _Sun = GameObject.Find("Sun").transform;
    _Sky = GameObject.Find("Sky").transform;
    _SpawnLine = GameObject.Find("SpawnLine").transform;
    _ContainerDead = GameObject.Find("Dead").transform;
    _Arrows = GameObject.Find("Arrows").transform;
    _UI = GameObject.Find("UI").transform;

    _ParticlesCoins = GameObject.Find("CoinSystem").GetComponent<ParticleSystem>();
    _ParticlesArrowHit = GameObject.Find("ArrowHit").GetComponent<ParticleSystem>();

    _SliderSloMo = GameObject.Find("SlowMoUI").transform.GetChild(0).GetComponent<UnityEngine.UI.Slider>();
    _SliderShop = GameObject.Find("ShopTimerUI").transform.GetChild(0).GetComponent<UnityEngine.UI.Slider>();

    _AudioCoinDrop = GameObject.Find("CoinDrop").GetComponent<AudioSource>();

    _AudioSfxGround = GameObject
        .Find("ArrowSounds")
        .transform.Find("HitGround")
        .GetComponent<AudioSource>();
    _AudioSfxSensor = GameObject
        .Find("ArrowSounds")
        .transform.Find("HitSensor")
        .GetComponent<AudioSource>();
    _AudioSfxWood = GameObject
        .Find("ArrowSounds")
        .transform.Find("HitWood")
        .GetComponent<AudioSource>();
    _AudioSfxMetal = GameObject
        .Find("ArrowSounds")
        .transform.Find("HitMetal")
        .GetComponent<AudioSource>();
    _AudioSfxStone = GameObject
        .Find("ArrowSounds")
        .transform.Find("HitStone")
        .GetComponent<AudioSource>();
    _AudioSfxCrateBreak = GameObject
        .Find("ArrowSounds")
        .transform.Find("BreakCrate")
        .GetComponent<AudioSource>();
    _AudioSfxAir = GameObject
        .Find("ArrowSounds")
        .transform.Find("Air")
        .GetComponent<AudioSource>();

    _ColliderGround = GameObject.Find("Ground").GetComponent<Collider2D>();

    _TextCoins = GameObject.Find("CoinUI").transform.GetChild(1).GetComponent<TextMesh>();
  }
}
