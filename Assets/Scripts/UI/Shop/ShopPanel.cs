﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    public bool isMainPanel;
    public string weaponName;

    #pragma warning disable 0649
    [SerializeField] GameObject weaponModel;
    [SerializeField] Animator lockModel;
    #pragma warning restore 0649

    [HideInInspector] public RectTransform rectTransform;

    HubShop hubShop;
    RawImage rawImage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        hubShop = GetComponentInParent<HubShop>();
        rawImage = GetComponentInChildren<RawImage>();

        BlackOut(!HubShop.GetWeaponInfo(weaponName).isUnlocked);
    }

    public void StartMovingTo(Vector3 pos, Vector3 scale, bool isMainPanel)
    {
        StartCoroutine(MoveTo(pos, scale, 0.25f, isMainPanel));
    }

    IEnumerator MoveTo(Vector3 pos, Vector3 scale, float duration, bool isMainPanel)
    {
        float startTime = Time.time;

        while (Time.time < startTime + duration)
        {
            rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, pos, (Time.time - startTime));
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, scale, (Time.time - startTime));
            yield return null;
        }

        rectTransform.localPosition = pos;
        rectTransform.localScale = scale;
        this.isMainPanel = isMainPanel;

        if (isMainPanel)
        {
            hubShop.mainPanel = this;
            BlackOut(!HubShop.GetWeaponInfo(weaponName).isUnlocked);
        }

        hubShop.isMovingPanels = false;
    }

    public void BlackOut(bool isBlackedOut)
    {
        MeshRenderer mesh = weaponModel.GetComponentInChildren<MeshRenderer>();
        if (mesh != null)
        {
            mesh.material.color = isBlackedOut ? Color.black : Color.white;
        }
        else
        {
            SkinnedMeshRenderer skinnedMesh = weaponModel.GetComponentInChildren<SkinnedMeshRenderer>();
            if(skinnedMesh != null)
            {
                skinnedMesh.material.color = isBlackedOut ? Color.black : Color.white;
            }
        }
        if (!isBlackedOut) lockModel.SetTrigger("Unlock");
        //rawImage.color = isBlackedOut ? Color.black : Color.white;
    }
}
