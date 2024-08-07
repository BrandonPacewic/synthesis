import { describe, test, expect } from "vitest"
import { mirabuf } from "../proto/mirabuf"
import MirabufParser, { RigidNodeReadOnly } from "../mirabuf/MirabufParser"
import MirabufCachingService, { MiraType } from "../mirabuf/MirabufLoader"

describe("Mirabuf Parser Tests", () => {
    test("Generate Rigid Nodes (Dozer_v9.mira)", async () => {
        const spikeMira = await MirabufCachingService.CacheRemote(
            "/api/mira/Robots/Dozer_v9.mira",
            MiraType.ROBOT
        ).then(x => MirabufCachingService.Get(x!.id, MiraType.ROBOT))

        const t = new MirabufParser(spikeMira!)
        const rn = [...t.rigidNodes.values()]

        expect(filterNonPhysicsNodes(rn, spikeMira!).length).toBe(7)
    })

    test("Generate Rigid Nodes (FRC Field 2018_v13.mira)", async () => {
        const field = await MirabufCachingService.CacheRemote(
            "/api/mira/Fields/FRC Field 2018_v13.mira",
            MiraType.FIELD
        ).then(x => MirabufCachingService.Get(x!.id, MiraType.FIELD))
        const t = new MirabufParser(field!)

        expect(filterNonPhysicsNodes([...t.rigidNodes.values()], field!).length).toBe(34)
    })

    test("Generate Rigid Nodes (Team 2471 (2018)_v7.mira)", async () => {
        const mm = await MirabufCachingService.CacheRemote(
            "/api/mira/Robots/Team 2471 (2018)_v7.mira",
            MiraType.ROBOT
        ).then(x => MirabufCachingService.Get(x!.id, MiraType.ROBOT))
        const t = new MirabufParser(mm!)

        expect(filterNonPhysicsNodes([...t.rigidNodes.values()], mm!).length).toBe(10)
    })
})

function filterNonPhysicsNodes(nodes: RigidNodeReadOnly[], mira: mirabuf.Assembly): RigidNodeReadOnly[] {
    return nodes.filter(x => {
        for (const part of x.parts) {
            const inst = mira.data!.parts!.partInstances![part]!
            const def = mira.data!.parts!.partDefinitions![inst.partDefinitionReference!]!
            if (def.bodies && def.bodies.length > 0) {
                return true
            }
        }
        return false
    })
}

// function printRigidNodeParts(nodes: RigidNodeReadOnly[], mira: mirabuf.Assembly) {
//     nodes.forEach(x => {
//         console.log(`[ ${x.name} ]:`);
//         x.parts.forEach(y => console.log(`-> '${mira.data!.parts!.partInstances![y]!.info!.name!}'`));
//         console.log('');
//     });
// }
