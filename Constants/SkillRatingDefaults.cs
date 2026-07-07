namespace MixFlowWebApp.Constants
{
    public static class SkillRatingDefaults
    {
        public static readonly Dictionary<string, decimal> Ratings = new()
        {
            { "Novice", 3.0m },        // anything below 3.5
            { "Intermediate", 4.0m },  // between 3.5 and 4.5
            { "Advanced", 5.0m }       // anything above 4.5
        };
    }
}
