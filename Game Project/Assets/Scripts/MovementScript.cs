using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    //Script for other scripts to inherit from
    public Vector3 knockback = Vector3.zero;
    public float stun;
    public bool washit = false;

    public void SetKnockBack(Vector3 kbvec)
    {
        knockback = kbvec;
    }
    public void SetStun(float amnt)
    {
        stun = amnt;
    }

    public void SetHit()
    {
        washit = true;
    }
}
