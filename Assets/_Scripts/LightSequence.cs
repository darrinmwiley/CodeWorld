using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightSequenceController : MonoBehaviour
{
    [Header("Input Settings")]
    public KeyCode activationKey = KeyCode.L;
    public KeyCode resetKey = KeyCode.R; // New Reset Key

    [Header("Sequence Settings")]
    public List<GameObject> lightParents;
    public float delayBetweenLights = 0.3f;
    public string lightChildName = "light_ON";

    public bool IsFinished { get; private set; }
    private bool isSequenceRunning = false;

    void Update()
    {
        // Start sequence
        if (Input.GetKeyDown(activationKey) && !isSequenceRunning)
        {
            StartCoroutine(PlayLightSequence());
        }

        // Emergency Shutoff / Reset
        if (Input.GetKeyDown(resetKey))
        {
            ResetAllLights();
        }
    }

    public void StartSequence()
    {
        if (!isSequenceRunning)
        {
            IsFinished = false;
            StartCoroutine(PlayLightSequence());
        }
    }

    private IEnumerator PlayLightSequence()
    {
        isSequenceRunning = true;

        foreach (GameObject parent in lightParents)
        {
            SetLightState(parent, true);
            yield return new WaitForSeconds(delayBetweenLights);
        }

        // We leave isSequenceRunning as true so it doesn't loop, 
        // OR set it to false if you want to be able to play it again immediately.
        isSequenceRunning = false; 
        IsFinished = true;
    }

    public void ResetAllLights()
    {
        // 1. Stop the sequence coroutine so it doesn't keep turning lights ON
        StopAllCoroutines();
        isSequenceRunning = false;
        IsFinished = false;

        // 2. Force every light child to Deactive
        foreach (GameObject parent in lightParents)
        {
            SetLightState(parent, false);
        }
    }

    private void SetLightState(GameObject parent, bool state)
    {
        if (parent == null) return;

        Transform lightChild = parent.transform.Find(lightChildName);
        if (lightChild != null)
        {
            lightChild.gameObject.SetActive(state);
        }
    }
}