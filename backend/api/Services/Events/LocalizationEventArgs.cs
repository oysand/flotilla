namespace Api.Services.Events
{
    public class LocalizationTimerStartOrResetEventArgs(string robotId) : EventArgs
    {
        public string RobotId { get; } = robotId;
    }

    public class LocalizationTimeoutEventArgs(string robotId) : EventArgs
    {
        public string RobotId { get; } = robotId;
    }
}
