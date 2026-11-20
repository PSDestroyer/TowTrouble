using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class WinterExpedition
    {
        private WinterRegion region;
        internal bool ForestRoute { get; private set; }
        public WinterWeatherState Weather => WinterWeatherRules.At(session != null ? session.World.EnvironmentSeconds : 0);
        internal string PinewoodProgress => region?.ProgressText ?? "Pinewood · clear the access road";
        internal bool ServiceReady => region?.ServiceReady == true;
        internal Vector3 ServicePosition => region.ServicePosition;
        internal bool FollowingForestRoute => ForestRoute && HasCargo && HasRouteChoice(ActiveJobIndex);
        internal bool FollowingRegionalRoute => HasCargo && HasRouteChoice(ActiveJobIndex);
        internal List<Vector3> RegionalGuidanceRoute => ForestRoute ? region.Route(ActiveJobIndex == 15) : region.RoadRoute(ActiveJobIndex == 15);
        internal bool HasRouteChoice(int id) => id == 0 || id == 15;
        internal string RoutePlan => region.RoutePlan(ForestRoute);
        internal void ToggleRoute() => ForestRoute = !ForestRoute;
        internal bool CanUseService(Vector3 position) => ServiceReady && !Attached && !HasCargo &&
            Mathf.Abs(position.y - ServicePosition.y) < 3 &&
            Vector2.Distance(new Vector2(position.x, position.z), new Vector2(ServicePosition.x, ServicePosition.z)) < 7;
        internal float WeatherGrip(Vector3 point, bool packed) => WinterWeatherRules.Grip(point, packed, Weather);

        internal bool CanStart(int id) => string.IsNullOrEmpty(Requirement(id));
        internal string Requirement(int id)
        {
            if (session == null) return "Preparing expedition";
            int complete = session.Profile.winterJobs;
            bool Done(int job) => (complete & (1 << job)) != 0;
            // Existing receipts always keep their replay access, including out-of-order legacy progress.
            if (id < WinterRoadContracts.Offers.Length && id >= 0 && Done(id) ||
                id == WinterRoadContracts.Offers.Length && (session.Profile.winterSurveys & 1) != 0) return null;
            int prerequisite = id == 1 ? 3 : id == 0 ? 1 : id == WinterRoadContracts.Offers.Length ? 0 : -1;
            return prerequisite >= 0 && !Done(prerequisite) ? "Requires: " + WinterRoadContracts.Offers[prerequisite].Name : null;
        }
        internal string Story(int id)
        {
            string requirement = Requirement(id);
            if (requirement != null) return requirement;
            return id == 3 ? "Pinewood 1/4 · reopen the access road" : id == 1 ? "Pinewood 2/4 · bring back the service pickup" :
                id == 0 ? "Pinewood 3/4 · restock the regional station" : id == WinterRoadContracts.Offers.Length
                    ? "Pinewood 4/4 · file the weather report to open local service" : "Region work · changes remain after your return";
        }
    }
}
