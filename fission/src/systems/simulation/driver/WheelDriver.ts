import Jolt from "@barclah/jolt-physics"
import Driver from "./Driver"
import JOLT from "@/util/loading/JoltSyncLoader"
import { SimType } from "../wpilib_brain/WPILibBrain"
import { mirabuf } from "@/proto/mirabuf"

const LATERIAL_FRICTION = 0.6
const LONGITUDINAL_FRICTION = 0.8

class WheelDriver extends Driver {
    private _constraint: Jolt.VehicleConstraint
    private _wheel: Jolt.WheelWV
    public deviceType?: SimType
    public device?: string
    private _reversed: boolean

    private _targetWheelSpeed: number = 0.0

    public get targetWheelSpeed(): number {
        return this._targetWheelSpeed
    }
    public set targetWheelSpeed(radsPerSec: number) {
        this._targetWheelSpeed = radsPerSec
    }

    public get constraint(): Jolt.VehicleConstraint {
        return this._constraint
    }

    public constructor(
        constraint: Jolt.VehicleConstraint,
        info?: mirabuf.IInfo,
        deviceType?: SimType,
        device?: string,
        reversed: boolean = false
    ) {
        super(info)

        this._constraint = constraint
        this._reversed = reversed
        this.deviceType = deviceType
        this.device = device
        this._wheel = JOLT.castObject(this._constraint.GetWheel(0), JOLT.WheelWV)
        this._wheel.set_mCombinedLateralFriction(LATERIAL_FRICTION)
        this._wheel.set_mCombinedLongitudinalFriction(LONGITUDINAL_FRICTION)
    }

    public Update(_: number): void {
        this._wheel.SetAngularVelocity(this._targetWheelSpeed * (this._reversed ? -1 : 1))
    }

    public set reversed(val: boolean) {
        this._reversed = val
    }
}

export default WheelDriver
