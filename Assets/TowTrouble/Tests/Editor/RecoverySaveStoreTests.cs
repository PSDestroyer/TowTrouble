using System;
using System.IO;
using HalvaStudio.Save;
using NUnit.Framework;

namespace TowTrouble.Tests
{
    public sealed class RecoverySaveStoreTests
    {
        [Test] public void NewSaveRoundTripsCareerPreferencesAndHostAddress()
        {
            var disk=new MemorySaveStorage();var store=new SaveStore(disk);
            store.SetString("career","{\"credits\":1234}");store.SetString("host","127.0.0.1");Assert.That(store.Flush(),Is.True);
            var reload=new SaveStore(disk);Assert.That(reload.GetString("career"),Does.Contain("1234"));Assert.That(reload.GetString("host"),Is.EqualTo("127.0.0.1"));
        }
        [Test] public void InvalidPrimaryRecoversLastValidBackup()
        {
            var disk=new MemorySaveStorage();var store=new SaveStore(disk);store.SetString("career","old");store.Flush();store.SetString("career","new");store.Flush();disk.Document="{truncated";
            var reload=new SaveStore(disk);Assert.That(reload.RecoveredBackup,Is.True);Assert.That(reload.GetString("career"),Is.EqualTo("old"));Assert.That(reload.Flush(),Is.True);
            Assert.That(new SaveStore(disk).GetString("career"),Is.EqualTo("old"));
        }
        [Test] public void CorruptOrNewerSaveIsNeverOverwrittenWithDefaults()
        {
            foreach(var raw in new[]{"","{truncated","{}","{\"saveFormat\":999,\"payload\":\"x\",\"checksum\":\"x\"}"})
            {var disk=new MemorySaveStorage{Document=raw};var store=new SaveStore(disk);store.SetString("career","default");Assert.That(store.Flush(),Is.False);Assert.That(disk.Document,Is.EqualTo(raw));}
        }
        [Test] public void ChecksumDetectsValidJsonWithChangedPayload()
        {
            var disk=new MemorySaveStorage();var store=new SaveStore(disk);store.SetString("career","original");store.Flush();disk.Document=disk.Document.Replace("original","modified");
            Assert.That(new SaveStore(disk).CanWrite,Is.False);
        }
        private sealed class UnavailableStorage:ISaveStorage
        {
            public bool Fail=true;public string Document;
            public string Read(bool backup=false)=>backup?null:Document;
            public void Write(string raw,string previous){if(Fail)throw new IOException("Disk unavailable");Document=raw;}
        }
        [Test] public void WriteFailureKeepsLatestProgressPendingForRetry()
        {
            var disk=new UnavailableStorage();var store=new SaveStore(disk);store.SetString("career","first");Assert.That(store.Flush(),Is.False);Assert.That(store.Dirty,Is.True);
            store.SetString("career","latest");disk.Fail=false;Assert.That(store.Flush(),Is.True);Assert.That(store.Dirty,Is.False);Assert.That(new SaveStore(disk).GetString("career"),Is.EqualTo("latest"));
        }
        [Test] public void LocalHostAndClientHaveIndependentCareersAndRewardReceipts()
        {
            const string hostId="TowTrouble.SaveHost",clientId="TowTrouble.SaveClient";string prior=RecoveryProfile.ValidationStorageKey;
            try
            {
                RecoveryProfile.ValidationStorageKey=hostId;var host=new RecoveryProfile();RecoveryEconomy.ChooseStarter(host,0,1);host.credits=111;host.name="Host";host.Save();
                RecoveryProfile.ValidationStorageKey=clientId;var client=new RecoveryProfile();RecoveryEconomy.ChooseStarter(client,3,2);client.credits=222;client.name="Client";client.Save();
                RecoveryProfile.ValidationStorageKey=hostId;host=RecoveryProfile.Load();Assert.That(RecoveryEconomy.Claim(host,"shared-run",0,true,true),Is.GreaterThan(0));host.Save();
                RecoveryProfile.ValidationStorageKey=clientId;client=RecoveryProfile.Load();Assert.That(client.credits,Is.EqualTo(222));Assert.That(client.paidRuns,Is.Empty);Assert.That(client.vehicle,Is.EqualTo(3));Assert.That(client.paint,Is.EqualTo(2));
                int payout=RecoveryEconomy.Claim(client,"shared-run",0,true,true);Assert.That(payout,Is.GreaterThan(0));client.Save();client=RecoveryProfile.Load();Assert.That(client.credits,Is.EqualTo(222+payout));Assert.That(RecoveryEconomy.Claim(client,"shared-run",0,true,true),Is.Zero);
                RecoveryProfile.ValidationStorageKey=hostId;host=RecoveryProfile.Load();Assert.That(host.name,Is.EqualTo("Host"));Assert.That(host.paint,Is.EqualTo(1));Assert.That(host.paidRuns,Has.Count.EqualTo(1));Assert.That(host.credits,Is.EqualTo(111+payout));
            }
            finally{SaveManager.ReleaseIsolated(hostId);SaveManager.ReleaseIsolated(clientId);RecoveryProfile.ValidationStorageKey=prior;}
        }
        [Test] public void OriginalImportedSaveRetainsUnknownDataWhenExtended()
        {
            var disk=new MemorySaveStorage{Document="{\"money\":42,\"canSwim\":true,\"boughtCars\":{\"legacy\":true}}"};var store=new SaveStore(disk);store.SetString("career","new game");Assert.That(store.Flush(),Is.True);
            var decoded=SaveStore.Decode(disk.Document);Assert.That(SaveStore.Encode(decoded),Does.Contain("money").And.Contain("canSwim").And.Contain("42"));Assert.That(decoded.boughtCars["legacy"],Is.True);
        }
        [Test] public void NativeFileWritesKeepReloadablePreviousRevision()
        {
            string dir=Path.Combine(Path.GetTempPath(),"TowSaveTest_"+Guid.NewGuid().ToString("N"));
            try
            {
                var disk=new FileSaveStorage(Path.Combine(dir,"save.json"));var store=new SaveStore(disk);store.SetString("career","one");Assert.That(store.Flush(),Is.True);store.SetString("career","two");Assert.That(store.Flush(),Is.True);
                Assert.That(new SaveStore(disk).GetString("career"),Is.EqualTo("two"));File.WriteAllText(disk.Path,"incomplete");Assert.That(new SaveStore(disk).GetString("career"),Is.EqualTo("one"));Assert.That(File.Exists(disk.Path+".tmp"),Is.False);
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test] public void ValidationProfileNeverTouchesCareerDocument()
        {
            const string id="TowTrouble.SaveTest";string prior=RecoveryProfile.ValidationStorageKey;
            try
            {
                RecoveryProfile.ValidationStorageKey=id;var profile=new RecoveryProfile{credits=1234,starterChosen=true,starterVehicle=1,vehicle=1,ownedVehicles=2};profile.Save();var reload=RecoveryProfile.Load();Assert.That(reload.credits,Is.EqualTo(1234));Assert.That(reload.starterVehicle,Is.EqualTo(1));
            }
            finally{SaveManager.ReleaseIsolated(id);RecoveryProfile.ValidationStorageKey=prior;}
        }
    }
}
