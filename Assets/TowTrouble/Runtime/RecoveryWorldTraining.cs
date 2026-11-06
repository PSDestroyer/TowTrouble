using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryWorld
    {
        private RecoveryContract trainingContract;
        public bool TrainingWinchUsed {get;set;}
        public bool Training => trainingContract != null;
        // Solo-only, called locally by the owning session. No map asset or career contract is changed.
        public void BeginTrainingRecovery(RecoveryContract contract, Vector3 driverPosition)
        {
            if (!IsServerInitialized || Cooperative || !FreeRoam) return;
            StopScenario(); ClearContract(); trainingContract = contract; TrainingWinchUsed=false;
            drivers[0].SetSpawn(driverPosition, Quaternion.identity); drivers[0].SetSimulation(true);
            ApplyContract(); casualty.gameObject.SetActive(true);
            casualty.SetSpawn(contract.Target, Quaternion.identity); casualty.SetSimulation(true);
            runId.Value = System.Guid.NewGuid().ToString("N"); phase.Value = (int)RecoveryPhase.HookUp;
            BeginExtraction();
        }
        public void EndTrainingRecovery()
        {
            if (!IsServerInitialized || Cooperative || !FreeRoam) return;
            StopScenario(); ClearContract(); trainingContract = null; appliedContract = -1;
            phase.Value = (int)RecoveryPhase.Explore; casualty.SetSimulation(false); casualty.gameObject.SetActive(false);
            drivers[0].SetSpawn(garagePosition + new Vector3(-2.5f,.15f,-2), Quaternion.Euler(0,180,0));
            drivers[0].SetSimulation(true);
        }
    }
}
