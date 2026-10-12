using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AgnostosCompilator{
    public enum ParserType{LR0,SLR,LALR,LR1}

    

    public class Parser<N,T>where N:Enum where T:Enum{
        private static readonly int lengthColumn = 12;
        private Dictionary<Symbol<N,T>,Action<N,T>>[] actionTable;
        private Dictionary<Symbol<N,T>,int>[] goToTable;
        internal Dictionary<Symbol<N,T>, ISet<AbstractGrammarRule<N,T>>> startingRulesForSymbols;
        public readonly ParserType parserType;
        public int stateNumber => actionTable.Length;

        private ISet<Symbol<N,T>> notUsedymbolsInfo; public string printNotUsedSymbols(){
            StringBuilder sb = new StringBuilder();
            foreach(Symbol<N,T> S in notUsedymbolsInfo){
                sb.Append($"\n . {S.ToString()}");
            }
            return sb.ToString();
        }
        
    //cache temporanea
        private Dictionary<Symbol<N,T>, ISet<Symbol<N,T>>> _first;
        private Dictionary<Symbol<N,T>, ISet<Symbol<N,T>>> _follow;
        private List<string> foundConflicts;
        private ISet<Symbol<N,T>> nullGeneratingSymbols;
        internal bool canGenerateNull(Symbol<N,T> S) => nullGeneratingSymbols.Contains(S);

        
        internal ISet<Symbol<N,T>> first(Symbol<N,T> S) => _first[S];

        internal ISet<Symbol<N,T>> follow(Symbol<N,T> S) => _follow[S];



        
        private void clearCache(){
            _first =                    null;
            _follow =                   null;
            startingRulesForSymbols =   null;
            nullGeneratingSymbols =     null;
        }
        private void createCache(StartingGrammarRule<N,T> startingRule,GrammarRule<N,T>[] newRules){
            
            startingRulesForSymbols = new Dictionary<Symbol<N,T>,ISet<AbstractGrammarRule<N,T>>>();
            


            AbstractGrammarRule<N,T>[] rules = new AbstractGrammarRule<N,T>[newRules.Length+1];
            rules[0] = startingRule;
            for(int i=0;i<newRules.Length;++i){ //create list of all rules
                rules[i+1] = newRules[i];
            }

            foreach(AbstractGrammarRule<N,T> R in rules){ //each rule is group by its sauce non terminal
                ISet<AbstractGrammarRule<N,T>> ruleList;
                try{
                    ruleList = startingRulesForSymbols[R.sauce];
                }catch(KeyNotFoundException){
                    ruleList = new HashSet<AbstractGrammarRule<N,T>>();
                    startingRulesForSymbols[R.sauce] = ruleList;
                }
                ruleList.Add(R);
            }
            checkIfGrammarIsCompleted();
        }        
        
        
        
        private void calculateAllFirsts(){
            computeNullGeneratingSymbols();
            _first = new Dictionary<Symbol<N,T>, ISet<Symbol<N,T>>>();
            SelfCompattingCyclesGraph<Symbol<N,T>> graph = new SelfCompattingCyclesGraph<Symbol<N,T>>();
            foreach(T t in Enum.GetValues(typeof(T))){
                Symbol<N,T> S = new Symbol<N,T>(t);
                _first[S] = new HashSet<Symbol<N,T>> { S };
            }
            _first[Symbol<N,T>.finalChar] = new HashSet<Symbol<N,T>> { Symbol<N,T>.finalChar };
            foreach(Symbol<N,T> S in startingRulesForSymbols.Keys){
                if(S.isTerminal) throw new Exception("impossible");
                foreach(AbstractGrammarRule<N,T> R in startingRulesForSymbols[S]){
                    for(int i=0;i<R.symbolNumber;++i){
                        Symbol<N,T> iSymbol = R.getSymbol(i);
                        graph.addBow(iSymbol,S);
                        if(!canGenerateNull(iSymbol)) break;
                    }
                }
            }
            graph.executeFuncForEachMacronode((nodi)=>{
                if(nodi.First().isTerminal){
                    if(nodi.Count()==1) return;
                    throw new Exception("first of a terminal cannot be merge with other firsts");
                }
                ISet<Symbol<N,T>> union = new HashSet<Symbol<N,T>>();
                foreach(Symbol<N,T> S in nodi) _first[S] = union;
            });
            graph.executeFuncForEachBow((a,b)=>{
                if(b.isTerminal) throw new Exception("first of a terminal cannot be merge with other firsts");
                _first[b].UnionWith(_first[a]);
            });
        }

        
        

        private void computeAllFollows(){
            _follow = new Dictionary<Symbol<N,T>, ISet<Symbol<N,T>>>();
            SelfCompattingCyclesGraph<Symbol<N,T>> graph = new SelfCompattingCyclesGraph<Symbol<N,T>>();
            foreach(Symbol<N,T> S in startingRulesForSymbols.Keys){
                _follow[S] = new HashSet<Symbol<N,T>>();
            }
            foreach(Symbol<N,T> S in startingRulesForSymbols.Keys){
                foreach(AbstractGrammarRule<N,T> R in startingRulesForSymbols[S]){
                    if(R.emptyProduction) continue;
                    for(int i=1;i<R.symbolNumber;++i){
                        Symbol<N,T> previous = R.getSymbol(i-1); if(previous.isTerminal) continue;
                        Symbol<N,T> next = R.getSymbol(i);
                        
                        _follow[previous].UnionWith(first(next));
                        if(next.isTerminal || (!canGenerateNull(next))) continue;
                        graph.addBow(next,previous);
                    }
                    
                    Symbol<N,T> last = R.getSymbol(R.symbolNumber-1);
                    if(!last.isTerminal) graph.addBow(R.sauce,last);
                }
            }
            graph.executeFuncForEachMacronode((nodes)=>{
                ISet<Symbol<N,T>> union = new HashSet<Symbol<N,T>>();
                foreach(Symbol<N,T> S in nodes){
                    union.UnionWith(_follow[S]);
                    _follow[S] = union;
                }
            });
            graph.executeFuncForEachBow((a,b)=>{
                _follow[b].UnionWith(_follow[a]);
            });
        }
        
        
                
        
        
        
        
        
        
                
        
        
        
        
        
        
        
        private void computeNullGeneratingSymbols(){
            nullGeneratingSymbols =                         new HashSet<Symbol<N,T>>();
            ISet<Symbol<N,T>> notNullGeneratingSymbols =    new HashSet<Symbol<N,T>>();
            foreach(Symbol<N,T> S in startingRulesForSymbols.Keys){
                bool doesntGenerateNull = true;
                foreach(AbstractGrammarRule<N,T> R in startingRulesForSymbols[S]){
                    if(R.emptyProduction){
                        doesntGenerateNull = false;
                        nullGeneratingSymbols.Add(S);
                        break;
                    }
                }
                if(doesntGenerateNull){
                    notNullGeneratingSymbols.Add(S);
                }
            }
            bool change = true;
            while(change){
                change = false;
                ISet<Symbol<N,T>> temp = new HashSet<Symbol<N,T>>(notNullGeneratingSymbols);
                foreach(Symbol<N,T> S in temp){ //list of symbols that cannot be reduced
                    foreach(AbstractGrammarRule<N,T> R in startingRulesForSymbols[S]){
                        bool ruleCanGenerateNull = true;
                        for(int i=0;i<R.symbolNumber;++i){
                            Symbol<N,T> iSymbol = R.getSymbol(i);
                            if(iSymbol.isTerminal || notNullGeneratingSymbols.Contains(iSymbol)){
                                ruleCanGenerateNull = false;
                                break;
                            }
                        }
                        if(ruleCanGenerateNull){
                            nullGeneratingSymbols.Add(S);
                            notNullGeneratingSymbols.Remove(S);
                            change = true;
                        } 
                    }
                }
            }
        }
        
        
        
        
        
        
        
        
                
        
        
        
        private void checkIfGrammarIsCompleted(){
            notUsedymbolsInfo = new HashSet<Symbol<N,T>>();
            foreach(N n in Enum.GetValues(typeof(N))) notUsedymbolsInfo.Add(new Symbol<N,T>(n));
            foreach(T t in Enum.GetValues(typeof(T))) notUsedymbolsInfo.Add(new Symbol<N,T>(t));
            
            ControlledList<Symbol<N, T>> verifiedSymbols = new ControlledList<Symbol<N,T>>();
            verifiedSymbols.add(Symbol<N,T>.startState);
            while(verifiedSymbols.stillToVerify){
                Symbol<N, T> S = verifiedSymbols.pop();
                if(!startingRulesForSymbols.ContainsKey(S)){
                    throw new Exception("il simbolo "+S.ToString()+" non ha nessuna regola da cui parte");
                }
                foreach(AbstractGrammarRule<N,T> R in startingRulesForSymbols[S]){
                    for(int i=0;i<R.symbolNumber;++i){
                        Symbol<N,T> verified = R.getSymbol(i);
                        notUsedymbolsInfo.Remove(verified);
                        if(!verified.isTerminal) verifiedSymbols.add(verified);
                    }
                }
            }
            if(verifiedSymbols.length<startingRulesForSymbols.Keys.Count){
                List<Symbol<N,T>> toDelete = new List<Symbol<N,T>>();
                foreach(Symbol<N,T> S in startingRulesForSymbols.Keys){
                    if(!verifiedSymbols.contains(S)) toDelete.Add(S);
                }
                foreach(Symbol<N,T> S in toDelete){
                    startingRulesForSymbols.Remove(S);
                }
            }
        }




















        private string printFirstFollow(){
            StringBuilder sb = new StringBuilder();
            foreach(Symbol<N,T> S in _first.Keys){
                sb.Append("first( "+S.ToString() + " ) = \t{");
                foreach(Symbol<N,T> terminale in first(S)){
                    sb.Append(" "+terminale.ToString()+",");
                }
                sb.Append(" }\n");
            }
            sb.Append("\n");
            foreach(Symbol<N,T> S in _follow.Keys){
                sb.Append("follow( "+S.ToString() + " ) = \t{");
                foreach(Symbol<N,T> terminale in follow(S)){
                    sb.Append(" "+terminale.ToString()+",");
                }
                sb.Append(" }\n");
            }
            return sb.ToString();
        }
        
        
        
        
        public Parser(ParserType parserType,N start, params GrammarRule<N,T>[] rules){
            if(parserType==ParserType.LALR) throw new NotSupportedException();
            StartingGrammarRule<N,T> startingRule = new StartingGrammarRule<N,T>(start);
            createCache(startingRule,rules);
            this.parserType = parserType;
            if(this.parserType != ParserType.LR0){
                calculateAllFirsts();
                if(parserType == ParserType.SLR) computeAllFollows();
                //stampaFirstFollow();
            }

            ControlledList<itemStateLr<N,T>> states = new ControlledList<itemStateLr<N,T>>(); //creazione del primo stato
            states.add( new itemStateLr<N,T>(itemIniziale(startingRule),0,this) );
            while(states.stillToVerify){ //creation of following states
                itemStateLr<N, T> current = states.pop();
                foreach(Symbol<N,T> S in current.validSymbols){
                    ISet<ItemLr<N,T>> newKernel = current.getKernel(S);
                    bool toFind = true;
                    foreach(itemStateLr<N,T> state in states){
                        if(state==newKernel){
                            current.transictions.Add(S,state);
                            toFind = false;
                            break;
                        }
                    }
                    if(toFind){
                        itemStateLr<N,T> newItem = new itemStateLr<N,T>(newKernel,states.length,this);
                        states.add(newItem);
                        current.transictions.Add(S,newItem);
                    }
                }
            }
            int l = states.Count();
            int[] stateIndex = new int[l];
            int nStates = l;
            if(parserType==ParserType.LALR){
                List<int> originalKernels = new List<int>();
                for(int i=0;i<l;++i){
                    bool toControl = true;
                    foreach(int index in originalKernels){
                        if(states[i].equalsIfLr0(states[index])){
                            toControl = false;
                            stateIndex[i] = index;
                            break;
                        }
                    }
                    if(toControl){
                        stateIndex[i] = originalKernels.Count;
                        originalKernels.Add(i);
                    }
                }
                nStates = originalKernels.Count;
            }else{
                for(int i=0;i<l;++i) stateIndex[i] = i;
            }
            
        //from this moment the table will be created

            actionTable = new Dictionary<Symbol<N,T>, Action<N,T>>[l];
            goToTable = new Dictionary<Symbol<N,T>, int>[l];
            for(int i=0;i<nStates;++i){
                actionTable[i] = new Dictionary<Symbol<N,T>,Action<N,T>>();
                goToTable[i]   = new Dictionary<Symbol<N,T>,int>();
            }

            for(int i=0;i<l;++i){
                int k = stateIndex[i];

                itemStateLr<N,T> current = states[i];
                if(current.index != i) throw new Exception("wrong index");
                
                foreach(ItemLr<N,T> I in current.terminatedItems){
                    if(I.rule is StartingGrammarRule<N,T>){
                        insertIntoTable(actionTable[k],Symbol<N,T>.finalChar,new Accept<N,T>());
                    }
                    if(I.rule is GrammarRule<N,T> R){
                        if(this.parserType == ParserType.LR0){ //insert into the entire row
                            foreach(T t in Enum.GetValues(typeof(T))){
                                insertIntoTable(actionTable[k], new Symbol<N, T>(t), new Reduce<N, T>(R));
                            }
                            insertIntoTable(actionTable[k], Symbol<N, T>.finalChar,new Reduce<N, T>(R));
                            
                        }else if(this.parserType == ParserType.SLR){ //inserto into the follow of the non terminal, source of the tule
                            foreach(Symbol<N, T> S in follow(R.sauce)){
                                insertIntoTable(actionTable[k], S, new Reduce<N, T>(R));
                            }
                            
                        }else if(this.parserType == ParserType.LR1 || this.parserType == ParserType.LALR){ //insert into the lookup
                            insertIntoTable(actionTable[k], (I as ItemLr1<N, T>).lookup,new Reduce<N, T>(R));
                            
                        }else{
                            throw new NotImplementedException();
                        }
                    }
                }
                foreach(Symbol<N,T> S in current.validSymbols){
                    int index = stateIndex[current.transictions[S].index];
                    if(S.isTerminal){
                        insertIntoTable(actionTable[k],S,new Shift<N,T>(index));
                    }else{
                        insertIntoTable(goToTable[k],S,index);
                    }
                }
            }
            if(foundConflicts!=null){
                throw new ConflictException(string.Join("\n",foundConflicts));
            }
            clearCache();
        }






        private ItemLr<N,T> itemIniziale(StartingGrammarRule<N,T> inizio){
            if(parserType==ParserType.LR0||parserType==ParserType.SLR) return new ItemLr0<N,T>(inizio);
            if(parserType==ParserType.LALR||parserType==ParserType.LR1) return new ItemLr1<N,T>(inizio,Symbol<N,T>.finalChar);
            throw new NotImplementedException();
        }
        private void insertIntoTable<B>(Dictionary<Symbol<N,T>,B> row,Symbol<N,T> key,B element){
            if(
                row.ContainsKey(key) && !(parserType==ParserType.LALR && element.Equals(row[key])) 
            ){
                string first = string.Empty;
                if (row[key] is Shift<N, T>) first = "shift";
                else if (row[key] is Reduce<N, T>) first = "reduce";
                first+=" {"+row[key].ToString()+"}";

                string second = string.Empty;
                if(element is Shift<N,T>) second="shift";
                else if(element is Reduce<N,T>) second="reduce";
                second+=" {"+element.ToString()+"}";
                
                if(foundConflicts==null) foundConflicts=new List<string>();
                foundConflicts.Add($"conflict {first}-{second} on symbol {key}");
            }
            row[key] = element;
        }

        public override string ToString(){
            StringBuilder sb = new StringBuilder("     ");
            foreach(T t in Enum.GetValues(typeof(T))){
                sb.Append("| "+t.ToString().PadLeft(lengthColumn)+" ");
            }
            sb.Append("|  "+"$".PadLeft(lengthColumn)+" |");
            foreach(N nt in Enum.GetValues(typeof(N))){
                sb.Append("| "+nt.ToString().PadLeft(lengthColumn)+" ");
            }
            //ritorno.Append(string.Empty);
            for(int i=0;i<actionTable.Length;++i){
                sb.Append("\n"+i.ToString().PadLeft(4)+" |");
                Dictionary<Symbol<N,T>, Action<N,T>> leftRow = actionTable[i];
                string temp;
                foreach(T t in Enum.GetValues(typeof(T))){
                    try{
                        temp = leftRow[new Symbol<N,T>(t)].ToString();
                    }catch(KeyNotFoundException){
                        temp = " ";
                    }
                    sb.Append(" "+temp.PadLeft(lengthColumn)+"  ");
                }
                try{
                    temp = leftRow[Symbol<N,T>.finalChar].ToString();
                }catch(KeyNotFoundException){
                    temp = " ";
                }
                sb.Append("  "+temp.PadLeft(lengthColumn)+" ||");
                Dictionary<Symbol<N,T>, int> rightRow = goToTable[i];
                foreach(N nt in Enum.GetValues(typeof(N))){
                    try{
                        temp = rightRow[new Symbol<N,T>(nt)].ToString();
                    }catch(KeyNotFoundException){
                        temp = " ";
                    }
                    sb.Append(" "+temp.PadLeft(lengthColumn)+"  ");
                }
            }


            return sb.ToString();
        }
        
        
        
        
        private Action<N,T> pickFromTable(int i,T token){
            try{
                return actionTable[i][new Symbol<N, T>(token)];
            }catch(KeyNotFoundException){ throw new SyntaxErrorException($"Unexpected token: {token}"); }
        }

        private Action<N,T> pickLast(int i){
            try{
                return actionTable[i][Symbol<N, T>.finalChar];
            }catch(KeyNotFoundException){ throw new SyntaxErrorException("Unexpected end"); }
        }















        public object parse(IEnumerable<MacroToken<T>> source){
            Stack<int> states = new Stack<int>();
            states.Push(0);
            Stack<Symbol<N,T>> symbols = new Stack<Symbol<N,T>>();

            foreach(MacroToken<T> E in source){
                Symbol<N, T> input = new Symbol<N,T>(E.symbol);
                input.setArguments(E.arg,E.extremes);
                while(pickFromTable(states.Peek(),E.symbol).execute(states, symbols, goToTable, input));
            }

            while(pickLast(states.Peek()).executeLast(states, symbols, goToTable));
            object obj = symbols.Peek().arg;
            return obj is Wrp wrp?wrp.get():obj;
        }











    }






    public struct MacroToken<T>{
        public readonly T symbol;
        public readonly object arg;
        public readonly Extremes extremes;
        public MacroToken(T symbol,object arg,Extremes extremes){
            this.symbol = symbol;
            this.arg = arg;
            this.extremes = extremes;
        }
    }
    public class LexicalErrorException:Exception{
        public LexicalErrorException(string s):base(s){}
    }
    public class SyntaxErrorException:Exception{
        public SyntaxErrorException(string s):base(s){}
    }
    public class SemanticErrorException:Exception{
        public SemanticErrorException(string s):base(s){}
    }

    public class ItemLrEndedException : Exception{
        public ItemLrEndedException(){}
    }
    public class ConflictException : Exception{
        public ConflictException(string s):base(s){}
    }
}