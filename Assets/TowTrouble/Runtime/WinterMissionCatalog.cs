using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        internal readonly struct Offer
        {
            internal readonly WinterJob Kind;
            internal readonly string Name, Brief, SourceBay, TargetBay, SiteId;
            internal readonly int Reward, CargoUnits, Model, Tier, ExpectedSeconds;
            internal readonly float Load, SnowScale, CargoUnitMass, CargoFragility;
            internal readonly Vector3 RoadStart, RoadEnd;

            internal Offer(WinterJob kind, string name, string brief, int reward,
                string sourceBay = null, string targetBay = null, string siteId = null,
                Vector3 roadStart = default, Vector3 roadEnd = default, float load = 1350f,
                float snowScale = 1f, int cargoUnits = 1, int model = 3,
                int tier = 0, int expectedSeconds = 120, float cargoUnitMass = 110f, float cargoFragility = 1f)
            {
                Kind = kind;
                Name = name;
                Brief = brief;
                Reward = reward;
                SourceBay = sourceBay;
                TargetBay = targetBay;
                SiteId = siteId;
                RoadStart = roadStart;
                RoadEnd = roadEnd;
                Load = load;
                SnowScale = snowScale;
                CargoUnits = cargoUnits;
                Model = model;
                Tier = tier;
                ExpectedSeconds = expectedSeconds;
                CargoUnitMass = cargoUnitMass;
                CargoFragility = cargoFragility;
            }

            internal string Difficulty => DifficultyName(Tier);
            internal int IntactBonus => Kind == WinterJob.Supply
                ? Mathf.Clamp(Mathf.RoundToInt(Reward * .15f / 50f) * 50, 50, 300) : 0;
            internal string Preparation => Tier == 0 ? "Stock vehicle · winter tires recommended" :
                Kind == WinterJob.Recovery && Tier == 2 ? "Winter tires · winch upgrade and low range recommended" :
                Kind == WinterJob.Recovery || Kind == WinterJob.Tree ? "Winter tires · winch upgrade recommended" :
                Kind == WinterJob.Supply && Tier == 1 ? "Winter tires · low range recommended" :
                "Winter tires · engine upgrade and low range recommended";

            internal string Label => Kind switch
            {
                WinterJob.Supply => CargoUnits + " crate delivery",
                WinterJob.Recovery => "Vehicle recovery",
                WinterJob.Tree => "Road clearance",
                _ => "Snow plowing"
            };

            internal string Terrain => Kind switch
            {
                WinterJob.Supply => "Snow-covered connecting roads",
                WinterJob.Recovery => "Deep snow and icy banks",
                WinterJob.Tree => "Snowy road and shoulder",
                _ => SnowScale > 1f ? "Deep wind-packed snow" : "Fresh snow over packed road"
            };
        }

        internal static string DifficultyName(int tier) => tier == 0 ? "Easy" : tier == 1 ? "Standard" : "Demanding";

        // Estimates include a road approach from the garage, work and delivery. Waiting never changes the offer.
        // Durable job IDs. The first four bits retain the original DLC completion history.
        internal static readonly Offer[] Offers =
        {
            new(WinterJob.Supply, "Supplies for Pinewood",
                "Collect a crate at Lakeside landing and deliver it intact to Pinewood forest depot.", 900,
                sourceBay: "E", targetBay: "B", tier: 0, expectedSeconds: 165, cargoUnitMass: 110f, cargoFragility: 1f),
            new(WinterJob.Recovery, "Workshop Meadow rescue",
                "Pull the pickup out of the snowy ditch, then tow it to Pinewood forest depot.", 850,
                targetBay: "B", siteId: "R01", tier: 0, expectedSeconds: 150),
            new(WinterJob.Tree, "Timber on Lakeside Road",
                "Choose an end of the fallen pine and swing the whole trunk onto the marked shoulder.", 550,
                roadStart: new(-165.9f, 22f, -76.7f), roadEnd: new(-163.4f, 22f, -86.4f), load: 480f, tier: 0, expectedSeconds: 90),
            new(WinterJob.Plow, "Plow Pinewood Road",
                "Fit the blade at the forest approach. Clear both edges through the deep drift between the poles.", 500,
                roadStart: new(-184.931f, 21.873f, -3.588f), roadEnd: new(-220.793f, 21.297f, -10.415f), snowScale: 0.85f, tier: 0, expectedSeconds: 60),
            new(WinterJob.Plow, "Plow the Logging Link",
                "Reopen the logging link. Make a slow central pass and return for the packed shoulders.", 800,
                roadStart: new(-247.723f, 20.838f, -21.268f), roadEnd: new(-278.709f, 20.264f, -40.401f), snowScale: 1.05f, tier: 1, expectedSeconds: 105),
            new(WinterJob.Plow, "Hidden Bay Access",
                "Cut a lane down the isolated bay approach. Keep the blade down on the gentle descent.", 600,
                roadStart: new(-361.731f, 15.284f, -129.751f), roadEnd: new(-363.962f, 13.693f, -168.765f), snowScale: 0.82f, tier: 0, expectedSeconds: 105),
            new(WinterJob.Plow, "Lakeshore Service Road",
                "Clear the flat lakeshore link from the fitting bay to the exit poles.", 600,
                roadStart: new(-136.899f, 22.1f, -211.075f), roadEnd: new(-143.542f, 22.1f, -172.458f), snowScale: 0.72f, tier: 0, expectedSeconds: 105),
            new(WinterJob.Plow, "Beach Road Snowfall",
                "Open the beach road through a broad layer of fresh powder. Finish both marked edges.", 750,
                roadStart: new(21.56f, 18.1f, -297.788f), roadEnd: new(-14.469f, 18.1f, -298.896f), snowScale: 0.9f, tier: 0, expectedSeconds: 135),
            new(WinterJob.Plow, "Eastern Depot Drifts",
                "Remove the wind-packed drifts on the eastern depot approach. Use several shallow passes.", 1250,
                roadStart: new(252f, 22.1f, -81f), roadEnd: new(288.231f, 22.818f, -78.084f), snowScale: 1.2f, tier: 2, expectedSeconds: 150),
            new(WinterJob.Plow, "Canyon Freight Lane",
                "Clear the rising freight lane into the canyon depot. Keep a steady pull uphill.", 1300,
                roadStart: new(344.727f, 29.985f, -213.52f), roadEnd: new(345.762f, 31.593f, -250.19f), snowScale: 1.1f, tier: 1, expectedSeconds: 195),
            new(WinterJob.Plow, "Mountain Refuge Link",
                "Open the exposed mountain link. Sweep the full lane before the final exit poles.", 1050,
                roadStart: new(101.233f, 66.1f, 161.015f), roadEnd: new(137.915f, 66.1f, 162.348f), snowScale: 1.08f, tier: 1, expectedSeconds: 150),
            new(WinterJob.Plow, "North Ridge Climb",
                "Work uphill through the ridge drift. Raise the blade for repositioning and brake on the slope.", 1350,
                roadStart: new(-71.116f, 51.554f, 180.842f), roadEnd: new(-34.986f, 56.929f, 187.233f), snowScale: 0.95f, tier: 2, expectedSeconds: 165),
            new(WinterJob.Plow, "High Cabin Snowbound",
                "Reopen the high cabin switchback link. Deep snow and a climb demand repeated controlled passes.", 2250,
                roadStart: new(-6.819f, 140.964f, 308.622f), roadEnd: new(-42.877f, 145.81f, 310.096f), snowScale: 1.12f, tier: 2, expectedSeconds: 300),
            new(WinterJob.Supply, "Fuel for the Refuge",
                "Load two fuel cases at Pinewood and deliver them to the mountain refuge. The extra weight affects braking.", 1450,
                sourceBay: "B", targetBay: "C", cargoUnits: 2, tier: 1, expectedSeconds: 225, cargoUnitMass: 145f, cargoFragility: 1f),
            new(WinterJob.Supply, "High Cabin Provisions",
                "Carry one fragile food crate from the refuge up to the high cabin. Avoid hard impacts.", 1950,
                sourceBay: "C", targetBay: "D", tier: 2, expectedSeconds: 255, cargoUnitMass: 80f, cargoFragility: 1.35f),
            new(WinterJob.Supply, "Lakeside Repair Stock",
                "Take three heavy repair cases from Pinewood to the lakeside landing. Slow down before the descent.", 950,
                sourceBay: "B", targetBay: "E", cargoUnits: 3, tier: 1, expectedSeconds: 135, cargoUnitMass: 120f, cargoFragility: 1f),
            new(WinterJob.Supply, "Beach Shelter Supplies",
                "Deliver two emergency cases from the landing to the beach shelter across the snowy coastal roads.", 850,
                sourceBay: "E", targetBay: "F", cargoUnits: 2, tier: 0, expectedSeconds: 150, cargoUnitMass: 100f, cargoFragility: 1f),
            new(WinterJob.Supply, "Eastern Generator Parts",
                "Deliver a fragile generator component from the refuge to the eastern outpost.", 1550,
                sourceBay: "C", targetBay: "G", tier: 1, expectedSeconds: 240, cargoUnitMass: 180f, cargoFragility: 1.4f),
            new(WinterJob.Supply, "Canyon Relief Convoy",
                "Carry three relief cases from the eastern outpost to the canyon depot. Protect the load on the final climb.", 1200,
                sourceBay: "G", targetBay: "H", cargoUnits: 3, tier: 1, expectedSeconds: 180, cargoUnitMass: 100f, cargoFragility: 1f),
            new(WinterJob.Recovery, "Logging Truck Snow Trap",
                "Winch the heavy hauler along the marked escape line, then tow it to Pinewood.", 1200,
                targetBay: "B", siteId: "R02", load: 1750f, model: 1, tier: 1, expectedSeconds: 180),
            new(WinterJob.Recovery, "Lakeshore Stranded Scout",
                "Recover the scout from the snowy shore approach and tow it to Lakeside landing.", 2150,
                targetBay: "E", siteId: "R07", load: 1100f, model: 0, tier: 1, expectedSeconds: 345),
            new(WinterJob.Recovery, "Refuge Road Breakdown",
                "Pull the disabled car out of the roadside snow and deliver it to the mountain refuge.", 1450,
                targetBay: "C", siteId: "R09", load: 1250f, model: 2, tier: 1, expectedSeconds: 225),
            new(WinterJob.Recovery, "Mountain Link Snow Rescue",
                "Extract the off-roader from the mountain link drift, then tow it up to the high cabin.", 2350,
                targetBay: "D", siteId: "R13", load: 1500f,
                roadStart: new(109f, 66.1f, 161.3f), roadEnd: new(137.915f, 66.1f, 162.348f), tier: 2, expectedSeconds: 315),
            new(WinterJob.Recovery, "Canyon Freight Rescue",
                "Free the heavy hauler from the freight lane drift and tow it into the canyon depot bay.", 1300,
                targetBay: "H", siteId: "R23", load: 1750f, model: 1,
                roadStart: new(345f, 30.4f, -225f), roadEnd: new(345.762f, 31.593f, -250.19f), tier: 1, expectedSeconds: 195),
            new(WinterJob.Tree, "Logging Road Windfall",
                "A heavy pine blocks the logging link. Pull its tip to turn it into the marked turnout.", 800,
                roadStart: new(-280.701f, 20.228f, -41.527f), roadEnd: new(-289.503f, 20.228f, -46.274f), load: 650f, tier: 1, expectedSeconds: 105),
            new(WinterJob.Tree, "Beach Road Fallen Pine",
                "Rotate the lighter coastal trunk clear of both lane edges and settle it on the shoulder.", 700,
                roadStart: new(2.861f, 18.1f, -298.049f), roadEnd: new(-7.139f, 18.1f, -297.979f), load: 350f, tier: 0, expectedSeconds: 120),
            new(WinterJob.Tree, "Mountain Link Windfall",
                "Move the long mountain pine into the lay-by. Reconnect to the other end if it catches the lane.", 1150,
                roadStart: new(154.189f, 65.941f, 159.155f), roadEnd: new(149.871f, 65.941f, 168.174f), load: 720f, tier: 2, expectedSeconds: 135)
        };
    }
}
