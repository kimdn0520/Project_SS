using System;
using System.Collections.Generic;
namespace ProjectSS.Expedition
{
    public static class HeroRoster
    {
        public static readonly string[] Ids={"rowen","rin","mira"};
        public static readonly string[] Names={"로웬","린","미라"};
        public static readonly string[] Roles={"전사","도적","마법사"};
        public static readonly string[] Positions={"전열","중열","후열"};
        public static int Index(string id)=>Array.IndexOf(Ids,id);
        public static void Migrate(ExpeditionSave save)
        {
            var owned=new List<string>();
            bool legacy=save.version<4;
            // All three existing heroes were owned in the v3 save format.
            foreach(var id in legacy?Ids:save.ownedHeroes??Ids)if(Index(id)>=0&&!owned.Contains(id))owned.Add(id);
            if(owned.Count==0)owned.Add(Ids[0]);
            save.ownedHeroes=owned.ToArray();
            var slots=new string[3];var used=new HashSet<string>();
            for(int slot=0;slot<3;slot++)
            {
                string id=save.formation==null||(legacy&&save.formation.Length==0)?(slot<owned.Count?owned[slot]:null):slot<save.formation.Length?save.formation[slot]:null;
                if(id!=null&&owned.Contains(id)&&used.Add(id))slots[slot]=id;
            }
            if(used.Count==0)slots[0]=owned[0];
            save.formation=slots;save.version=Math.Max(save.version,4);save.autoBattle=true;
        }
    }
}
