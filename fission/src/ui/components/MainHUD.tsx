import React, { useEffect, useState } from "react"
import { BsCodeSquare } from "react-icons/bs"
import { FaCar, FaGear, FaMagnifyingGlass, FaPlus } from "react-icons/fa6"
import { BiMenuAltLeft } from "react-icons/bi"
import { GrFormClose } from "react-icons/gr"
import { GiSteeringWheel } from "react-icons/gi"
import { HiDownload } from "react-icons/hi"
import { IoGameControllerOutline, IoPeople } from "react-icons/io5"
import { useModalControlContext } from "@/ui/ModalContext"
import { usePanelControlContext } from "@/ui/PanelContext"
import { motion } from "framer-motion"
import logo from "@/assets/autodesk_logo.png"
import { ToastType, useToastContext } from "@/ui/ToastContext"
import { Random } from "@/util/Random"
import APS, { APS_USER_INFO_UPDATE_EVENT } from "@/aps/APS"
import { UserIcon } from "./UserIcon"

type ButtonProps = {
    value: string
    icon: React.ReactNode
    onClick?: () => void
    larger?: boolean
}

const MainHUDButton: React.FC<ButtonProps> = ({
    value,
    icon,
    onClick,
    larger,
}) => {
    if (larger == null) larger = false
    return (
        <div
            onClick={onClick}
            className={`relative flex flex-row cursor-pointer bg-background w-full m-auto px-2 py-1 text-main-text rounded-md ${larger ? "justify-center" : ""
                } items-center hover:backdrop-brightness-105`}
        >
            {larger && icon}
            {!larger && (
                <span
                    onClick={onClick}
                    className="absolute left-3 text-main-hud-icon"
                >
                    {icon}
                </span>
            )}
            <input
                type="button"
                className={`px-2 ${larger ? "py-2" : "py-1 ml-6"
                    } text-main-text cursor-pointer`}
                value={value}
                onClick={onClick}
            />
        </div>
    )
}

const variants = {
    open: { opacity: 1, y: "-50%", x: 0 },
    closed: { opacity: 0, y: "-50%", x: "-100%" },
}

const MainHUD: React.FC = () => {

    // console.debug('Creating MainHUD');

    const { openModal } = useModalControlContext()
    const { openPanel } = usePanelControlContext()
    const { addToast } = useToastContext()
    const [isOpen, setIsOpen] = useState(false)

    const [userInfo, setUserInfo] = useState(APS.userInfo);

    useEffect(() => {
        document.addEventListener(APS_USER_INFO_UPDATE_EVENT, () => {
            setUserInfo(APS.userInfo)
        });
    }, [])

    return (
        <>
            {!isOpen && (
                <button
                    onClick={() => setIsOpen(!isOpen)}
                    className="absolute left-6 top-6"
                >
                    <BiMenuAltLeft
                        size={40}
                        className="text-main-hud-close-icon"
                    />
                </button>
            )}
            <motion.div
                initial="closed"
                animate={isOpen ? "open" : "closed"}
                variants={variants}
                className="fixed flex flex-col gap-2 bg-gradient-to-b from-interactive-element-right to-interactive-element-left w-min p-4 rounded-3xl ml-4 top-1/2 -translate-y-1/2"
            >
                <div className="flex flex-row gap-2 w-60 h-10">
                    <img src={logo} className="w-[80%] h-[100%] object-contain" />
                    <button onClick={() => setIsOpen(false)}>
                        <GrFormClose
                            color="bg-icon"
                            size={20}
                            className="text-main-hud-close-icon"
                        />
                    </button>
                </div>
                <MainHUDButton
                    value={"Spawn Asset"}
                    icon={<FaPlus />}
                    larger={true}
                    onClick={() => openModal("spawning")}
                />
                <div className="flex flex-col gap-0 bg-background w-full rounded-3xl">
                    <MainHUDButton
                        value={"Manage Assemblies"}
                        icon={<FaGear />}
                        onClick={() => openModal("manage-assembles")}
                    />
                    <MainHUDButton
                        value={"Settings"}
                        icon={<FaGear />}
                        onClick={() => openModal("settings")}
                    />
                    <MainHUDButton
                        value={"View"}
                        icon={<FaMagnifyingGlass />}
                        onClick={() => openModal("view")}
                    />
                    <MainHUDButton
                        value={"Controls"}
                        icon={<IoGameControllerOutline />}
                        onClick={() => openModal("change-inputs")}
                    />
                    <MainHUDButton
                        value={"MultiBot"}
                        icon={<IoPeople />}
                        onClick={() => openPanel("multibot")}
                    />
                    <MainHUDButton
                        value={"Import Mira"}
                        icon={<IoPeople />}
                        onClick={() => openModal("import-mirabuf")}
                    />
                </div>
                <div className="flex flex-col gap-0 bg-background w-full rounded-3xl">
                    <MainHUDButton
                        value={"Download Asset"}
                        icon={<HiDownload />}
                        onClick={() => openModal("download-assets")}
                    />
                    <MainHUDButton
                        value={"RoboRIO"}
                        icon={<BsCodeSquare />}
                        onClick={() => openModal("roborio")}
                    />
                    <MainHUDButton
                        value={"Driver Station"}
                        icon={<GiSteeringWheel />}
                        onClick={() => openPanel("driver-station")}
                    />
                    <MainHUDButton
                        value={"Drivetrain"}
                        icon={<FaCar />}
                        onClick={() => openModal("drivetrain")}
                    />
                    <MainHUDButton
                        value={"Toasts"}
                        icon={<FaCar />}
                        onClick={() => {
                            const type: ToastType = [
                                "info",
                                "warning",
                                "error",
                            ][Math.floor(Random() * 3)] as ToastType
                            addToast(
                                type,
                                type,
                                "This is a test toast to test the toast system"
                            )
                        }}
                    />
                </div>
                {userInfo
                    ?
                        <MainHUDButton
                            value={`Hi, ${userInfo.givenName}`}
                            icon={<UserIcon className="h-[20pt] m-[5pt] rounded-full" />}
                            larger={true}
                            onClick={() => APS.logout()}
                        />
                    :
                        <MainHUDButton
                            value={`APS Login`}
                            icon={<IoPeople />}
                            larger={true}
                            onClick={() => APS.requestAuthCode()}
                        />
                }
            </motion.div>
        </>
    )
}

export default MainHUD
