using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TurtleCommander : MonoBehaviour
{
    [Header("References")]
    public GraphingUtility graph;
    public CommandDisplay display;

    public NarrativeManager narrativeManager;

    [Header("Execution Settings")]
    public float stepDelay = 0.6f; 
    public float resetDelay = 2.0f;
    public Color trailColor = Color.red; // Default path color
    public Color winColor = Color.green;

    private List<TurtleCommand> _commandBank = new List<TurtleCommand>();
    private List<Vector2> _visitHistory = new List<Vector2>();
    private bool _isExecuting = false;
    
    private Vector2 _initialPos;
    private int _initialDir;

    void Start()
    {
        if (graph != null)
        {
            _initialPos = graph.turtlePosition;
            _initialDir = graph.turtleDirection;
        }
    }

    public void AddCommand(TurtleCommand cmd)
    {
        if (_isExecuting) return;
        if (display != null && !display.HasSpace) return;
        _commandBank.Add(cmd);
        if (display != null) display.AddCommandIcon(cmd);
    }

    public void Backspace()
    {
        if (_isExecuting || _commandBank.Count == 0) return;
        _commandBank.RemoveAt(_commandBank.Count - 1);
        if (display != null) display.RemoveLastIcon();
    }

    public void Play()
    {
        if (_isExecuting || _commandBank.Count == 0) return;
        StartCoroutine(ExecuteRoutine());
    }

    private IEnumerator ExecuteRoutine()
    {
        _isExecuting = true;
        _visitHistory.Clear();
        
        // 1. HARD RESET: Position and Trail
        graph.turtlePosition = _initialPos;
        graph.turtleDirection = _initialDir;
        _visitHistory.Add(graph.turtlePosition);

        graph.ClearTrail();
        graph.ClearTemporaryElements();
        graph.Refresh();

        // 2. ANIMATE
        for (int i = 0; i < _commandBank.Count; i++)
        {
            if (display != null) display.HighlightCommand(i);

            TurtleCommand cmd = _commandBank[i];
            Vector2 startPos = graph.turtlePosition;

            if (cmd == TurtleCommand.Forward)
            {
                Vector2 moveStep = graph.GetDirectionVector();
                graph.turtlePosition += moveStep;
                graph.AddTrailSegment(startPos, graph.turtlePosition, trailColor);
                _visitHistory.Add(graph.turtlePosition);
            }
            else if (cmd == TurtleCommand.Clockwise)
            {
                graph.turtleDirection = (graph.turtleDirection + 1) % 8;
            }
            else if (cmd == TurtleCommand.CounterClockwise)
            {
                graph.turtleDirection = (graph.turtleDirection + 7) % 8;
            }

            graph.Refresh();
            yield return new WaitForSeconds(stepDelay);
        }

        if (display != null) display.ClearHighlights();

        // 3. WIN CHECK
        if (CheckWin())
        {
            graph.SetTrailColor(winColor); 
            yield return new WaitForSeconds(resetDelay);
            
            // ONLY CLEAR DATA ON WIN
            _commandBank.Clear();
            if (display != null) 
            {
                int count = display.transform.childCount;
                for(int i = 0; i < count; i++) display.RemoveLastIcon();
            }

            AdvanceLevel();
        }
        else
        {
            // FAIL STATE: Wait, but keep commands in _commandBank
            yield return new WaitForSeconds(resetDelay);
        }

        // 4. POST-RUN CLEANUP (Keep the commands, just reset turtle position)
        graph.turtlePosition = _initialPos;
        graph.turtleDirection = _initialDir;
        graph.ClearTrail();
        graph.Refresh();

        _isExecuting = false;
    }

    private bool CheckWin()
    {
        Vector2[] goalPath = graph.GetCurrentLevelPath();
        
        // 1. Must have same number of points
        if (_visitHistory.Count != goalPath.Length) return false;

        // 2. Check forward match
        bool forwardMatch = true;
        for (int i = 0; i < goalPath.Length; i++)
        {
            if (Vector2.Distance(_visitHistory[i], goalPath[i]) > 0.01f)
            {
                forwardMatch = false;
                break;
            }
        }
        if (forwardMatch) return true;

        // 3. Check backward match
        bool backwardMatch = true;
        for (int i = 0; i < goalPath.Length; i++)
        {
            if (Vector2.Distance(_visitHistory[i], goalPath[goalPath.Length - 1 - i]) > 0.01f)
            {
                backwardMatch = false;
                break;
            }
        }
        return backwardMatch;
    }

    private void AdvanceLevel()
    {
        int current = (int)graph.currentLevel;
        if (current < System.Enum.GetValues(typeof(GraphingUtility.Level)).Length - 1)
        {
            graph.currentLevel = (GraphingUtility.Level)(current + 1);
            Debug.Log("Level Up! New Level: " + graph.currentLevel);
        }
        else
        {
            if (narrativeManager != null)
            {
                narrativeManager.TriggerVictoryDialogue();
            }
            Debug.Log("CONGRATS! You beat all levels!");
        }
    }
}