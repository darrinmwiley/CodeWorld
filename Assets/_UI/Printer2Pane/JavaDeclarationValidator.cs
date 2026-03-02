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
    public ManualSyntaxHighlighter outputHighlighter;

    [Header("Printer System")]
    public VariablePrinter printer;

    [Header("Settings")]
    [SerializeField] private int maxCharactersPerLine = 40; 

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
        string code = string.Join("\n", inputConsole.lines).Trim();
        
        if (string.IsNullOrEmpty(code))
        {
            LogMessage("Error: Input is empty.", errorColor);
            return;
        }

        if (!code.EndsWith(";"))
        {
            LogMessage("Error: Missing semicolon ';' at the end.", errorColor);
            return;
        }

        string cleanCode = code.TrimEnd(';').Trim();
        string[] assignmentParts = cleanCode.Split('=');

        if (assignmentParts.Length != 2)
        {
            LogMessage("Error: Invalid assignment format.", errorColor);
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

        string[] supportedTypes = { "int", "boolean", "double" };
        if (!supportedTypes.Contains(type))
        {
            LogMessage($"Error: Unsupported type '{type}'.", errorColor);
            return;
        }

        if (!IsValidValue(type, val))
        {
            LogMessage($"Error: Value '{val}' is not a valid {type}.", errorColor);
            return;
        }

        // Success Path
        LogMessage($"Success: '{name}' is a valid {type} declaration.", successColor);

        if (printer != null)
        {
            printer.PrintShape(type);
        }
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

    private void LogMessage(string msg, Color color)
    {
        outputConsole.lines.Clear();
        if (outputHighlighter != null) outputHighlighter.Clear();

        List<string> wrappedLines = WrapText(msg);

        for (int i = 0; i < wrappedLines.Count; i++)
        {
            outputConsole.lines.Add(wrappedLines[i]);
            if (outputHighlighter != null)
                outputHighlighter.AddRange(i, 0, wrappedLines[i].Length, color);
        }

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
            }
            currentLine.Append(word + " ");
        }
        if (currentLine.Length > 0) lines.Add(currentLine.ToString().TrimEnd());
        return lines;
    }
}