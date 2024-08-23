using Api.Database.Models;
using Api.Utilities;
using Api.Services.Events;
namespace Api.Services
{
    public interface ILocalizationService
    {
        public Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition);
        public Task<bool> RobotIsLocalized(string robotId);
        public Task DelocalizeRobot(string robotId);
        public Task<bool> RobotIsOnSameDeckAsMission(string robotId, string areaId);
        public void TriggerLocalizationTimeout(LocalizationTimeoutEventArgs e);
        public void TriggerLocalizationTimerStartOrReset(LocalizationTimerStartOrResetEventArgs e);
    }

    public class LocalizationService(ILogger<LocalizationService> logger, IRobotService robotService, IInstallationService installationService, IAreaService areaService) : ILocalizationService
    {

        public async Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition)
        {
            var missionInstallation = await installationService.ReadByName(missionDefinition.InstallationCode, readOnly: true);

            if (missionInstallation is null)
            {
                string errorMessage = $"Could not find installation for installation code {missionDefinition.InstallationCode}";
                logger.LogError("{Message}", errorMessage);
                throw new InstallationNotFoundException(errorMessage);
            }

            if (robot.CurrentInstallation.Id != missionInstallation.Id)
            {
                string errorMessage = $"The robot {robot.Name} is on installation {robot.CurrentInstallation.Name} which is not the same as the mission installation {missionInstallation.Name}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotInSameInstallationAsMissionException(errorMessage);
            }
        }

        public async Task LocalizeRobot(string robotId, string areaId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }
            await robotService.UpdateCurrentArea(robot.Id, null);
        }

        public async Task<bool> RobotIsLocalized(string robotId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            return robot.CurrentArea is not null;
        }

        public async Task DelocalizeRobot(string robotId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
                // Remember to add try catch where delocalized is used
            }
            // Try catch the one below
            await robotService.UpdateCurrentArea(robot.Id, null);
        }

        public async Task<bool> RobotIsOnSameDeckAsMission(string robotId, string areaId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"The robot with ID {robotId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.CurrentArea is null)
            {
                const string ErrorMessage = "The robot is not associated with an area and a mission may not be started";
                logger.LogError("{Message}", ErrorMessage);
                throw new RobotCurrentAreaMissingException(ErrorMessage);
            }

            var missionArea = await areaService.ReadById(areaId, readOnly: true);
            if (missionArea is null)
            {
                const string ErrorMessage = "The robot is not located on the same deck as the mission as the area has not been set";
                logger.LogError("{Message}", ErrorMessage);
                throw new AreaNotFoundException(ErrorMessage);
            }

            if (robot.CurrentArea?.Deck is null)
            {
                const string ErrorMessage = "The robot area is not associated with any deck";
                logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }
            if (missionArea.Deck is null)
            {
                const string ErrorMessage = "The mission area is not associated with any deck";
                logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }

            return robot.CurrentArea.Deck.Id == missionArea.Deck.Id;
        }

        public void TriggerLocalizationTimeout(LocalizationTimeoutEventArgs e) { OnLocalizationTimeout(e); }
        protected virtual void OnLocalizationTimeout(LocalizationTimeoutEventArgs e) { LocalizationTimeout?.Invoke(this, e); }
        public static event EventHandler<LocalizationTimeoutEventArgs>? LocalizationTimeout;

        public void TriggerLocalizationTimerStartOrReset(LocalizationTimerStartOrResetEventArgs e) { OnLocalizationTimerStartOrReset(e); }
        protected virtual void OnLocalizationTimerStartOrReset(LocalizationTimerStartOrResetEventArgs e) { LocalizationTimerStartOrReset?.Invoke(this, e); }
        public static event EventHandler<LocalizationTimerStartOrResetEventArgs>? LocalizationTimerStartOrReset;
    }
}
