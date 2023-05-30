using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResources
{

  //
  public Camera _CameraMain;

  public Transform _Sun, _SpawnLine, _ContainerDead;

  public ParticleSystem _ParticlesCoins;

  public AudioSource _AudioCoinDrop;

  // Constructor
  public static GameResources s_Instance;
  public GameResources()
  {
    s_Instance = this;

    // Find objects
    _CameraMain = Camera.main;

    _Sun = GameObject.Find("Sun").transform;
    _SpawnLine = GameObject.Find("SpawnLine").transform;
    _ContainerDead = GameObject.Find("Dead").transform;

    _ParticlesCoins = GameObject.Find("CoinSystem").GetComponent<ParticleSystem>();

    _AudioCoinDrop = GameObject.Find("CoinDrop").GetComponent<AudioSource>();
  }

}
