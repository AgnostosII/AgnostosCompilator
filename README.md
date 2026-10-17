# AgnostosCompilator
A C# library implementing LR-family parser and lexer generators in runtime.
## Overview
AgnostosCompilator is a library that implements both a lexer (where each token is identified from a regex) and a parser (from a context-free grammar) and gives semantic actions 

Originally developed for use in a Unity project, the library was designed to remain independent of the Unity Engine, making its core functionality reusable in other C# applications.

## Features
- **Lexer** that use regex ro recognize each token every token from a string, including a regex of tokens to ignore (for example white spaces). The lexer CANNOT recognize empty strings, even if a regex could produce it.
- **LR parsing algorithms:** LR(0), SLR and LR(1) (LALR algorithm is not yet implemented)
- **Error handling:** for both parser conflicts (at the creation of the parser) and lexical and syntax errors (at the parsing of a string)

## Technologies
- C# 8.0
- .NET Standard 2.0

## Quick Start
You can download the library as a dll file with this link https://github.com/AgnostosII/AgnostosCompilator/releases/tag/v1.0.0

Here there's a quick example on how to make a compiler:
```C#
using AgnostosCompilator;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public enum Token{a,b}
public enum NonTerminal{S,A}

public class Program{
    public static void Main(string args){
        //lexer creation
        Lexer<Token> lexer = new(
            new Regex(@"\s"),
            (Token.a, @"a",null),
            (Token.b, @"b",null)
        );
        //parser creation
        Parser<NonTerminal,Token> parser = new(ParserType.LR1,NonTerminal.S,
            new(NonTerminal.S , NonTerminal.A,                  (S)=>S),
            new(NonTerminal.A , Token.a,NonTerminal.A,Token.b,  (a,A,b)=>A.get<int>()+1),
            new(NonTerminal.A , Token.a,Token.b,                (a,b)=>1)
        );
        //creating an example string ready to be parsed;
        string example = "aaabbb";
        //lexical analysis
        IEnumerable<MacroToken<Token>> tokenList = lexer.parse(example);
        //parsing
        int? result = parser.parse(tokenList) as int?;
        //print result
        Console.WriteLine($"There are {result} repetitions");
    }
}
```

## How to use
Note that you can download the file Example.cs in this repository to have the full example written in one single file

### Lexer creation
You first need to create an enum to represent your tokens, in this example:
```C#
public enum Token{NUMBER,PLUS,MINUS,MULT,DIVIDE,OPEN_B,CLOSE_B,ABS,FUNC}
```
Then, you can use it to make your lexer, the constructor use the token type as generic parameter, then the first argument (optional) is a regex on which characters are supposed to be ignored, then write a tuple for each rule. The tuple has the following parameters:
- **Token:** It needs to be recognized (from the token type used as generic)
- **Regex:** Can either be a string, or directly an object of the class Regex
- **Function:** Accept a string as parameter (which corresponds to the string that match the regex) and return any object you want as the argument of that token
The following code is an example on how to write a lexer
```C#
Lexer<Token> lexer = new Lexer<Token>(  //the lexer is used to create a list of tokens from the input string
                                        //in this case, this lexer recognize numbers, each operator, and words used for functions
    new System.Text.RegularExpressions.Regex(@"\s"), //the first regex is used to know which token ignore (like spaces, that usually just fill the void between real tokens)
    (NUMBER,    @"[0-9]+",                  (s) => (double) int.Parse(s,CultureInfo.InvariantCulture)),
    (NUMBER,    @"[0-9]+\.[0-9]+",          (s) => double.Parse(s,CultureInfo.InvariantCulture)),
    (NUMBER,    @"\.[0-9]+",                (s) => double.Parse("0"+s,CultureInfo.InvariantCulture)),
    (NUMBER,    @"pi?",                     (s)=>Math.PI),  //match the value of pi , approximately 3.14
    (NUMBER,    @"e",                       (s)=>Math.E),   //match the value of euler number , approximately 2.72
    (PLUS,      @"\+",                      null),          //operators don't need to bring any value because the information of which operator is already enough
    (MINUS,     @"\-",                      null),
    (MULT,      @"\*",                      null),
    (DIVIDE,    @"\/",                      null),
    (OPEN_B,    @"\(",                      null),
    (CLOSE_B,   @"\)",                      null),
    (ABS,       @"\|",                      null),  //match the absolute value
    (FUNC,      @"[A-Za-z][A-Za-z0-9]*",    (s)=>s) //match any 1 parameter function by its name, what means each name is written in the parser
);
```
### How to use the lexer
The only function defined in the lexer is *parse*, which require a string as parameter (the string you want to be tokenized) and it return an IEnumerable of *Macrotoken*, a wrapper class that, for each recognized token, stores the following informations:
- Which token is
- Its argument (the returning object of the function you write for that token)
- the start index of the token in the original string
- the end index of the token, which is exactly the index right after the last character of the token
The following code explain how to use the lexer:
```C#
string example = "sqrt(3*3 + 4.0*4.0)";
IEnumerable<MacroToken<Token>> tokenList = lexer.parse(example);
try{
    //the following code is a way to read each information from each token
    foreach(MacroToken<Token> macroToken in tokenList){
        Console.WriteLine(
            $"token: {macroToken.symbol}\narg: {macroToken.arg}\nstart: {macroToken.extremes.start}\nend: {macroToken.extremes.end}\n"
        );
    }
}catch(LexicalErrorException E){
    Console.WriteLine(E.Message);
}
```
As you can see, if the string is not valid, the iteration will throw a *LexicalErrorException*

### Parser creation
A parser needs a grammar, and a grammar needs a set of non terminals. In this example:
```C#
public enum NonTerminal{E,T,F}
```
Then, you can use both enums as generics to make your parser (the non terminals' enum BEFORE the terminals' one). Note that enums should be different, or else you cannot distinguish between a terminal and a non terminal.

Parameters of the Parser are:
- **Parser Type:** Which can be either LR0, SLR, LALR (i will gives you a *NotImplementedException* for now), and LR1
- **Start symbol:** The non terminal that can generate the entire grammar
- **Production rules:** All production rules, which are objects of the class *GrammarRule*, and each rule require the following parameters:
    - **Left-Hand Side:** The non terminal that generate the production
    - **Right-Hand Side:** The string of terminals and non terminals generated by this rule
    - **Semantic Action:** The function that return the object attached to the non terminal, it contains a parameter of reach symbol in the **Right-Hand Side:** of the type *Wrp*, which is simply a wrapper for the argument of the symbol, to get the actual object you pass, use the function *get*, that works both in the generic and non generic version

The following code is an example on how to write a parser
```C#
try{
    Parser<NonTerminal,Token> parser = new(ParserType.LR1,E,
        new(E,  E,PLUS,T,               (a,op,b) => a.get<double>()+b.get<double>()),
        new(E,  E,MINUS,T,              (a,op,b) => a.get<double>()-b.get<double>()),
        new(E,  T,                      (n)=>n),
        new(T,  T,MULT,F,               (a,op,b) => a.get<double>()*b.get<double>()),
        new(T,  T,DIVIDE,F,             (a,op,b) => a.get<double>()/b.get<double>()),
        new(T,  F,                      (n)=>n),
        new(F,  OPEN_B,E,CLOSE_B,       (open,n,close) => n), //you can either returning the object of the right type you want, or returning the wrapper, that will be unwrapped implicitly
        new(F,  ABS,E,ABS,              (open,n,close) => Math.Abs(n.get<double>())),
        new(F,  FUNC,OPEN_B,E,CLOSE_B,  (func,open,n,close) => {
                                            double arg = n.get<double>();
                                            switch(func.get<string>()){
                                                case "sqrt":    return Math.Sqrt(arg);
                                                case "sin":     return Math.Sin(arg);
                                                case "cos":     return Math.Cos(arg);
                                                case "tan":     return Math.Tan(arg);
                                                case "ln":      return Math.Log(arg);
                                            }
                                            throw new SemanticErrorException(
                                                $"Function \"{func.get<string>()}\" from position {func.l} to {func.r} is not recognized"
                                                //the values 'l' and 'r' tells you the index when the element start (in the original string) and the character right after, if you want the length, use 
                                                //'r' value is the character index right after the element
                                            );
                                        }),
        new(F,  NUMBER,                 (n)=>n)
    );
}catch(ConflictException E){
    Console.WriteLine(E.Message);
}
```
As you can see, if the grammar is not of the choosen parser type, it will give a *ConflictException*

### How to use the parser
The main function of the parser is *parse*, which require as parameter an object of the class *IEnumerable\<MacroToken\<Token\>\>* (the same class returned by the *parse* function of the lexer). The function returns an object of the class *object*, which can be converted into the actual type of the object, here the example on oh to use the parser:
```C#
try{
    object obj = parser.parse(tokenList); //remember tokenList is an object of the class IEnumerable<MacroToken<Token>>
    double? result = obj as double?;
    Console.WriteLine($"\nthe result of the expression {example} is {result}");
}catch(SyntaxErrorException E){
    Console.WriteLine(E.Message);
}catch(SemanticErrorException E){
    Console.WriteLine(E.Message);
}
```

*SyntaxErrorException* is thrown by the parser itself if there's a syntax error, but no semantic error will be thrown by the parser, you should thrown a *SemanticErrorException* inside the function of any semantic action you want.

Note that you're not forced to use the parser with the lexer of this library, you could make your custom lexer as well like in this example:
```C#
private static IEnumerable<MacroToken<Token>> customLexer(int n){   //this create a list of numbers from 1 to n
                                                                    //all multiplied together, which correspond
                                                                    //to the factorial function, in this case,
                                                                    //it will create the token sequence 1*2*3*4*5
    yield return new MacroToken<Token>(NUMBER,1.0,Extremes.nullExtremes); //extremes are not necessary, if you don't need them
    for(int i=2;i<=n;++i){
        yield return new MacroToken<Token>(MULT,null,Extremes.nullExtremes);
        yield return new MacroToken<Token>(NUMBER,(double) i,new Extremes(i,i+1));
        //if you need to write extremes, just create them with the start index and the end index (which is NOT the index of the last character, but the index right after)
    }
}
```
in this case, you'll need to properly create each object of the class *MacroToken*

