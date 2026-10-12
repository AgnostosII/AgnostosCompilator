using System;
using System.Collections.Generic;


namespace AgnostosCompilator{
    internal abstract class Action<N,T>where N:Enum where T:Enum{
        internal abstract bool execute(Stack<int> states,Stack<Symbol<N,T>> symbols,Dictionary<Symbol<N,T>, int>[] goToTable,Symbol<N,T> S);//last argument is a terminal
        internal abstract bool executeLast(Stack<int> states, Stack<Symbol<N, T>> symbols, Dictionary<Symbol<N, T>, int>[] goToTable);
    }
    internal class Shift<N,T>:Action<N,T>where N:Enum where T:Enum{
        private int _state;
        internal Shift(int state){
            _state = state;
        }
        public override string ToString(){
            return $"S{_state}";
        }
        internal override bool execute(Stack<int> states,Stack<Symbol<N,T>> symbols,Dictionary<Symbol<N,T>, int>[] goToTable,Symbol<N,T> S){
            symbols.Push(S);
            states.Push(_state);
            return false;
        }
        internal override bool executeLast(Stack<int> states, Stack<Symbol<N, T>> symbols, Dictionary<Symbol<N, T>, int>[] goToTable){
            throw new SyntaxErrorException("There are still symbols left to read");
        }
        public override bool Equals(object obj) => obj is Shift<N,T> other && _state == other._state;
        public override int GetHashCode() => _state.GetHashCode();

    }
    internal class Reduce<N,T>:Action<N,T>where N:Enum where T:Enum{
        private GrammarRule<N, T> rule;
        internal Reduce(GrammarRule<N,T> rule){
            this.rule = rule;
        }
        public override string ToString() => rule.ToString();

        internal override bool execute(Stack<int> states,Stack<Symbol<N,T>> symbols,Dictionary<Symbol<N,T>, int>[] goToTable,Symbol<N,T> S){
            return executeLast(states,symbols,goToTable);
        }
        internal override bool executeLast(Stack<int> states,Stack<Symbol<N,T>> symbols,Dictionary<Symbol<N,T>, int>[] goToTable){
            Symbol<N,T> R = rule.sauce;
            R.setArguments(rule.apply(states,symbols,out Extremes extremes),extremes);
            symbols.Push(R);
            states.Push( goToTable[states.Peek()][R] );
            return true;
        }
        public override bool Equals(object obj) => obj is Reduce<N,T> other && rule==other.rule;
        public override int GetHashCode() => rule.GetHashCode();

    }
    internal class Accept<N,T>:Action<N,T>where N:Enum where T:Enum{
        public override string ToString() => "A";
        internal override bool execute(Stack<int> states,Stack<Symbol<N,T>> symbols,Dictionary<Symbol<N,T>, int>[] goToTable,Symbol<N,T> S){
            throw new SyntaxErrorException("Unexpected end");
        }
        internal override bool executeLast(Stack<int> states,Stack<Symbol<N,T>> symbols,Dictionary<Symbol<N,T>, int>[] goToTable){
            return false;
        }
        public override bool Equals(object obj) => obj is Accept<N,T>;
        public override int GetHashCode() => -1;

    }
}