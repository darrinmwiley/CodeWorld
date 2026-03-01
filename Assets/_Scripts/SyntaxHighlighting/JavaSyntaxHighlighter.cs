using UnityEngine;
using Antlr4.Runtime;
using System.Collections.Generic;

public class JavaSyntaxHighlighter : SyntaxHighlighter
{
    public Color keywordColor = new Color(0.3f, 0.3f, 1.0f);    // Blue
    public Color typeColor = Color.yellow;
    public Color literalColor = new Color(1.0f, 0.6f, 0.0f);   // Orange
    public Color commentColor = Color.green;
    public Color identifierColor = new Color(0.5f, 0.8f, 1.0f); // Cyan
    public Color defaultColor = Color.white;

    private static readonly HashSet<int> KEYWORDS = new HashSet<int> {
        JavaLexer.ABSTRACT, JavaLexer.BREAK, JavaLexer.CASE, JavaLexer.CLASS, JavaLexer.DO, 
        JavaLexer.ELSE, JavaLexer.FOR, JavaLexer.IF, JavaLexer.IMPORT, JavaLexer.NEW, 
        JavaLexer.PACKAGE, JavaLexer.PRIVATE, JavaLexer.PROTECTED, JavaLexer.PUBLIC, 
        JavaLexer.RETURN, JavaLexer.STATIC, JavaLexer.THIS, JavaLexer.VOID, JavaLexer.WHILE
    };

    private static readonly HashSet<int> TYPES = new HashSet<int> {
        JavaLexer.BOOLEAN, JavaLexer.BYTE, JavaLexer.CHAR, JavaLexer.DOUBLE, 
        JavaLexer.FLOAT, JavaLexer.INT, JavaLexer.LONG, JavaLexer.SHORT
    };

    public override void Highlight(ConsoleController console)
    {
        string code = string.Join("\n", console.lines);
        var inputStream = new AntlrInputStream(code);
        var lexer = new JavaLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();

        foreach (var token in tokenStream.GetTokens())
        {
            if (token.Type == TokenConstants.EOF) continue;

            Color targetColor = defaultColor;

            if (KEYWORDS.Contains(token.Type)) targetColor = keywordColor;
            else if (TYPES.Contains(token.Type)) targetColor = typeColor;
            else if (token.Type == JavaLexer.STRING_LITERAL || token.Type == JavaLexer.BOOL_LITERAL) targetColor = literalColor;
            else if (token.Type == JavaLexer.COMMENT || token.Type == JavaLexer.LINE_COMMENT) targetColor = commentColor;
            else if (token.Type == JavaLexer.IDENTIFIER) targetColor = identifierColor;

            PaintToken(console, token, targetColor);
        }
    }
}