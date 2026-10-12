using System;


namespace AgnostosCompilator{
    
    internal struct Symbol<N,T>: IEquatable<Symbol<N,T>> where N:Enum where T:Enum{
        private object _return;
        private Extremes _extremes;
        internal object arg => _return;
        internal Extremes extremes => _extremes;
        internal void setArguments(object arg,Extremes extremes){
            _return = arg;
            _extremes = extremes;
        }
        private N nonTerminal;
        private T terminal;
        internal readonly bool isTerminal;
        private bool special;
        internal static Symbol<N,T> startState => new Symbol<N,T>(false);
        internal static Symbol<N,T> finalChar => new Symbol<N,T>(true);
        internal Symbol(N arg){
            nonTerminal = arg;
            terminal = default;
            isTerminal = false;
            _return = null;
            special = false;
            _extremes = new Extremes(0,0);
        }
        internal Symbol(T arg){
            nonTerminal = default;
            terminal = arg;
            isTerminal = true;
            _return = null;
            special = false;
            _extremes = new Extremes(0,0);
        }
        private Symbol(bool isTerminal){
            this.isTerminal = isTerminal;
            terminal = default;
            nonTerminal = default;
            _return = null;
            special = true;
            _extremes = new Extremes(0,0);
        }
        public readonly override int GetHashCode(){
            if(special)     return isTerminal ? -1 : -2;
            if(isTerminal)  return terminal.GetHashCode();
            else            return nonTerminal.GetHashCode();
        }
        public readonly override string ToString(){
            if(special){
                if(isTerminal) return "$";
                return "Start";
            }
            if(isTerminal) return terminal.ToString();
            return nonTerminal.ToString();
        }
        public readonly bool Equals(Symbol<N, T> other){
            if(special){
                if(!other.special) return false;
                return isTerminal == other.isTerminal;
            }
            if(other.special) return false;
            if(isTerminal){
                return other.isTerminal && terminal.Equals(other.terminal);
            }
            return (!other.isTerminal) && nonTerminal.Equals(other.nonTerminal);
        }
        public readonly override bool Equals(object obj){
            if(obj is Symbol<N,T> other){
                return Equals(other);
            }
            if(obj is T t){
                return isTerminal && terminal.Equals(t);
            }
            if(obj is N nt){
                return (!isTerminal) && nonTerminal.Equals(nt);
            }
            return false;
        }
        public static bool operator==(Symbol<N,T> a,Symbol<N,T> b) => a.Equals(b);
        public static bool operator!=(Symbol<N,T> a,Symbol<N,T> b) => !a.Equals(b);
        public static bool operator==(Symbol<N,T> a,T b) => a.Equals(b);
        public static bool operator!=(Symbol<N,T> a,T b) => !a.Equals(b);
        public static bool operator==(Symbol<N,T> a,N b) => a.Equals(b);
        public static bool operator!=(Symbol<N,T> a,N b) => !a.Equals(b);
    }
}