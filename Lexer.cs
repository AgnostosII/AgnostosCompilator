using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AgnostosCompilator{
    public class Lexer<Token>{
        private readonly Regex[] rules;
        private readonly Token[] tokenList;
        private readonly Argumenter[] argumenters;
        private readonly Regex toIgnore;
        public delegate object Argumenter(string arg);
        private Lexer(Regex toIgnore,int length,object obj){
            if(length==0) throw new ArgumentException("At least one rule is needed",nameof(obj));
            tokenList = new Token[length];
            rules = new Regex[length];
            argumenters = new Argumenter[length];
            this.toIgnore = toIgnore;
        }
        public Lexer(Regex toIgnore = null,params (Token,string,Argumenter)[] rules):this(toIgnore,rules.Length,rules){
            for(int i=0;i<rules.Length;++i){
                if(string.IsNullOrEmpty(rules[i].Item2)) throw new ArgumentException($"Rule {i} has an empty pattern.");
                tokenList[i] = rules[i].Item1;
                this.rules[i] = new Regex(rules[i].Item2,RegexOptions.Compiled);
                argumenters[i] = rules[i].Item3;
            }
        }
        public Lexer(Regex toIgnore = null,params (Token,Regex,Argumenter)[] rules):this(toIgnore,rules.Length,rules){
            for(int i=0;i<rules.Length;++i){
                tokenList[i] = rules[i].Item1;
                this.rules[i] = rules[i].Item2;
                argumenters[i] = rules[i].Item3;
            }
        }
        public IEnumerable<MacroToken<Token>> parse(string input){
            int position = 0;
            while(position<input.Length){
                Match match = null;
                int ruleIndex = -1;
                int length = 0;
                for(int i=0;i<tokenList.Length;++i){
                    Match current = rules[i].Match(input,position);
                    if(!current.Success || current.Index!=position || current.Length<=length) continue;
                    if(current.Length==0) throw new InvalidOperationException($"A token rule matched an empty string at position {position}");
                    length = current.Length;
                    ruleIndex = i;
                    match = current;
                }
                if(toIgnore!=null){
                    Match current = toIgnore.Match(input,position);
                    if(current.Success && current.Index==position || current.Length>length){
                        if(current.Length==0) throw new InvalidOperationException($"A token rule matched an empty string at position {position}");
                        length = current.Length;
                        ruleIndex = tokenList.Length;
                        match = current;
                    }
                }
                if(ruleIndex==-1) throw new LexicalErrorException($"Unexpected character '{input[position]}' at position {position}");
                if(ruleIndex < tokenList.Length){ //else, the match is supposed to be ignored, for example spaces
                    object arg = argumenters[ruleIndex] is Argumenter argumenter?argumenter(input.Substring(position,match.Length)):null;
                    yield return new MacroToken<Token>(tokenList[ruleIndex],arg,new Extremes(position,position+match.Length));
                }
                position += match.Length;
            }
        }
    }
}