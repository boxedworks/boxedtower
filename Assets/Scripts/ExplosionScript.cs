using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionScript {

    public static void SpawnExplosion(Vector3 position, float radius = 2f)
    {
        GameObject alive = GameObject.Find("Alive");
        for(int i = 0; i < alive.transform.childCount; i++)
        {
            Transform child = alive.transform.GetChild(i);
            EnemyScript s = child.GetComponent<EnemyScript>();
            foreach(MeshRenderer r in s._Sensors)
            {

            }
        }
    }
}
