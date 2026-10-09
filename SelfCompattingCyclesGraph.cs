
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AgnostosCompilators{
    public class SelfCompattingCyclesGraph<T>{
        public override string ToString(){
            StringBuilder sb = new StringBuilder();
            foreach(MacroNode M in _macroNodes){
                sb.Append(M.ToString() + "\n");
            }
            return sb.ToString();
        }
        private readonly Dictionary<T,MacroNode> nodo2Macro;
        private readonly ISet<MacroNode> _macroNodes;
        public SelfCompattingCyclesGraph(){
            nodo2Macro = new Dictionary<T,MacroNode>();
            _macroNodes = new HashSet<MacroNode>();
        }
        
        private class MacroNode:IComparable<MacroNode>{
            public override string ToString(){
                StringBuilder sb = new StringBuilder("{ ");
                foreach(T nodo in _nodes){
                    sb.Append(" "+nodo.ToString()+" ,");
                }
                return sb.ToString()+" }";
            }
            public int CompareTo(MacroNode altro) => altro.topologicOrder - topologicOrder;

            private int topologicOrder;
            private Dictionary<T,MacroNode> node2Macro;
            private ISet<T> _nodes; public ISet<T> nodes => _nodes;
            private ISet<T> _targetNodes; //it's actually a set of macronodes
            public ISet<MacroNode> targetNodes{get{
                ISet<MacroNode> ritorno=new HashSet<MacroNode>();
                foreach(T nodo in _targetNodes){
                    ritorno.Add(node2Macro[nodo]);
                }
                return ritorno;
            }}
            public MacroNode(T firstElement,Dictionary<T,MacroNode> node2Macro){
                _targetNodes = new HashSet<T>();
                _nodes = new HashSet<T>{ firstElement };
                this.node2Macro = node2Macro;
            }
            public MacroNode(IEnumerable<MacroNode> toCompact,Dictionary<T,MacroNode> node2Macro){
                _targetNodes=new HashSet<T>();
                _nodes = new HashSet<T>();
                foreach(MacroNode M in toCompact){
                    _targetNodes.UnionWith(M._targetNodes);
                    _nodes.UnionWith(M._nodes);
                }
                foreach(T node in _nodes){
                    _targetNodes.Remove(node);
                }
                this.node2Macro = node2Macro;
            }
            public bool contains(T elemento) => _nodes.Contains(elemento);

            public T rappresentante(){
                return _nodes.First();
            }

            public void addLink(MacroNode qualeMacroNodo){
                _targetNodes.Add(qualeMacroNodo.rappresentante());
            }
            public IEnumerable<MacroNode> trovaPercorso(MacroNode b){
                ISet<MacroNode> esplorati=new HashSet<MacroNode>();
                ISet<MacroNode> founds = new HashSet<MacroNode>();
                trovaPercorso(b,founds,esplorati);
                return founds;
            }
            private bool trovaPercorso(MacroNode b,ISet<MacroNode> trovati,ISet<MacroNode> esplorati){
                if(this==b){
                    trovati.Add(this);
                    return true;
                }
                bool ritorno = false;
                foreach(T nodo in _targetNodes){
                    MacroNode M = node2Macro[nodo];
                    if(esplorati.Contains(M)){
                        if(trovati.Contains(M)){
                            trovati.Add(this);
                            ritorno = true;
                        }
                        continue;
                    }
                    esplorati.Add(M);
                    if(M.trovaPercorso(b,trovati,esplorati)){
                        trovati.Add(this);
                        ritorno = true;
                    }
                }
                return ritorno;
            }
            public void ordinaTopologicamente(SortedSet<MacroNode> listaOrdinata,Dictionary<MacroNode,Colore> colori,ref int tempo){
                colori[this] = Colore.grigio;
                ++tempo;
                foreach(MacroNode M in targetNodes){
                    if(colori[M]==Colore.bianco){
                        M.ordinaTopologicamente(listaOrdinata,colori,ref tempo);
                    }else if(colori[M]==Colore.grigio) throw new Exception("grafo ciclico");
                }
                colori[this] = Colore.nero;
                topologicOrder = ++tempo;
                listaOrdinata.Add(this);
            }
        }
        
        public void addBow(T start,T target){
            bool vecchio = true;
            if(!nodo2Macro.ContainsKey(start)){
                vecchio = false;
                nodo2Macro[start] = new MacroNode(start,nodo2Macro);
                _macroNodes.Add(nodo2Macro[start]);
            }
            if(start.Equals(target)) return;

            if(!nodo2Macro.ContainsKey(target)){
                vecchio = false;
                nodo2Macro[target] = new MacroNode(target,nodo2Macro);
                _macroNodes.Add(nodo2Macro[target]);
            }
            if(vecchio){
                if(nodo2Macro[start]==nodo2Macro[target]) return ;
                IEnumerable<MacroNode> daCompattare = nodo2Macro[target].trovaPercorso(nodo2Macro[start]);
                if(daCompattare.Count()>0){
                    MacroNode unione = new MacroNode(daCompattare,nodo2Macro);
                    ISet<T> chiaviDaFondere=new HashSet<T>();
                    foreach(T chiave in nodo2Macro.Keys){
                        if(unione.contains(chiave)){
                            _macroNodes.Remove(nodo2Macro[chiave]);
                            chiaviDaFondere.Add(chiave);
                        }
                    }
                    foreach(T chiave in chiaviDaFondere){
                        nodo2Macro[chiave] = unione;
                    }
                    _macroNodes.Add(unione);
                    return;
                }
            }
            nodo2Macro[start].addLink(nodo2Macro[target]);
        }

        public delegate void FuncForEachMacronode(IEnumerable<T> nodi);
        public delegate void FuncForEachBow(T a,T b);
        private enum Colore{bianco,grigio,nero}
        public void executeFuncForEachMacronode(FuncForEachMacronode function){
            foreach(MacroNode M in _macroNodes){
                function(M.nodes);
            }
        }
        public void executeFuncForEachBow(FuncForEachBow function){
            Dictionary<MacroNode,Colore> colori = new Dictionary<MacroNode,Colore>();
            SortedSet<MacroNode> listaOrdinata = new SortedSet<MacroNode>();
            ISet<MacroNode> nonBase = new HashSet<MacroNode>();
            foreach(MacroNode M in _macroNodes){
                nonBase.UnionWith(M.targetNodes);
                colori[M] = Colore.bianco;
            }
            ISet<MacroNode> macroNodiBase = new HashSet<MacroNode>();
            foreach(MacroNode M in _macroNodes){
                if(!nonBase.Contains(M)){
                    macroNodiBase.Add(M);
                }
            }
            int tempo = 0;
            foreach(MacroNode M in macroNodiBase){
                M.ordinaTopologicamente(listaOrdinata,colori,ref tempo);
            }
            foreach(MacroNode M in listaOrdinata){
                foreach(MacroNode N in M.targetNodes){
                    function(M.rappresentante(),N.rappresentante());
                }
                
            }
        }
    }
}