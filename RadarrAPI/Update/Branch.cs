namespace RadarrAPI.Update
{
    /// <summary>
    ///     Contains all update branches of Radarr
    ///     which can have releases.
    /// </summary>
    public enum Branch
    {
        Unknown = 0,
        Develop = 1,
        Nightly = 2,
        Master = 3
    }
}
