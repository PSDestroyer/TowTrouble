using System.Collections.Generic;
namespace TowTrouble
{
    public static class RecoveryBoardEntries
    {
        // Preserve authored order and durable completion IDs. Available never contains replays.
        public static List<int> Select(RecoveryContract[] contracts,int completedMask,bool completed)
        {
            var list=new List<int>();
            for(int i=0;i<contracts.Length;i++)
            {
                int id=contracts[i].CompletionId;
                bool done=id>=0&&id<31&&(completedMask&(1<<id))!=0;
                if(done!=completed)continue;list.Add(i);
                if(!completed&&list.Count==3)break;
            }
            return list;
        }
    }
}
