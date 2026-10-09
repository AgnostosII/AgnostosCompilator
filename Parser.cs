using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AgnostosCompilators{
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
                        Symbol<N,T> precedente=R.getSymbol(i-1); if(precedente.isTerminal) continue;
                        Symbol<N,T> successivo=R.getSymbol(i);
                        
                        _follow[precedente].UnionWith(first(successivo));
                        if(successivo.isTerminal || (!canGenerateNull(successivo))) continue;
                        graph.addBow(successivo,precedente);
                    }
                    
                    Symbol<N,T> ultimo=R.getSymbol(R.symbolNumber-1);
                    if(!ultimo.isTerminal){
                        graph.addBow(R.sauce,ultimo);
                    }
                }
            }
            graph.executeFuncForEachMacronode((nodi)=>{
                ISet<Symbol<N,T>> unione = new HashSet<Symbol<N,T>>();
                foreach(Symbol<N,T> S in nodi){
                    unione.UnionWith(_follow[S]);
                    _follow[S] = unione;
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
            /*StringBuilder daStampare = new StringBuilder(" simboli che possono generare il nulla:\n ");
            foreach(Simbolo<N,T> S in simboliChePossonoGenerareIlNulla){
                daStampare.Append(" "+S.ToString());
            }
            UnityEngine.Debug.Log(daStampare.ToString());*/
        }
        
        
        
        
        
        
        
        
                
        
        
        
        private void checkIfGrammarIsCompleted(){
            notUsedymbolsInfo = new HashSet<Symbol<N,T>>();
            foreach(N n in Enum.GetValues(typeof(N))) notUsedymbolsInfo.Add(new Symbol<N,T>(n));
            foreach(T t in Enum.GetValues(typeof(T))) notUsedymbolsInfo.Add(new Symbol<N,T>(t));
            
            ControlledList<Symbol<N, T>> verified = new ControlledList<Symbol<N,T>>();
            verified.add(Symbol<N,T>.startState);
            while(verified.stillToVerify){
                Symbol<N, T> S = verified.pop();
                if(!startingRulesForSymbols.ContainsKey(S)){
                    throw new Exception("il simbolo "+S.ToString()+" non ha nessuna regola da cui parte");
                }
                foreach(AbstractGrammarRule<N,T> R in startingRulesForSymbols[S]){
                    for(int i=0;i<R.symbolNumber;++i){
                        Symbol<N,T> verificato = R.getSymbol(i);
                        notUsedymbolsInfo.Remove(verificato);
                        if(!verificato.isTerminal){
                            verified.add(verificato);
                        }
                    }
                }
            }
            if(verified.length<startingRulesForSymbols.Keys.Count){
                List<Symbol<N,T>> daEliminare = new List<Symbol<N,T>>();
                foreach(Symbol<N,T> S in startingRulesForSymbols.Keys){
                    if(!verified.contains(S)) daEliminare.Add(S);
                }
                foreach(Symbol<N,T> S in daEliminare){
                    startingRulesForSymbols.Remove(S);
                }
            }
            /*if(verificati.dimensione<regoleChePartonoPerOgniSimbolo.Keys.Count){
                StringBuilder lista = new StringBuilder();
                foreach(Simbolo<N,T> S in regoleChePartonoPerOgniSimbolo.Keys){
                    if(!verificati.contiene(S)) lista.Append(S.ToString()+", ");
                }
                throw new Exception("ci sono regole pendenti: "+lista.ToString());
            }*/
            
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

            ControlledList<itemStateLr<N,T>> stati = new ControlledList<itemStateLr<N,T>>(); //creazione del primo stato
            stati.add( new itemStateLr<N,T>(itemIniziale(startingRule),0,this) );
            while(stati.stillToVerify){ //creazione degli stati a seguire, contenenti gli item Lr
                itemStateLr<N, T> corrente = stati.pop();
                foreach(Symbol<N,T> S in corrente.validSymbols){
                    ISet<ItemLr<N,T>> nuovoKernel = corrente.itemPassati(S);
                    bool daTrovare = true;
                    foreach(itemStateLr<N,T> stato in stati){
                        if(stato==nuovoKernel){
                            corrente.transictions.Add(S,stato);
                            daTrovare = false;
                            break;
                        }
                    }
                    if(daTrovare){
                        itemStateLr<N,T> nuovo = new itemStateLr<N,T>(nuovoKernel,stati.length,this);
                        stati.add(nuovo);
                        corrente.transictions.Add(S,nuovo);
                    }
                }
            }
            int l = stati.Count();
            int[] indiceStessoStato=new int[l];
            int nStati = l;
            if(parserType==ParserType.LALR){
                List<int> kernelOriginali = new List<int>();
                for(int i=0;i<l;++i){
                    bool daControllare = true;
                    foreach(int indice in kernelOriginali){
                        if(stati[i].equalsIfLr0(stati[indice])){
                            daControllare = false;
                            indiceStessoStato[i] = indice;
                            break;
                        }
                    }
                    if(daControllare){
                        indiceStessoStato[i] = kernelOriginali.Count;
                        kernelOriginali.Add(i);
                    }
                }
                nStati = kernelOriginali.Count;
            }else{
                for(int i=0;i<l;++i){
                    indiceStessoStato[i] = i;
                }
            }
            
        //da questo momento in poi vi è la creazione della tabella

            actionTable = new Dictionary<Symbol<N,T>, Action<N,T>>[l];
            goToTable = new Dictionary<Symbol<N,T>, int>[l];
            for(int i=0;i<nStati;++i){
                actionTable[i] = new Dictionary<Symbol<N,T>,Action<N,T>>();
                goToTable[i]   = new Dictionary<Symbol<N,T>,int>();
            }

            for(int i=0;i<l;++i){
                int k = indiceStessoStato[i];

                itemStateLr<N,T> corrente = stati[i];
                if(corrente.index != i) throw new Exception("indice spattato");
                
                foreach(ItemLr<N,T> I in corrente.itemTerminati){
                    if(I.rule is StartingGrammarRule<N,T>){
                        inserisciInTabella(actionTable[k],Symbol<N,T>.finalChar,new Accept<N,T>());
                    }
                    if(I.rule is GrammarRule<N,T> R){
                        if(this.parserType == ParserType.LR0){ //inserisci in tutta la riga
                            foreach(T t in Enum.GetValues(typeof(T))){
                                inserisciInTabella(actionTable[k], new Symbol<N, T>(t), new Reduce<N, T>(R));
                            }
                            inserisciInTabella(actionTable[k], Symbol<N, T>.finalChar,new Reduce<N, T>(R));
                            
                        }else if(this.parserType == ParserType.SLR){ //inserisci nel follow del non terminale fonte della regola
                            foreach(Symbol<N, T> S in follow(R.sauce)){
                                inserisciInTabella(actionTable[k], S, new Reduce<N, T>(R));
                            }
                            
                        }else if(this.parserType == ParserType.LR1 || this.parserType == ParserType.LALR){ //inserisci nel lookup
                            inserisciInTabella(actionTable[k], (I as ItemLr1<N, T>).lookup,new Reduce<N, T>(R));
                            
                        }else{
                            throw new NotImplementedException();
                        }
                    }
                }
                foreach(Symbol<N,T> S in corrente.validSymbols){
                    int indiceDiArrivo=indiceStessoStato[corrente.transictions[S].index];
                    if(S.isTerminal){
                        inserisciInTabella(actionTable[k],S,new Shift<N,T>(indiceDiArrivo));
                    }else{
                        inserisciInTabella(goToTable[k],S,indiceDiArrivo);
                    }
                }
            }
            if(foundConflicts!=null){
                throw new Conflict(string.Join("\n",foundConflicts));
            }
            clearCache();
        }






        private ItemLr<N,T> itemIniziale(StartingGrammarRule<N,T> inizio){
            if(parserType==ParserType.LR0||parserType==ParserType.SLR) return new ItemLr0<N,T>(inizio);
            if(parserType==ParserType.LALR||parserType==ParserType.LR1) return new ItemLr1<N,T>(inizio,Symbol<N,T>.finalChar);
            throw new NotImplementedException();
        }
        private void inserisciInTabella<B>(Dictionary<Symbol<N,T>,B> rigaTabella,Symbol<N,T> chiave,B nuovo){
            if(
                rigaTabella.ContainsKey(chiave) && !(parserType==ParserType.LALR && nuovo.Equals(rigaTabella[chiave])) 
            ){
                string primo=string.Empty;
                if (rigaTabella[chiave] is Shift<N, T>) primo = "shift";
                else if (rigaTabella[chiave] is Reduce<N, T>) primo = "reduce";
                primo+=" {"+rigaTabella[chiave].ToString()+"}";

                string secondo=string.Empty;
                if(nuovo is Shift<N,T>) secondo="shift";
                else if(nuovo is Reduce<N,T>) secondo="reduce";
                secondo+=" {"+nuovo.ToString()+"}";
                
                if(foundConflicts==null) foundConflicts=new List<string>();
                foundConflicts.Add("conflitto " + primo + "-" + secondo + " sul simbolo " + chiave.ToString());
            }
            rigaTabella[chiave] = nuovo;
        }

        public override string ToString(){
            StringBuilder ritorno = new StringBuilder("     ");
            foreach(T t in Enum.GetValues(typeof(T))){
                ritorno.Append("| "+t.ToString().PadLeft(lengthColumn)+" ");
            }
            ritorno.Append("|  "+"$".PadLeft(lengthColumn)+" |");
            foreach(N nt in Enum.GetValues(typeof(N))){
                ritorno.Append("| "+nt.ToString().PadLeft(lengthColumn)+" ");
            }
            //ritorno.Append(string.Empty);
            for(int i=0;i<actionTable.Length;++i){
                ritorno.Append("\n"+i.ToString().PadLeft(4)+" |");
                Dictionary<Symbol<N,T>, Action<N,T>> rigaSinistra=actionTable[i];
                string temp;
                foreach(T t in Enum.GetValues(typeof(T))){
                    try{
                        temp = rigaSinistra[new Symbol<N,T>(t)].ToString();
                    }catch(KeyNotFoundException){
                        temp = " ";
                    }
                    ritorno.Append(" "+temp.PadLeft(lengthColumn)+"  ");
                }
                try{
                    temp = rigaSinistra[Symbol<N,T>.finalChar].ToString();
                }catch(KeyNotFoundException){
                    temp = " ";
                }
                ritorno.Append("  "+temp.PadLeft(lengthColumn)+" ||");
                Dictionary<Symbol<N,T>, int> rigaDestra=goToTable[i];
                foreach(N nt in Enum.GetValues(typeof(N))){
                    try{
                        temp = rigaDestra[new Symbol<N,T>(nt)].ToString();
                    }catch(KeyNotFoundException){
                        temp = " ";
                    }
                    ritorno.Append(" "+temp.PadLeft(lengthColumn)+"  ");
                }
            }


            return ritorno.ToString();
        }
        
        
        
        
        private Action<N,T> prelevaDallaTabella(int i,T token){
            try{
                return actionTable[i][new Symbol<N, T>(token)];
            }catch(KeyNotFoundException){ throw new SyntaxErrorException("simbolo non previsto: "+token.ToString()); }
        }

        private Action<N,T> prelevaUltimo(int i){
            try{
                return actionTable[i][Symbol<N, T>.finalChar];
            }catch(KeyNotFoundException){ throw new SyntaxErrorException("fine prematura"); }
        }















        public object parse(IEnumerable<MacroToken<T>> sorgente){
            Stack<int> stati = new Stack<int>();
            stati.Push(0);
            Stack<Symbol<N,T>> simboli = new Stack<Symbol<N,T>>();

            foreach(MacroToken<T> E in sorgente){
                Symbol<N, T> input = new Symbol<N,T>(E.symbol);
                input.setArguments(E.arg,E.extremes);
                while(prelevaDallaTabella(stati.Peek(),E.symbol).execute(stati, simboli, goToTable, input));
            }

            while(prelevaUltimo(stati.Peek()).executeLast(stati, simboli, goToTable));
            object ritorno = simboli.Peek().arg;
            return ritorno is Wrp wrp?wrp.get():ritorno;
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
    public class SyntaxErrorException:Exception{
        public SyntaxErrorException(string s):base(s){}
    }
    public class SemanticErrorException:Exception{
        public SemanticErrorException(string s):base(s){}
    }

    public class ItemLrEndedException : Exception{
        public ItemLrEndedException(){}
    }
    public class Conflict : Exception{
        public Conflict(string s):base(s){}
    }
}