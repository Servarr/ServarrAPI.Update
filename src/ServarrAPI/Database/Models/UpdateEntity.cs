using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServarrAPI.Database.Models
{
    [Table("Updates")]
    public class UpdateEntity
    {
        public UpdateEntity()
        {
            New = new List<string>();
            Fixed = new List<string>();
            UpdateFiles = new List<UpdateFileEntity>();
        }

        /// <summary>
        ///     The unique identifier.
        /// </summary>
        public int UpdateEntityId { get; set; }

        /// <summary>
        ///     The version number.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     The release date.
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        [NotMapped]
        public List<string> New { get; set; }

        [NotMapped]
        public List<string> Fixed { get; set; }

        /// <summary>
        ///     The update branch.
        /// </summary>
        public string Branch { get; set; }

        /// <summary>
        ///     The Status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        ///     The files that belong to this update.
        /// </summary>
        public List<UpdateFileEntity> UpdateFiles { get; set; }
        
        [JsonIgnore]
        [Column("New")]
        public string NewStr
        {
            get { return JsonSerializer.Serialize(New); }
            set { New = JsonSerializer.Deserialize<List<string>>(value); }
        }

        [JsonIgnore]
        [Column("Fixed")]
        public string FixedStr
        {
            get { return JsonSerializer.Serialize(Fixed); }
            set { Fixed = JsonSerializer.Deserialize<List<string>>(value); }
        }
    }
}
