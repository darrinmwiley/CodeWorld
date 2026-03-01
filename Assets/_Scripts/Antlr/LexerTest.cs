using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antlr4.Runtime;

class LexerTest : MonoBehaviour
{
    public void Start()
    {
        var input = new AntlrInputStream("public class MyClass { \n public int print(){//this function returns 5\n/*multi\nline\ncomment*/\n String bla = \"bla\";\nreturn 5;} }");
        var lexer = new CSharpLexer(input);

        CommonTokenStream tokens = new CommonTokenStream(lexer);

        tokens.Fill();
        foreach (var token in tokens.GetTokens())
        {
            Debug.Log($"{CSharpLexer.DefaultVocabulary.GetSymbolicName(token.Type)}: {token.Text}");
        }
    }
}