﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrokenPot : MonoBehaviour {

    private Rigidbody[] pieces;

    void Start() {
        pieces = GetComponentsInChildren<Rigidbody>();

        this.gameObject.SetActive(false);
    }

    public void Explode(Vector3 force, Vector3 pos) {
        foreach(Rigidbody rb in pieces) {
            rb.AddExplosionForce(force.magnitude, pos, 10.0f, 1.0f, ForceMode.Impulse);
        }
    }
}
