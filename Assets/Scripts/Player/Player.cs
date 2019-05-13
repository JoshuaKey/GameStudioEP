﻿using Luminosity.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour {

    [Header("Movement")]
    public bool CanMove = true;
    public bool CanWalk = true;
    public float MaxSpeed = 7.5f; // + 2.5 Speed Boost
    public float AccelerationFactor = 0.1f;
    public float MaxVelocity = 100.0f;

    [Header("Jump")]
    public bool CanJump = true;
    public float JumpPower = 15;
    public float FallStrength = 2.5f;
    public float LowJumpStrength = 2f;
    public float Gravity = 30f;

    [Header("Rotation")]
    public bool CanRotate = true;
    public float XRotationSpeed = 60f;
    public float YRotationSpeed = 60f;
    public float HorizontalRotationSensitivity = 1.0f;
    public float VerticalRotationSensitivity = 1.0f;
    public float YMinRotation = -40f;
    public float YMaxRotation = 40f;
    public new Camera camera;

    [Header("Combat")]
    public bool CanAttack = true;
    public bool CanSwapWeapon = false;
    public int CurrWeaponIndex = 0;
    public float WeaponWheeltimeScale = .5f;
    public Health health;
    public GameObject WeaponParent;

    [Header("Interact")]
    public float InteractDistance = 2.0f;
    public LayerMask InteractLayer;
    public bool CanInteract = true;
    
    [Header("Compass")]
    public CompassPot compass;
    public Transform Shoulder;

    [Space]
    [Header("Debug")]
    public Sword SwordPrefab;
    public Bow BowPrefab;
    public Hammer HammerPrefab;
    public Spear SpearPrefab;
    public CrossBow CrossBowPrefab;
    public Magic MagicPrefab;

    public static Player Instance;

    [HideInInspector]
    public Vector3 velocity = Vector3.zero;
    [HideInInspector]
    public Vector3 rotation = Vector3.zero;
    [HideInInspector]
    public List<Weapon> weapons = new List<Weapon>();
    [HideInInspector]
    public int layerMask;

    private new Collider collider;
    private CharacterController controller;
    private Vector2 weaponWheelRotation = Vector2.zero;

    void Start() {
        if (Instance != null) { Destroy(this.gameObject); return; }
        Instance = this;

        if (collider == null) { collider = GetComponentInChildren<Collider>(true); }

        if (controller == null) { controller = GetComponentInChildren<CharacterController>(true); }

        if (health == null) { health = GetComponentInChildren<Health>(true); }

        if (camera == null) { camera = GetComponentInChildren<Camera>(true); }

        weapons.AddRange(GetComponentsInChildren<Weapon>(true));
        foreach (Weapon w in weapons) {
            PlayerStats.Instance.Weapons[w.name] = true;
            w.gameObject.SetActive(false);
        }
        CurrWeaponIndex = Mathf.Min(weapons.Count - 1, CurrWeaponIndex);

        Weapon newWeapon = GetCurrentWeapon();
        newWeapon.gameObject.SetActive(true);
        newWeapon.transform.SetParent(camera.transform, false);

        string[] weaponNames = weapons.Select(x => x.name).ToArray();
        PlayerHud.Instance.SetWeaponWheel(weaponNames);
        PlayerHud.Instance.DisableWeaponWheel();
        if (weapons.Count > 1) {
            PlayerHud.Instance.EnableWeaponToggle();
            CanSwapWeapon = true;
        } else {
            PlayerHud.Instance.DisableWeaponToggle();
            CanSwapWeapon = false;
        }

        // Physics
        layerMask = 1 << this.gameObject.layer;

        // Camera
        Cursor.lockState = CursorLockMode.Locked;
        rotation = this.transform.rotation.eulerAngles;

        // Compass
        compass.Origin = Shoulder;
        compass.Base = camera.transform;
        compass.transform.SetParent(Shoulder);

        // Input Scheme Rotation
        CheckInputScheme();

        PlayerHud.Instance.EnablePlayerHealthBar();
        this.health.OnDeath += this.Die;
        this.health.OnDamage += ChangeHealthUI;
        this.health.OnHeal += ChangeHealthUI;
        Settings.OnLoad += OnSettingsLoad;
        PlayerStats.Instance.OnLoad += OnStatsLoad;
        InputManager.ControlSchemesChanged += OnControlSchemeChanged;
        InputManager.PlayerControlsChanged += OnPlayerControlChanged;
    }

    void Update() {
        if (CanMove) {
            UpdateMovement();
        }
        UpdateCombat();
        if (CanInteract) {
            UpdateInteractable();
        }
        if (InputManager.GetButtonDown("Pause Menu"))
        {
            if (Time.timeScale == 0)
            {
                PauseMenu.Instance.DeactivatePauseMenu();
            }
            else
            {
                PauseMenu.Instance.ActivatePauseMenu();
            }
        }

        // Debug...
        if (Application.isEditor) {
            if (Input.GetKeyDown(KeyCode.T)) {
                this.health.TakeDamage(DamageType.TRUE, 0.5f);
            }
            if (Input.GetKeyDown(KeyCode.Z)) {
                Weapon w = WeaponManager.Instance.GetWeapon("Bow");
                w.transform.SetParent(WeaponParent.transform, false);
                AddWeapon(w);
            }
            if (Input.GetKeyDown(KeyCode.X)) {
                Weapon w = WeaponManager.Instance.GetWeapon("Hammer");
                w.transform.SetParent(WeaponParent.transform, false);
                AddWeapon(w);
            }
            if (Input.GetKeyDown(KeyCode.C)) {
                Weapon w = WeaponManager.Instance.GetWeapon("Spear");
                w.transform.SetParent(WeaponParent.transform, false);
                AddWeapon(w);
            }
            if (Input.GetKeyDown(KeyCode.V)) {
                Weapon w = WeaponManager.Instance.GetWeapon("CrossBow");
                w.transform.SetParent(WeaponParent.transform, false);
                AddWeapon(w);
            }
            if (Input.GetKeyDown(KeyCode.B)) {
                Weapon w = WeaponManager.Instance.GetWeapon("Magic");
                w.transform.SetParent(WeaponParent.transform, false);
                AddWeapon(w);
            }
        }

    }
    private void LateUpdate() {
        if (CanRotate) {
            UpdateCamera();
        }   
    }

    public void UpdateCamera() {
        // Rotation Input
        float xRot = InputManager.GetAxis("Vertical Rotation");
        xRot *= YRotationSpeed * HorizontalRotationSensitivity * Time.deltaTime;
        float yRot = InputManager.GetAxis("Horizontal Rotation");
        yRot *= XRotationSpeed * VerticalRotationSensitivity * Time.deltaTime;

        // Add to Existing Rotation
        rotation += new Vector3(xRot, yRot, 0.0f);

        // Constrain Rotation
        rotation.x = Mathf.Min(rotation.x, YMaxRotation);
        rotation.x = Mathf.Max(rotation.x, YMinRotation);
        if (rotation.y > 360 || rotation.y < -360) {
            rotation.y = rotation.y % 360;
        }

        camera.transform.forward = Quaternion.Euler(rotation) * Vector3.forward;
    }
    public void UpdateMovement() {
        Walk();

        if (CanJump) {
            Jump();
        }      

        // Add Gravity
        if (!controller.isGrounded) {
            velocity.y -= Gravity * Time.deltaTime;
        }

        velocity = Vector3.ClampMagnitude(velocity, MaxVelocity);

        // Move with Delta Time
        controller.Move(velocity * Time.deltaTime);
    }
    public void UpdateCombat() {
        Weapon weapon = GetCurrentWeapon();

        // Check for Weapon Swap
        if (CanSwapWeapon && weapon.CanAttack()) {
            // Weapon Toggle
            if (InputManager.GetButtonDown("Next Weapon")) {
                int nextIndex = CurrWeaponIndex + 1 >= weapons.Count ? 0 : CurrWeaponIndex + 1;
                SwapWeapon(nextIndex);
            }
            if (InputManager.GetButtonDown("Prev Weapon")) {
                int prevIndex = CurrWeaponIndex - 1 < 0 ? weapons.Count - 1 : CurrWeaponIndex - 1;
                SwapWeapon(prevIndex);
            }

            // Weapon Wheel
            if (InputManager.GetButtonDown("Weapon Wheel")) {
                Time.timeScale = WeaponWheeltimeScale;
                PlayerHud.Instance.EnableWeaponWheel();
                CanRotate = false;
                CanAttack = false;
            } 
            else if (InputManager.GetButtonUp("Weapon Wheel")) {
                Time.timeScale = 1f;
                PlayerHud.Instance.DisableWeaponWheel();
                CanRotate = true;
                CanAttack = true;
                weaponWheelRotation = Vector3.zero;
            } 
            else if (InputManager.GetButton("Weapon Wheel")) {
                UpdateWeaponWheelRotation();

                int index = -1;
                float currAngle = 0;
                if (weaponWheelRotation != Vector2.zero) {
                    float weaponAngle = Mathf.PI * 2 / weapons.Count;
                    currAngle = Mathf.Atan2(weaponWheelRotation.x, weaponWheelRotation.y);
                    
                    if(currAngle < 0) {
                        currAngle = Mathf.PI * 2 + currAngle;
                    }

                    index = (int)(currAngle / weaponAngle);
                }
                PlayerHud.Instance.HighlightWeaponWheel(index, weaponWheelRotation);
                if(index != -1) {
                    this.SwapWeapon(index);
                }
            }

            // Number Bar
            if (InputManager.GetButtonDown("Weapon1")) { 
                SwapWeapon(0);
            }
            if (InputManager.GetButtonDown("Weapon2")) {
                SwapWeapon(1);
            }
            if (InputManager.GetButtonDown("Weapon3")) {
                SwapWeapon(2);
            }
            if (InputManager.GetButtonDown("Weapon4")) {
                SwapWeapon(3);
            }
            if (InputManager.GetButtonDown("Weapon5")) {
                SwapWeapon(4);
            }
            if (InputManager.GetButtonDown("Weapon6")) {
                SwapWeapon(5);
            }

            // Scroll Wheel
            if (Input.GetAxis("Mouse ScrollWheel") != 0.0f) {
                int index = (int)(CurrWeaponIndex + Input.mouseScrollDelta.y);
                if(index >= weapons.Count) {
                    index = 0;
                } else if (index < 0) {
                    index = weapons.Count - 1;
                }
                SwapWeapon(index);
            }
        }

        // Check for Attack
        if (CanAttack) {          
            if (weapon.CanAttack()) {
                if (weapon.CanCharge) {
                    if (InputManager.GetButton("Attack")) {
                        weapon.Charge();
                    } else {
                        weapon.Attack();
                    }
                } else {
                    if (InputManager.GetButton("Attack")) {
                        weapon.Attack();
                    }
                }
            }
        }
    }
    public void UpdateInteractable() {
        // Check for Compass
        {
            if (InputManager.GetButtonDown("Compass")) {
                compass.Activate();
            } 
            else if (compass.gameObject.activeInHierarchy) {
                Transform target;
                if (EnemyManager.Instance.MainProgression.IsComplete()) {
                    target = EnemyManager.Instance.MainProgression.ProgressionObject.transform;
                } else {
                    target = EnemyManager.Instance.GetClosestEnemy(this.transform.position).transform;
                }
                compass.Target = target;
            }
        }


        // Check for interactable
        {
            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, InteractDistance, InteractLayer)) {
                Interactable interactable = hit.collider.GetComponentInChildren<Interactable>(true);
                if (interactable == null) { interactable = hit.collider.GetComponentInParent<Interactable>(); }

                if (interactable.CanInteract) {
                    PlayerHud.Instance.EnableInteractText();

                    if (InputManager.GetButtonDown("Interact")) {
                        interactable.Interact();
                    }
                } else {
                    PlayerHud.Instance.DisableInteractText();
                }

            } else {
                PlayerHud.Instance.DisableInteractText();
            }
        }
    }

    public void Walk() {
        Vector3 movement = Vector3.zero;
        float rightMove = 0.0f;
        float frontMove = 0.0f;

        if (CanWalk) {
            rightMove = InputManager.GetAxisRaw("Vertical Movement");
            frontMove = InputManager.GetAxisRaw("Horizontal Movement");
        }      

        if (Mathf.Abs(rightMove) >= 0.1f || Mathf.Abs(frontMove) >= 0.1f) { // Hot Fix
            // Get Movement
            Vector3 forward = camera.transform.forward * rightMove;
            Vector3 right = camera.transform.right * frontMove;
            // Combine Forward and Right Movement
            movement = forward + right;
            // Ignore Y component
            movement.y = 0f;
            // Normalize Movement Direction
            movement = movement.normalized;
        }

        // Ignore Y Component
        Vector3 currMovement = velocity;
        currMovement.y = 0f;

        // This Equation moves us towards out desired Movement based off our curr Velocity.
        // The Acceleration allows us to move instantly (1), or build up (.1)
        velocity += ((movement * MaxSpeed) - currMovement) * AccelerationFactor;
    }
    public void Jump() {
        // Check for Jump
        if (controller.isGrounded) {
            if (InputManager.GetButtonDown("Jump")) {
                velocity.y = JumpPower;
            } 
        }
        // By Default the Player does a "long jump" by holding the Jump Button
        else {
            // If we are falling, add more Gravity
            if (velocity.y < 0.0f) {
                velocity.y -= Gravity * (FallStrength - 1f) * Time.deltaTime;
            }
            // If we are not doing a "long jump", add more Gravity 
            else if (velocity.y > 0.0f && !InputManager.GetButton("Jump")) {
                velocity.y -= Gravity * (LowJumpStrength - 1f) * Time.deltaTime;
            }
        }
    }
    public void Knockback(Vector3 force) {
        velocity += force;
    }

    public void ChangeHealthUI(float val) {
        PlayerHud.Instance.SetPlayerHealthBar(this.health.CurrentHealth / this.health.MaxHealth);
    }
    public void Die() {
        LevelManager.Instance.RestartLevel();
    }

    public void LookTowards(Vector3 forward) {
        camera.transform.forward = forward;
        rotation = camera.transform.rotation.eulerAngles;

        if (rotation.x > 180) { rotation.x = rotation.x - 360; }
        if (rotation.x < -180) { rotation.x = rotation.x + 360; }
    }

    public Vector3 UpdateWeaponWheelRotation() {
        // Rotation Input
        float yRot = -InputManager.GetAxis("Vertical Rotation") * YRotationSpeed * Time.deltaTime;
        float xRot = InputManager.GetAxis("Horizontal Rotation") * XRotationSpeed * Time.deltaTime;

        // Add to Existing Rotation
        weaponWheelRotation += new Vector2(xRot, yRot);
        weaponWheelRotation = weaponWheelRotation.normalized;

        return weaponWheelRotation;
    }
    public void AddWeapon(Weapon newWeapon, Weapon oldWeapon = null) {
        PlayerStats.Instance.Weapons[newWeapon.name] = true;

        if(oldWeapon == null) {
            weapons.Add(newWeapon);
        } else {
            int index = weapons.FindIndex(x => x.name == oldWeapon.name);
            weapons[index] = newWeapon;
        }  

        newWeapon.gameObject.SetActive(false);
        newWeapon.transform.SetParent(WeaponParent.transform, false);

        string[] weaponNames = weapons.Select(x => x.name).ToArray();
        int nextIndex = CurrWeaponIndex + 1 >= weapons.Count ? 0 : CurrWeaponIndex + 1;
        int prevIndex = CurrWeaponIndex - 1 < 0 ? weapons.Count - 1 : CurrWeaponIndex - 1;

        PlayerHud.Instance.SetWeaponToggle(weapons[prevIndex].name, 
            weapons[CurrWeaponIndex].name, weapons[nextIndex].name);
        PlayerHud.Instance.SetWeaponWheel(weaponNames);
        PlayerHud.Instance.DisableWeaponWheel();

        if (weapons.Count > 1) {
            PlayerHud.Instance.EnableWeaponToggle();
            Player.Instance.CanSwapWeapon = true;
        } else {
            PlayerHud.Instance.DisableWeaponToggle();
            Player.Instance.CanSwapWeapon = false;
        }   
    }
    public void SwapWeapon(int index) {
        if (index == CurrWeaponIndex) { return; }
        if(index < 0) { index = 0; }
        if(index >= weapons.Count) { index = weapons.Count - 1; }

        Weapon oldWeapon = GetCurrentWeapon();
        oldWeapon.gameObject.SetActive(false);
        oldWeapon.transform.SetParent(WeaponParent.transform, false);

        CurrWeaponIndex = index;

        Weapon newWeapon = GetCurrentWeapon();
        newWeapon.gameObject.SetActive(true);
        newWeapon.transform.SetParent(camera.transform, false);

        int nextIndex = CurrWeaponIndex + 1 >= weapons.Count ? 0 : CurrWeaponIndex + 1;
        int prevIndex = CurrWeaponIndex - 1 < 0 ? weapons.Count - 1 : CurrWeaponIndex - 1;
        PlayerHud.Instance.SetWeaponToggle(weapons[prevIndex].name, newWeapon.name, weapons[nextIndex].name);
    }
    public Weapon GetCurrentWeapon() {
        return weapons[CurrWeaponIndex];
    }

    private void OnPlayerControlChanged(PlayerID id) { CheckInputScheme(); }
    private void OnControlSchemeChanged() { CheckInputScheme(); }
    public void CheckInputScheme() {
        if(InputManager.PlayerOneControlScheme.Name == InputController.Instance.ControllerSchemeName) {
            XRotationSpeed = 150f;
            YRotationSpeed = 100f;
        } else {
            XRotationSpeed = 60f;
            YRotationSpeed = 60f;
        }
    }
    private void OnSettingsLoad(Settings settings) {
        Player.Instance.camera.fieldOfView = settings.FOV;
        Player.Instance.VerticalRotationSensitivity = settings.VerticalSensitivity;
        Player.Instance.HorizontalRotationSensitivity = settings.HorizontalSensitivity;
    }
    private void OnStatsLoad(PlayerStats stats) {
        foreach (Weapon w in weapons) { Destroy(w.gameObject); }
        weapons.Clear();
        CurrWeaponIndex = 0;

        foreach(KeyValuePair<string, bool> pair in stats.Weapons) {
            if (pair.Value) {
                Weapon w = WeaponManager.Instance.GetWeapon(pair.Key);
                w.transform.SetParent(WeaponParent.transform, false);
                weapons.Add(w);
                w.gameObject.SetActive(false);
            }
        }

        Weapon newWeapon = GetCurrentWeapon();
        newWeapon.gameObject.SetActive(true);
        newWeapon.transform.SetParent(camera.transform, false);

        string[] weaponNames = weapons.Select(x => x.name).ToArray();
        PlayerHud.Instance.SetWeaponWheel(weaponNames);
        PlayerHud.Instance.DisableWeaponWheel();
        if (weapons.Count > 1) {
            PlayerHud.Instance.EnableWeaponToggle();
            CanSwapWeapon = true;
        } else {
            PlayerHud.Instance.DisableWeaponToggle();
            CanSwapWeapon = false;
        }
    }

    // Testing --------------------------------------------------------------
    private void OnGUI() {
        GUI.Label(new Rect(10, 10, 150, 20), "Vel: " + velocity);
        GUI.Label(new Rect(10, 30, 150, 20), "Rot: " + rotation);

        GUI.Label(new Rect(10, 50, 150, 20), "Inp: " + new Vector2(InputManager.GetAxisRaw("Vertical Movement"), InputManager.GetAxisRaw("Horizontal Movement")));
        GUI.Label(new Rect(10, 70, 150, 20), "Wea Rot: " + weaponWheelRotation);
    }

}