namespace NewsApi.Application.Configuration
{
    public class FeedOptions
    {
        public int CachingTime { get; set; } = 2;
        public double FastRequestTime { get; set; } = 100;
        public double AverageRequestTime { get; set; } = 200;
        public double SlowRequestTime { get; set; } = 5000;
        public int AnomalyCheckInterval { get; set; } = 1;
        public int AnomalyDetectionWindow { get; set; } = 5;
        public double AnomalyThresholdMultiplier { get; set; } = 1.5;
    }
}
