using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyScript : MonoBehaviour {

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

  void OnCollisionEnter2D(Collision2D c)
    {
        Destroy(c.gameObject);
    }
}
