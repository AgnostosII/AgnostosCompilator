using System;
using System.Collections.Generic;
using System.Text;

namespace AgnostosCompilators{
    internal class itemStateLr<N,T>where N:Enum where T:Enum{
        private ISet<ItemLr<N,T>> kernel;
        private ISet<ItemLr<N,T>> allItem;
        private ISet<ItemLr<N, T>> _terminalItem;
        private ISet<Symbol<N,T>> _validSymbols; internal IEnumerable<Symbol<N,T>> validSymbols => _validSymbols;

        internal Dictionary<Symbol<N,T>,itemStateLr<N,T>> transictions = new Dictionary<Symbol<N, T>, itemStateLr<N, T>>();
        private int _index; internal int index => _index;
        
#region RELATED_TO_PARSER
            private Parser<N,T> parser;
            private Dictionary<Symbol<N,T>, ISet<AbstractGrammarRule<N,T>>> startingRulesForSymbols => parser.startingRulesForSymbols;
            private ParserType parserType => parser.parserType;
            private ISet<Symbol<N,T>> first(Symbol<N,T> S) => parser.first(S);

            private bool canGenerateNull(Symbol<N,T> S) => parser.canGenerateNull(S);
#endregion

        public override string ToString(){
            StringBuilder sb = new StringBuilder("Item Lr state:");
            foreach(ItemLr<N,T> I in allItem) sb.Append("\n "+I.ToString());
            return sb.ToString();
        }
        

        public static bool operator==(itemStateLr<N,T> a,itemStateLr<N,T> b){
            return a.Equals(b);
        }
        public static bool operator!=(itemStateLr<N,T> a,itemStateLr<N,T> b){
            return !a.Equals(b);
        }
        public static bool operator==(itemStateLr<N,T> a,IEnumerable<ItemLr<N,T>> b){
            return a.Equals(b);
        }
        public static bool operator!=(itemStateLr<N,T> a,IEnumerable<ItemLr<N,T>> b){
            return !a.Equals(b);
        }
        public override bool Equals(object obj){
            if(obj is itemStateLr<N,T> other){
                return kernel.SetEquals(other.kernel);
            }
            if(obj is IEnumerable<ItemLr<N,T>> other2){
                return kernel.SetEquals(other2);
            }
            return false;
        }
        public override int GetHashCode(){
            int hash = 0;
            foreach(ItemLr<N,T> item in kernel) hash = hash * 31 + item.GetHashCode();

            return hash;
        }
        internal bool equalsIfLr0(itemStateLr<N,T> altro) => kernelLr0.SetEquals(altro.kernelLr0);

        private ISet<ItemLr<N,T>> _kernelLr0;
        private ISet<ItemLr<N,T>> kernelLr0{get{
            if(_kernelLr0==null){
                _kernelLr0 = new HashSet<ItemLr<N,T>>();
                foreach(ItemLr<N,T> I in kernel) _kernelLr0.Add(I.toLr0());
            }
            return _kernelLr0;
        }}
        
        
        internal itemStateLr(
            ISet<ItemLr<N,T>> kernel,
            int stateIndex,
            Parser<N,T> parser
        ){
            if(kernel.Count==0) throw new ArgumentException("kernel vuoto");
            _index = stateIndex;
            this.kernel = kernel;
            this.parser = parser;

            allItem = new HashSet<ItemLr<N,T>>(kernel); //all items in the state, both from kernel and added


            ControlledList<ItemLr<N,T>> simboli = new ControlledList<ItemLr<N,T>>(); //non terminali da aggiungere
            foreach(ItemLr<N,T> I in kernel){
                if( !(I.ended || I.getSymbol().isTerminal) ){ simboli.add(I); }
            }
            
            while(simboli.stillToVerify){
                ItemLr<N,T> itemBase = simboli.pop();
                Symbol<N,T> S = itemBase.getSymbol();
                foreach(AbstractGrammarRule<N,T> R in startingRulesForSymbols[S]){
                    if( parserType==ParserType.LR0 || parserType==ParserType.SLR ){
                        ItemLr<N,T> I = new ItemLr0<N,T>(R);
                        allItem.Add(I);
                        if( !(I.ended || I.getSymbol().isTerminal) ) simboli.add(I);
                    }else if(parserType==ParserType.LALR||parserType==ParserType.LR1){
                        ISet<Symbol<N,T>> insiemeFirst=new HashSet<Symbol<N,T>>();
                        bool proseguendo = true;
                        foreach(Symbol<N,T> iesimo in itemBase.getFollowingSymbols()){
                            insiemeFirst.UnionWith(first(iesimo));
                            if(!canGenerateNull(iesimo)){
                                proseguendo = false;
                                break;
                            }
                        }
                        if(proseguendo){
                            insiemeFirst.Add((itemBase as ItemLr1<N,T>).lookup);
                        }
                        foreach(Symbol<N,T> iesimo in insiemeFirst){
                            ItemLr<N,T> I = new ItemLr1<N,T>(R,iesimo);
                            allItem.Add(I);
                            if( !(I.ended || I.getSymbol().isTerminal) ) simboli.add(I);
                        }
                    }else{
                        throw new NotImplementedException();
                    }
                }
            }
            _validSymbols = new HashSet<Symbol<N,T>>(); //array dei simboli da cui può partire una transizione in un altro stato
            _terminalItem = new HashSet<ItemLr<N, T>>();
            foreach(ItemLr<N,T> I in allItem){ //verifica quali item sono stati completati per riportare il reduce
                if(I.ended){
                    _terminalItem.Add(I);
                }else{
                    _validSymbols.Add(I.getSymbol());
                }
            }
        }
        internal itemStateLr(
            ItemLr<N,T> kernel,
            int indiceStato,
            Parser<N,T> parser
        ):this(
            new HashSet<ItemLr<N,T>>(new ItemLr<N,T>[]{kernel}),
            indiceStato,
            parser
        ){}
        
        internal IEnumerable<ItemLr<N,T>> itemTerminati{get{
            return _terminalItem;
        }}
        
        internal ISet<ItemLr<N,T>> itemPassati(Symbol<N,T> S){
            ISet<ItemLr<N,T>> ritorno = new HashSet<ItemLr<N,T>>();
            foreach(ItemLr<N,T> I in allItem){
                if( (!I.ended) && I.getSymbol().Equals(S) ){
                    ritorno.Add(I.next());
                }
            }
            return ritorno;
        }
    }
}