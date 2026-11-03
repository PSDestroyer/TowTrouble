using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryBoardEntriesTests
    {
        private static RecoveryContract[] Jobs()=>Enumerable.Range(0,24).Select(i=>new RecoveryContract(i+1,0,"","","","",Vector3.zero,Vector3.zero,0,0){IsOpenWorld=true,ProgressId=i+3}).ToArray();

        [Test] public void NewProfileGetsOnlyTheFirstThreeJobsInAuthoredOrder()
        {
            var jobs=Jobs();Assert.That(RecoveryBoardEntries.Select(jobs,0,false),Is.EqualTo(new[]{0,1,2}));
            Assert.That(RecoveryBoardEntries.Select(jobs,0,true),Is.Empty);
        }
        [Test] public void CompletionMovesTheJobIntoHistoryAndFillsTheAvailableSlot()
        {
            var jobs=Jobs();int mask=(1<<jobs[1].CompletionId)|(1<<jobs[7].CompletionId);
            Assert.That(RecoveryBoardEntries.Select(jobs,mask,false),Is.EqualTo(new[]{0,2,3}));
            Assert.That(RecoveryBoardEntries.Select(jobs,mask,true),Is.EqualTo(new[]{1,7}));
        }
        [Test] public void TutorialCompletionDoesNotCompleteWorldJobs()
        {
            var jobs=Jobs();Assert.That(RecoveryBoardEntries.Select(jobs,7,false),Is.EqualTo(new[]{0,1,2}));
            Assert.That(RecoveryBoardEntries.Select(jobs,7,true),Is.Empty);
        }
        [Test] public void FinalAvailableSlotsShrinkAndHistoryContainsEveryCompletedJob()
        {
            var jobs=Jobs();int mask=0;
            foreach(var job in jobs.Take(22))mask|=1<<job.CompletionId;
            Assert.That(RecoveryBoardEntries.Select(jobs,mask,false),Is.EqualTo(new[]{22,23}));
            mask|=1<<jobs[22].CompletionId;
            Assert.That(RecoveryBoardEntries.Select(jobs,mask,false),Is.EqualTo(new[]{23}));
            mask|=1<<jobs[23].CompletionId;
            Assert.That(RecoveryBoardEntries.Select(jobs,mask,false),Is.Empty);
            Assert.That(RecoveryBoardEntries.Select(jobs,mask,true),Is.EqualTo(Enumerable.Range(0,24)));
        }
        [Test] public void ReplayingACompletedJobStillPaysWithoutChangingTheAvailableQueue()
        {
            var jobs=Jobs();var profile=new RecoveryProfile{completedMask=1<<jobs[0].CompletionId};
            var before=RecoveryBoardEntries.Select(jobs,profile.completedMask,false);
            Assert.That(RecoveryEconomy.Claim(profile,"board-replay",jobs[0],false,false),Is.EqualTo(jobs[0].Reward));
            Assert.That(RecoveryEconomy.Claim(profile,"board-replay",jobs[0],false,false),Is.Zero);
            Assert.That(RecoveryBoardEntries.Select(jobs,profile.completedMask,false),Is.EqualTo(before));
            Assert.That(RecoveryBoardEntries.Select(jobs,profile.completedMask,true),Is.EqualTo(new[]{0}));
        }
    }
}
