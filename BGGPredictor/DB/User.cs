using System;

namespace BGGPredictor.DB
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }
        public DateTime CollectionLastFetched { get; set; }
    }
}
