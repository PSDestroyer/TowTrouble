using System.Collections.Generic;
using System.Linq;
namespace TowTrouble
{
    public static class RecoveryBoardEntries
    {
        // Preserve authored order and durable completion IDs. Available never contains replays.
        public static List<int> Select(RecoveryContract[] contracts,int completedMask,bool completed)
        {
            var list=new List<int>();
            var order=Enumerable.Range(0,contracts.Length);
            if(!completed)order=order.OrderBy(i=>contracts[i].CareerOrder<0?i:contracts[i].CareerOrder);
            foreach(int i in order)
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
