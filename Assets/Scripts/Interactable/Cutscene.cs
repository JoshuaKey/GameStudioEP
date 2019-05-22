﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Cutscene : MonoBehaviour
{
    [SerializeField] Camera camera;

    private Camera mainCamera;

    private void Start()
    {
        Interactable interactble = GetComponent<Interactable>();
        if (interactble == null) throw new System.Exception("A Cutscene tried to find an Interactable but couldn't.");

        interactble.Subscribe(OnInteract);

        mainCamera = Player.Instance.GetComponentInChildren<Camera>();
    }

    public void OnInteract()
    {
        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        Debug.Log($"Main Camera: {mainCamera}\nCutscene Camera: {camera}");

        mainCamera.gameObject.SetActive(false);
        camera.gameObject.SetActive(true);

        NavMeshAgent[] navMeshAgents = EnemyManager.Instance.GetComponentsInChildren<NavMeshAgent>();

        foreach(NavMeshAgent agent in navMeshAgents)
        {
            agent.enabled = false;
        }

        yield return new WaitForSeconds(1f);

        mainCamera.gameObject.SetActive(true);
        camera.gameObject.SetActive(false);

        foreach(NavMeshAgent agent in navMeshAgents)
        {
            agent.enabled = true;
        }
    }
}
