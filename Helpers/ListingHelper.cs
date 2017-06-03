namespace SearchProcurement.Helpers
{

    public static class ListingTypes
    {
        public const string Simple = "simple";
        public const string Umbrella = "umbrella";
    }

    public struct Attachment
    {
        public string DocumentName;
        public string FileName;
        public string Url;
    }

}