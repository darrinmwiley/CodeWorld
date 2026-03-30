using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CodeWorld/Console Level File Set")]
public class ConsoleLevelFileSet : ScriptableObject
{
    public List<ConsoleLevelFileEntry> files = new List<ConsoleLevelFileEntry>();
}

[Serializable]
public class ConsoleLevelFileEntry
{
    [Tooltip("Path shown in the file tree, e.g. Assets/Scripts/Player.cs")]
    public string virtualPath;

    [Tooltip("Actual source text to load into the console")]
    public TextAsset fileAsset;

    [Tooltip("1-based lock spec. Examples: all   |   1,2,5-9   |   3, 7-10")]
    public string lockedLines;
}