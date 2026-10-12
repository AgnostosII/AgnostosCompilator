

using System;



namespace AgnostosCompilator{
    public readonly struct Extremes{
        public readonly int start;
        public readonly int end;
        public int length => end-start;
        public Extremes(int start,int end){
            if(end<start||start<0||end<0) throw new ArgumentException($"extremes not valid: [ {start} , {end} ]");
            this.start = start;
            this.end = end;
        }
        private Extremes(int no){
            start = end = -1;
        }
        public static Extremes nullExtremes => new Extremes(0);
        public bool notValid => start==-1;
        public static Extremes operator+(Extremes e,int a) => new Extremes(e.start+a,e.end+a);
        public static Extremes union(Extremes a,Extremes b){
            if(a.notValid) return b;
            if(b.notValid) return a;
            return new Extremes(
                a.start<b.start?a.start:b.start,
                a.end>b.end?a.end:b.end
            );
        }
        public string filterString(string s) => s.Substring(start,end-start);

    }
}