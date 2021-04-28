using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PlayerSpellManager : MonoBehaviour
{
    public List<Spell> spells;
    int currentspellind = 0;
    public Spell currentspell;

    public Text spelltext;
    public Image leftswap;
    public Image rightswap;

    [FMODUnity.EventRef]
    public string SwitchSpellEvent = "";

    public void Awake()
    {
        currentspell = spells[0];
        spelltext.text = currentspell.name;
        leftswap.CrossFadeAlpha(.2f, 0f, true);
        rightswap.CrossFadeAlpha(1f, 0f, true);
    }

    public void SwapSpellNext()
    {
        if (currentspellind < spells.Count - 1)
        {
            FMODUnity.RuntimeManager.PlayOneShot(SwitchSpellEvent, transform.position);
            currentspellind++;
            currentspell = spells[currentspellind];
            spelltext.text = currentspell.name;
            if (currentspellind == 0)
            {
                leftswap.CrossFadeAlpha(.2f, 0f, true);
                rightswap.CrossFadeAlpha(1f, 0f, true);
            }
            else if (currentspellind == spells.Count - 1)
            {
                leftswap.CrossFadeAlpha(1f, 0f, true);
                rightswap.CrossFadeAlpha(.2f, 0f, true);
            }
            else
            {
                leftswap.CrossFadeAlpha(1f, 0f, true);
                rightswap.CrossFadeAlpha(1f, 0f, true);
            }
        }
    }

    public void SwapSpellPrev()
    {
        if (currentspellind > 0)
        {
            FMODUnity.RuntimeManager.PlayOneShot(SwitchSpellEvent, transform.position);
            currentspellind--;
            currentspell = spells[currentspellind];
            spelltext.text = currentspell.name;
        }
        if (currentspellind == 0)
        {
            leftswap.CrossFadeAlpha(.2f, 0f, true);
            rightswap.CrossFadeAlpha(1f, 0f, true);
        }
        else if (currentspellind == spells.Count - 1)
        {
            leftswap.CrossFadeAlpha(1f, 0f, true);
            rightswap.CrossFadeAlpha(.2f, 0f, true);
        }
        else
        {
            leftswap.CrossFadeAlpha(1f, 0f, true);
            rightswap.CrossFadeAlpha(1f, 0f, true);
        }
    }
}
