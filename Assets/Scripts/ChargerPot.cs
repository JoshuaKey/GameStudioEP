﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargerPot : Pot
{
    [SerializeField] public float aggroRadius = 5;

    private void Start()
    {
        stateMachine = new StateMachine();
        stateMachine.Init(gameObject, 
            new Charger_Idle(), 
            new Charger_Charge());
    }

    private void Update()
    {
        stateMachine.Update();
        if (agent.desiredVelocity.magnitude > 0)
        {
            Hop();
        }
    }
}

public class Charger_Idle : State
{
    GameObject player;

    public override void Enter()
    {
        if(player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    public override void Exit()
    { }

    public override string Update()
    {
        if(Vector3.Distance(owner.transform.position, player.transform.position) < owner.GetComponent<ChargerPot>().aggroRadius)
        {
            return "Charger_Charge";
        }

        return null;
    }
}

public class Charger_Charge : State
{
    GameObject player;

    public override void Enter()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    public override void Exit()
    { }

    public override string Update()
    {
        if (Vector3.Distance(owner.transform.position, player.transform.position) > owner.GetComponent<ChargerPot>().aggroRadius)
        {
            return "Charger_Idle";
        }

        agent.SetDestination(player.transform.position);

        return null;
    }
}

