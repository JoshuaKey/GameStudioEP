﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetProjectile : MonoBehaviour {

    //public float TargetSpeed = 5;
    //public float MaxSpeed = 10;
    public bool InstantFire = false;

    [Header("Targets")]
    public float PeakHeight = 10;
    public float SpeedOverTime = .2f;

    [Header("Components")]
    public Attack attack;
    public new Rigidbody rigidbody;
    public new Collider collider;

    public Action<TargetProjectile> OnFire;
    public Action<TargetProjectile, GameObject> OnHit;
    public Action<TargetProjectile> OnMiss;

    private bool hasFired;
    private GameObject sender;
    private GameObject target;

    // Start is called before the first frame update
    void Start() {
        if (collider == null) { collider = GetComponentInChildren<Collider>(true); }
        if (rigidbody == null) { rigidbody = GetComponentInChildren<Rigidbody>(true); }
        if (attack == null) { attack = GetComponentInChildren<Attack>(true); }

        if (!InstantFire) {
            collider.enabled = false;
            rigidbody.isKinematic = true;
            attack.isAttacking = false;
        }
    }

    private void OnDisable() {
        OnMiss?.Invoke(this);
        hasFired = false;
        sender = null;
        target = null;
    }

    public void Fire(GameObject _sender, GameObject _target, Vector3 impulse) {
        sender = _sender;
        target = _target;
        hasFired = true;

        //if(GameObject.ReferenceEquals(sender, Player.Instance.gameObject)) {
        //    this.gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
        //} else {
        //    this.gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
        //}

        collider.enabled = true;
        rigidbody.isKinematic = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        attack.isAttacking = true;

        this.transform.forward = impulse.normalized;

        StopAllCoroutines();
        if(SpeedOverTime > 0) {
            StartCoroutine(FireAnimation());
        } else {
            rigidbody.AddForce(impulse, ForceMode.Impulse);
        }   

        this.transform.parent = null;
        LevelManager.Instance.MoveToScene(this.gameObject);

        OnFire?.Invoke(this);
    }

    public void Hit(GameObject _sender, Vector3 impulse) {
        if(GameObject.ReferenceEquals(_sender, sender)) { return; }
        target = sender;
        sender = _sender;
        //print("Hit by " + _sender.name);

        this.transform.forward = impulse.normalized;

        //if (GameObject.ReferenceEquals(sender, Player.Instance.gameObject)) {
        //    this.gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
        //} else {
        //    this.gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
        //}

        StopAllCoroutines();
        if (SpeedOverTime > 0) {
            StartCoroutine(FireAnimation());
        } else {
            rigidbody.velocity = Vector3.zero;
            rigidbody.AddForce(impulse, ForceMode.Impulse);
        }

        OnFire?.Invoke(this);
    }

    private IEnumerator FireAnimation() {
        if (target == null) {
            yield break;
        }

        Vector3 startPos = this.transform.position;
        Vector3 peak = Utility.CreatePeak(startPos, target.transform.position, PeakHeight);

        float startTime = Time.time;

        float length = (target.transform.position - startPos).magnitude * SpeedOverTime;
        while (Time.time < startTime + length) {
            float t = (Time.time - startTime) / length;

            if (target != null) {
                Vector3 pos = Interpolation.BezierCurve(startPos, peak, target.transform.position, t);
                this.transform.position = pos;
            } else {
                Dissipate();
                yield break;
            }

            yield return null;
        }

        Dissipate();

        if (target != null) {
            this.transform.position = target.transform.position;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if(GameObject.ReferenceEquals(other.gameObject, target)) {
            OnHit?.Invoke(this, other.gameObject);
        } else if (other.CompareTag("TargetBlock")) {
            OnMiss?.Invoke(this);
        }
    }

    private void Dissipate() {
        StopAllCoroutines();

        Destroy(this.gameObject);
    }

    public GameObject GetTarget() { return target; }
    public GameObject GetSender() { return sender; }
    public bool HasFired() { return hasFired; }
}
