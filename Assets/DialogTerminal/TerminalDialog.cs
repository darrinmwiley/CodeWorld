using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class TerminalDialogue : MonoBehaviour
{
    private Label _messageLabel;
    private Label _promptLabel; 
    private VisualElement _avatarBox;
    private VisualElement _container;

    [Header("Typewriter Settings")]
    public float charsPerSecond = 25f;
    public bool IsTyping { get; private set; } = false;

    [Header("Portrait Textures")]
    public Texture2D idleTex;
    public Texture2D talkTex1;
    public Texture2D talkTex2;
    public float mouthFlapSpeed = 0.15f;

    void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        _avatarBox = root.Q<VisualElement>("AvatarBox");
        _messageLabel = root.Q<Label>("MessageContent");
        _promptLabel = root.Q<Label>("Prompt");
        _container = root.Q<VisualElement>("DialogContainer");

        _container.style.display = DisplayStyle.None;
        if (_promptLabel != null) _promptLabel.style.display = DisplayStyle.None;
        
        if (idleTex != null) _avatarBox.style.backgroundImage = idleTex;
    }

    public void PlayDialogue(string text)
    {
        _container.style.display = DisplayStyle.Flex;
        if (_promptLabel != null) _promptLabel.style.display = DisplayStyle.None; 

        StopAllCoroutines();
        StartCoroutine(TypeTextAndAnimateRoutine(text));
    }

    public void HideDialogue()
    {
        _container.style.display = DisplayStyle.None;
    }

    private IEnumerator TypeTextAndAnimateRoutine(string text)
    {
        IsTyping = true;
        _messageLabel.text = "";
        float mouthTimer = 0f;
        bool useTalk1 = true;

        foreach (char c in text)
        {
            _messageLabel.text += c;
            if (!char.IsWhiteSpace(c))
            {
                mouthTimer += (1f / charsPerSecond);
                if (mouthTimer >= mouthFlapSpeed)
                {
                    mouthTimer = 0;
                    useTalk1 = !useTalk1;
                    _avatarBox.style.backgroundImage = useTalk1 ? talkTex1 : talkTex2;
                }
            }
            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        IsTyping = false;
        _avatarBox.style.backgroundImage = idleTex;

        // Visual change to indicate mouse input
        if (_promptLabel != null)
        {
            _promptLabel.text = "Click to continue..."; 
            _promptLabel.style.display = DisplayStyle.Flex;
        }
    }
}