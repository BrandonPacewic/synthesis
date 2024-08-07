import React, { useReducer } from "react"
import Modal, { ModalPropsImpl } from "@/components/Modal"
import { FaWrench } from "react-icons/fa6"
import Button from "@/components/Button"
import Label, { LabelSize } from "@/components/Label"
import World from "@/systems/World"
import MirabufSceneObject from "@/mirabuf/MirabufSceneObject"

interface AssemblyCardProps {
    mira: MirabufSceneObject
    update: React.DispatchWithoutAction
}

const AssemblyCard: React.FC<AssemblyCardProps> = ({ mira, update }) => {
    return (
        <div
            key={mira.id}
            className="flex flex-row align-middle justify-between items-center bg-background rounded-sm p-2 gap-2"
        >
            <Label className="text-wrap break-all">{mira.assemblyName}</Label>
            <Button
                value="Delete"
                onClick={() => {
                    World.SceneRenderer.RemoveSceneObject(mira.id)
                    update()
                }}
            />
        </div>
    )
}

const ManageAssembliesModal: React.FC<ModalPropsImpl> = ({ modalId }) => {
    // update tooltip based on type of drivetrain, receive message from Synthesis
    // const { showTooltip } = useTooltipControlContext()

    const [_, update] = useReducer(x => !x, false)

    const assemblies = [...World.SceneRenderer.sceneObjects.entries()]
        .filter(x => {
            const y = x[1] instanceof MirabufSceneObject
            return y
        })
        .map(x => x[1] as MirabufSceneObject)

    return (
        <Modal
            name={"Manage Assemblies"}
            icon={<FaWrench />}
            modalId={modalId}
            onAccept={() => {
                // showTooltip("controls", [
                //     { control: "WASD", description: "Drive" },
                //     { control: "E", description: "Intake" },
                //     { control: "Q", description: "Dispense" },
                // ]);
            }}
        >
            <div className="flex overflow-y-auto flex-col gap-2 min-w-[50vw] max-h-[60vh] bg-background-secondary rounded-md p-2">
                <Label size={LabelSize.Medium} className="text-center border-b-[1pt] mt-[4pt] mb-[2pt] mx-[5%]">
                    {assemblies ? `${assemblies.length} Assemblies` : "No Assemblies"}
                </Label>
                {assemblies.map(x => AssemblyCard({ mira: x, update: update }))}
            </div>
        </Modal>
    )
}

export default ManageAssembliesModal
