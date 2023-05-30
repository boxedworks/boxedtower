using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetScript : MonoBehaviour {

    Rigidbody _rb;
    Vector2 _target;

    public bool _dead;
    int _moveIter;

    public float _forceModifier;

    float _xDecide;

    public TargetType _type;

    public enum TargetType
    {
        FLY,
        ROLL,
        BOMB
    }

	public void Init(Vector2 position, TargetType type)
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = false;

        _target = position;

        _type = type;

        Color c = PlayerScript._GemColor;
        // Change _rb if type is different
        if (_type == TargetType.ROLL)
        {
            // USe gravity and lower drag
            _rb.useGravity = true;
            _rb.drag = 0.5f;
            c = Color.blue;
        }else if(_type == TargetType.BOMB)
        {
            // Set position
            _rb.position = new Vector3(50f, 13f - Random.value * 5f, 0f);
            c = Color.green;
            _xDecide = 5f + Random.value * 10f;
        }
        // Set color based on type
        transform.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = c;
    }

    // Update is called once per frame
    void FixedUpdate () {
        if (_rb == null || _dead) return;
        if (_type == TargetType.ROLL)
        {
            _rb.AddTorque(new Vector3(0f, 0f, 1f) * 200f * _forceModifier);
            return;
        }else if(_type == TargetType.BOMB)
        {
            if(_rb.position.x <= _xDecide)
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                _rb.AddForce(new Vector3(0f, -1f, 0f) * 35f * _forceModifier);
                if(_rb.position.y < 0f)
                {
                    _type = TargetType.ROLL;
                    _rb.useGravity = true;
                    _rb.drag = 0.5f;
                }
                return;
            }
            _rb.AddForce(new Vector3(-1f, 0f, 0f) * 30f * _forceModifier);
            return;
        }
        Vector3 t = GameObject.Find("ShootEffect").transform.parent.position;
        _target = new Vector2(t.x, t.y);
        Vector3 distance = new Vector3(_target.x, _target.y, 0f) - _rb.position;
        _rb.AddForce((distance).normalized * 50f * _forceModifier);
        _rb.AddTorque(new Vector3(0f, 0f, 1f) * 10f * _forceModifier);
    }

  void OnCollisionEnter2D(Collision2D c)
    {
        if (_dead) return;
        if (c.gameObject.name.Equals("Tower"))
        {
            PlayerScript._Player.Hit();
            Die(false);
        }
    }

    public void Die(bool givePoint = true)
    {
        if (_dead) return;
        if(givePoint) PlayerScript.AddTarget();
        _rb.constraints = RigidbodyConstraints.FreezePositionZ;
        transform.position += new Vector3(0f, 0f, -7f);
        _rb.velocity = Vector3.zero;
        _rb.useGravity = true;
        _rb.drag = 0f;
        _rb.AddForce(new Vector3(-0.5f + UnityEngine.Random.value * 1f, 1f, 0f) * Random.value * 300f);
        _rb.AddTorque(new Vector3(0f, 0f, -1f * Random.value * 2f) * (200f + Random.value * 700f));
        transform.parent = GameResources.s_Instance._ContainerDead;

        transform.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = Color.black;
        transform.GetChild(0).GetComponent<MeshRenderer>().materials[1].color = Color.black;

        StartCoroutine(SpawnExplosions());

        if (PlayerScript.GetAmmo() == 0) PlayerScript.GiveAmmo();

        _dead = true;
    }

    IEnumerator SpawnExplosions()
    {
        float timer = 0.05f * Mathf.RoundToInt(3f + Random.value * 3f);
        float startPos = -(transform.localScale.x) * 3f;
        while(timer > 0f)
        {
            GameScript.ShakeLight();
            GameScript.SpawnExplosion(new Vector3(startPos + Random.value * 2f * -startPos, startPos + Random.value * 2f * -startPos, 0f) + transform.position);
            timer -= 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
    }

    public static bool IsTarget(GameObject g)
    {
        if (g.transform.parent.GetComponent<TargetScript>() != null) return true;
        return false;
    }
}
