using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunScript : MonoBehaviour {

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

  void OnCollisionEnter2D(Collision2D c)
    {
        GameScript.PlaySound(transform.GetChild(3).gameObject, 0.9f, 1.1f);
        if (c.gameObject.name.Equals("Arrow"))
        {
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(2).gameObject.SetActive(true);
            return;
        }
        EnemyScript s = c.gameObject.GetComponent<EnemyScript>();
        if (s == null) return;
        s.Die();
    }
}
