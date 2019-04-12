﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : Weapon {

    private bool isSwinging = false;
    private List<GameObject> enemiesHit = new List<GameObject>();

    private void Start() {
        CanCharge = false;
        Type = DamageType.BASIC;
    }
    public override void Attack() {
        if (!CanAttack()) { return;  }

        base.Attack();
        StartCoroutine(Swing());
    }

    private IEnumerator Swing() {
        collider.enabled = true;

        Vector3 startPos = new Vector3(0.5f, 0.5f, 0.777f);
        Vector3 endPos = new Vector3(-0.2f, 0.2f, 0.777f);
        Quaternion startRot = Quaternion.Euler(new Vector3(0, 45, -10));
        Quaternion endRot = Quaternion.Euler(new Vector3(10, 45, 120));

        // Swing Blade Down
        float startTime = Time.time;
        float length = AttackSpeed * 0.4f; // (40%)
        while (Time.time < startTime + length) {
            float t = (Time.time - startTime) / length;

            Vector3 pos = this.transform.localPosition;
            Quaternion rot = this.transform.localRotation;

            pos.x = Mathf.Lerp(startPos.x, endPos.x, t);
            pos.y = Mathf.Lerp(startPos.y, endPos.y, t);
            pos.z = Mathf.Lerp(startPos.z, endPos.z, t);

            rot.x = Mathf.Lerp(startRot.x, endRot.x, t);
            rot.y = Mathf.Lerp(startRot.y, endRot.y, t);
            rot.z = Mathf.Lerp(startRot.z, endRot.z, t);
            rot.w = Mathf.Lerp(startRot.w, endRot.w, t);

            this.transform.localPosition = pos;
            this.transform.localRotation = rot;
            yield return null;
        }
        this.transform.localPosition = endPos;
        this.transform.localRotation = endRot;

        // Move to Start Pos
        startTime = Time.time;
        length = AttackSpeed * 0.6f; // (60%)
        while (Time.time < startTime + length) {
            float t = (Time.time - startTime) / length;

            Vector3 pos = this.transform.localPosition;
            Quaternion rot = this.transform.localRotation;

            pos.x = Mathf.Lerp(endPos.x, startPos.x, t);
            pos.y = Mathf.Lerp(endPos.y, startPos.y, t);
            pos.z = Mathf.Lerp(endPos.z, startPos.z, t);

            rot.x = Mathf.Lerp(endRot.x, startRot.x, t);
            rot.y = Mathf.Lerp(endRot.y, startRot.y, t);
            rot.z = Mathf.Lerp(endRot.z, startRot.z, t);
            rot.w = Mathf.Lerp(endRot.w, startRot.w, t);

            this.transform.localPosition = pos;
            this.transform.localRotation = rot;
            yield return null;
        }
        this.transform.localPosition = startPos;
        this.transform.localRotation = startRot;

        // No longer Attacking
        collider.enabled = false;
        enemiesHit.Clear();
    }

    protected void OnTriggerEnter(Collider other) {
        if (!enemiesHit.Contains(other.gameObject)) {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) {
                print("Dealing Damage to " + other.name);

                enemiesHit.Add(other.gameObject);

                OnEnemyHit?.Invoke(enemy);
            }
        }
    }
}
