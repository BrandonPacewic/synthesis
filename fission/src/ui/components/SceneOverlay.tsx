import { Box } from "@mui/material"
import { useEffect, useReducer } from "react"
import { SceneOverlayTag, SceneOverlayTagEvent } from "./SceneOverlayEvents"

const tagMap = new Map<number, SceneOverlayTag>()

function SceneOverlay() {
    /* h1 text for each tagMap tag */
    const [components, updateComponents] = useReducer(() => {
        return [...tagMap.values()].map(x => (
            <h1
                className="text-2xl text-white font-bold"
                key={x.id}
                style={{ position: "absolute", left: x.position[0], top: x.position[1] }}
            >
                {x.text}
            </h1>
        ))
    }, [])

    /* Creating listener for tag events to update tagMap and rerender overlay */
    useEffect(() => {
        const onTagUpdate = (e: Event) => {
            const tagEvent = e as SceneOverlayTagEvent
            const tag = tagEvent.tag
            tagMap.set(tag.id, tag)
            // console.log(tag.position)
            updateComponents()
        }

        SceneOverlayTagEvent.Listen(onTagUpdate)

        return () => {
            SceneOverlayTagEvent.RemoveListener(onTagUpdate)
        }
    }, [])

    /* Render the overlay as a box that spans the entire screen and does not intercept any user interaction */
    return (
        <Box
            component="div"
            display="flex"
            sx={{
                position: "fixed",
                left: "0pt",
                top: "0pt",
                width: "100vw",
                height: "100vh",
                overflow: "hidden",
                pointerEvents: "none",
            }}
        >
            {components ?? <></>}
        </Box>
    )
}

export default SceneOverlay
