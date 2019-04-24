﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour {

    // A cannon can be interacted with
    // When Interacted, the Player will align with the Barrel
    // Then the player will shoot, in a specific direction and velocit

    [Header("Launch")]
    //[HideInInspector]
    //public float BaseAngle;
    //[HideInInspector]
    //public float BarrelAngle;
    public Transform Target;
    public Transform Peak; // Aka , how high will we get?
    public Transform BarrelChargePos; // z = -.5
    public bool AutoAlign = false;

    [Header("Damage")]
    public float Damage;
    public float RadiusSize;
    public LayerMask EnemyLayer;

    [Header("Time")]
    public float ChargeTime = 2.5f;
    public float LeapTime = 10.0f;

    [Header("Object")]
    public GameObject Barrel;
    public GameObject Base;

    private Interactable interactable;

    private WaitForSeconds chargedWait;
    private WaitForSeconds leapWait;
    private bool aligning = false;

    // Start is called before the first frame update
    void Start() {
        interactable = GetComponent<Interactable>();
        if (interactable == null) { interactable = GetComponentInChildren<Interactable>(); }

        interactable.Subscribe(FirePlayer);

        //BarrelAngle = Barrel.transform.root.localEulerAngles.x;
        //BaseAngle = Base.transform.root.localEulerAngles.y;
        if (AutoAlign) {
            Align(Target.position, Peak.position);
        }  
    }

    public void Rotate(Transform target, Transform peak) => Rotate(target, peak, ChargeTime, LeapTime);
    public void Rotate(Transform target, Transform peak, float chargeTime, float leapTime) {
        ChargeTime = chargeTime == 0.0f ? ChargeTime : chargeTime;
        LeapTime = leapTime == 0.0f ? LeapTime : leapTime;

        Vector3 targetPos = target == null ? Target.position : target.position;
        Vector3 peakPos = peak == null ? Peak.position : peak.position;

        StartCoroutine(Rotate(targetPos, peakPos));

        Target = target == null ? Target : target;
        Peak = peak == null ? Peak : peak;

        print("Here");
    }

    private IEnumerator Rotate(Vector3 newTarget, Vector3 newPeak) {
        aligning = true;
        Vector3 oldTarget = Target.position;
        Vector3 oldPeak = Peak.position;

        print("Here2");

        Vector3 oldBaseDir = oldTarget - Base.transform.position;
        oldBaseDir.y = 0.0f;
        oldBaseDir = oldBaseDir.normalized;

        Vector3 newBaseDir = newTarget - Base.transform.position;
        newBaseDir.y = 0.0f;
        newBaseDir = newBaseDir.normalized;
        
        // Base Rotation
        {
            float startTime = Time.time;
            float length = 0.5f;
            while (Time.time < startTime + length) {
                float t = (Time.time - startTime) / length;

                Vector3 baseDir = Vector3.Lerp(oldBaseDir, newBaseDir, t);

                Base.transform.forward = baseDir;
                print("Here4");
                yield return null;
            }
            Base.transform.forward = newBaseDir;
        }

        Vector3 oldBarrelDir = Barrel.transform.forward;

        Vector3 newBarrelDir = newPeak - Barrel.transform.position;
        newBarrelDir = newBarrelDir.normalized;

        // Barrel Rotation
        {
            float startTime = Time.time;
            float length = 0.5f;
            while (Time.time < startTime + length) {
                float t = (Time.time - startTime) / length;

                Vector3 barrelDir = Vector3.Lerp(oldBarrelDir, newBarrelDir, t);

                Barrel.transform.forward = barrelDir;
                print("Here5");
                yield return null;
            }
            Barrel.transform.forward = newBarrelDir;
        }

        aligning = false;

        print("Here3");

    }

    private void Align(Vector3 target, Vector3 peak) {
        Vector3 baseDir = target - Base.transform.position;
        baseDir.y = 0.0f;
        baseDir = baseDir.normalized;

        Vector3 barrelDir = peak - Barrel.transform.position;
        //barrelDir.x = 0.0f;
        barrelDir = barrelDir.normalized;

        Base.transform.forward = baseDir; // Angle on Y Axis
        Barrel.transform.forward = barrelDir; // Angle on X Axis
    }

    public void FirePlayer() {
        Player player = Player.Instance;
        StartCoroutine(Shoot(player));
    }

    public IEnumerator Shoot(Player player) {
        Transform oldParent = player.transform.parent;
        Quaternion oldRot = player.transform.localRotation;

        player.transform.position = Barrel.transform.position;
        player.transform.SetParent(Barrel.transform, true);
        player.transform.up = Barrel.transform.forward;
        player.LookTowards(Barrel.transform.forward);

        player.CanWalk = false;
        player.CanMove = false;
        player.CanRotate = false;

        while (aligning) {
            yield return null;
        }

        // Charge Animation
        {
            Vector3 startPos = Barrel.transform.position;
            Vector3 endPos = BarrelChargePos.position;
            float startTime = Time.time;
            while (Time.time < startTime + ChargeTime) {
                float t = (Time.time - startTime) / ChargeTime;

                t = Interpolation.BounceOut(t);

                Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                player.transform.position = pos;
                yield return null;
            }
        }

        player.LookTowards(Barrel.transform.forward);
        player.CanRotate = true;

        // Launch Animation
        {
            Vector3 startPos = BarrelChargePos.position;
            float startTime = Time.time;
            while (Time.time < startTime + LeapTime) {
                float t = (Time.time - startTime) / LeapTime;

                Vector3 pos = Utility.BezierCurve(startPos, Peak.position, Target.position, t);

                player.transform.position = pos;
                yield return null;
            }
            player.transform.position = Target.position;
        }

        Explosion(player.transform.position);

        player.CanWalk = true;
        player.CanMove = true;
        player.transform.SetParent(oldParent, true);
        player.transform.localRotation = oldRot;
    }

    [ContextMenu("Find Mid Point")]
    private void FindMidPoint() {
        Vector3 mid = Barrel.transform.position + .5f * (Target.position - Barrel.transform.position);
        Peak.position = mid;
        Peak.forward = (Target.position - Barrel.transform.position).normalized;
    }

    public void Explosion(Vector3 pos) {
        Collider[] colliders = Physics.OverlapSphere(pos, RadiusSize, EnemyLayer);
        foreach(Collider other in colliders) {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.health.TakeDamage(DamageType.EXPLOSIVE, this.Damage);
                //OnEnemyHit?.Invoke(enemy);
            }
        }
    }

}