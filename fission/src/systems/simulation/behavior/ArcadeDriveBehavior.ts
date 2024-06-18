import WheelDriver from "../driver/WheelDriver";
import WheelRotationStimulus from "../stimulus/WheelStimulus";
import Behavior from "./Behavior";
import InputSystem from "@/systems/input/InputSystem";

class ArcadeDriveBehavior extends Behavior {
    leftWheels: WheelDriver[];
    rightWheels: WheelDriver[];

    private driveSpeed = 30;
    private turnSpeed = 30;

    constructor(leftWheels: WheelDriver[], rightWheels: WheelDriver[], leftStimuli: WheelRotationStimulus[], rightStimuli: WheelRotationStimulus[]) {
        super(leftWheels.concat(rightWheels), leftStimuli.concat(rightStimuli));
        this.leftWheels = leftWheels;
        this.rightWheels = rightWheels;
    }

    driveSpeeds(linearVelocity: number, rotationVelocity: number) {
        let leftSpeed = linearVelocity + rotationVelocity;
        let rightSpeed = linearVelocity - rotationVelocity;
    
        this.leftWheels.forEach((wheel) => wheel.targetWheelSpeed = leftSpeed);
        this.rightWheels.forEach((wheel) => wheel.targetWheelSpeed = rightSpeed);
    }

    public Update(_: number): void {
        this.driveSpeeds(InputSystem.getAxis("arcadeForward", "arcadeBackward")*this.driveSpeed, 
        InputSystem.getAxis("arcadeRight", "arcadeLeft")*this.turnSpeed);
    }
}

export default ArcadeDriveBehavior;