using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Level2NarrativeManager : MonoBehaviour
{
    [Header("References")]
    public TerminalDialogue dialogueUI;
    public FacePowerClickController powerController;

    private Queue<string> _lines = new Queue<string>();
    private bool _inConversation = false;
    private bool _introTriggered = false;

    void OnEnable()
    {
        if (powerController != null)
            powerController.OnFaceTurnedOn.AddListener(HandleFacePowerOn);
    }

    void OnDisable()
    {
        if (powerController != null)
            powerController.OnFaceTurnedOn.RemoveListener(HandleFacePowerOn);
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

        _lines.Clear();
        foreach (string line in sequence) _lines.Enqueue(line);
        _inConversation = true;
        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        if (_lines.Count > 0)
        {
            dialogueUI.PlayDialogue(_lines.Dequeue());
        }
        else
        {
            _inConversation = false;
            dialogueUI.HideDialogue();

            if (powerController != null)
                powerController.SetLockOnDuringDialogue(false);
        }
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
