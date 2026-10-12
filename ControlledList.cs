using System.Collections;
using System.Collections.Generic;



namespace AgnostosCompilator{
    public class ControlledList<T>:IEnumerable<T>{
        private readonly List<T> seen;
        private readonly Stack<T> toVerify;
        public int length => seen.Count;
        public ControlledList(){
            seen = new List<T>();
            toVerify = new Stack<T>();
        }
        public bool contains(T nuovo) => seen.Contains(nuovo);

        public void add(T nuovo){
            if(!seen.Contains(nuovo)){
                seen.Add(nuovo);
                toVerify.Push(nuovo);
            }
        }
        public T this[int i] => seen[i];
        public bool stillToVerify => toVerify.Count > 0;
        public T pop() => toVerify.Pop();

        public IEnumerator<T> GetEnumerator(){
            foreach(T corrente in seen) yield return corrente;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}