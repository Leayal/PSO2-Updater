namespace Leayal.PSO2.Updater
{
    public enum UpdateResult : short
    {
        Cancelled = -2,
        Failed = -1,
        Unknown = 0,
        Success = 1,
        MissingSomeFiles = 2
    }

    public enum PREPATCH_STATUS : byte
    {
        NONE,
        DOWNLOADING,
        APPLIED,
        UNKNOWN
    }

    public enum UpdateStep : byte
    {
        PSO2UpdateManager_DownloadingPatchList,
        PSO2UpdateManager_BuildingFileList,
        PSO2Updater_FoundInvalidPrepatch,
        PSO2Updater_DeletingInvalidPrepatch,
        PSO2Updater_BeginFileCheckAndDownload,
        PSO2UpdateManager_DownloadingFileStart,
        PSO2UpdateManager_DownloadingFileEnd
    }
}
