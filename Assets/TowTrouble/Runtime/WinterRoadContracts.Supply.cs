using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        private RecoveryVehicle cargoCarrier;
        private Vector3 cargoVelocity, cargoPosition, crateSeat;
        private int cargoGeometry;
        private float cargoDamage, cargoRattle;
        private bool cargoSampleReady;
        private float carriedMass;
        internal int CargoBonus => cargoDamage < .3f ? CurrentOffer.IntactBonus : 0;
        internal bool CargoImpact { get; private set; }
        internal bool CargoWarning => cargoRattle > 0;

        private bool TickSupply(RecoveryVehicle driver, bool attached)
        {
            if (!Held(Near(driver.transform.position, Destination, 5.5f) && driver.Speed < .65f && !attached)) return false;
            if (loaded) return true;
            loaded = true;
            cargoDamage = 0;
            SwitchToDeliveryMarker();
            cargoCarrier = driver;
            crate.transform.SetParent(driver.transform, false);
            FitCargo();
            return false;
        }

        private void FitCargo()
        {
            cargoGeometry = cargoCarrier.GeometryVersion;
            carriedMass = CurrentOffer.CargoUnitMass * CurrentOffer.CargoUnits;
            cargoCarrier.Body.mass += carriedMass;
            crateSeat = cargoCarrier.transform.InverseTransformPoint(cargoCarrier.RearHitch) + new Vector3(0, .83f, .48f);
            crate.transform.localPosition = crateSeat;
            crate.transform.localRotation = Quaternion.identity;
            cargoSampleReady = false;
        }

        private void ReleaseCargo()
        {
            if (cargoCarrier != null && cargoGeometry == cargoCarrier.GeometryVersion)
                cargoCarrier.Body.mass -= carriedMass;
            cargoCarrier = null;
            carriedMass = 0;
            cargoRattle = 0;
            cargoSampleReady = CargoImpact = false;
        }

        internal void TickCargo(RecoveryVehicle driver)
        {
            CargoImpact = false;
            if (!loaded || cargoCarrier != driver) return;
            if (cargoGeometry != driver.GeometryVersion) FitCargo();
            Vector3 velocity = driver.Body.linearVelocity;
            Vector3 position = driver.Body.position;
            if (cargoSampleReady && (position - cargoPosition).sqrMagnitude < 4f)
            {
                float speedLoss = cargoVelocity.magnitude - velocity.magnitude;
                float jolt = Mathf.Max(speedLoss, Mathf.Abs(velocity.y - cargoVelocity.y));
                if (jolt > 3.5f && cargoRattle <= 0)
                {
                    float damage = Mathf.Lerp(.12f, .38f, Mathf.InverseLerp(3.5f, 7f, jolt));
                    cargoDamage = Mathf.Min(1, cargoDamage + damage * CurrentOffer.CargoFragility);
                    cargoRattle = .55f;
                    CargoImpact = true;
                }
            }
            cargoSampleReady = true;
            cargoVelocity = velocity;
            cargoPosition = position;
            cargoRattle = Mathf.Max(0, cargoRattle - Time.fixedDeltaTime);
            crate.transform.localRotation = Quaternion.Euler(Mathf.Sin(cargoRattle * 54f) * cargoRattle * 17f, 0,
                Mathf.Cos(cargoRattle * 47f) * cargoRattle * 11f);
            crate.transform.localPosition = crateSeat + Vector3.up * (cargoRattle * .065f);
        }

        private string SupplyInstruction(bool near)
        {
            if (HoldProgress > 0) return loaded ? "Unloading crate · stay stopped" : "Loading crate · stay stopped";
            if (near) return loaded ? "Park inside the marked unloading bay" : "Park inside the marked loading bay";
            if (!loaded) return DestinationName;
            if (cargoRattle > 0) return CargoBonus > 0 ? "Cargo jolted · slow down" : "Intact delivery bonus lost";
            return "Deliver " + CurrentOffer.CargoUnits + " crate(s) to " + destinationName +
                (CargoBonus > 0 ? " · cargo intact" : " · cargo damaged");
        }
    }
}
