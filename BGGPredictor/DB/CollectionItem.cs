using System.ComponentModel.DataAnnotations;

namespace BGGPredictor.DB
{
    public class CollectionItem
    {
        public int Id { get; set; }

        [Required]
        public User User { get; set; }

        [Required]
        public Game Game { get; set; }
        
        public float Rating { get; set; }
    }
}
