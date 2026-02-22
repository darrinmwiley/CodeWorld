using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class TerminalDialogue : MonoBehaviour
{
    private Label _messageLabel;
    private VisualElement _avatarBox;
    private VisualElement _container;

    private VisualElement _terminalDialog;

    [Header("Typewriter Settings")]
    public float charsPerSecond = 25f;
    private bool _isTyping = false;

    [Header("Test Data")]
    public KeyCode testKey = KeyCode.T;
    public Texture2D testPortrait;
    private int _testIndex = 0;
    private string[] _testMessages = new string[] {
        "SYSTEM READY. STANDBY FOR INPUT.",
        "WARNING: TURTLE POSITION DATA IS FLUCTUATING. PLEASE ENSURE GRID ALIGNMENT IS WITHIN NOMINAL PARAMETERS.",
        "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG. THIS MESSAGE IS LONG ON PURPOSE TO TEST IF YOUR TEXT WRAPPING AND FLEXBOX GROW RULES ARE WORKING CORRECTLY."
    };

    void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        _avatarBox = root.Q<VisualElement>("AvatarBox");
        _messageLabel = root.Q<Label>("MessageContent");
        _container = root.Q<VisualElement>("DialogContainer");

        _container.style.display = DisplayStyle.None;

        root.style.backgroundColor = new StyleColor(Color.clear);
    }

    void Update()
    {
        // Press 'T' to trigger dialogue
        if (Input.GetKeyDown(testKey))
        {
            CycleTestDialogue();
        }
    }

    private void CycleTestDialogue()
    {
        string msg = _testMessages[_testIndex];
        PlayDialogue(msg, testPortrait);
        
        // Cycle to next message for next press
        _testIndex = (_testIndex + 1) % _testMessages.Length;
    }

    public void PlayDialogue(string text, Texture2D portrait = null)
    {
        _container.style.display = DisplayStyle.Flex;
        
        if (portrait != null)
        {
            _avatarBox.style.display = DisplayStyle.Flex;
            _avatarBox.style.backgroundImage = portrait;
        }

        StopAllCoroutines();
        StartCoroutine(TypeTextRoutine(text));
    }

    private IEnumerator TypeTextRoutine(string text)
    {
        _isTyping = true;
        _messageLabel.text = "";

        foreach (char c in text)
        {
            _messageLabel.text += c;
            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        _isTyping = false;
    }
}