import Jolt from "@barclah/jolt-physics"
import Driver, { DriverControlMode } from "./Driver"
import { GetLastDeltaT } from "@/systems/physics/PhysicsSystem"
import JOLT from "@/util/loading/JoltSyncLoader"
import { mirabuf } from "@/proto/mirabuf"

class SliderDriver extends Driver {
    private _constraint: Jolt.SliderConstraint

    private _controlMode: DriverControlMode = DriverControlMode.Velocity
    private _targetVelocity: number = 0.0
    private _targetPosition: number = 0.0

    public get targetVelocity(): number {
        return this._targetVelocity
    }
    public set targetVelocity(radsPerSec: number) {
        this._targetVelocity = radsPerSec
    }

    public get targetPosition(): number {
        return this._targetPosition
    }
    public set targetPosition(position: number) {
        this._targetPosition = Math.max(
            this._constraint.GetLimitsMin(),
            Math.min(this._constraint.GetLimitsMax(), position)
        )
    }

    public set minForceLimit(newtons: number) {
        const motorSettings = this._constraint.GetMotorSettings()
        motorSettings.mMinForceLimit = newtons
    }
    public set maxForceLimit(newtons: number) {
        const motorSettings = this._constraint.GetMotorSettings()
        motorSettings.mMaxForceLimit = newtons
    }

    public get controlMode(): DriverControlMode {
        return this._controlMode
    }

    public set controlMode(mode: DriverControlMode) {
        this._controlMode = mode
        switch (mode) {
            case DriverControlMode.Velocity:
                this._constraint.SetMotorState(JOLT.EMotorState_Velocity)
                break
            case DriverControlMode.Position:
                this._constraint.SetMotorState(JOLT.EMotorState_Position)
                break
            default:
                // idk
                break
        }
    }

    public constructor(constraint: Jolt.SliderConstraint, info?: mirabuf.IInfo) {
        super(info)

        this._constraint = constraint

        const motorSettings = this._constraint.GetMotorSettings()
        const springSettings = motorSettings.mSpringSettings
        springSettings.mFrequency = 20 * (1.0 / GetLastDeltaT())
        springSettings.mDamping = 0.999

        motorSettings.mSpringSettings = springSettings
        motorSettings.mMinForceLimit = -900.0
        motorSettings.mMaxForceLimit = 900.0

        this._constraint.SetMotorState(JOLT.EMotorState_Velocity)
        this.controlMode = DriverControlMode.Velocity
    }

    public Update(_: number): void {
        if (this._controlMode == DriverControlMode.Velocity) {
            this._constraint.SetTargetVelocity(this._targetVelocity)
        } else if (this._controlMode == DriverControlMode.Position) {
            this._constraint.SetTargetPosition(this._targetPosition)
        }
    }
}

export default SliderDriver
