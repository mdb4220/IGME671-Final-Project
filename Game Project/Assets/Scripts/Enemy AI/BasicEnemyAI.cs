using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BasicEnemyAI : MovementScript
{
    Transform player;
    Animator anim;
    Unit unit;
    CharacterController controller;
    public LayerMask groundLayer, playerLayer;

    //Movement Variables
    public float speed = 6f;
    public float grav = 30f;
    public float jumpspeed = 15;
    public float attackmovespeed = 5f;

    float tempspeed;
    float vspeed = 0;

    //Wander
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    //Attack
    public float timeBetweenAttacks;
    float attacktimer = 0f;
    bool attacking = false;

    //State
    public float avgidletime = 2f;
    float idletimer = 0f;
    public float sightRange, attackRange;
    bool playerInSight, playerInAttackRange;

    // rotation
    float targetAngle = 0;
    float turnSmoothVel;
    public float turnSmoothing = .1f;
    float angle = 0;

    //animation smoothing
    float animSmoothVel;
    public float animSmoothing = .4f;

    //attack stuff
    public GameObject prefabHitBox;
    GameObject currentmeleehitbox = null;


    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        anim = GetComponent<Animator>();
        unit = GetComponent<Unit>();
        controller = GetComponent<CharacterController>();
        idletimer = Random.Range(avgidletime * .2f, avgidletime * 1.8f);
    }

    private void Update()
    {
        //set stun in animator
        if (washit)
        {
            washit = false;
            anim.SetTrigger("WasHit");
        }

        //dampen knockback
        float knockbackamount = knockback.magnitude;
        knockbackamount -= ((knockbackamount - (knockbackamount * 0.95f)) * 200f) * Time.deltaTime;
        knockbackamount = Mathf.Clamp(knockbackamount, 0, 999);
        knockback = knockback.normalized * knockbackamount;

        //choose state type
        playerInSight = Physics.CheckSphere(transform.position, sightRange, playerLayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerLayer);

        if (stun > 0.1f)
        {
            Stun();
        }
        else
        {
            if (!playerInSight && !attacking)
            {
                if (idletimer <= 0f)
                {
                    Wander();
                }
                else
                {
                    Idle();
                }           
            }
            else if (!playerInAttackRange && !attacking)
            {
                Chase();
            }
            else
            {
                Attack();
            }
        }

        //update idle timer
        idletimer -= Time.deltaTime;

        //update attack timer
        attacktimer += Time.deltaTime;

        //update stun timer
        if (stun > 0)
        {
            stun -= Time.deltaTime;
        }
        if (stun < 0)
        {
            stun = 0;
        }
        anim.SetFloat("Stun", stun);
    }

    //states
    private void Stun()
    {
        if (currentmeleehitbox != null)
        {
            Destroy(currentmeleehitbox);
        }
        attacking = false;
        anim.ResetTrigger("Attack");
        Vector3 vel = new Vector3(0f, vspeed, 0f);

        controller.Move((vel + knockback) * Time.deltaTime);
    }

    private void Idle()
    {
        walkPointSet = false;
        DefaultMovement(transform.position);
    }

    private void Wander()
    {
        attacking = false;
        anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), 0.8f, ref animSmoothVel, animSmoothing));
        if (!walkPointSet)
        {
            SearchWalkPoint();
            DefaultMovement(transform.position);
        }
        else if (walkPointSet)
        {
            DefaultMovement(walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
            idletimer = Random.Range(avgidletime * .2f, avgidletime * 1.8f);
        }
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, groundLayer))
        {
            walkPointSet = true;
        }


    }

    private void Chase()
    {
        DefaultMovement(player.position);
        attacking = false;
    }

    private void Attack()
    {
        AttackMovement();

        //attack
        if (!attacking)
        {
            //face towards the player
            Vector3 direction = player.position - transform.position;
            targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            if(attacktimer > timeBetweenAttacks)
            {
                anim.SetTrigger("Attack");
                attacking = true;
            }          
        }
        
    }

    //Movement
    public void DefaultMovement(Vector3 targetpoint)
    {
        

        Vector3 direction = targetpoint - transform.position;

        //update direction facing
        if (direction.magnitude >= .2f)
        {
            targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, turnSmoothing);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

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

        if (direction.magnitude < .2f)
        {
            vel = Vector3.zero;
        }

        anim.SetFloat("VSpeed", vspeed);
        

        //move
        if (!Physics.Raycast(transform.position + (vel * 4 * Time.deltaTime), -transform.up, 2f, groundLayer))
        {
            vel = Vector3.zero;
            anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), vel.magnitude, ref animSmoothVel, animSmoothing));
        }
        else
        {
            anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), vel.magnitude, ref animSmoothVel, animSmoothing));
        }


        //Push out of enemy
        Vector3 pushout = Vector3.zero;
        RaycastHit[] hits;
        Vector3 toppoint = new Vector3(transform.position.x, transform.position.y + controller.radius - controller.height / 2f, transform.position.z);
        Vector3 botpoint = new Vector3(transform.position.x, transform.position.y - controller.radius + controller.height / 2f, transform.position.z);
        hits = (Physics.CapsuleCastAll(toppoint, botpoint, controller.radius, transform.forward, 0));
        Collider[] mycolliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < hits.Length; i++)
        {
            bool ismycollider = false;
            foreach(Collider c in mycolliders)
            {
                if (hits[i].collider == c)
                {
                    ismycollider = true;
                }
            }

            if (hits[i].transform.CompareTag("Enemy") && !ismycollider)
            {
                Vector3 dirtoout = transform.position - hits[i].transform.position;
                dirtoout = new Vector3(dirtoout.x, 0f, dirtoout.z);
                if (dirtoout.magnitude < .01)
                {
                    dirtoout = transform.forward;
                }

                Vector3 outpoint = hits[i].collider.ClosestPointOnBounds(hits[i].transform.position + (dirtoout * 9999));

                float disttopush = (Vector3.Distance(transform.position, outpoint) + controller.radius) * 0.8f;


                pushout += dirtoout.normalized * disttopush;
            }
        }

        vel = new Vector3(vel.x, vspeed, vel.z);

        controller.Move((vel + pushout + knockback) * Time.deltaTime);
    }

    public void AttackMovement()
    {
        //update direction
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


        Vector3 vel = moveDirection.normalized * tempspeed;

        anim.SetFloat("VSpeed", vspeed);
        anim.SetFloat("HSpeed", Mathf.SmoothDampAngle(anim.GetFloat("HSpeed"), 0f, ref animSmoothVel, animSmoothing));

        vel = new Vector3(vel.x, vspeed, vel.z);

        //move
        controller.Move((vel + knockback) * Time.deltaTime);

        tempspeed -= ((tempspeed - (tempspeed * 0.99f)) * 400f) * Time.deltaTime;
        tempspeed = Mathf.Clamp(tempspeed, 0, 999);
    }

    public void AttackEnd()
    {
        attacking = false;
        attacktimer = 0f;
        anim.SetFloat("HSpeed", 0f);
    }

    public void AttackPushForward()
    {
        tempspeed = attackmovespeed; 
    }

    public void CreateProjectile()
    {
        GameObject newProj = Instantiate(prefabHitBox, transform.position, transform.rotation);
        newProj.GetComponent<HitBox>().creator = unit;
    }

    public void CreateHitBox()
    {
        currentmeleehitbox = Instantiate(prefabHitBox, transform.position, transform.rotation, transform);
        currentmeleehitbox.GetComponent<HitBox>().creator = unit;
        currentmeleehitbox.GetComponent<HitBox>().knockbackAmount = 10f;
        currentmeleehitbox.GetComponent<HitBox>().knockbackdir = transform.forward;
    }
    public void DestroyHitBox()
    {
        if (currentmeleehitbox != null)
        {
            Destroy(currentmeleehitbox);
        }
    }
}
