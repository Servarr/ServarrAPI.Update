using System.ComponentModel.DataAnnotations.Schema;
using LidarrAPI.Update;

namespace LidarrAPI.Database.Models
{
    [Table("UpdateFiles")]
    public class UpdateFileEntity
    {
        /// <summary>
        ///     The unique identifier of the update this file belongs to.
        /// </summary>
        public int UpdateEntityId { get; set; }

        /// <summary>
        ///     The <see cref="UpdateEntity"/> this <see cref="UpdateFileEntity"/> belongs to.
        /// </summary>
        public UpdateEntity Update { get; set; }

        /// <summary>
        ///     The <see cref="OperatingSystem"/> this update file belongs to.
        /// </summary>
        public OperatingSystem OperatingSystem { get; set; }

        /// <summary>
        ///     The <see cref="Runtime"/> this file requires.
        /// </summary>
        public Runtime Runtime { get; set; }

        /// <summary>
        ///     The <see cref="Architecture"/> this file requires.
        /// </summary>
        public System.Runtime.InteropServices.Architecture Architecture { get; set; }

        /// <summary>
        ///     The zip file name.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        ///     The zip file download location.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        ///     The hash of the zip file.
        /// </summary>
        public string Hash { get; set; }
    }
}
