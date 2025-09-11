using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Tilemaps;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class CameraZone : MonoBehaviour
{
    public CinemachineConfiner2D confiner;
    public Collider2D cameraZoneCollider;
    public float slowingDistance = 0f;

    public Tilemap[] elementsToShow;
    public Tilemap[] elementsToHide;
    public float fadeTime = 1f;

    public BoxCollider2D[] collidersToEnable;
    public BoxCollider2D[] collidersToDisable;

    private CinemachineCamera vCam;
    private CinemachinePositionComposer positionComposer;

    void Start()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        if (confiner == null)
        {
            Debug.LogError("No CinemachineConfiner2D found in the scene!");
            return;
        }

        vCam = confiner.GetComponent<CinemachineCamera>();
        if (vCam == null)
        {
            vCam = FindFirstObjectByType<CinemachineCamera>();
        }

        if (vCam != null)
        {
            positionComposer = vCam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        }

        if (cameraZoneCollider == null)
        {
            Debug.LogError("No Camera Zone Collider2D found on this CameraZone object!");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && confiner != null)
        {
            confiner.BoundingShape2D = cameraZoneCollider;
            confiner.InvalidateBoundingShapeCache();

            if (confiner != null)
            {
                confiner.SlowingDistance = slowingDistance;
            }

            if (elementsToShow.Length > 0)
                StartCoroutine(FadeTilemaps(elementsToShow, 0f, 1f, fadeTime));
            if (elementsToHide.Length > 0)
                StartCoroutine(FadeTilemaps(elementsToHide, 1f, 0f, fadeTime));

            if (collidersToEnable != null)
            {
                foreach (var col in collidersToEnable)
                    if (col != null) col.enabled = true;
            }

            if (collidersToDisable != null)
            {
                foreach (var col in collidersToDisable)
                    if (col != null) col.enabled = false;
            }
        }
    }

    IEnumerator FadeTilemaps(Tilemap[] tilemaps, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        Color[] originalColors = new Color[tilemaps.Length];
        for (int i = 0; i < tilemaps.Length; i++)
            originalColors[i] = tilemaps[i].color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < tilemaps.Length; i++)
            {
                Color c = originalColors[i];
                c.a = Mathf.Lerp(startAlpha, endAlpha, t);
                tilemaps[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Color c = originalColors[i];
            c.a = endAlpha;
            tilemaps[i].color = c;
        }
    }
}