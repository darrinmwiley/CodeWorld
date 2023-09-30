using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

public class SyntaxHighlighter : MonoBehaviour
{

    enum ColorGroup{
        RESERVED,   //dark blue (any keyword)
        TYPE,       //yellow ()
        IDENTIFIER, //light blue
        LITERAL,    //orange
        COMMENT,    //green
        OTHER,      //white
    };

    public static List<int> RESERVED_KEYWORDS = new List<int>(){CSharpLexer.PUBLIC, CSharpLexer.CLASS, CSharpLexer.STRING, CSharpLexer.INT, CSharpLexer.LONG, CSharpLexer.CHAR};

    public static List<Vector3Int> GetCommentLocations(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new CSharpLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();

        List<Vector3Int> tokenPositions = new List<Vector3Int>();

        foreach (var token in tokenStream.GetTokens())
        {
            // Check if the token type is in the list of reserved keywords
            if (token.Type == CSharpLexer.SINGLE_LINE_COMMENT)
            {
                var startLine = token.Line;
                var startColumn = token.Column;
                var endColumn = startColumn + token.Text.Length;

                Vector3Int position = new Vector3Int(startLine, startColumn, endColumn);
                tokenPositions.Add(position);

                Debug.Log($"{CSharpLexer.DefaultVocabulary.GetSymbolicName(token.Type)}: " +
                          $"Row {startLine}, Start Column {startColumn}, End Column {endColumn}");
            }else if(token.Type == CSharpLexer.DELIMITED_COMMENT)
            {
                var startLine = token.Line;
                var startColumn = token.Column;
                string[] split = token.Text.Split("\n");
                tokenPositions.Add(new Vector3Int(startLine, startColumn, startColumn + split[0].Length));
                for(int i = 1;i<split.Length;i++)
                {
                    tokenPositions.Add(new Vector3Int(startLine + i, 0, split[i].Length));
                }
            }
        }

        return tokenPositions;
    }

    public static List<Vector3Int> GetStringLocations(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new CSharpLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();

        List<Vector3Int> tokenPositions = new List<Vector3Int>();

        foreach (var token in tokenStream.GetTokens())
        {
            // Check if the token type is in the list of reserved keywords
            if (token.Type == CSharpLexer.REGULAR_STRING)
            {
                var startLine = token.Line;
                var startColumn = token.Column;
                var endColumn = startColumn + token.Text.Length;

                Vector3Int position = new Vector3Int(startLine, startColumn, endColumn);
                tokenPositions.Add(position);

                Debug.Log($"{CSharpLexer.DefaultVocabulary.GetSymbolicName(token.Type)}: " +
                          $"Row {startLine}, Start Column {startColumn}, End Column {endColumn}");
            }
        }

        return tokenPositions;
    }

    public static List<Vector3Int> GetKeywordLocations(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new CSharpLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();

        List<Vector3Int> tokenPositions = new List<Vector3Int>();

        foreach (var token in tokenStream.GetTokens())
        {
            // Check if the token type is in the list of reserved keywords
            if (RESERVED_KEYWORDS.Contains(token.Type))
            {
                var startLine = token.Line;
                var startColumn = token.Column;
                var endColumn = startColumn + token.Text.Length;

                Vector3Int position = new Vector3Int(startLine, startColumn, endColumn);
                tokenPositions.Add(position);

                Debug.Log($"{CSharpLexer.DefaultVocabulary.GetSymbolicName(token.Type)}: " +
                          $"Row {startLine}, Start Column {startColumn}, End Column {endColumn}");
            }
        }

        return tokenPositions;
    }
}
