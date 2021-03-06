﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : MonoBehaviour {

    [Header("Damage")]
    public float Damage;
    public DamageType Type;

    [Header("Components")]
    public Collider Trigger;
    public Collider DamageCollider;

    [Header("Visuals")]
    public GameObject TrapModel;
    public Transform WaitPos;
    public Transform SpringPos;
    public float WaitTime;
    public float ReloadTime;
    public float SpringTime;
    public float RecoilTime;

    private List<GameObject> enemiesHit = new List<GameObject>();
    private SoundClip gears;

    // Start is called before the first frame update
    void Start() {
        Trigger.enabled = true;
        DamageCollider.enabled = false;
    }

    private IEnumerator Activate() {
        Trigger.enabled = false;
        DamageCollider.enabled = false;

        float startTime = Time.time;
        while (Time.time < startTime + WaitTime) {
            //print("Waiting");
            yield return null;
        }

        startTime = Time.time;
        AudioManager.Instance.PlaySoundWithParent("spikes", ESoundChannel.SFX, gameObject);
        while (Time.time < startTime + SpringTime) {
            //print("Springing");

            float t = (Time.time - startTime) / SpringTime;
            t = Interpolation.ExpoIn(t);

            DamageCollider.enabled = t > .1f;

            TrapModel.transform.position = Vector3.Lerp(WaitPos.position, SpringPos.position, t);
            yield return null;
        }
        TrapModel.transform.position = SpringPos.position;

        startTime = Time.time;
        gears = AudioManager.Instance.PlaySoundWithParent("gears", ESoundChannel.SFX, gameObject, true);
        while (Time.time < startTime + RecoilTime) {
            //print("Recoiling");

            float t = (Time.time - startTime) / RecoilTime;

            TrapModel.transform.position = Vector3.Lerp(SpringPos.position, WaitPos.position, t);
            yield return null;
        }
        TrapModel.transform.position = WaitPos.position;

        DamageCollider.enabled = false;

        startTime = Time.time;
        while (Time.time < startTime + ReloadTime) {
            //print("Waiting");
            yield return null;
        }
        gears.Stop();

        Trigger.enabled = true;
        enemiesHit.Clear();
    }

    private void OnTriggerEnter(Collider other) {
        // Activate Trap
        if (Trigger.enabled) {
            Health health = other.GetComponentInChildren<Health>(true);
            if (health == null) { health = other.GetComponentInParent<Health>(); }

            if (health != null) {
                StartCoroutine(Activate());
            }      
        }
        // Deal Damage
        else if(!enemiesHit.Contains(other.gameObject)) {
            Health health = other.GetComponentInChildren<Health>(true);
            if (health == null) { health = other.GetComponentInParent<Health>(); }

            if (health != null) {
                enemiesHit.Add(other.gameObject);
                health.TakeDamage(Type, Damage);
            }
        }
    }
}
