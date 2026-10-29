using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    public sealed partial class RuneFxGraphic
    {
        // Cached flights and twelve bounded impact records; no runtime UI creation.
        // Reward coordinates are local UI coordinates, shared with RewardCoinPool.
        public RuneCoinFlight[] rewardFlights=System.Array.Empty<RuneCoinFlight>();
        private struct CoinImpact {public Vector2 point;public float age,life;public bool reduced;}
        private readonly CoinImpact[] coinImpacts=new CoinImpact[12];
        private int coinImpactCursor;
        public void CoinArrival(Vector2 point,bool reduced)
        {
            coinImpacts[coinImpactCursor]=new CoinImpact{point=point,life=reduced?.11f:.24f,reduced=reduced};coinImpactCursor=(coinImpactCursor+1)%coinImpacts.Length;SetVerticesDirty();
        }
        private void ClearReward(){System.Array.Clear(coinImpacts,0,coinImpacts.Length);coinImpactCursor=0;}
        private void TickReward(float dt){for(int i=0;i<coinImpacts.Length;i++)coinImpacts[i].age+=dt;}
        private void DrawReward(VertexHelper vh)
        {
            foreach(var flight in rewardFlights)
            {
                if(flight==null||!flight.TrailVisible)continue;
                for(int i=0;i<7;i++)
                {
                    float a=(i+1)/8f,b=(i+2)/8f;var c=new Color(1,.79f,.12f,(1-a)*.32f);
                    RuneUiMesh.Line(vh,flight.TrailPoint(.045f*a),flight.TrailPoint(.045f*b),(1-a)*5.5f+.4f,c);
                }
            }
            foreach(var impact in coinImpacts)
            {
                if(impact.age>=impact.life)continue;float u=impact.age/impact.life;var center=impact.point;
                float radius=14+u*(impact.reduced?6:24);var ring=new Color(1,.84f,.24f,(1-u)*(impact.reduced?.3f:.7f));
                for(int i=0;i<28;i++){float a=i*Mathf.PI*2/28,b=(i+1)*Mathf.PI*2/28;RuneUiMesh.Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,(1-u)*2.2f+.5f,ring);}
                if(impact.reduced)continue;
                for(int i=0;i<6;i++)
                {
                    float a=(i+.3f)*Mathf.PI/3;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float start=19+u*23,length=(1-u)*(i%2==0?9:6);
                    var spark=new Color(1,i%2==0?.91f:.71f,i%2==0?.52f:.08f,(1-u)*.85f);
                    RuneUiMesh.Line(vh,center+d*start,center+d*(start+length),i%2==0?2.8f:1.8f,spark);
                }
            }
        }
    }
}
