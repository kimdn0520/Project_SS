using System;
using UnityEngine;
namespace RuneBreak
{
    // A single saved receipt keeps the balance and its last credit in the same record.
    public sealed class RuneCoinReward
    {
        public readonly string id=Guid.NewGuid().ToString("N");
        public readonly int amount;
        public bool Claimed {get;private set;}
        public RuneCoinReward(int amount){this.amount=Mathf.Max(0,amount);}
        public const string SaveKey="RuneBreak.Wallet";
        [Serializable] private sealed class Receipt {public int balance;public string lastReward;}
        private static string cachedText;
        private static Receipt cachedReceipt;
        private static Receipt Read(){var text=PlayerPrefs.GetString(SaveKey,"");if(cachedReceipt!=null&&text==cachedText)return cachedReceipt;cachedText=text;try{cachedReceipt=string.IsNullOrEmpty(text)?new Receipt():JsonUtility.FromJson<Receipt>(text)??new Receipt();}catch{cachedReceipt=new Receipt();}return cachedReceipt;}
        public static int Balance=>Mathf.Max(0,Read().balance);
        public bool Claim()
        {
            if(Claimed)return false;var receipt=Read();Claimed=true;if(receipt.lastReward==id)return false;
            receipt.balance=(int)Math.Min(int.MaxValue,(long)receipt.balance+amount);receipt.lastReward=id;
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(receipt));PlayerPrefs.Save();return true;
        }
    }
}
