import React, { ReactNode } from "react"
import { usePanelControlContext } from "../PanelContext"

export type PanelPropsImpl = {
    panelId: string;
}

type PanelProps = {
    name: string
    icon: ReactNode | string
    panelId: string
    onCancel?: () => void
    onAccept?: () => void
    children?: ReactNode
}

const Panel: React.FC<PanelProps> = ({
    children,
    name,
    icon,
    panelId,
    onCancel,
    onAccept,
}) => {
    const { closePanel } = usePanelControlContext();
    const iconEl: ReactNode =
        typeof icon === "string" ? (
            <img src={icon} className="w-6" alt="Icon" />
        ) : (
            icon
        )
    return (
        <div
            id={name}
            className="absolute w-min h-min right-4 top-1/2 -translate-y-1/2 bg-black text-white m-auto border-5 rounded-2xl shadow-sm shadow-slate-800">
            <div id="header" className="flex items-center gap-8 h-16">
                <span className="flex justify-center align-center ml-8">
                    {iconEl}
                </span>
                <h1 className="text-3xl inline-block align-middle">{name}</h1>
            </div>
            <div id="content" className="mx-16 flex flex-col gap-8">{children}</div>
            <div id="footer" className="flex justify-between mx-10 py-8">
                <input
                    type="button"
                    value="Cancel"
                    onClick={() => { closePanel(panelId); if (onCancel) onCancel() }}
                    className="bg-red-500 rounded-md cursor-pointer px-4 py-1 text-black font-bold duration-100 hover:bg-red-600"
                />
                <input
                    type="button"
                    value="Accept"
                    onClick={() => { closePanel(panelId); if (onAccept) onAccept(); }}
                    className="bg-blue-500 rounded-md cursor-pointer px-4 py-1 text-black font-bold duration-100 hover:bg-blue-600"
                />
            </div>
        </div>
    )
}

export default Panel
