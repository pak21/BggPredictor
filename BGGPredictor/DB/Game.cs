using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BGGPredictor
{
    public class Game
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public DateTime LastUpdate { get; set; }

        public string Name { get; set; }
        public float? Rating { get; set; }
        public float? BayesRating { get; set; }
        public float? Weight { get; set; }
        public int MinimumPlayers { get; set; }
        public int MaximumPlayers { get; set; }
        public TimeSpan PlayingTime { get; set; }
        public int MinimumAge { get; set; }
    }
}
