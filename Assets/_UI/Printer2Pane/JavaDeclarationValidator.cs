using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Text;

public class JavaDeclarationValidator : MonoBehaviour
{
    public ConsoleController inputConsole;
    public ConsoleController outputConsole;
    public UIDocument uiDocument;
    
    [Header("Highlighter")]
    [Tooltip("Assign the ManualSyntaxHighlighter attached to the Output Console")]
    public ManualSyntaxHighlighter outputHighlighter;

    [Header("Settings")]
    [SerializeField] private int maxCharactersPerLine = 40; 

    // Colors for the Output Console
    private Color errorColor = Color.red;
    private Color successColor = Color.white;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        var playButton = root.Q<Button>("PlayButton");
        if (playButton != null)
            playButton.clicked += ValidateInput;
    }

    private void ValidateInput()
    {
        // We do NOT touch the inputHighlighter here. 
        // The Input Console's prefab handles its own Java highlighting.

        string code = string.Join("\n", inputConsole.lines).Trim();
        
        if (string.IsNullOrEmpty(code))
        {
            LogMessage("Error: Input is empty.", errorColor);
            return;
        }

        // 1. Semicolon Check
        if (!code.EndsWith(";"))
        {
            LogMessage("Error: Missing semicolon ';' at the end of the statement.", errorColor);
            return;
        }

        // 2. Logic Parsing
        string cleanCode = code.TrimEnd(';').Trim();
        string[] assignmentParts = cleanCode.Split('=');

        if (assignmentParts.Length > 2)
        {
            LogMessage("Error: Extraneous '=' found. Only one assignment allowed.", errorColor);
            return;
        }
        if (assignmentParts.Length < 2)
        {
            LogMessage("Error: Missing assignment operator '='.", errorColor);
            return;
        }

        string leftSide = assignmentParts[0].Trim();
        string val = assignmentParts[1].Trim();
        string[] leftParts = Regex.Split(leftSide, @"\s+");

        if (leftParts.Length < 2)
        {
            LogMessage("Error: Missing variable name or type.", errorColor);
            return;
        }

        string type = leftParts[0];
        string name = leftParts[1];

        // 3. Type & Reserved Word Validation
        string[] supportedTypes = { "int", "boolean", "double" };
        if (!supportedTypes.Contains(type))
        {
            LogMessage($"Error: Unsupported type '{type}'. Use 'int', 'double', or 'boolean'.", errorColor);
            return;
        }

        string[] reserved = { "int", "boolean", "double", "if", "else", "for", "while", "class", "true", "false", "void" };
        if (reserved.Contains(name))
        {
            LogMessage($"Error: '{name}' is a reserved Java keyword.", errorColor);
            return;
        }

        // 4. Value Compatibility
        if (!IsValidValue(type, val))
        {
            LogMessage($"Error: Value '{val}' is incompatible with type '{type}'.", errorColor);
            return;
        }

        LogMessage($"Success: '{name}' is a valid {type} declaration.", successColor);
    }

    private bool IsValidValue(string type, string val)
    {
        switch (type)
        {
            case "int": return int.TryParse(val, out _);
            case "double": return double.TryParse(val.TrimEnd('d', 'D'), out _);
            case "boolean": return val == "true" || val == "false";
            default: return false;
        }
    }

    /// <summary>
    /// Logs a message to the output console using plain text 
    /// and uses the Manual Highlighter to apply colors.
    /// </summary>
    private void LogMessage(string msg, Color color)
    {
        // Clear previous state
        outputConsole.lines.Clear();
        if (outputHighlighter != null)
        {
            outputHighlighter.Clear();
        }

        // Wrap the text
        List<string> wrappedLines = WrapText(msg);

        // Add plain text to the console and register color ranges
        for (int i = 0; i < wrappedLines.Count; i++)
        {
            outputConsole.lines.Add(wrappedLines[i]);
            
            if (outputHighlighter != null)
            {
                // Paint the entire line the target color
                outputHighlighter.AddRange(i, 0, wrappedLines[i].Length, color);
            }
        }

        // Force the console to redraw
        outputConsole.UpdateConsole();
    }

    private List<string> WrapText(string text)
    {
        List<string> lines = new List<string>();
        string[] words = text.Split(' ');
        StringBuilder currentLine = new StringBuilder();

        foreach (string word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxCharactersPerLine)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString().TrimEnd());
                    currentLine.Clear();
                }
                currentLine.Append(word + " ");
            }
            else
            {
                currentLine.Append(word + " ");
            }
        }
        if (currentLine.Length > 0) 
            lines.Add(currentLine.ToString().TrimEnd());

        return lines;
    }
}