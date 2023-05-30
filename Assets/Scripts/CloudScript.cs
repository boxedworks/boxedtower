using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudScript : MonoBehaviour {

    GameObject[] _children;

	// Use this for initialization
	void Start () {
        _children = new GameObject[transform.childCount];
        for(int i = 0; i < _children.Length; i++)
        {
            _children[i] = transform.GetChild(i).gameObject;
        }
	}
	
	// Update is called once per frame
	void Update () {
        transform.position += new Vector3(-1f, 0f, 0f) * Time.deltaTime * 1.25f;

        if(transform.position.x < -50f)
        {
            float ySpawn = 2f + Random.value * 6f;
            transform.localPosition = new Vector3(90f + Random.value * 150f, ySpawn, transform.position.z);
        }
	}
}
