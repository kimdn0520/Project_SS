using System;
namespace ProjectSS.Expedition
{
    public static class MoleSupportDaily
    {
        public const int Limit=5;
        public static int Day(DateTime localTime) => localTime.Year*10000+localTime.Month*100+localTime.Day;
        public static bool Refresh(ExpeditionSave save, int day)
        {
            if(save.moleSupportDay>=day)return false;
            save.moleSupportDay=day;save.moleSupportUsed=0;return true;
        }
        public static int Remaining(ExpeditionSave save) => Math.Max(0,Limit-save.moleSupportUsed);
        public static bool Complete(ExpeditionSave save, int day)
        {
            Refresh(save,day);
            if(Remaining(save)==0)return false;
            save.moleSupportUsed++;return true;
        }
    }
}
