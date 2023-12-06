using System.ComponentModel.DataAnnotations;

namespace TotsChallenge.Models
{
    public class Repository
    {
        [Required]
        public string repo { get; set; }
    }

    public class File
    {
        [Required]
        public string name { get; set; }
    }
}
