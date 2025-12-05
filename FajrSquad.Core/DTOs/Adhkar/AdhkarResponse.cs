namespace FajrSquad.Core.DTOs.Adhkar
{
    public class AdhkarResponse
    {
        public Guid Id { get; set; }
        public string Arabic { get; set; } = string.Empty;
        public string? Transliteration { get; set; }
        public string Translation { get; set; } = string.Empty;
        public int Repetitions { get; set; }
        public string? Source { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Language { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public int CurrentCount { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class AdhkarStatsResponse
    {
        public List<Guid> CompletedToday { get; set; } = new();
        public int TotalCompleted { get; set; }
        public int Streak { get; set; }
        public DateTime? LastCompleted { get; set; }
    }

    public class IncrementAdhkarRequest
    {
        public DateTime? Date { get; set; } // Opzionale, default: oggi
    }

    public class CompleteAdhkarRequest
    {
        public DateTime? Date { get; set; } // Opzionale, default: oggi
    }

    public class ToggleFavoriteRequest
    {
        public bool IsFavorite { get; set; }
    }
}



