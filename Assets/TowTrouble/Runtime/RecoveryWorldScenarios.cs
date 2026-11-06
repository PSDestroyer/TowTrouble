using UnityEngine;
using FishNet.Object.Synchronizing;
namespace TowTrouble
{
    public sealed partial class RecoveryWorld
    {
        [SerializeField] private RecoveryTowObstacle[] siteObstacles=System.Array.Empty<RecoveryTowObstacle>();
        [SerializeField] private RecoveryDistressVisuals distressVisuals;
        public void ConfigureDistressVisuals(RecoveryDistressVisuals value){distressVisuals=value;}
        private void PresentDistress()
        {if(distressVisuals!=null)distressVisuals.Present(Contract.SituationKind,FreeRoam&&HasContract);}
        private readonly SyncVar<bool> obstaclePending=new SyncVar<bool>();
        private RecoveryTowObstacle activeObstacle;
        private Vector3 obstacleRemotePosition;
        private int presentedContract=-1;
        private Quaternion obstacleRemoteRotation=Quaternion.identity;
        public bool ClearingObstacle=>FreeRoam&&HasContract&&obstaclePending.Value&&activeObstacle!=null;
        public Vector3 TowPoint=>ClearingObstacle?activeObstacle.Hitch:casualty.FrontHitch;
        public Vector3 TowTargetPosition=>ClearingObstacle?activeObstacle.transform.position:casualty.transform.position;
        private Rigidbody TowBody=>ClearingObstacle?activeObstacle.Body:casualty.Body;
        public void ConfigureObstacles(RecoveryTowObstacle[] values){siteObstacles=values;}
        private void FindObstacle()
        {var previous=activeObstacle;activeObstacle=null;if(!FreeRoam||Training)return;foreach(var item in siteObstacles)if(item!=null&&item.SiteIndex==ContractIndex){activeObstacle=item;break;}if(previous!=activeObstacle&&activeObstacle!=null){obstacleRemotePosition=activeObstacle.Home;obstacleRemoteRotation=activeObstacle.HomeRotation;}}
        private void BeginScenario()
        {
            foreach(var item in siteObstacles)if(item!=null)item.ResetObstacle();FindObstacle();
            obstaclePending.Value=activeObstacle!=null;
            if(activeObstacle!=null)activeObstacle.Body.isKinematic=false;
            casualty.SetSimulation(!obstaclePending.Value);
            BeginExtraction();
        }
        private void StopScenario()
        {ResetExtraction();obstaclePending.Value=false;if(Training){activeObstacle=null;return;}foreach(var item in siteObstacles)if(item!=null)item.ResetObstacle();activeObstacle=null;}
        private void TickScenario(Vector3 totalPull)
        {
            if(!FreeRoam||!HasContract)return;
            if(ClearingObstacle)
            {
                if(!activeObstacle.Cleared)return;
                // A new object always requires a fresh connection; no cable teleports to the car.
                activeObstacle.Body.isKinematic=true;obstaclePending.Value=false;
                for(int i=0;i<2;i++){connected[i]=false;tensions[i]=0;winching[i]=false;extending[i]=false;}
                casualty.SetSimulation(true);phase.Value=(int)RecoveryPhase.HookUp;BeginExtraction();return;
            }
            if(extraction!=null){TickExtraction(totalPull);return;}
            float remaining=1-Mathf.Clamp01(Vector3.Distance(casualty.transform.position,Contract.Target)/Contract.Extraction);
            float resistance=Contract.BreakoutResistance*remaining;
            var velocity=Vector3.ProjectOnPlane(casualty.Body.linearVelocity,Vector3.up);
            Vector3 oppose=velocity.magnitude>.15f?velocity.normalized:Vector3.ProjectOnPlane(totalPull,Vector3.up).normalized;
            float magnitude=velocity.magnitude>.15f?resistance:Mathf.Min(resistance,totalPull.magnitude);
            casualty.Body.AddForce(-oppose*magnitude);
        }
        private void ReceiveObstacle(Vector3 position,Quaternion rotation)
        {FindObstacle();obstacleRemotePosition=position;obstacleRemoteRotation=rotation;}
        private void PresentObstacle()
        {
            if(IsServerInitialized||!IsClientInitialized)return;
            int current=FreeRoam&&HasContract?ContractIndex:-1;
            if(current!=presentedContract){foreach(var item in siteObstacles)if(item!=null&&item!=activeObstacle)item.ResetObstacle();if(current<0&&activeObstacle!=null)activeObstacle.ResetObstacle();presentedContract=current;}
            if(current<0)return;
            if(activeObstacle==null)FindObstacle();
            if(activeObstacle!=null){activeObstacle.Body.isKinematic=true;activeObstacle.transform.SetPositionAndRotation(obstacleRemotePosition,obstacleRemoteRotation);}
        }
    }
}
