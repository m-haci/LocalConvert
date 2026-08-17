namespace LocalConvert.Core.Conversion;

public static class ConversionOptionKeys
{
    public const string SplitMode = "splitMode";
    public const string PageRange = "pageRange";
    public const string PageOrder = "pageOrder";
    public const string RotateDegrees = "rotateDegrees";
    public const string OfficeEngine = "officeEngine";

    public static class SplitModes
    {
        public const string AllPages = "allPages";
        public const string Range = "range";
    }

    public static class RotateDegreesValues
    {
        public const string Ninety = "90";
        public const string OneHundredEighty = "180";
        public const string TwoHundredSeventy = "270";
    }
}
