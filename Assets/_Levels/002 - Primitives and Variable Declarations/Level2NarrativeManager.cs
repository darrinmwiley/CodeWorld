using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class Level2NarrativeManager : MonoBehaviour
{
    [Header("References")]
    public TerminalDialogue dialogueUI;
    public FacePowerClickController powerController;

    [Header("Player / Scene Sequencing")]
    public FirstPersonMovement playerMovement;
    public GameObject escapeRoomObject;
    public float escapeRoomSpawnY = 80f;
    public float escapeRoomTargetY = 25f;
    public float escapeRoomDescentDuration = 8f;

    [Header("Level Completion")]
    public BoxCollider levelFinishTrigger;

    private Queue<string> _lines = new Queue<string>();
    private bool _inConversation = false;
    private bool _introTriggered = false;
    private Coroutine _escapeRoomDescentRoutine;
    private bool _escapeRoomDescentStarted;
    private float _levelTimerStartTime;
    private bool _levelTimerRunning;
    private bool _levelCompleted;

    void OnEnable()
    {
        if (powerController != null)
            powerController.OnFaceTurnedOn.AddListener(HandleFacePowerOn);
    }

    void OnDisable()
    {
        if (powerController != null)
            powerController.OnFaceTurnedOn.RemoveListener(HandleFacePowerOn);

        // Safety unlock if this manager gets disabled mid-dialogue.
        SetPlayerMovementLocked(false);
        _levelTimerRunning = false;
    }

    void Update()
    {
        if (_inConversation && !dialogueUI.IsTyping)
        {
            if (Input.GetMouseButtonDown(0))
            {
                DisplayNextLine();
            }
        }

        TryHandleLevelCompletion();
    }

    private void HandleFacePowerOn()
    {
        if (!_introTriggered)
        {
            TriggerIntroduction();
            _introTriggered = true;
        }
    }

    private void StartConversation(string[] sequence)
    {
        if (powerController != null)
            powerController.SetLockOnDuringDialogue(true);

        SetPlayerMovementLocked(true);
        _inConversation = true;

        _escapeRoomDescentStarted = false;
        _lines.Clear();
        foreach (string line in sequence) _lines.Enqueue(line);
        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        if (_lines.Count > 0)
        {
            string nextLine = _lines.Dequeue();

            if (!_escapeRoomDescentStarted && nextLine.IndexOf("escape-room style level", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _escapeRoomDescentStarted = true;
                BeginEscapeRoomDescent();
            }

            dialogueUI.PlayDialogue(nextLine);
        }
        else
        {
            _inConversation = false;
            dialogueUI.HideDialogue();

            if (powerController != null)
                powerController.SetLockOnDuringDialogue(false);

            if (escapeRoomObject != null)
            {
                if (_escapeRoomDescentRoutine != null)
                {
                    StopCoroutine(_escapeRoomDescentRoutine);
                    _escapeRoomDescentRoutine = null;
                }

                Vector3 roomPos = escapeRoomObject.transform.position;
                escapeRoomObject.transform.position = new Vector3(roomPos.x, escapeRoomTargetY, roomPos.z);
            }

            SetPlayerMovementLocked(false);

            if (!_levelCompleted && !_levelTimerRunning)
                StartLevelCompletionTimer();
        }
    }

    private void StartLevelCompletionTimer()
    {
        _levelTimerStartTime = Time.time;
        _levelTimerRunning = true;
        _levelCompleted = false;
    }

    private void TryHandleLevelCompletion()
    {
        if (!_levelTimerRunning || _levelCompleted) return;
        if (levelFinishTrigger == null || playerMovement == null) return;

        if (!levelFinishTrigger.enabled || !levelFinishTrigger.gameObject.activeInHierarchy) return;

        Collider playerCollider = playerMovement.GetComponent<Collider>();
        if (playerCollider == null) return;

        if (levelFinishTrigger.bounds.Intersects(playerCollider.bounds))
        {
            float elapsed = Time.time - _levelTimerStartTime;
            _levelCompleted = true;
            _levelTimerRunning = false;
            TriggerLevelFinishedConversation(elapsed);
        }
    }

    private void TriggerLevelFinishedConversation(float elapsedSeconds)
    {
        TimeSpan elapsed = TimeSpan.FromSeconds(Mathf.Max(0f, elapsedSeconds));
        string formattedElapsed = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);

        string[] completion = {
            $"Nice, looks like you escaped! and it only took you {formattedElapsed}",
            "Once level 3 materializes a little more I'll figure out what the reward here should be. Probably access to some more documentation and ability to do more than just print variables."
        };

        StartConversation(completion);
    }

    private void SetPlayerMovementLocked(bool shouldLock)
    {
        if (playerMovement != null)
            playerMovement.movementLocked = shouldLock;
    }

    private void BeginEscapeRoomDescent()
    {
        if (escapeRoomObject == null) return;

        escapeRoomObject.SetActive(true);

        Vector3 roomPos = escapeRoomObject.transform.position;
        escapeRoomObject.transform.position = new Vector3(roomPos.x, escapeRoomSpawnY, roomPos.z);

        if (_escapeRoomDescentRoutine != null)
            StopCoroutine(_escapeRoomDescentRoutine);

        _escapeRoomDescentRoutine = StartCoroutine(DescendEscapeRoomRoutine());
    }

    private IEnumerator DescendEscapeRoomRoutine()
    {
        if (escapeRoomObject == null) yield break;

        Vector3 startPos = new Vector3(escapeRoomObject.transform.position.x, escapeRoomSpawnY, escapeRoomObject.transform.position.z);
        Vector3 endPos = new Vector3(startPos.x, escapeRoomTargetY, startPos.z);
        float safeDuration = Mathf.Max(0.01f, escapeRoomDescentDuration);
        float elapsed = 0f;

        while (elapsed < safeDuration && _inConversation)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            // Smooth easing for a less mechanical drop.
            float eased = t * t * (3f - 2f * t);
            escapeRoomObject.transform.position = Vector3.Lerp(startPos, endPos, eased);
            yield return null;
        }

        if (escapeRoomObject != null)
            escapeRoomObject.transform.position = endPos;

        _escapeRoomDescentRoutine = null;
    }

    public void TriggerIntroduction()
    {
        string[] intro = {
            "Hello again :)",
            "In the first level, we pressed buttons to schedule different move sequences. That was a cool metaphor for how coding works - you give instructions, and the computer will follow them in order. The only difference between that and real coding is that there isn't a button for everything one might want a computer to do.",
            "Instead, we use programming languages to translate instructions into a format that the computer can understand. In this level, we'll learn one of the most foundational instructions in a programming language - variable creation.",
            "Variables are essentially named containers for data. There are many different types of variables that one can make, and each can be used to track different sorts of data and do different kinds of things.",
            "I've set up an escape-room style level, based around the idea of creating and using different types of variables. To get you started, the only advice I will give is to try pressing tab. The rest is up to you - good luck!"
        };
        StartConversation(intro);
    }
}
