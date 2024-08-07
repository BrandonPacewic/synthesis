import {
    Array_ThreeMatrix4,
    JoltMat44_ThreeMatrix4,
    ThreeQuaternion_JoltQuat,
    ThreeVector3_JoltVec3,
} from "@/util/TypeConversions"
import MirabufSceneObject, { RigidNodeAssociate } from "./MirabufSceneObject"
import JOLT from "@/util/loading/JoltSyncLoader"
import World from "@/systems/World"
import Jolt from "@barclah/jolt-physics"
import * as THREE from "three"
import { OnContactAddedEvent, OnContactRemovedEvent } from "@/systems/physics/ContactEvents"
import SceneObject from "@/systems/scene/SceneObject"
import { ScoringZonePreferences } from "@/systems/preferences/PreferenceTypes"
import SimulationSystem from "@/systems/simulation/SimulationSystem"
import PreferencesSystem from "@/systems/preferences/PreferencesSystem"
import { DeltaFieldTransforms_PhysicalProp } from "@/util/threejs/MeshCreation"

class ScoringZoneSceneObject extends SceneObject {
    //Official FIRST hex
    static redMaterial = new THREE.MeshPhongMaterial({
        color: 0xed1c24,
        shininess: 0.0,
        opacity: 0.7,
        transparent: true,
    })
    static blueMaterial = new THREE.MeshPhongMaterial({
        color: 0x0066b3,
        shininess: 0.0,
        opacity: 0.7,
        transparent: true,
    }) //0x0000ff
    static transparentMaterial = new THREE.MeshPhongMaterial({
        color: 0x0000,
        shininess: 0.0,
        opacity: 0.0,
        transparent: true,
    })

    private _parentAssembly: MirabufSceneObject
    private _parentBodyId?: Jolt.BodyID
    private _deltaTransformation?: THREE.Matrix4

    private _toRender: boolean
    private _prefs?: ScoringZonePreferences
    private _joltBodyId?: Jolt.BodyID
    private _mesh?: THREE.Mesh
    private _collision?: (event: OnContactAddedEvent) => void
    private _collisionRemoved?: (event: OnContactRemovedEvent) => void

    private _gpContacted: Jolt.BodyID[] = []
    private _prevGP: Jolt.BodyID[] = []

    public get gpContacted() {
        return this._gpContacted
    }

    public constructor(parentAssembly: MirabufSceneObject, index: number, render?: boolean) {
        super()

        console.debug("Trying to create scoring zone...")

        this._parentAssembly = parentAssembly
        this._prefs = this._parentAssembly.fieldPreferences?.scoringZones[index]
        this._toRender = render ?? PreferencesSystem.getGlobalPreference<boolean>("RenderScoringZones")
    }

    public Setup(): void {
        if (this._prefs) {
            this._parentBodyId = this._parentAssembly.mechanism.nodeToBody.get(
                this._prefs.parentNode ?? this._parentAssembly.rootNodeId
            )

            if (this._parentBodyId) {
                // Create a default sensor
                this._joltBodyId = World.PhysicsSystem.CreateSensor(new JOLT.BoxShapeSettings(new JOLT.Vec3(1, 1, 1)))
                if (!this._joltBodyId) {
                    console.log("Failed to create scoring zone. No Jolt Body")
                    return
                }

                // Position/rotate/scale sensor to settings
                this._deltaTransformation = Array_ThreeMatrix4(this._prefs.deltaTransformation)
                const fieldTransformation = JoltMat44_ThreeMatrix4(
                    World.PhysicsSystem.GetBody(this._parentBodyId).GetWorldTransform()
                )
                const props = DeltaFieldTransforms_PhysicalProp(this._deltaTransformation, fieldTransformation)

                World.PhysicsSystem.SetBodyPosition(this._joltBodyId, ThreeVector3_JoltVec3(props.translation))
                World.PhysicsSystem.SetBodyRotation(this._joltBodyId, ThreeQuaternion_JoltQuat(props.rotation))
                const shapeSettings = new JOLT.BoxShapeSettings(
                    new JOLT.Vec3(props.scale.x / 2, props.scale.y / 2, props.scale.z / 2)
                )
                const shape = shapeSettings.Create()
                World.PhysicsSystem.SetShape(this._joltBodyId, shape.Get(), false, Jolt.EActivation_Activate)

                // Mesh for the user to visualize sensor
                this._mesh = World.SceneRenderer.CreateBox(
                    new JOLT.Vec3(1, 1, 1),
                    ScoringZoneSceneObject.transparentMaterial
                )
                World.SceneRenderer.scene.add(this._mesh)

                if (this._toRender) {
                    this._mesh.position.set(props.translation.x, props.translation.y, props.translation.z)
                    this._mesh.rotation.setFromQuaternion(props.rotation)
                    this._mesh.scale.set(props.scale.x, props.scale.y, props.scale.z)
                }

                // Detect new gamepiece listener
                this._collision = (event: OnContactAddedEvent) => {
                    const body1 = event.message.body1
                    const body2 = event.message.body2

                    if (body1.GetIndexAndSequenceNumber() == this._joltBodyId?.GetIndexAndSequenceNumber()) {
                        this.ZoneCollision(body2)
                    } else if (body2.GetIndexAndSequenceNumber() == this._joltBodyId?.GetIndexAndSequenceNumber()) {
                        this.ZoneCollision(body1)
                    }
                }
                OnContactAddedEvent.AddListener(this._collision)

                // If persistent, detect gamepiece removed listener
                if (this._prefs.persistentPoints) {
                    this._collisionRemoved = (event: OnContactRemovedEvent) => {
                        if (this._prefs?.persistentPoints) {
                            const body1 = event.message.GetBody1ID()
                            const body2 = event.message.GetBody2ID()

                            if (body1.GetIndexAndSequenceNumber() == this._joltBodyId?.GetIndexAndSequenceNumber()) {
                                this.ZoneCollisionRemoved(body2)
                            } else if (
                                body2.GetIndexAndSequenceNumber() == this._joltBodyId?.GetIndexAndSequenceNumber()
                            ) {
                                this.ZoneCollisionRemoved(body1)
                            }
                        }
                    }
                    OnContactRemovedEvent.AddListener(this._collisionRemoved)
                }

                console.debug("Scoring zone created successfully")
            }
        }
    }

    public Update(): void {
        if (this._parentBodyId && this._deltaTransformation && this._joltBodyId && this._prefs) {
            // Update translation, rotation, and scale
            const fieldTransformation = JoltMat44_ThreeMatrix4(
                World.PhysicsSystem.GetBody(this._parentBodyId).GetWorldTransform()
            )
            const props = DeltaFieldTransforms_PhysicalProp(this._deltaTransformation, fieldTransformation)

            World.PhysicsSystem.SetBodyPosition(this._joltBodyId, ThreeVector3_JoltVec3(props.translation))
            World.PhysicsSystem.SetBodyRotation(this._joltBodyId, ThreeQuaternion_JoltQuat(props.rotation))
            const shapeSettings = new JOLT.BoxShapeSettings(
                new JOLT.Vec3(props.scale.x / 2, props.scale.y / 2, props.scale.z / 2)
            )
            const shape = shapeSettings.Create()
            World.PhysicsSystem.SetShape(this._joltBodyId, shape.Get(), false, Jolt.EActivation_Activate)

            // Mesh for visualization
            this._toRender = PreferencesSystem.getGlobalPreference<boolean>("RenderScoringZones")
            if (this._mesh)
                if (this._toRender) {
                    this._mesh.position.set(props.translation.x, props.translation.y, props.translation.z)
                    this._mesh.rotation.setFromQuaternion(props.rotation)
                    this._mesh.scale.set(props.scale.x, props.scale.y, props.scale.z)
                    this._mesh.material =
                        this._prefs.alliance == "red"
                            ? ScoringZoneSceneObject.redMaterial
                            : ScoringZoneSceneObject.blueMaterial
                } else {
                    this._mesh.material = ScoringZoneSceneObject.transparentMaterial
                }

            // If persistent points, update points based on how many gamepieces in zone
            if (this._prefs.persistentPoints)
                if (this._gpContacted.length != this._prevGP.length) {
                    if (this._prefs.alliance == "red") {
                        SimulationSystem.redScore +=
                            (this._gpContacted.length - this._prevGP.length) * this._prefs.points
                    } else {
                        SimulationSystem.blueScore +=
                            (this._gpContacted.length - this._prevGP.length) * this._prefs.points
                    }
                    const event = new OnScoreChangedEvent(SimulationSystem.redScore, SimulationSystem.blueScore)
                    event.Dispatch()
                    this._prevGP = Object.assign([], this._gpContacted)
                }
        } else {
            console.debug("Failed to update scoring zone")
        }
    }

    public Dispose(): void {
        console.debug("Destroying scoring zone")

        if (this._joltBodyId) {
            World.PhysicsSystem.DestroyBodyIds(this._joltBodyId)
            if (this._mesh) {
                this._mesh.geometry.dispose()
                ;(this._mesh.material as THREE.Material).dispose()
                World.SceneRenderer.scene.remove(this._mesh)
            }
        }

        if (this._collision) OnContactAddedEvent.RemoveListener(this._collision)
        if (this._collisionRemoved) OnContactRemovedEvent.RemoveListener(this._collisionRemoved)
    }

    private ZoneCollision(gpID: Jolt.BodyID) {
        const associate = <RigidNodeAssociate>World.PhysicsSystem.GetBodyAssociation(gpID)
        if (associate?.isGamePiece && this._prefs) {
            // If persistent, Update() will handle points
            if (this._prefs.persistentPoints) {
                this._gpContacted.push(gpID)
            } else {
                if (this._prefs.alliance == "red") {
                    SimulationSystem.redScore += this._prefs.points
                } else {
                    SimulationSystem.blueScore += this._prefs.points
                }
                const event = new OnScoreChangedEvent(SimulationSystem.redScore, SimulationSystem.blueScore)
                event.Dispatch()
            }
        }
    }

    // Private gamepiece removal called anytime collision removed from zone. Score update in Update()
    private ZoneCollisionRemoved(gpID: Jolt.BodyID) {
        if (this._prefs?.persistentPoints) {
            const associate = <RigidNodeAssociate>World.PhysicsSystem.GetBodyAssociation(gpID)
            if (associate?.isGamePiece) {
                const temp = this._gpContacted.filter(x => {
                    return x.GetIndexAndSequenceNumber() != gpID.GetIndexAndSequenceNumber()
                })
                if (this._gpContacted != temp) this._gpContacted = Object.assign([], temp)
            }
        }
    }

    // Public gamepiece removal called anytime EjectableSceneObject created in case gamepiece was in persistent zone
    // Score update in Update()
    public static RemoveGamepiece(zone: ScoringZoneSceneObject, gpID: Jolt.BodyID) {
        if (zone._prefs && zone._prefs.persistentPoints) {
            const temp = zone._gpContacted.filter(x => {
                return x.GetIndexAndSequenceNumber() != gpID.GetIndexAndSequenceNumber()
            })
            if (zone._gpContacted != temp) zone._gpContacted = Object.assign([], temp)
        }
    }
}

export class OnScoreChangedEvent extends Event {
    public static readonly EVENT_KEY = "OnScoreChangedEvent"

    public red: number
    public blue: number

    public constructor(redScore: number, blueScore: number) {
        super(OnScoreChangedEvent.EVENT_KEY)

        this.red = redScore
        this.blue = blueScore
    }

    public Dispatch(): void {
        window.dispatchEvent(this)
    }

    public static AddListener(func: (e: OnScoreChangedEvent) => void) {
        window.addEventListener(OnScoreChangedEvent.EVENT_KEY, func as (e: Event) => void)
    }

    public static RemoveListener(func: (e: OnScoreChangedEvent) => void) {
        window.removeEventListener(OnScoreChangedEvent.EVENT_KEY, func as (e: Event) => void)
    }
}

export default ScoringZoneSceneObject
