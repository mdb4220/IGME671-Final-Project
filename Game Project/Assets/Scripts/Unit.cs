using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    new public string name;

    public float hp;
    public float maxhp;

    public float attackmod = 1;
    public float defmod = 1;

    public float knockbackres = 1;
    public float stunres = 1;

    public MovementScript movescript;

    public void Update()
    {
        if(hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}
