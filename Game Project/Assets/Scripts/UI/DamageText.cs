using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageText : MonoBehaviour
{
    public Animator anim;
    private Text text;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        AnimatorClipInfo[] clipinfo = anim.GetCurrentAnimatorClipInfo(0);
        Destroy(gameObject, clipinfo[0].clip.length);

        text = anim.GetComponent<Text>();
    }

    public void SetText(string newtext)
    {
        text.text = newtext;
    }
}
