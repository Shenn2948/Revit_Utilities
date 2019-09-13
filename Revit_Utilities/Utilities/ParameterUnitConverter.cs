namespace Revit_Utilities.Utilities
{
    using System;
    using System.Collections.Generic;

    using Autodesk.Revit.DB;

    public static class ParameterUnitConverter
    {
        private const double MetersInFeet = 0.3048;

        public static double FeetAsMeters(this double param)
        {
            double imperialValue = param;

            return imperialValue * MetersInFeet; // feet
        }

        public static double FeetAsMillimeters(this double param)
        {
            double imperialValue = param;

            return imperialValue * MetersInFeet * 1000;
        }

        public static double FeetAsCentimeters(this double param)
        {
            double imperialValue = param;

            return imperialValue * MetersInFeet * 100;
        }
    }
}