using static AgnostosCompilator.Example.Token;
using static AgnostosCompilator.Example.NonTerminal;
using System.Globalization;
using System.Collections.Generic;
using System;


namespace AgnostosCompilator.Example{
    public enum Token{NUMBER,PLUS,MINUS,MULT,DIVIDE,OPEN_B,CLOSE_B,ABS,FUNC}
    public enum NonTerminal{E,T,F}
    public class Program{
        public static void Main(string[] args){
            Console.WriteLine("creating lexer..."); //a compiler is made of 2 parts: the lexer and the parser
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
            Console.WriteLine("creating parser..."); //then the parser is initialized with the grammar
            Parser<NonTerminal,Token> parser = new(ParserType.LR1,E,
            /*  the following rules correspond to this grammar:
                start: E
                E -> E+T | E-T | T
                T -> T*F | T/F | F
                F -> (E) | |E| | f(E) | n
            */
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
            Console.WriteLine("\nstart parsing");

            
            string example = "sqrt(3*3 + 4.0*4.0)";
            double? result = parser.parse(lexer.parse(example)) as double?; //note: parsing and tokenizing are made toghether, each token, right after been recognized, il already sent into the parser
            Console.WriteLine($"\nthe result of the expression {example} is {result}");

            Console.WriteLine("\nhere will be a lexical error");
            try{
                string exampleLexicalError = "2*5&7";
                result = parser.parse(lexer.parse(exampleLexicalError)) as double?; //the lexer would give error once it reach '&' , because it's not defined
                Console.WriteLine($"the result of the expression {exampleLexicalError} is {result}");
            }catch(LexicalErrorException E){
                Console.WriteLine(E.Message);
            }
            Console.WriteLine("\nhere will be a syntax error");
            try{
                string exampleSyntaxError = "2*3+4)";
                result = parser.parse(lexer.parse(exampleSyntaxError)) as double?; //the parser would give error once it reach ')' , because the bracket never open before
                Console.WriteLine($"the result of the expression {exampleSyntaxError} is {result}");
            }catch(SyntaxErrorException E){
                Console.WriteLine(E.Message);
            }
            Console.WriteLine("\nhere will be a semantic error");
            try{
                string exampleSemanticError = "2+3*sqr(2)/2";
                result = parser.parse(lexer.parse(exampleSemanticError)) as double?; //the parser would give error once it reach 'sqr' , because this function is not defined (it should be "sqrt")
                Console.WriteLine($"the result of the expression {exampleSemanticError} is {result}");
            }catch(SemanticErrorException E){
                Console.WriteLine(E.Message);
            }

            result = parser.parse(customLexer(5)) as double?; //you can also use custom lexer
            Console.WriteLine($"\nthe result of the custom expression is {result}");
            //note: parsing and tokenizing are made toghether, each token, right after been recognized, il already sent into the parser
        }

        private static IEnumerable<MacroToken<Token>> customLexer(int n){ //this create a list of numbers from 1 to n all multiplied together, which correspond to the factorial function, in this case it will create the token sequence 1*2*3*4*5
            yield return new MacroToken<Token>(NUMBER,1.0,Extremes.nullExtremes); //extremes are not necessary, if you don't need them
            for(int i=2;i<=n;++i){
                yield return new MacroToken<Token>(MULT,null,Extremes.nullExtremes);
                yield return new MacroToken<Token>(NUMBER,(double) i,new Extremes(i,i+1)); //or, if you need them, just create them with the start index and the end index (which is NOT the index of the last character, but the index right after)
            }
        }

    }

}

