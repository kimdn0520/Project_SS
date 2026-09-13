using System;
using System.IO;
using UnityEditor;
using ProjectSS.Expedition;
public static class VeinDurabilityCheck
{
    public static void Execute()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
        var results=new string[3];
        for(int tier=0;tier<3;tier++)
        {
            var data=ExpeditionSave.Fresh(catalog.gear.Length);data.cleared=tier*10;
            var model=new ExpeditionModel(catalog,data,42);int hits=0;
            while(data.excavations==0&&hits<30){model.Dig();hits++;}
            int expected=(int)Math.Ceiling(16.0/model.MiningPower);
            if(hits!=expected||data.excavations!=1)throw new Exception("Vein durability mismatch");
            results[tier]=$"PASS: power {model.MiningPower}, {hits} strikes for a 16 HP vein";
        }
        File.WriteAllLines("PrototypeQA/vein-durability.txt",results);
    }
}
