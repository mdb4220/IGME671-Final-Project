using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MovementScript
{
    Unit unit;
    CharacterController controller;
    InputMaster controls;
    Transform cam;
    Animator anim;
    CapsuleCollider hurtbox;

    //movement input
    public Vector2 move;

    //movement variables
    public float speed = 6f;
    public float grav = 30f;
    public float jumpspeed = 15;

    float vspeed = 0;

    bool extrajump = true;

    //movement during attacks/rolling
    public float tempspeed = 0f;
    public float rollspeed = 12f;
    public float attackmovespeed = 3f;
    public float airattackpushamount = 3f;
    public float castmovespeed = 3f;
    public float groundcastfallspeed = 10f;

    //smoothing rotation
    public float turnSmoothing = .1f;
    float turnSmoothVel;
    float angle = 0;
    float targetAngle = 0;
    Vector3 direction;

    //smoothing animation
    public float animSmoothing = .1f;
    float animSmoothVel;


    //attacking and rolling
    bool attacking = false;
    bool airattacking = false;
    bool plungeattacking = false;
    bool casting = false;
    bool rolling = false;
    bool actionable = true;
    bool queueable = false;

    int queuedaction = 0;

    GameObject currentmeleehitbox;
    public GameObject prefabHitBox;

    PlayerSpellManager spellManager;

    //Sound Effects
    [FMODUnity.EventRef]
    public string FootStepEvent = "";
    [FMODUnity.EventRef]
    public string JumpEvent = "";
    [FMODUnity.EventRef]
    public string RollEvent = "";
    [FMODUnity.EventRef]
    public string SlashEvent = "";

    [FMODUnity.EventRef]
    public string FireEvent = "";
    [FMODUnity.EventRef]
    public string EarthquakeEvent = "";

    [FMODUnity.EventRef]
    public string LavaEvent = "";
    [FMODUnity.EventRef]
    public string CrackleEvent = "";
    [FMODUnity.EventRef]
    public string EarthCrackEvent = "";


    //create input
    private void Awake()
    {
        cam = Camera.main.transform;
        anim = GetComponent<Animator>();
        unit = GetComponent<Unit>();
        controller = GetComponent<CharacterController>();
        hurtbox = GetComponentInChildren<CapsuleCollider>();
        spellManager = GetComponent<PlayerSpellManager>();

        controls = new InputMaster();
        controls.Player.Movement.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Movement.canceled += ctx => move = Vector2.zero;
        controls.Player.Jump.performed += ctx => Jump();
        controls.Player.Roll.performed += ctx => Roll();
        controls.Player.Attack.performed += ctx => Attack();
        controls.Player.SwapSpellNext.performed += ctx => SwapSpellNext();
        controls.Player.SwapSpellPrev.performed += ctx => SwapSpellPrev();
        controls.Player.Cast.performed += ctx => Cast();

        // Poll gamepads at 120 Hz.
        InputSystem.pollingFrequency = 120;
    }

    //enable and disable inputs
    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Update()
    {
        FMODUnity.RuntimeManager.PlayOneShot(LavaEvent, transform.position);
        FMODUnity.RuntimeManager.PlayOneShot(CrackleEvent, transform.position);
        FMODUnity.RuntimeManager.PlayOneShot(EarthCrackEvent, transform.position);


        //horizontal movement
        float horizontal = move.x;
        float vertical = move.y;
        direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (actionable && queuedaction != 0)
        {
            if (queuedaction == 1) //attack
            {
                BeginAttack();
            }
            else if (queuedaction == 2) //roll
            {
                BeginRoll();
            }
            else if (queuedaction == 3) //Cast
            {
                BeginCast();
            }

            queuedaction = 0;
        }

        //resolve errors
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            ResetActionVariables();
            actionable = true;
        }

        //dampen knockback
        float knockbackamount = knockback.magnitude;
        knockbackamount -= ((knockbackamount - (knockbackamount * 0.95f)) * 200f) * Time.deltaTime;
        knockbackamount = Mathf.Clamp(knockbackamount, 0, 999);
        knockback = knockback.normalized * knockbackamount;

        //decide what movement to use
        if (stun > 0)
        {
            StunMovement();
        }
        else if (attacking)
        {
            AttackMovement();
        }
        else if (rolling)
        {
            RollMovement();
        }
        else if (airattacking)
        {
            AirAttackMovement();
        }
        else if (casting)
        {
            CastingMovement();
        }
        else
        {
            DefaultMovement();
        }

        if (controller.isGrounded)
        {
            extrajump = true;
        }

        //update stun timer
        if (stun > 0)
        {
            stun -= Time.deltaTime;
        }
        if (stun < 0)
        {
            stun = 0;
        }

    }

    private void StunMovement()
    {
        //Push out of enemy
        Vector3 pushout = Vector3.zero;

        RaycastHit[] hits;
        Vector3 toppoint = new Vector3(transform.position.x, transform.position.y + controller.radius - controller.height / 2f, transform.position.z);
        Vector3 botpoint = new Vector3(transform.position.x, transform.position.y - controller.radius + controller.height / 2f, transform.position.z);
        hits = (Physics.CapsuleCastAll(toppoint, botpoint, controller.radius, transform.forward, 0));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.CompareTag("Enemy"))
            {
                Vector3 dirtoout = transform.position - hits[i].transform.position;
                dirtoout = new Vector3(dirtoout.x, 0f, dirtoout.z);
                if (dirtoout.magnitude < .01)
                {
                    dirtoout = transform.forward;
                }

                Vector3 outpoint = hits[i].collider.ClosestPointOnBounds(hits[i].transform.position + (dirtoout * 9999));

                float disttopush = (Vector3.Distance(transform.position, outpoint) + controller.radius) * 4f;


                pushout += dirtoout.normalized * disttopush;
            }
        }

        Vector3 vel = new Vector3(0f, vspeed, 0f);

        controller.Move((vel + pushout + knockback) * Time.deltaTime);
    }

    private void DefaultMovement()
    {
        if (direction.magnitude >= 0.1f)
        {
            targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            anim.SetBool("IsMoving", true);
        }
        else
        {
            anim.SetBool("IsMoving", false);
        }

        //update direction the player is facing
        angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, turnSmoothing);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector4 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        //vertical movement
        if (controller.isGrounded)
        {
            vspeed = -2f;
            anim.SetBool("Onground", true);
        }
        else
        {
            vspeed -= grav * Time.deltaTime;

            anim.SetBool("Onground", false);
        }
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && vspeed > 0.1)
        {
            vspeed = 0;
        }


        Vector3 vel = moveDirection.normalized * speed;

        if (move.magnitude == 0)
        {
            vel = Vector3.zero;
        }

        anim.SetFloat("VSpeed", vspeed);
        anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), vel.normalized.magnitude, ref animSmoothVel, animSmoothing));

        vel = new Vector3(vel.x, vspeed, vel.z);


        //Push out of enemy
        Vector3 pushout = Vector3.zero;

        RaycastHit[] hits;
        Vector3 toppoint = new Vector3(transform.position.x, transform.position.y + controller.radius - controller.height / 2f, transform.position.z);
        Vector3 botpoint = new Vector3(transform.position.x, transform.position.y - controller.radius + controller.height / 2f, transform.position.z);
        hits = (Physics.CapsuleCastAll(toppoint, botpoint, controller.radius, transform.forward, 0));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.CompareTag("Enemy"))
            {
                Vector3 dirtoout = transform.position - hits[i].transform.position;
                dirtoout = new Vector3(dirtoout.x, 0f, dirtoout.z);
                if (dirtoout.magnitude < .01)
                {
                    dirtoout = transform.forward;
                }

                Vector3 outpoint = hits[i].collider.ClosestPointOnBounds(hits[i].transform.position + (dirtoout * 9999));

                float disttopush = (Vector3.Distance(transform.position, outpoint) + controller.radius) * 4f;


                pushout += dirtoout.normalized * disttopush;
            }
        }

        controller.Move((vel + pushout + knockback) * Time.deltaTime);



    }

    private void AirAttackMovement()
    {
        //update direction the player is facing
        angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, turnSmoothing);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector4 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        //vertical movement
        if (controller.isGrounded)
        {
            vspeed = -1f;
            anim.SetBool("Onground", true);
        }
        else
        {
            vspeed -= grav * Time.deltaTime;

            anim.SetBool("Onground", false);
        }
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && vspeed > 0.1)
        {
            vspeed = 0;
        }


        Vector3 vel = moveDirection.normalized * (speed * .5f);

        if (move.magnitude == 0)
        {
            vel = Vector3.zero;
        }

        anim.SetFloat("VSpeed", vspeed);
        anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), vel.normalized.magnitude, ref animSmoothVel, animSmoothing));

        vel = new Vector3(vel.x, vspeed, vel.z);

        //Push out of enemy
        Vector3 pushout = Vector3.zero;

        RaycastHit[] hits;
        Vector3 toppoint = new Vector3(transform.position.x, transform.position.y + controller.radius - controller.height / 2f, transform.position.z);
        Vector3 botpoint = new Vector3(transform.position.x, transform.position.y - controller.radius + controller.height / 2f, transform.position.z);
        hits = (Physics.CapsuleCastAll(toppoint, botpoint, controller.radius, transform.forward, 0));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.CompareTag("Enemy"))
            {
                Vector3 dirtoout = transform.position - hits[i].transform.position;
                dirtoout = new Vector3(dirtoout.x, 0f, dirtoout.z);
                if (dirtoout.magnitude < .01)
                {
                    dirtoout = transform.forward;
                }

                Vector3 outpoint = hits[i].collider.ClosestPointOnBounds(hits[i].transform.position + (dirtoout * 9999));

                float disttopush = (Vector3.Distance(transform.position, outpoint) + controller.radius) * 4f;


                pushout += dirtoout.normalized * disttopush;
            }
        }

        controller.Move((vel + pushout + knockback) * Time.deltaTime);
    }

    private void AttackMovement()
    {
        //vertical movement
        if (controller.isGrounded)
        {
            vspeed = -1f;
            anim.SetBool("Onground", true);
        }
        else
        {
            vspeed -= grav * Time.deltaTime;

            anim.SetBool("Onground", false);
        }
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && vspeed > 0.1)
        {
            vspeed = 0;
        }

        //update direction the player is facing
        angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, turnSmoothing);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector4 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        Vector3 vel = moveDirection.normalized * tempspeed;

        vel = new Vector3(vel.x, vspeed, vel.z);
        anim.SetFloat("VSpeed", vspeed);
        anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), vel.normalized.magnitude, ref animSmoothVel, animSmoothing));

        //Push out of enemy
        Vector3 pushout = Vector3.zero;

        RaycastHit[] hits;
        Vector3 toppoint = new Vector3(transform.position.x, transform.position.y + controller.radius - controller.height / 2f, transform.position.z);
        Vector3 botpoint = new Vector3(transform.position.x, transform.position.y - controller.radius + controller.height / 2f, transform.position.z);
        hits = (Physics.CapsuleCastAll(toppoint, botpoint, controller.radius, transform.forward, 0));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.CompareTag("Enemy"))
            {
                Vector3 dirtoout = transform.position - hits[i].transform.position;
                dirtoout = new Vector3(dirtoout.x, 0f, dirtoout.z);
                if (dirtoout.magnitude < .01)
                {
                    dirtoout = transform.forward;
                }

                Vector3 outpoint = hits[i].collider.ClosestPointOnBounds(hits[i].transform.position + (dirtoout * 9999));

                float disttopush = (Vector3.Distance(transform.position, outpoint) + controller.radius) * 4f;


                pushout += dirtoout.normalized * disttopush;
            }
        }

        controller.Move((vel + pushout + knockback) * Time.deltaTime);

        tempspeed -= ((tempspeed - (tempspeed * 0.99f)) * 400f) * Time.deltaTime;
        tempspeed = Mathf.Clamp(tempspeed, 0, 999);
    }

    private void RollMovement()
    {
        //vertical movement
        if (controller.isGrounded)
        {
            vspeed = -1f;
            anim.SetBool("Onground", true);
        }
        else
        {
            vspeed -= (grav * .5f) * Time.deltaTime;

            anim.SetBool("Onground", false);
        }
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && vspeed > 0.1)
        {
            vspeed = 0;
        }

        //update direction the player is facing
        angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, turnSmoothing);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector4 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        Vector3 vel = moveDirection.normalized * tempspeed;

        vel = new Vector3(vel.x, vspeed * .5f, vel.z);
        anim.SetFloat("VSpeed", vspeed);
        anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), vel.normalized.magnitude, ref animSmoothVel, animSmoothing));

        //Push out of enemy
        Vector3 pushout = Vector3.zero;

        RaycastHit[] hits;
        Vector3 toppoint = new Vector3(transform.position.x, transform.position.y + controller.radius - controller.height / 2f, transform.position.z);
        Vector3 botpoint = new Vector3(transform.position.x, transform.position.y - controller.radius + controller.height / 2f, transform.position.z);
        hits = (Physics.CapsuleCastAll(toppoint, botpoint, controller.radius, transform.forward, 0));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.CompareTag("Enemy"))
            {
                Vector3 dirtoout = transform.position - hits[i].transform.position;
                dirtoout = new Vector3(dirtoout.x, 0f, dirtoout.z);
                if (dirtoout.magnitude < .01)
                {
                    dirtoout = transform.forward;
                }

                Vector3 outpoint = hits[i].collider.ClosestPointOnBounds(hits[i].transform.position + (dirtoout * 9999));

                float disttopush = (Vector3.Distance(transform.position, outpoint) + controller.radius) * 4f;


                pushout += dirtoout.normalized * disttopush;
            }
        }

        controller.Move((vel + pushout + knockback) * Time.deltaTime);

        tempspeed -= ((tempspeed - (tempspeed * 0.9925f)) * 400f) * Time.deltaTime;
        tempspeed = Mathf.Clamp(tempspeed, 0, 999);
    }

    private void CastingMovement()
    {
        //vertical movement
        if (controller.isGrounded)
        {
            vspeed = -1f;
            anim.SetBool("Onground", true);
        }
        else
        {
            vspeed -= (grav * .5f) * Time.deltaTime;

            anim.SetBool("Onground", false);



        }
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && vspeed > 0.1)
        {
            vspeed = 0;
        }

        //update direction the player is facing
        angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, turnSmoothing);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector4 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        Vector3 vel = moveDirection.normalized * tempspeed;

        vel = new Vector3(vel.x, vspeed * .5f, vel.z);
        anim.SetFloat("VSpeed", vspeed);
        anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), vel.normalized.magnitude, ref animSmoothVel, animSmoothing));

        //Push out of enemy
        Vector3 pushout = Vector3.zero;

        RaycastHit[] hits;
        Vector3 toppoint = new Vector3(transform.position.x, transform.position.y + controller.radius - controller.height / 2f, transform.position.z);
        Vector3 botpoint = new Vector3(transform.position.x, transform.position.y - controller.radius + controller.height / 2f, transform.position.z);
        hits = (Physics.CapsuleCastAll(toppoint, botpoint, controller.radius, transform.forward, 0));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.CompareTag("Enemy"))
            {
                Vector3 dirtoout = transform.position - hits[i].transform.position;
                dirtoout = new Vector3(dirtoout.x, 0f, dirtoout.z);
                if (dirtoout.magnitude < .01)
                {
                    dirtoout = transform.forward;
                }

                Vector3 outpoint = hits[i].collider.ClosestPointOnBounds(hits[i].transform.position + (dirtoout * 9999));

                float disttopush = (Vector3.Distance(transform.position, outpoint) + controller.radius) * 4f;


                pushout += dirtoout.normalized * disttopush;
            }
        }

        controller.Move((vel + pushout + knockback) * Time.deltaTime);

        tempspeed -= ((tempspeed - (tempspeed * 0.97f)) * 400f) * Time.deltaTime;
        tempspeed = Mathf.Clamp(tempspeed, 0, 999);
    }


    //control functions
    private void Jump()
    {

        if ((controller.isGrounded || extrajump) && !rolling & !attacking && stun == 0)
        {
            if (!controller.isGrounded)
            {
                extrajump = false;
            }
            vspeed = jumpspeed;
            controller.Move(new Vector3(0f, 0f, 0f));
            FMODUnity.RuntimeManager.PlayOneShot(JumpEvent, transform.position);
        }
    }

    private void Roll()
    {
        if (actionable && stun == 0)
        {
            BeginRoll();
            FMODUnity.RuntimeManager.PlayOneShot(RollEvent, transform.position);
        }
        else if (queueable && stun == 0)
        {
            queuedaction = 2;
        }
    }

    private void Attack()
    {
        if (actionable && stun == 0)
        {
            BeginAttack();
        }
        else if (queueable && anim.GetInteger("AttackNum") != 4 && stun == 0)
        {
            queuedaction = 1;
        }
    }

    private void Cast()
    {
        if (actionable && stun == 0)
        {
            BeginCast();
        }
        else if (queueable && stun == 0)
        {
            queuedaction = 3;
        }
    }

    private void BeginAttack()
    {
        ResetActionTriggers();
        anim.SetTrigger("Attack");
        DestroyHitBox();
    }

    private void BeginRoll()
    {
        ResetActionTriggers();
        anim.SetTrigger("Roll");
        DestroyHitBox();
    }

    private void BeginCast()
    {
        ResetActionTriggers();
        anim.SetInteger("SpellAnimType", spellManager.currentspell.animtype);
        anim.SetTrigger("Cast");
        DestroyHitBox();
    }

    private void SwapSpellNext()
    {
        spellManager.SwapSpellNext();
    }

    private void SwapSpellPrev()
    {
        spellManager.SwapSpellPrev();
    }

    public void ResetActionTriggers()
    {
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("Roll");
        anim.ResetTrigger("Cast");
        //anim.ResetTrigger("AttackEnd");
        //anim.ResetTrigger("CastEnd");
    }


    //animation events

    //ROLL
    public void RollStart()
    {
        tempspeed = rollspeed;
        vspeed = 0f;
        if (move.magnitude > 0)
        {
            targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
        }

        ResetActionVariables();
        rolling = true;
    }

    public void RollEnd()
    {
        rolling = false;
        actionable = true;
        queueable = false;
    }

    //NORMAL ATTACKS
    public void AttackStart(int num)
    {
        anim.SetInteger("AttackNum", num);
        ResetActionVariables();
        attacking = true;
    }


    public void AttackEnd()
    {
        attacking = false;
        queueable = false;
        anim.SetTrigger("AttackEnd");
    }

    public void AttackPushForward()
    {

        tempspeed = attackmovespeed;


        if (move.magnitude > 0)
        {
            targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
        }


    }

    public void ResetActionVariables()
    {
        rolling = false;
        attacking = false;
        actionable = false;
        queueable = false;
        airattacking = false;
        plungeattacking = false;
        casting = false;
    }

    //AIR ATTACKS
    public void AirAttackStart()
    {
        ResetActionVariables();
        airattacking = true;
    }

    public void AirAttackEnd()
    {
        airattacking = false;
        queueable = false;
        anim.SetTrigger("AttackEnd");
    }

    public void AirAttackPushUp(int attacknum)
    {
        if (vspeed < jumpspeed * .2)
        {
            vspeed += airattackpushamount;
        }


        if (move.magnitude > 0)
        {
            targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
        }
    }

    //PLUNGE ATTACK
    public void PlungeAttackStart()
    {
        ResetActionVariables();
        airattacking = true;
    }

    //CASTING
    public void CastStart()
    {
        ResetActionVariables();
        casting = true;
        tempspeed = move.normalized.magnitude * speed;
    }

    public void CastPushForward()
    {
        tempspeed = castmovespeed;
    }

    public void CastPushDownward()
    {
        vspeed = -groundcastfallspeed;
    }

    public void CastGroundExtend()
    {
        if (!controller.isGrounded)
        {
            anim.Play(0, 0, 0.48f);
        }
    }

    public void CastCreateProj()
    {
        if (anim.GetInteger("SpellAnimType") != 1 || (anim.GetInteger("SpellAnimType") == 1 && controller.isGrounded))
        {
            GameObject proj = Instantiate(spellManager.currentspell.prefab, new Vector3(transform.position.x, transform.position.y + .5f, transform.position.z), transform.rotation);
            proj.GetComponent<HitBox>().creator = unit;
        }
    }

    public void CastEnd()
    {
        casting = false;
        queueable = false;
        anim.SetTrigger("CastEnd");
    }


    //GENERAL
    public void BecomeActionable()
    {
        actionable = true;
        queueable = false;
    }

    public void BecomeQueueable()
    {
        queueable = true;
    }


    public void CreateHitBox(int damage)
    {
        currentmeleehitbox = Instantiate(prefabHitBox, transform.position, transform.rotation, transform);
        currentmeleehitbox.GetComponent<HitBox>().creator = unit;
        currentmeleehitbox.GetComponent<HitBox>().knockbackAmount = 20f;
        currentmeleehitbox.GetComponent<HitBox>().knockbackdir = transform.forward;
        currentmeleehitbox.GetComponent<HitBox>().damage = damage;
        MakeSlashSound();
    }
    public void DestroyHitBox()
    {
        if (currentmeleehitbox != null)
        {
            Destroy(currentmeleehitbox);
        }
    }

    public void MakeStepSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(FootStepEvent, transform.position);
    }

    public void MakeSlashSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(SlashEvent, transform.position);
    }

    public void MakeFireSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(FireEvent, transform.position);
    }

    public void MakeEarthquakeSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(EarthquakeEvent, transform.position);
    }
}
