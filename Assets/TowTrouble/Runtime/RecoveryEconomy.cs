using System;
using UnityEngine;

namespace TowTrouble
{
    public static class RecoveryEconomy
    {
        public static readonly int[] Rewards={450,800,1300,1900,2700};
        public static int Payout(int baseReward,bool noResets,bool noSnaps,bool first,int tier=-1)
        {int fee=BaseOffer(baseReward,first,tier);return fee*(100+(noResets?10:0)+(noSnaps?10:0)+(first?RecoveryCareerBalance.FirstBonus:0))/100;}
        public static int BaseOffer(int reward,bool first,int tier=-1)=>Mathf.Max(0,reward)*(first?100:RecoveryCareerBalance.RepeatRate(tier))/100;
        public static int BaseOffer(RecoveryContract contract,bool first)=>BaseOffer(contract.Reward,first,contract.IsOpenWorld?contract.Tier:contract.Id-1);
        public static int Payout(RecoveryContract contract,bool noResets,bool noSnaps,bool first)=>Payout(contract.Reward,noResets,noSnaps,first,contract.IsOpenWorld?contract.Tier:contract.Id-1);
        public static int LegacyPayout(int reward,bool noResets,bool noSnaps)=>Mathf.Max(0,reward)*(105+(noResets?10:0)+(noSnaps?10:0))/100;
        public static int Price(RecoveryProfile profile,GaragePart part)=>part.Id=="engine_3"&&profile.Owns("engine_2")?Mathf.Max(0,part.Price-800):part.Price;
        public static int VehiclePrice(int model)=>model<0||model>=4?0:1200;
        public static bool ChooseStarter(RecoveryProfile profile,int model)
        {
            if(profile.starterChosen||model<0||model>=4)return false;
            profile.starterChosen=true;profile.starterVehicle=profile.vehicle=model;profile.ownedVehicles=1<<model;return true;
        }
        public static bool BuyVehicle(RecoveryProfile profile,int model)
        {
            if(!profile.starterChosen||model<0||model>=4||profile.OwnsVehicle(model))return false;
            int price=VehiclePrice(model);if(profile.credits<price)return false;
            profile.credits-=price;profile.ownedVehicles|=1<<model;return true;
        }
        public static bool Buy(RecoveryProfile profile,GaragePart part)
        {
            if(part==null||part.Price<=0||profile.Owns(part.Id)||profile.credits<Price(profile,part))return false;
            profile.EnsureGarage();int cost=Price(profile,part);profile.credits-=cost;
            if(part.Id=="engine_3"&&profile.ownedParts.Remove("engine_2"))
            {
                foreach(var build in profile.builds)ReplaceEngine(build);
                foreach(var preset in profile.presets)if(preset.Build!=null)ReplaceEngine(preset.Build);
            }
            profile.ownedParts.Add(part.Id);return true;
        }
        private static void ReplaceEngine(RecoveryBuild build){if(build.Items==null)return;for(int i=0;i<build.Items.Length;i++)if(build.Items[i]=="engine_2")build.Items[i]="engine_3";}
        public static bool Sell(RecoveryProfile profile,GaragePart part,RecoveryGarageCatalog catalog)
        {
            profile.EnsureGarage();if(part==null||part.Price<=0||!profile.Owns(part.Id))return false;
            foreach(var b in profile.builds)if(b.Items!=null&&Array.IndexOf(b.Items,part.Id)>=0)return false;
            foreach(var p in profile.presets)if(p.Build?.Items!=null&&Array.IndexOf(p.Build.Items,part.Id)>=0)return false;
            profile.ownedParts.Remove(part.Id);profile.credits=Mathf.Min(1000000000,profile.credits+part.Price*70/100);return true;
        }
        public static int Claim(RecoveryProfile profile,string run,int contract,bool noResets,bool noSnaps)
        {
            if(contract<0||contract>=RecoveryContract.All.Length)return 0;
            return Claim(profile,run,RecoveryContract.Get(contract),noResets,noSnaps);
        }
        public static int Claim(RecoveryProfile profile,string run,RecoveryContract contract,bool noResets,bool noSnaps)
        {
            if(contract==null||string.IsNullOrEmpty(run)||profile.paidRuns.Contains(run)||contract.CompletionId<0||contract.CompletionId>26)return 0;
            int payout=Payout(contract,noResets,noSnaps,!profile.Completed(contract.CompletionId));
            profile.paidRuns.Add(run);if(profile.paidRuns.Count>128)profile.paidRuns.RemoveAt(0);
            profile.credits=Mathf.Min(1000000000,profile.credits+payout);profile.lastPayout=payout;
            profile.RecordFinish(contract.CompletionId,noResets,noSnaps);return payout;
        }
    }
}
