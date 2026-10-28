using UnityEngine;

namespace RuneBreak
{
    // Pure port of block-punch.html's punch-1 trajectory. Never changes game rules.
    public struct RuneReboundMotion
    {
        public Vector2 from,contact,kick,to,c1,c2,direction,surface;
        public float launch,press,back,hop;
        public bool reduced,enhanced;
        public string dir;
        public float Release=>launch+press;
        public float Depart=>Release+back;
        public float Duration=>Depart+hop;
        public static RuneReboundMotion Create(Vector2 from,Vector2 edge,Vector2 to,string dir,bool reduced)
        {
            RuneRules.Direction(dir,out int dx,out int dy);var d=new Vector2(dx,dy);
            var contact=edge-d*25;float distance=Vector2.Distance(contact,from);
            var kick=contact-d*(reduced?7:dir=="S"?37:43);float hopDistance=Vector2.Distance(to,kick);var delta=to-kick;
            return new RuneReboundMotion {
                from=from,contact=contact,kick=kick,to=to,direction=d,dir=dir,reduced=reduced,
                launch=reduced?.10f:.085f+Mathf.Min(.125f,distance/1600),press=reduced?.025f:.062f,back=reduced?.035f:.055f,
                hop=reduced?.145f:.145f+Mathf.Min(.105f,hopDistance/3700),
                c1=new Vector2(Mathf.Clamp(kick.x+delta.x*.10f-d.x*(reduced?3:33),57,423),kick.y+delta.y*.12f-d.y*(reduced?3:37)-(reduced?0:10)),
                c2=new Vector2(to.x-delta.x*.12f,to.y-(reduced?16:Mathf.Min(102,hopDistance*.19f+36))),surface=contact+d*15
            };
        }
        public static RuneReboundMotion CreateSurface(Vector2 from,Vector2 edge,Vector2 to,string dir,bool reduced)
        {
            var m=Create(from,edge,to,dir,reduced);m.enhanced=true;m.contact=edge-m.direction*21;m.surface=edge;
            m.kick=m.contact-m.direction*(reduced?9:61);var delta=to-m.kick;float distance=delta.magnitude;
            m.launch=reduced?.10f:.075f+Mathf.Min(.13f,Vector2.Distance(from,m.contact)/1800);m.press=reduced?.025f:.07f;m.back=reduced?.035f:.045f;m.hop=reduced?.16f:.20f+Mathf.Min(.11f,distance/3500);
            m.c1=new Vector2(Mathf.Clamp(m.kick.x+delta.x*.12f-m.direction.x*(reduced?3:40),45,435),m.kick.y+delta.y*.08f-m.direction.y*(reduced?3:45)-(reduced?0:14));
            m.c2=new Vector2(to.x-delta.x*.13f,to.y-(reduced?16:Mathf.Min(118,distance*.20f+40)));return m;
        }
        public Vector2 Sample(float elapsed)
        {
            float t=Mathf.Max(0,elapsed);if(t>=Duration)return to;
            if(t<launch){float p=t/launch;if(p<.12f)return from-direction*2.4f*Mathf.Sin(p/.12f*Mathf.PI);return Vector2.LerpUnclamped(from,contact,Mathf.Pow((p-.12f)/.88f,2.25f));}
            if(t<Release)return contact;
            if(t<Depart)return Vector2.LerpUnclamped(contact,kick,enhanced?1-Mathf.Pow(1-(t-Release)/back,2.7f):Ease((t-Release)/back));
            float q=Ease(Mathf.Clamp01((t-Depart)/hop)),u=1-q;
            return u*u*u*kick+3*u*u*q*c1+3*u*q*q*c2+q*q*q*to;
        }
        private static float Ease(float t)=>1-Mathf.Pow(1-t,1.65f);
        public struct Pose {public float sx,sy,angle,size,lift,flash,hop;public bool airborne;}
        public Pose GetPose(float elapsed)
        {
            float t=Mathf.Max(0,elapsed);bool vertical=direction.x==0;
            var p=new Pose{sx=1,sy=1,size=36,airborne=t>=Release&&t<Duration,hop=Mathf.Clamp01((t-Depart)/hop)};
            float pressStart=launch-.018f;
            if(t>=pressStart&&t<Release)
            {
                float rise=Mathf.Clamp01((t-pressStart)/.027f),squeeze=(reduced?.07f:enhanced?.48f:.43f)*Mathf.Sin(rise*Mathf.PI/2);
                p.sx=vertical?1+squeeze*.62f:1-squeeze;p.sy=vertical?1-squeeze:1+squeeze*.62f;
                p.flash=reduced?0:Mathf.Max(0,1-(t-launch)/.055f)*.42f;
            }
            else if(p.airborne)
            {
                float q=Mathf.Clamp01((t-Release)/(Duration-Release)),pop=reduced?0:Mathf.Sin(Mathf.Min(1,q/.42f)*Mathf.PI)*(enhanced?.30f:.20f);
                p.size=36-3*q+(reduced?0:5.5f*Mathf.Sin(q*Mathf.PI));
                float spin=dir=="E"?-.48f:dir=="W"?.48f:to.x>=from.x?.36f:-.36f;
                p.angle=reduced?0:Mathf.Sin(q*Mathf.PI)*spin*(enhanced?1.4f:1);p.lift=reduced?0:Mathf.Sin(q*Mathf.PI)*(enhanced?32:22);
                p.sx=vertical?1-pop*.55f:1+pop;p.sy=vertical?1+pop:1-pop*.55f;
            }
            else if(t>=Duration)p.size=33;
            return p;
        }
    }
}
