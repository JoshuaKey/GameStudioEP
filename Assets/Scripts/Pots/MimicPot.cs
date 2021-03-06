﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MimicPot : Pot
{


    [SerializeField] public float aggroRadius = 5f; // this is when the pot first wakes up
    [SerializeField] public float chaseRadius = 10.0f; // this is after the pot is awake
    //This is all atack stuff and i currently dont wanna change it
    [SerializeField] public float attackRadius = 2.5f;
    [SerializeField] public float attackDuration = 0.25f;

    void Start()
    {
        stateMachine = new StateMachine();
        stateMachine.Init(gameObject,
            new Mimic_Idle(),
            new Mimic_Charge(),
            new Mimic_Attack(attackDuration));
    }

    public override void Animate()
    {
        // I have to come up with a cute way to do this
        Hop();
        //but for now i Hop
    }

    //public override void SetMaterial(Material m) {
    //    renderer.material = m;
    //}

}



public class Mimic_Idle : State
{
    MimicPot mimicPot;
    Player player;
    Health hp;
    bool isDamaged;

    public override void Init(GameObject owner)
    {
        base.Init(owner);
        //player = GameObject.FindGameObjectWithTag("Player");
        mimicPot = owner.GetComponent<MimicPot>();
        player = Player.Instance;
        hp = owner.GetComponent<Health>();
    }

    public override void Enter()
    {
        isDamaged = false;
        hp.OnDamage += OnDamage;
    }

    public override void Exit()
    {
        hp.OnDamage -= OnDamage;
    }

    public override string Update()
    {
        //if (owner.GetComponent<MimicPot>() == null || player == null)
        //{
        //    return null;
        //}
        //Debug.Log(owner.GetComponent<MimicPot>());
        //Debug.Log(player);

        Vector3 towardPlayer = Player.Instance.transform.position - owner.transform.position;
        //this now checks if the pot can wake up or not
        RaycastHit hit;
        if ((Physics.Raycast(owner.transform.position, towardPlayer, out hit, mimicPot.aggroRadius, ~LayerMask.GetMask("Enemy", "Trigger"))
            && hit.collider.tag == "Player")
            || isDamaged)
        {
            return "Mimic_Charge";
        }

        //This will check if it can wake up or not
        //if (Vector3.Distance(owner.transform.position, player.transform.position) <mimicPot.aggroRadius)
        //{
        //    return "Mimic_Charge";
        //}

        //this is code isnt needed anymore

        //This will check if the player is in the chase radius and has hit the pot 
        //(so if they attack it from range it will charge them)
        //if (Vector3.Distance(owner.transform.position, player.transform.position) < mimicPot.chaseRadius
        //    //this is awful and i hate it
        //    && hp.CurrentHealth != hp.MaxHealth)
        //{
        //    return "Mimic_Charge";
        //}

        return null;
    }

    public void OnDamage(float damage)
    {
        isDamaged = true;
    }
}

public class Mimic_Charge : State
{
    Player player;

    public override void Init(GameObject owner)
    {
        base.Init(owner);
        //player = GameObject.FindGameObjectWithTag("Player");
        player = Player.Instance;
    }

    public override void Enter()
    {
    }

    public override void Exit()
    {// this might actually be useful because we can tell it to like close its mouth
    }

    public override string Update()
    {
        float distance = Vector3.Distance(owner.transform.position, player.transform.position);
        MimicPot mp = owner.GetComponent<MimicPot>();

        if (distance > mp.chaseRadius)
        {
            return "Mimic_Idle";
        }

        if (distance < mp.attackRadius)
        {
            return "PUSH.Mimic_Attack";
        }

        if (agent.isActiveAndEnabled && !mp.stunned)
        {
            agent.SetDestination(player.transform.position);
        }

        return null;
    }
}

public class Mimic_Attack : TimedState
{
    GameObject player;
    Animator animator;
    Attack attack;
    MimicPot mp;

    public Mimic_Attack(float seconds)
        :base(seconds)
    { }

    public override void Init(GameObject owner)
    {
        base.Init(owner);
        animator = owner.GetComponentInChildren<Animator>();
        attack = owner.GetComponentInChildren<Attack>();
        mp = owner.GetComponent<MimicPot>();
    }

    public override void Enter()
    {
        base.Enter();

        animator.SetTrigger("Attack");
        attack.isAttacking = true;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    public override void Exit()
    {
        attack.isAttacking = false;
        attack.hasHitPlayer = false;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    public override string Update()
    {
        base.Update();

        //turn pot towards player
        Vector3 newForward = Player.Instance.transform.position - owner.transform.position;
        owner.transform.forward = newForward;

        if (timer >= seconds || mp.stunned)
        {
            return "POP";
        }

        return null;
    }
}
