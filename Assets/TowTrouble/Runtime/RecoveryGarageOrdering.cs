using System;

namespace TowTrouble
{
    public static class RecoveryGarageOrdering
    {
        private static readonly string[] Prefixes = { "Attachment Top ", "Motor Exhaust ", "Motor Carb ", "Chassis ", "Fender ", "Bumper ", "Bonnet ", "Wheel ", "Body ", "Historic ", "Rod " };
        public static string DisplayName(GaragePart part, RecoveryProfile profile)
        {
            string name = part.Label(profile) ?? "";
            foreach (string prefix in Prefixes)
                if (name.StartsWith(prefix, StringComparison.Ordinal)) return name.Substring(prefix.Length);
            return name;
        }

        public static int Compare(GaragePart a, GaragePart b, RecoveryProfile profile)
        {
            int order = RecoveryEconomy.Price(profile, a).CompareTo(RecoveryEconomy.Price(profile, b));
            if (order != 0) return order;
            if (a.Slot == GarageSlot.Assembly && b.Slot == GarageSlot.Assembly)
            {
                order = a.BodyDrive.CompareTo(b.BodyDrive);
                if (order != 0) return order;
                order = a.BodyMass.CompareTo(b.BodyMass);
                if (order != 0) return order;
                order = a.BodySpeed.CompareTo(b.BodySpeed);
                if (order != 0) return order;
            }
            order = NaturalCompare(DisplayName(a, profile), DisplayName(b, profile));
            return order != 0 ? order : string.CompareOrdinal(a.Id, b.Id);
        }

        // Numeric model/level suffixes read as 2, 3, 10 rather than 10, 2, 3.
        public static int NaturalCompare(string a, string b)
        {
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
                {
                    int endA = i, endB = j;
                    while (endA < a.Length && char.IsDigit(a[endA])) endA++;
                    while (endB < b.Length && char.IsDigit(b[endB])) endB++;
                    int startA = i, startB = j;
                    while (startA < endA - 1 && a[startA] == '0') startA++;
                    while (startB < endB - 1 && b[startB] == '0') startB++;
                    int length = (endA - startA).CompareTo(endB - startB);
                    if (length != 0) return length;
                    for (int n = 0; n < endA - startA; n++)
                        if (a[startA + n] != b[startB + n]) return a[startA + n].CompareTo(b[startB + n]);
                    i = endA; j = endB;
                }
                else
                {
                    int order = char.ToUpperInvariant(a[i]).CompareTo(char.ToUpperInvariant(b[j]));
                    if (order != 0) return order;
                    i++; j++;
                }
            }
            return (a.Length - i).CompareTo(b.Length - j);
        }
    }
}
