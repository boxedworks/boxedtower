using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResources
{

  //
  public Camera _CameraMain;

  public Transform _Sun, _Sky, _SpawnLine, _ContainerDead, _Arrows;

  public ParticleSystem _ParticlesCoins;

  public AudioSource _AudioCoinDrop;

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
    _ContainerDead = GameObject.Find("Arrows").transform;

    _ParticlesCoins = GameObject.Find("CoinSystem").GetComponent<ParticleSystem>();

    _AudioCoinDrop = GameObject.Find("CoinDrop").GetComponent<AudioSource>();

    _ColliderGround = GameObject.Find("Ground").GetComponent<Collider2D>();
  }

}
