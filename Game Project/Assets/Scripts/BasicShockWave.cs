using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicShockWave : MonoBehaviour
{
    public HitBox hitbox;
    Vector3 targetScale = new Vector3(1f, 1f, 1f);
    public float speed = 2f;

    // Update is called once per frame
    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, speed * Time.deltaTime);
        hitbox.boxSize = Vector3.Lerp(hitbox.boxSize, targetScale, speed * Time.deltaTime);
        if (transform.localScale.x >= 0.98f)
        {
            Destroy(gameObject);
        }
    }
}
