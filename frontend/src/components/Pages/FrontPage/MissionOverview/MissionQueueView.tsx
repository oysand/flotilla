import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { Robot } from 'models/Robot'
import { RefreshProps } from '../FrontPage'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { CreateMissionButton } from './CreateMissionButton'
import { MissionDefinition } from 'models/MissionDefinition'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`

const MissionTable = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 1rem;
`

const MissionButtonView = styled.div`
    display: flex;
    gap: 1rem;
`
const mapEchoMissionToString = (missions: MissionDefinition[]): Map<string, MissionDefinition> => {
    var missionMap = new Map<string, MissionDefinition>()
    missions.forEach((mission: MissionDefinition) => {
        missionMap.set(mission.echoMissionId + ': ' + mission.name, mission)
    })
    return missionMap
}

const mapRobotsToString = (robots: Robot[]): Map<string, Robot> => {
    var robotMap = new Map<string, Robot>()
    robots.forEach((robot: Robot) => {
        robotMap.set(robot.name + ' (' + robot.model.type + ')', robot)
    })
    return robotMap
}

export function MissionQueueView({ refreshInterval }: RefreshProps) {
    const missionPageSize = 100
    const [missionQueue, setMissionQueue] = useState<Mission[]>([])
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<MissionDefinition[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, MissionDefinition>>(
        new Map<string, MissionDefinition>()
    )
    const [robotOptions, setRobotOptions] = useState<Map<string, Robot>>(new Map<string, Robot>())
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [isFetchingEchoMissions, setIsFetchingEchoMissions] = useState<boolean>(false)
    const { assetCode } = useAssetContext()

    const fetchEchoMissions = () => {
        setIsFetchingEchoMissions(true)
        BackendAPICaller.getAvailableEchoMission(assetCode as string).then((missions) => {
            const echoMissionsMap: Map<string, MissionDefinition> = mapEchoMissionToString(missions)
            setEchoMissions(echoMissionsMap)
            setIsFetchingEchoMissions(false)
        })
    }

    const onSelectedEchoMissions = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: MissionDefinition[] = []
        selectedEchoMissions.forEach((selectedEchoMission: string) => {
            if (echoMissions) echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as MissionDefinition)
        })
        setSelectedEchoMissions(echoMissionsToSchedule)
    }
    const onSelectedRobot = (selectedRobot: string) => {
        if (robotOptions === undefined) return

        setSelectedRobot(robotOptions.get(selectedRobot) as Robot)
    }

    const onScheduleButtonPress = () => {
        if (selectedRobot === undefined) return

        selectedEchoMissions.forEach((mission: MissionDefinition) => {
            BackendAPICaller.postMission(mission.echoMissionId, selectedRobot.id, assetCode)
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const onDeleteMission = (mission: Mission) => {
        BackendAPICaller.deleteMission(mission.id)
    }

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getEnabledRobots().then((robots) => {
                const mappedRobots: Map<string, Robot> = mapRobotsToString(robots)
                setRobotOptions(mappedRobots)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getMissions({
                statuses: [MissionStatus.Pending],
                pageSize: missionPageSize,
                orderBy: 'DesiredStartTime',
            }).then((missions) => {
                setMissionQueue(missions.content)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    useEffect(() => {
        if (selectedRobot === undefined || selectedEchoMissions.length === 0) {
            setScheduleButtonDisabled(true)
        } else {
            setScheduleButtonDisabled(false)
        }
    }, [selectedRobot, selectedEchoMissions])

    useEffect(() => {
        if (Array.from(robotOptions.keys()).length === 0 || assetCode === '') {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [robotOptions, assetCode])

    var missionQueueDisplay = missionQueue.map(function (mission, index) {
        return <MissionQueueCard key={index} mission={mission} onDeleteMission={onDeleteMission} />
    })

    return (
        <StyledMissionView>
            <Typography variant="h1" color="resting">
                {TranslateText('Mission Queue')}
            </Typography>
            <MissionTable>
                {missionQueue.length > 0 && missionQueueDisplay}
                {missionQueue.length === 0 && <EmptyMissionQueuePlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog
                    robotOptions={Array.from(robotOptions.keys())}
                    echoMissionsOptions={Array.from(echoMissions.keys())}
                    onSelectedMissions={onSelectedEchoMissions}
                    onSelectedRobot={onSelectedRobot}
                    onScheduleButtonPress={onScheduleButtonPress}
                    fetchEchoMissions={fetchEchoMissions}
                    scheduleButtonDisabled={scheduleButtonDisabled}
                    frontPageScheduleButtonDisabled={frontPageScheduleButtonDisabled}
                    isFetchingEchoMissions={isFetchingEchoMissions}
                ></ScheduleMissionDialog>
                {CreateMissionButton()}
            </MissionButtonView>
        </StyledMissionView>
    )
}
