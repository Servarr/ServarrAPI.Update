namespace ServarrAPI.Controllers.Update
{
    public class UpdatePackageContainer
    {
        /// <summary>
        ///     Set to true if this is an update the client needs.
        ///     If set to false, don't add a <see cref="UpdatePackage"/>.
        /// </summary>
        public bool Available { get; set; }

        public UpdatePackage UpdatePackage { get; set; }
    }
}
