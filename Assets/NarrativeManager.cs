using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class NarrativeManager : MonoBehaviour
{
    [Header("References")]
    public TerminalDialogue dialogueUI;
    public FacePowerClickController powerController;
    
    [Header("Turtle Settings")]
    public GameObject turtleController; // Drag the Turtle GO here
    public float easeDuration = 2.0f;
    public float startY = 10f;
    public float targetY = 1.5f;

    private Queue<string> _lines = new Queue<string>();
    private bool _inConversation = false;
    private bool _introTriggered = false;

    void OnEnable()
    {
        if (powerController != null)
            powerController.OnFaceTurnedOn.AddListener(HandleFacePowerOn);
        
        // Ensure turtle starts disabled and high up
        if (turtleController != null)
        {
            turtleController.SetActive(false);
            Vector3 pos = turtleController.transform.position;
            turtleController.transform.position = new Vector3(pos.x, startY, pos.z);
        }
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
            
            // CONVERSATION OVER: Bring in the Turtle!
            StartCoroutine(EaseInTurtle());
        }
    }

    private IEnumerator EaseInTurtle()
    {
        if (turtleController == null) yield break;

        turtleController.SetActive(true);
        
        Vector3 startPos = turtleController.transform.position;
        Vector3 endPos = new Vector3(startPos.x, targetY, startPos.z);
        float elapsed = 0;

        while (elapsed < easeDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / easeDuration;
            
            // "SmoothStep" for a nice easing effect (starts fast, slows down)
            float t = percent * percent * (3f - 2f * percent);
            
            turtleController.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        turtleController.transform.position = endPos;
        Debug.Log("Turtle Deployed.");
    }

    public void TriggerIntroduction()
    {
        string[] intro = {
            "Hey you :) I don't really know what to say here.",
            "I still need to nail down the introduction dialog - I just haven't decided who this really is yet.",
            "I was thinking maybe this thang would be 'The Object' that object oriented programming is all about, but IDK.",
            "Maybe instead this is just like, a characterization of the game's creator used as a way to communicate with the player?",
            "Maybe it's just Jeeves. Or maybe we just leave it mysterious for now. Anyways - The main point I want to get across with the first dialog is an intro to computation & computational thinking.",
            "One of the cool things about programming is that it really allows us to DO and CREATE anything we can imagine. It's just a matter of thinking about things in the right ways, breaking down problems into smaller pieces, and giving the right instructions.",
            "Computers are great at following instructions. People aren't too bad at giving them either, but to err is human.",
            "To give you an idea of what I'm talking about here, I'd like to show you a little puzzle I've created. See if you can figure it out!"
        };
        StartConversation(intro);
    }

    public void TriggerVictoryDialogue()
    {
        Debug.Log("Triggering Victory Dialogue");
        string[] victory = {
            "Great job! Hope that was a fun intro to what I mean about giving instructions, making mistakes, etc...",
            "This is what programming is about. We have a goal in mind, we break it down into simple steps that we can describe using the commands at our disposal, and then we let the computer do it's thing.",
            "It may not seem so powerful when all you have is 5 buttons to control a little arrow moving around, but imagine having thousands of buttons, each with it's own unique function, and the ability to combine them in any way you want.",
            "It becomes a very powerful tool for creation and problem solving.",
            "Anyways, this is just the beginning. Lots more to learn and explore in different scenes, but I hope you enjoyed this little demo of the core concepts. Thanks for playing!"
        };
        StartConversation(victory);
    }
}