﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    public float Knockback = 1;
    public float RigidbodyKnockback = 5;
    public LayerMask EnemyLayer;

    [Header("Player")]
    public float PlayerRotateSpeed = 30;

    [Header("Time")]
    public float ChargeTime = 2.5f;
    public float LeapTime = 10.0f;
    public float BarrelRotateTime = 0.5f;
    public float BaseRotateTime = 0.5f;

    [Header("Object")]
    public GameObject Barrel;
    public GameObject Base;
    public ParticleSystem LandingEffect;
    public new LineRenderer renderer;

    [Header("Destroy Object")]
    public bool DestoryObjectOnImpact;
    public GameObject DestroyableObject;

    private Interactable interactable;

    private WaitForSeconds chargedWait;
    private WaitForSeconds leapWait;
    private bool aligning = false;

    // Start is called before the first frame update
    void Start() {
        interactable = GetComponent<Interactable>();
        if (interactable == null) { interactable = GetComponentInChildren<Interactable>(); }

		renderer = GetComponentInChildren<LineRenderer>();

        interactable.Subscribe(FirePlayer);

        //BarrelAngle = Barrel.transform.root.localEulerAngles.x;
        //BaseAngle = Base.transform.root.localEulerAngles.y;
        if (AutoAlign) {
            Align(Target.position, Peak.position);
        }

        SetLineRenderPoints();
    }

	public void SetLineRenderPoints()
	{
        int amo = 11;
		renderer.positionCount = amo;

        Vector3[] positions = {
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.0f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.1f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.2f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.3f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.4f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.5f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.6f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.7f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.8f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 0.9f),
            Interpolation.BezierCurve((BarrelChargePos.position + BarrelChargePos.forward), Peak.position, Target.position, 1.0f) };
        renderer.SetPositions(positions);
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
    }

    private IEnumerator Rotate(Vector3 newTarget, Vector3 newPeak) {
        aligning = true;
        Vector3 oldTarget = Target.position;
        Vector3 oldPeak = Peak.position;

        Vector3 oldBaseDir = oldTarget - Base.transform.position;
        oldBaseDir.y = 0.0f;
        oldBaseDir = oldBaseDir.normalized;

        Vector3 newBaseDir = newTarget - Base.transform.position;
        newBaseDir.y = 0.0f;
        newBaseDir = newBaseDir.normalized;
        
        // Base Rotation
        {
            float startTime = Time.time;
            while (Time.time < startTime + BaseRotateTime) {
                float t = (Time.time - startTime) / BaseRotateTime;

                Vector3 baseDir = Vector3.Slerp(oldBaseDir, newBaseDir, t);

                Base.transform.forward = baseDir;

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
            while (Time.time < startTime + BarrelRotateTime) {
                float t = (Time.time - startTime) / BarrelRotateTime;

                Vector3 barrelDir = Vector3.Slerp(oldBarrelDir, newBarrelDir, t);

                Barrel.transform.forward = barrelDir;

                yield return null;
            }
            Barrel.transform.forward = newBarrelDir;
        }

        aligning = false;

		SetLineRenderPoints();
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
        renderer.enabled = false;
        StartCoroutine(Shoot(player));
    }

	public IEnumerator Shoot(Player player) {
        Transform oldParent = player.transform.parent;
        Quaternion oldRot = player.transform.localRotation;
        DamageType resistance = player.health.Resistance;

        player.transform.position = Barrel.transform.position;
        player.transform.SetParent(Barrel.transform, true);
        player.transform.up = Barrel.transform.forward;
        player.LookTowards(Barrel.transform.forward);

        // Immune to all...
        player.health.Resistance = DamageType.BASIC | DamageType.EXPLOSIVE | DamageType.FIRE | DamageType.ICE | DamageType.LIGHTNING | DamageType.EARTH | DamageType.TRUE;

        player.CanWalk = false;
        player.CanMove = false;
        player.CanRotate = false;
        player.CanAttack = false;
        player.HideWeapon();

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
            AudioManager.Instance.PlaySoundWithParent("cannon", ESoundChannel.SFX, gameObject);
            player.ShowWeapon();

            Vector3 startPos = BarrelChargePos.position;
            float startTime = Time.time;
            while (Time.time < startTime + LeapTime) {
                float t = (Time.time - startTime) / LeapTime;

                Vector3 pos = Utility.BezierCurve(startPos, Peak.position, Target.position, t);

                player.transform.position = pos;

                player.rotation += Vector3.right * PlayerRotateSpeed * Time.deltaTime;

                yield return null;
            }
            player.transform.position = Target.position;
        }

        if (DestoryObjectOnImpact && DestroyableObject) {
            StartCoroutine(MakeDestroyableObjectFall());
        }

        Explosion(player.transform.position);
        AudioManager.Instance.PlaySoundWithParent("thud", ESoundChannel.SFX, player.gameObject);

        player.CanWalk = true;
        player.CanMove = true;
        player.CanAttack = true;
        player.transform.SetParent(oldParent, true);
        player.transform.localRotation = oldRot;
        player.health.Resistance = resistance;
    }

    [ContextMenu("Find Mid Point")]
    private void FindMidPoint() {
        Vector3 mid = Barrel.transform.position + .5f * (Target.position - Barrel.transform.position);
        Peak.position = mid;
        Peak.forward = (Target.position - Barrel.transform.position).normalized;
    }

    private IEnumerator MakeDestroyableObjectFall() {
        Vector3 vel = Vector3.zero;
        float startTime = Time.time;
        while (DestroyableObject.activeInHierarchy && Time.time < startTime + 10f) {
            vel += Vector3.up * -9.8f * Time.deltaTime;

            DestroyableObject.transform.position += vel * Time.deltaTime;

            yield return null;
        }

        Destroy(DestroyableObject);
    }

    public void Explosion(Vector3 pos) {
        LandingEffect.transform.position = pos;
        LandingEffect.Play();

        Collider[] colliders = Physics.OverlapSphere(pos, RadiusSize, EnemyLayer);
        foreach(Collider other in colliders) {
            Enemy enemy = other.GetComponentInChildren<Enemy>();
            if (enemy == null) { enemy = other.GetComponentInParent<Enemy>(); }
            if (enemy != null) {
                // Damage
                float damage = enemy.health.TakeDamage(DamageType.EXPLOSIVE, this.Damage);
                bool isDead = enemy.health.IsDead();

                // Explosion
                if (damage > 0 && isDead) {
                    Vector3 forward = this.transform.forward;
                    forward.y = 0.0f;
                    forward = forward.normalized;
                    enemy.Explode(forward * RigidbodyKnockback, pos);
                }
            }
        }    
    }
}
