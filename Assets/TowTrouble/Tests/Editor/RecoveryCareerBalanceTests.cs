using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryCareerBalanceTests
    {
        private RecoveryGarageCatalog Catalog=>AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
        [Test] public void NewJobsPayMoreThanReplaysWithoutAWaitingBonus()
        {
            Assert.That(RecoveryEconomy.Payout(1000,false,false,true),Is.EqualTo(1250));
            Assert.That(RecoveryEconomy.Payout(1000,true,true,true),Is.EqualTo(1450));
            Assert.That(RecoveryEconomy.Payout(1000,true,true,false),Is.EqualTo(780));
            Assert.That(RecoveryEconomy.Payout(1000,false,false,false),Is.EqualTo(650));
            Assert.That(RecoveryCareerBalance.WorldReward(1,300),Is.GreaterThan(RecoveryCareerBalance.WorldReward(1,100)));
            Assert.That(RecoveryCareerBalance.WorldReward(3,300),Is.GreaterThan(RecoveryCareerBalance.WorldReward(0,300)));
        }
        [Test] public void IntroOrdersSixAccessibleJobsWithoutChangingCompletionIdsOrHistory()
        {
            var jobs=Enumerable.Range(0,24).Select(i=>new RecoveryContract(i+1,0,"","","","",Vector3.zero,Vector3.zero,0,0){IsOpenWorld=true,ProgressId=i+3,CareerOrder=RecoveryCareerBalance.Order(i,i%5)}).ToArray();
            int mask=0;foreach(int next in new[]{0,1,4,5,8,9})
            {var available=RecoveryBoardEntries.Select(jobs,mask,false);Assert.That(available.Count,Is.EqualTo(3));Assert.That(available[0],Is.EqualTo(next));mask|=1<<jobs[next].CompletionId;}
            Assert.That(RecoveryBoardEntries.Select(jobs,mask,true),Is.EqualTo(new[]{0,1,4,5,8,9}));
            Assert.That(jobs.Select(j=>j.CompletionId),Is.EqualTo(Enumerable.Range(3,24)));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void TwoUncleanFirstJobsCanBuyFirstBodyAndMotorRemainsAnAffordableAlternative(int model)
        {
            var p=new RecoveryProfile();RecoveryEconomy.ChooseStarter(p,model);
            var first=Catalog.Parts.Where(x=>x.Slot==GarageSlot.Assembly&&x.Price>0).OrderBy(x=>x.Price).First();
            int floor=RecoveryEconomy.Payout(RecoveryCareerBalance.WorldReward(0,1),false,false,true)+RecoveryEconomy.Payout(RecoveryCareerBalance.WorldReward(1,1),false,false,true);
            Assert.That(floor,Is.GreaterThanOrEqualTo(first.Price));
            var engine=Catalog.Find("engine_2");Assert.That(engine.Price,Is.LessThan(first.Price));
            var build=p.Build(model);var upgraded=RecoveryGarageWorkflow.Preview(build,engine,Catalog);
            Assert.That(GaragePerformance.Calculate(upgraded,Catalog).Drive,Is.GreaterThan(GaragePerformance.Calculate(build,Catalog).Drive));
            Assert.That(engine.Price,Is.EqualTo(800));
        }
        [Test] public void CatalogueAndAuthoringSharePricesAndEngineTradeCannotCreateMoney()
        {
            Assert.That(Catalog.Parts.All(x=>x.Price==RecoveryCareerBalance.Price(x)),Is.True);
            var p=new RecoveryProfile{credits=2600};Assert.That(RecoveryEconomy.Buy(p,Catalog.Find("engine_2")),Is.True);
            Assert.That(RecoveryEconomy.Price(p,Catalog.Find("engine_3")),Is.EqualTo(p.credits));
            Assert.That(RecoveryEconomy.Buy(p,Catalog.Find("engine_3")),Is.True);Assert.That(p.credits,Is.Zero);
            Assert.That(p.Owns("engine_2"),Is.False);Assert.That(p.Owns("engine_3"),Is.True);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void AdvertisedReplayFeeMatchesTheSinglePaymentAndDuplicatesDoNotPay(int tier)
        {
            var c=new RecoveryContract(1,0,"","","","",Vector3.zero,Vector3.zero,0,0){IsOpenWorld=true,Tier=tier,ProgressId=3,ExpectedSeconds=180};
            var p=new RecoveryProfile{completedMask=1<<3};int quote=RecoveryEconomy.BaseOffer(c,false);
            int paid=RecoveryEconomy.Claim(p,"same-run",c,true,true);
            Assert.That(paid,Is.EqualTo(quote*120/100));Assert.That(RecoveryEconomy.Claim(p,"same-run",c,true,true),Is.Zero);
            Assert.That(p.credits,Is.EqualTo(paid));
            Assert.That(RecoveryEconomy.Payout(c,true,true,true),Is.GreaterThan(paid));
        }
    }
}
