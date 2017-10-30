using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LidarrAPI.Database.Models
{
    [Table("Trakt")]
    public class TraktEntity
    {
        public int Id { get; set; }

        public Guid State { get; set; }

        public string Target { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
