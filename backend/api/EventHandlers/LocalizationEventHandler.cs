using System.Collections.Concurrent;
using Api.Database.Models;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;
using Timer = System.Timers.Timer;
namespace Api.EventHandlers
{
    /// <summary>
    ///     A background service which listens to events and performs callback functions.
    /// </summary>
    public class LocalizationEventHandler : EventHandlerBase
    {

        private readonly int _localizationTimerTimeout;

        private readonly ConcurrentDictionary<string, Timer> _localizationTimerTimers = new();
        private readonly ILogger<LocalizationEventHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public LocalizationEventHandler(
            ILogger<LocalizationEventHandler> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config
        )
        {
            _logger = logger;

            _localizationTimerTimeout = config.GetValue<int>("LocalizationTimeout");

            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        private ILocalizationService LocalizationServiceInstance =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILocalizationService>();

        private IRobotService RobotService =>
                _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private IMissionRunService MissionRunService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

        private IMissionSchedulingService MissionSchedulingService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionSchedulingService>();

        public override void Subscribe()
        {
            LocalizationService.LocalizationTimeout += OnLocalizationTimeout;
            LocalizationService.LocalizationTimerStartOrReset += OnLocalizationTimerStartOrReset;
        }

        public override void Unsubscribe()
        {
            LocalizationService.LocalizationTimeout -= OnLocalizationTimeout;
            LocalizationService.LocalizationTimerStartOrReset -= OnLocalizationTimerStartOrReset;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        public async void OnLocalizationTimeout(object? sender, LocalizationTimeoutEventArgs e)
        {
            try { await LocalizationServiceInstance.DelocalizeRobot(e.RobotId); }
            catch (RobotNotFoundException)
            {
                _logger.LogError("Could not find robot with ID '{RobotId}' when trying to delocalize", e.RobotId);
                return;
            }

        }

        private async void OnLocalizationTimerStartOrReset(object? sender, LocalizationTimerStartOrResetEventArgs e)
        {

            // var isarRobotHeartbeat = (IsarRobotHeartbeatMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByIsarId(e.RobotId);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Could not find robot with id '{RobotId}' when starting or resetting timer",
                    e.RobotId
                );
                return;
            }

            // if (!_localizationTimerTimers.ContainsKey(robot.IsarId)) { AddTimerForRobot(isarRobotHeartbeat, robot); }

            // _logger.LogDebug(
            //     "Reset connection timer for ISAR '{IsarId}' ('{RobotName}')",
            //     robot.IsarId,
            //     robot.Name
            // );

            // _localizationTimerTimers[robot.IsarId].Reset();

            // if (robot.IsarConnected) { return; }
            // try
            // {
            //     await RobotService.UpdateCurrentArea(robot.Id, null);
            // }
            // catch (Exception e)
            // {
            //     _logger.LogWarning(
            //         "Failed to set robot to ISAR connected for ISAR ID '{IsarId}' ('{RobotName}')'. Exception: {Message} ",
            //         isarRobotHeartbeat.IsarId,
            //         isarRobotHeartbeat.RobotName,
            //         e.Message
            //     );
            //     return;
            // }
        }

        private void AddTimerForRobot(IsarRobotHeartbeatMessage isarRobotHeartbeat, Robot robot)
        {
            var timer = new Timer(_localizationTimerTimeout * 1000);
            timer.Elapsed += (_, _) => OnTimeoutEvent(isarRobotHeartbeat);
            timer.Start();

            if (_localizationTimerTimers.TryAdd(robot.IsarId, timer)) { _logger.LogInformation("Added new timer for ISAR '{IsarId}' ('{RobotName}')", robot.IsarId, robot.Name); }
            else
            {
                _logger.LogWarning("Failed to add new timer for ISAR '{IsarId}' ('{RobotName})'", robot.IsarId, robot.Name);
                timer.Close();
            }
        }

        private async void OnTimeoutEvent(IsarRobotHeartbeatMessage robotHeartbeatMessage)
        {
            var robot = await RobotService.ReadByIsarId(robotHeartbeatMessage.IsarId);
            if (robot is null)
            {
                _logger.LogError(
                    "Connection to ISAR instance '{Id}' ('{RobotName}') timed out but the corresponding robot could not be found in the database",
                    robotHeartbeatMessage.IsarId,
                    robotHeartbeatMessage.IsarId
                );
            }
            else
            {
                _logger.LogWarning(
                    "Connection to ISAR instance '{Id}' timed out - It will be disabled and active missions aborted",
                    robotHeartbeatMessage.IsarId
                );

                if (robot.CurrentMissionId != null)
                {
                    var missionRun = await MissionRunService.ReadById(robot.CurrentMissionId);
                    if (missionRun != null)
                    {
                        _logger.LogError(
                            "Mission '{MissionId}' ('{MissionName}') failed due to ISAR timeout",
                            missionRun.Id,
                            missionRun.Name
                        );
                        missionRun.SetToFailed("Lost connection to ISAR during mission");
                        await MissionRunService.Update(missionRun);
                    }
                }

                try
                {
                    await RobotService.UpdateRobotIsarConnected(robot.Id, false);
                    await RobotService.UpdateCurrentMissionId(robot.Id, null);
                }
                catch (RobotNotFoundException) { return; }
            }

            if (!_localizationTimerTimers.TryGetValue(robotHeartbeatMessage.IsarId, out var timer)) { return; }
            timer.Close();
            _localizationTimerTimers.Remove(robotHeartbeatMessage.IsarId, out _);
            _logger.LogError("Removed timer for ISAR instance {RobotName} with ID '{Id}'", robotHeartbeatMessage.RobotName, robotHeartbeatMessage.IsarId);
        }
    }
}
