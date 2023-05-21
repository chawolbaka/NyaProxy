using System.Text.Json;

namespace Analysis
{
    public static class AnalysisData
    {
        public static readonly Dictionary<long, ServerListPingAnalysisData> Pings = new Dictionary<long, ServerListPingAnalysisData>();
        public static readonly Dictionary<long, SessionAnalysisData> Sessions = new Dictionary<long, SessionAnalysisData>();
        private static string _saveDirectory;


        public static void SetDirectory(string dir)
        {
            _saveDirectory = dir;
        }

        public static void Clear()
        {
            Pings.Clear();
            Sessions.Clear();
        }

        public static async Task SaveAsync()
        {
            if (Pings.Count > 0 || Sessions.Count > 0)
            {
                string saveDirectory = Path.Combine(_saveDirectory, "Data");
                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                if (Pings.Count > 0)
                {
                    using FileStream fs = new FileStream(Path.Combine(saveDirectory, $"ping-{DateTime.Now: yyyy-MM-dd mmHHssss}.json"), FileMode.OpenOrCreate);
                    await JsonSerializer.SerializeAsync(fs, Sessions);
                }
                if (Sessions.Count > 0)
                {
                    using FileStream fs = new FileStream(Path.Combine(saveDirectory, $"play-{DateTime.Now: yyyy-MM-dd mmHHssss}.json"), FileMode.OpenOrCreate);
                    await JsonSerializer.SerializeAsync(fs, Sessions);
                }
            }
        }
    }
}