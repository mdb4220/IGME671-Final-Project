using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMissile : MonoBehaviour
{
    public float speed = 2f;

    // Update is called once per frame
    void Update()
    {
        transform.position += (transform.forward * speed) * Time.deltaTime;
    }
}
