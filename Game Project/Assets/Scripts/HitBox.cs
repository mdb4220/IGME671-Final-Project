using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum KnockbackType { Directional, Radial, Forward}

public class HitBox : MonoBehaviour
{
    //size
    public Vector3 position;
    public Vector3 boxSize;
    public float rotX;
    public float rotY;
    public float rotZ;
    public LayerMask mask;

    //functionality
    List<Unit> units = new List<Unit>();
    private static GameObject damagetext;
    private static GameObject canvas;

    //stats
    public Unit creator;
    public float damage = 0;
    public bool pierce = true;
    public float stundur = 0;
    public KnockbackType kbtype;
    public float knockbackAmount = 0;
    public Vector3 knockbackdir = Vector3.zero;

    //Sound
    [FMODUnity.EventRef]
    public string PlayerDamagedEvent = "";
    [FMODUnity.EventRef]
    public string ButterflyDamagedEvent = "";
    [FMODUnity.EventRef]
    public string LizardDamagedEvent = "";

    [FMODUnity.EventRef]
    public string HitEvent = "";

    FMOD.Studio.PARAMETER_ID damageParameterId;

    public void Awake()
    {
        canvas = GameObject.Find("Main Canvas");
        damagetext = GameObject.Find("GameManager").GetComponent<GameManager>().damagetextprefab;
    }

    private void Start()
    {
        if (creator == null)
        {
            creator.name = "Something";
        }

        FMOD.Studio.EventDescription damageEventDescription = FMODUnity.RuntimeManager.GetEventDescription(HitEvent);
        FMOD.Studio.PARAMETER_DESCRIPTION damageParameterDescription;
        damageEventDescription.getParameterDescriptionByName("AttackPower", out damageParameterDescription);
        damageParameterId = damageParameterDescription.id;
    }



    // Update is called once per frame
    void Update()
    {
        Collider[] colliders = Physics.OverlapBox(transform.position + (transform.rotation * position), boxSize, transform.rotation * Quaternion.Euler(rotX, rotY, rotZ), mask);

        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                //try to find "Unit" script
                Unit unit = colliders[i].GetComponentInParent<Unit>();
                //if the hurtbox is attatched to a unit
                if (unit != null)
                {
                    //if we havent hit this already
                    if (!units.Contains(unit) && unit != creator)
                    {
                        //calculate damage

                        //deal damage
                        Debug.Log(creator.name + " hits " + unit.name + " for " + damage + " damage");
                        int damageamount = Mathf.RoundToInt(damage * unit.defmod);
                        unit.hp -= damageamount;


                        FMOD.Studio.EventInstance hit = FMODUnity.RuntimeManager.CreateInstance(HitEvent);
                        hit.setParameterByID(damageParameterId, damageamount);
                        hit.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
                        hit.start();
                        hit.release();

                        if (unit.name == "Player")
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(PlayerDamagedEvent, transform.position);
                        }
                        if (unit.name == "Lizard")
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(LizardDamagedEvent, transform.position);
                        }
                        if (unit.name == "Butterfly")
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(ButterflyDamagedEvent, transform.position);
                        }
                        Vector3 knockbackdirection = Vector3.zero;
                        if (kbtype == KnockbackType.Directional)
                        {
                            knockbackdirection = knockbackdir.normalized;
                        }
                        else if (kbtype == KnockbackType.Radial)
                        {
                            knockbackdirection = (unit.transform.position - transform.position).normalized;
                        }
                        else if (kbtype == KnockbackType.Forward)
                        {
                            knockbackdirection = transform.forward;
                        }
                        unit.movescript.SetKnockBack(knockbackdirection * (knockbackAmount * unit.knockbackres));
                        unit.movescript.SetStun(stundur * unit.stunres);
                        unit.movescript.SetHit();

                        //shake the camera
                        //Camera.main.GetComponent<CameraControl>().shake = .1f;

                        //create text
                        GameObject dmgtext = Instantiate(damagetext);
                        DamageText dmgtextText = dmgtext.GetComponent<DamageText>();
                        dmgtextText.SetText(damageamount.ToString());
                        dmgtext.transform.SetParent(canvas.transform, false);
                        Vector3 textpoint = Vector3.Lerp(transform.position, unit.transform.position, 0.8f);
                        textpoint = new Vector3(textpoint.x + Random.Range(-.6f, .6f), textpoint.y + 1f + Random.Range(-.6f, .6f), textpoint.z + Random.Range(-.6f, .6f));
                        dmgtext.transform.position = Camera.main.WorldToScreenPoint(textpoint);

                        //add to the list of hit units
                        units.Add(unit);

                        if (!pierce)
                        {
                            Destroy(gameObject);
                        }
                    }
                }
                else
                {
                    Debug.Log("Error: Disjointed Hurtbox");
                }

            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, .2f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * position), transform.rotation * Quaternion.Euler(rotX, rotY, rotZ), transform.localScale);
        Gizmos.DrawCube(Vector3.zero, new Vector3(boxSize.x * 2, boxSize.y * 2, boxSize.z * 2));
    }
}
