import { Vector3Tuple } from "three"
import { InputScheme } from "../input/DefaultInputs"

export type GlobalPreference =
    | "ScreenMode"
    | "QualitySettings"
    | "ZoomSensitivity"
    | "PitchSensitivity"
    | "YawSensitivity"
    | "ReportAnalytics"
    | "UseMetric"
    | "RenderScoringZones"

export const RobotPreferencesKey: string = "Robots"
export const FieldPreferencesKey: string = "Fields"

export const DefaultGlobalPreferences: { [key: string]: unknown } = {
    ScreenMode: "Windowed",
    QualitySettings: "High",
    ZoomSensitivity: 15,
    PitchSensitivity: 10,
    YawSensitivity: 3,
    ReportAnalytics: false,
    UseMetric: false,
    RenderScoringZones: true,
}

export type IntakePreferences = {
    deltaTransformation: number[]
    zoneDiameter: number
    parentNode: string | undefined
}

export type EjectorPreferences = {
    deltaTransformation: number[]
    ejectorVelocity: number
    parentNode: string | undefined
}

export type RobotPreferences = {
    inputsSchemes: InputScheme[]
    intake: IntakePreferences
    ejector: EjectorPreferences
}

export type ScoringZonePreferences = {
    name: string
    alliance: "red" | "blue"
    parentNode: string | undefined
    points: number
    destroyGamepiece: boolean
    persistentPoints: boolean
    position: [number, number, number]
    rotation: [number, number, number, number]
    scale: [number, number, number]
}

export type FieldPreferences = {
    defaultSpawnLocation: Vector3Tuple
    scoringZones: ScoringZonePreferences[]
}

export function DefaultRobotPreferences(): RobotPreferences {
    return {
        inputsSchemes: [],
        intake: {
            deltaTransformation: [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1],
            zoneDiameter: 0.5,
            parentNode: undefined,
        },
        ejector: {
            deltaTransformation: [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1],
            ejectorVelocity: 1,
            parentNode: undefined,
        },
    }
}

export function DefaultFieldPreferences(): FieldPreferences {
    return { defaultSpawnLocation: [0, 1, 0], scoringZones: [] }
}
