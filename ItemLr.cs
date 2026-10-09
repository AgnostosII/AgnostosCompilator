using System;
using System.Collections.Generic;

namespace AgnostosCompilators{
    internal abstract class ItemLr<N,T>where N:Enum where T:Enum{
        private AbstractGrammarRule<N,T> _rule; internal AbstractGrammarRule<N,T> rule => _rule;
        private int count;
        private int symbolNumber;
        internal ItemLr(AbstractGrammarRule<N,T> rule){
            _rule = rule;
            count = 0;
            if(rule is GrammarRule<N,T>) symbolNumber = _rule.symbolNumber;
            else symbolNumber = 1;
        }








        public ItemLr0<N,T> toLr0(){
            ItemLr0<N, T> item = new ItemLr0<N, T>(_rule){
                count = count
            };
            return item;
        }

        public override string ToString() => _rule.ToString(count);

        internal IEnumerable<Symbol<N,T>> getFollowingSymbols(){
            List<Symbol<N,T>> symbols = new List<Symbol<N,T>>();
            for(int i=count+1;i<rule.symbolNumber;++i) symbols.Add(rule.getSymbol(i));
            return symbols;
        }
        
        internal Symbol<N,T> getSymbol() => _rule.getSymbol(count);

        internal bool ended => count == symbolNumber;


        protected abstract ItemLr<N, T> clone();
        internal ItemLr<N,T> next(){
            if(count>=symbolNumber) throw new ItemLrEndedException();
            ItemLr<N,T> item = clone();
            item.count = count + 1;
            return item;
        }
        public override bool Equals(object obj) => obj is ItemLr<N,T> other && count==other.count && _rule==other._rule;
        
        public static bool operator==(ItemLr<N,T> a,ItemLr<N,T> b) => a.Equals(b);

        public static bool operator!=(ItemLr<N,T> a,ItemLr<N,T> b) => !a.Equals(b);

        public override int GetHashCode() => _rule.GetHashCode()*symbolNumber+count;

    }
    
    
    internal class ItemLr0<N,T>:ItemLr<N,T>where N:Enum where T:Enum{
        internal ItemLr0(AbstractGrammarRule<N,T> rule):base(rule){}
        protected override ItemLr<N, T> clone() => new ItemLr0<N,T>(rule);
    }
    internal class ItemLr1<N,T>:ItemLr<N,T>where N:Enum where T:Enum{
        private Symbol<N,T> _lookup; public Symbol<N,T> lookup => _lookup;
        internal ItemLr1(AbstractGrammarRule<N,T> rule,Symbol<N,T> lookupSymbol):base(rule){
            _lookup = lookupSymbol;
        }
        protected override ItemLr<N,T> clone() => new ItemLr1<N,T>(rule,_lookup);

        public override bool Equals(object obj) => obj is ItemLr1<N,T> other && _lookup==other._lookup && base.Equals(obj);


        public override int GetHashCode() => base.GetHashCode()+_lookup.GetHashCode()*4999;

    }
}