using System;
using System.Text;
using System.Collections.Generic;



namespace AgnostosCompilator{
    public class Wrp{
        private readonly object obj;
        public readonly Extremes extremes;
        public int l => extremes.start;
        public int r => extremes.end;
        public int size => extremes.length;
        internal Wrp(object obj,Extremes extremes){
            this.obj = obj;
            this.extremes = extremes;
        }
        internal object get() => obj;
        public T get<T>(){
            if(this.obj==null) return default;
            if(this.obj is T obj) return obj;
            throw new ArgumentException($"Invalid type: expected {this.obj.GetType()}, found {typeof(T)}");
        }
        public bool tryGet<T>(out T value){
            if(this.obj is T obj){
                value = obj;
                return true;
            }
            value = default;
            return false;
        }
        public bool isNull => obj==null;
    }
    public abstract class AbstractGrammarRule<N,T>where N:Enum where T:Enum{
        internal abstract int symbolNumber{ get; }
        internal abstract Symbol<N,T> getSymbol(int n);
        internal abstract Symbol<N,T> sauce{ get; }
        internal abstract string ToString(int arrivato);
        internal bool emptyProduction => symbolNumber == 0;
    }
    public class StartingGrammarRule<N,T>:AbstractGrammarRule<N,T>where N:Enum where T:Enum{
        private N symbol;
        internal StartingGrammarRule(N symbol){
            this.symbol = symbol;
        }
        internal override int symbolNumber => 2;

        internal override Symbol<N,T> getSymbol(int n){
            if(n == 0) return new Symbol<N,T>(symbol);
            if(n == 1) return Symbol<N,T>.finalChar;
            throw new IndexOutOfRangeException($"cannot read more symbols, only readable 0 and 1, instead of {n}");
        }
        internal override string ToString(int count){
            if(count==0) return Symbol<N,T>.startState.ToString()+" -> * "+symbol.ToString();
            if(count==1) return Symbol<N,T>.startState.ToString()+" ->  "+symbol.ToString()+" *";
            throw new IndexOutOfRangeException($"cannot read more symbols, only readable 0 and 1, instead of {count}");
        }
        public override string ToString(){
            return Symbol<N,T>.startState.ToString()+" -> "+symbol.ToString();
        }
        internal override Symbol<N,T> sauce => Symbol<N,T>.startState;
        public override int GetHashCode() => 0;

    }
    public class GrammarRule<N,T>:AbstractGrammarRule<N,T>where N:Enum where T:Enum{
        private N _sauce;
        private Symbol<N,T>[] production;
        private Function function;
        public delegate object Function(Wrp[] O);
        public delegate object Function0();
        public delegate object Function1(Wrp arg0);
        public delegate object Function2(Wrp arg0,Wrp arg1);
        public delegate object Function3(Wrp arg0,Wrp arg1,Wrp arg2);
        public delegate object Function4(Wrp arg0,Wrp arg1,Wrp arg2,Wrp arg3);
        public delegate object Function5(Wrp arg0,Wrp arg1,Wrp arg2,Wrp arg3,Wrp arg4);
        public delegate object Function6(Wrp arg0,Wrp arg1,Wrp arg2,Wrp arg3,Wrp arg4,Wrp arg5);
        public delegate object Function7(Wrp arg0,Wrp arg1,Wrp arg2,Wrp arg3,Wrp arg4,Wrp arg5,Wrp arg6);
        public delegate object Function8(Wrp arg0,Wrp arg1,Wrp arg2,Wrp arg3,Wrp arg4,Wrp arg5,Wrp arg6,Wrp arg7);
        
        
        public GrammarRule(N generatingSymbol, params Enum[] production) : this(
            (list) => { return null; }
        , generatingSymbol, production){ }
        public GrammarRule(Function function,N generatingSymbol,params Enum[] production){
            _sauce = generatingSymbol;
            this.production = new Symbol<N,T>[production.Length];
            for(int i=0;i<production.Length;++i){
                this.production[i] = nuovo(production[i]);
            }
            this.function = function;
        }
        public GrammarRule(N generatingSymbol,Function0 f):this(
            (args)=>f(),
        generatingSymbol){}

        public GrammarRule(N generatingSymbol,Enum der0,Function1 f):this(
            (args)=>f(args[0]),
        generatingSymbol,der0){}
        
        public GrammarRule(N generatingSymbol,Enum der0,Enum der1,Function2 f):this(
            (args)=>f(args[0],args[1]),
        generatingSymbol,der0,der1){}

        public GrammarRule(N generatingSymbol,Enum der0,Enum der1,Enum der2,Function3 f):this(
            (args)=>f(args[0],args[1],args[2]),
        generatingSymbol,der0,der1,der2){}

        public GrammarRule(N generatingSymbol,Enum der0,Enum der1,Enum der2,Enum der3,Function4 f):this(
            (args)=>f(args[0],args[1],args[2],args[3]),
        generatingSymbol,der0,der1,der2,der3){}

        public GrammarRule(N generatingSymbol,Enum der0,Enum der1,Enum der2,Enum der3,Enum der4,Function5 f):this(
            (args)=>f(args[0],args[1],args[2],args[3],args[4]),
        generatingSymbol,der0,der1,der2,der3,der4){} 

        public GrammarRule(N generatingSymbol,Enum der0,Enum der1,Enum der2,Enum der3,Enum der4,Enum der5,Function6 f):this(
            (args)=>f(args[0],args[1],args[2],args[3],args[4],args[5]),
        generatingSymbol,der0,der1,der2,der3,der4,der5){} 
        
        public GrammarRule(N generatingSymbol,Enum der0,Enum der1,Enum der2,Enum der3,Enum der4,Enum der5,Enum der6,Function7 f):this(
            (args)=>f(args[0],args[1],args[2],args[3],args[4],args[5],args[6]),
        generatingSymbol,der0,der1,der2,der3,der4,der5,der6){} 

        public GrammarRule(N generatingSymbol,Enum der0,Enum der1,Enum der2,Enum der3,Enum der4,Enum der5,Enum der6,Enum der7,Function8 f):this(
            (args)=>f(args[0],args[1],args[2],args[3],args[4],args[5],args[6],args[7]),
        generatingSymbol,der0,der1,der2,der3,der4,der5,der6,der7){} 


        
        internal override string ToString(int count){
            if(count>production.Length) throw new IndexOutOfRangeException($"cannot read more than or equal to {production.Length}, read {count}");
            StringBuilder sb = new StringBuilder(sauce.ToString()+" -> ");
            for(int i=0;i<count;++i) sb.Append($" {production[i]} ");

            sb.Append("*");
            for(int i=count;i<production.Length;++i) sb.Append($" {production[i]} ");

            return sb.ToString();
        }
        public override string ToString(){
            StringBuilder sb = new StringBuilder($"{sauce} ->");
            for(int i=0;i<production.Length;++i) sb.Append($" {production[i]}");

            return sb.ToString();
        }
        internal override int symbolNumber => production.Length;


        internal override Symbol<N,T> getSymbol(int n) => production[n];

        private static Symbol<N,T> nuovo(Enum arg){
            if(arg is T t) return new Symbol<N,T>(t);
            if(arg is N n) return new Symbol<N,T>(n);
            throw new ArgumentException($"symbol must be a token or a non terminal symbol, instead of type {arg.GetType()}");
        }
        internal override Symbol<N,T> sauce => new Symbol<N,T>(_sauce);
        
        
        
        internal object apply(Stack<int> states,Stack<Symbol<N,T>> symbols,out Extremes extremes){
            if(production.Length>symbols.Count) throw new SyntaxErrorException("Bad applied rule");
            Wrp[] ritorno = new Wrp[production.Length];
            extremes = Extremes.nullExtremes;
            for(int i=production.Length-1;i>=0;--i){
                Symbol<N,T> S = symbols.Pop();
                if( !production[i].Equals(S) ) throw new SyntaxErrorException("Bad applied rule");
                states.Pop();
                ritorno[i] = new Wrp( S.arg is Wrp wrp?wrp.get():S.arg , S.extremes );
                extremes = Extremes.union(extremes,S.extremes);
            }
            return function(ritorno);
        }
        public override int GetHashCode(){
            int hash = _sauce.GetHashCode();
            for( int i=0;i<production.Length;++i ){
                Symbol<N, T> S = production[i];
                hash = hash*17 + S.GetHashCode();
            }
            return hash;
        }
    }
}