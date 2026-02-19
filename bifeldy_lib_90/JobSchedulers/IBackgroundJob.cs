namespace bifeldy_lib_90.JobSchedulers {

    public interface IBackgroundJob {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }

}