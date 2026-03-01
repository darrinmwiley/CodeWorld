using UnityEngine;
using Antlr4.Runtime;
using System.Collections.Generic;

public class CSharpSyntaxHighlighter : SyntaxHighlighter
{
    [Header("Colors")]
    public Color keywordColor = new Color(86/255f, 156/255f, 214/255f);  // VS Blue
    public Color typeColor = new Color(78/255f, 201/255f, 176/255f);    // Teal
    public Color literalColor = new Color(206/255f, 132/255f, 77/255f); // Orange
    public Color commentColor = new Color(106/255f, 153/255f, 85/255f); // Green
    public Color defaultColor = Color.white;

    public override void Highlight(ConsoleController console)
    {
        string code = string.Join("\n", console.lines);
        var inputStream = new AntlrInputStream(code);
        var lexer = new CSharpLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();

        foreach (var token in tokenStream.GetTokens())
        {
            if (token.Type == TokenConstants.EOF) continue;

            Color targetColor = defaultColor;

            // Keywords: Mapping range from your CSharpLexer (ABSTRACT to WHILE)
            if (token.Type >= CSharpLexer.ABSTRACT && token.Type <= CSharpLexer.WHILE)
            {
                targetColor = keywordColor;
            }
            // Types
            else if (token.Type == CSharpLexer.INT || token.Type == CSharpLexer.BOOL || token.Type == CSharpLexer.STRING)
            {
                targetColor = typeColor;
            }
            // Literals (The fix for your definition error is here)
            else if (token.Type == CSharpLexer.INTEGER_LITERAL || 
                     token.Type == CSharpLexer.REGULAR_STRING || 
                     token.Type == CSharpLexer.VERBATIUM_STRING)
            {
                targetColor = literalColor;
            }
            // Comments
            else if (token.Type == CSharpLexer.SINGLE_LINE_COMMENT || 
                     token.Type == CSharpLexer.DELIMITED_COMMENT)
            {
                targetColor = commentColor;
            }

            PaintToken(console, token, targetColor);
        }
    }
}