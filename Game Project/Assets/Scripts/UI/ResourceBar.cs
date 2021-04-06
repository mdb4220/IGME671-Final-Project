using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Resourcetype { Health, Atma, Experience }

public class ResourceBar : MonoBehaviour
{
    public Slider slider;
    public Resourcetype resourcetype;
    Unit player;

    public void Start()
    {
        player = GameObject.Find("Player").GetComponent<Unit>();
    }

    public void Update()
    {
        if (resourcetype == Resourcetype.Health)
        {
            slider.value = player.hp / player.maxhp;
        }
    }
}


