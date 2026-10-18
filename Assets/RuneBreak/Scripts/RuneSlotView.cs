using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RuneBreak
{
    public sealed class RuneSlotView : MonoBehaviour
    {
        public Image panel, gem, locked, sparkle, loading;
        public RectTransform gemRect,loadingRect;
        public TMP_Text label;
        public Button button;
        private bool occupied, ready, pending, full;
        private int index,colorIndex;
        private RuneArtLibrary library;
        public void Render(RuneArtLibrary art,RuneRules rules,RuneState s,int slot,bool pending)
        {
            index=slot;int id=s.slots[slot];occupied=id>=0;this.pending=pending;full=s.Filled==5;library=art;
            panel.enabled=occupied;gem.enabled=occupied&&!pending;locked.enabled=false;label.enabled=true;
            sparkle.enabled=false;loading.enabled=occupied&&pending;
            if(!occupied){label.text=(slot+1).ToString();label.color=RuneArtLibrary.Hex("77827a");return;}
            var b=rules.Level.blocks[id];ready=rules.Target(s,b.color)>=0;
            colorIndex=RuneArtLibrary.ColorIndex(b.color);gem.sprite=art.storedRunes[colorIndex];gem.color=Color.white;sparkle.color=RuneArtLibrary.Light(b.color);loading.color=RuneArtLibrary.Mid(b.color);
            // WAIT is baked by the original per-rectangle alpha compositor, not a whole-sprite fade.
            bool waiting=!ready&&!pending;gem.enabled=occupied&&!pending&&!waiting;locked.enabled=false;
            label.text=pending?"...":waiting?"WAIT":"TAP";label.color=waiting?RuneArtLibrary.Hex("b5bbac"):RuneArtLibrary.Light(b.color);
        }
        public void Tick(float time,bool reduced)
        {
            if(occupied)gemRect.anchoredPosition=new Vector2(30,-32+(ready&&!reduced?-2*Mathf.Sin(time*2+index):0));
            if(library==null)return;bool pulse=full&&!reduced&&Mathf.Sin(time*5)>.1f;panel.sprite=occupied&&!ready&&!pending?(pulse?library.fullWaitSlots[colorIndex]:library.waitSlots[colorIndex]):pulse?library.fullSlot:library.slot;
            sparkle.enabled=occupied&&ready&&!pending&&!reduced&&Mathf.Sin(time*4+index)>.4f;
            if(loading.enabled)loadingRect.sizeDelta=new Vector2(42*(.25f+.75f*(.5f+.5f*Mathf.Sin(time*8))),2);
        }
    }
}
