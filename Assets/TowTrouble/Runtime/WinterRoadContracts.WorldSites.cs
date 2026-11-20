using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        // A site preview owns its geometry separately from the active job. Completion receipts rebuild it on load.
        internal void BuildWorldSite(int index, bool completed)
        {
            ActiveIndex = index;
            EnsureMaterials();
            if (Offers[index].Kind == WinterJob.Plow)
            {
                BuildPinewoodRoad(Offers[index]);
                pinewoodRoad.name = Offers[index].Name + (completed ? " · reopened" : " · snowbound");
                SetPinewoodRoad(completed);
                return;
            }
            ConfigureJob(index);
            props = new GameObject(Offers[index].Name + (completed ? " · shoulder" : " · blocked lane"));
            props.transform.SetParent(owner, false);
            var tree = CreateTree(props.transform, (completed ? layBy : treeRoad) + Vector3.up * .37f,
                completed ? treeHeading : treeHeading * Quaternion.Euler(0, 82f, 0));
            tree.isKinematic = true;
        }
        internal void ShowWorldSite(bool show)
        {
            if (pinewoodRoad != null) pinewoodRoad.SetActive(show);
            if (props != null) props.SetActive(show);
        }
    }
}
