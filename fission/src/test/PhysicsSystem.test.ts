import { test, expect, describe, assert } from "vitest"
import PhysicsSystem, { LayerReserve } from "../systems/physics/PhysicsSystem"
import MirabufParser from "@/mirabuf/MirabufParser"
import * as THREE from "three"
import Jolt from "@barclah/jolt-physics"
import MirabufCachingService, { MiraType } from "@/mirabuf/MirabufLoader"

describe("Physics Sansity Checks", () => {
    test("Convex Hull Shape (Cube)", () => {
        const points: Float32Array = new Float32Array([
            0.5, -0.5, 0.5, -0.5, -0.5, 0.5, -0.5, -0.5, -0.5, 0.5, -0.5, -0.5, 0.5, 0.5, 0.5, -0.5, 0.5, 0.5, -0.5,
            0.5, -0.5, 0.5, 0.5, -0.5,
        ])

        const system = new PhysicsSystem()
        const shapeResult = system.CreateConvexHull(points)

        assert(shapeResult.HasError() == false, shapeResult.GetError().c_str())
        expect(shapeResult.IsValid()).toBe(true)

        const shape = shapeResult.Get()

        expect(shape.GetVolume() - 1.0).toBeLessThan(0.001)
        expect(shape.GetCenterOfMass().Length()).toBe(0.0)

        shape.Release()
        system.Destroy()
    })
    test("Convex Hull Shape (Tetrahedron)", () => {
        const points: Float32Array = new Float32Array([0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0])

        const system = new PhysicsSystem()
        const shapeResult = system.CreateConvexHull(points)

        assert(shapeResult.HasError() == false, shapeResult.GetError().c_str())
        expect(shapeResult.IsValid()).toBe(true)

        const shape = shapeResult.Get()
        const bounds = shape.GetLocalBounds()
        const boxSize = bounds.mMax.Sub(bounds.mMin)

        expect(boxSize.GetX() - 1.0).toBeLessThan(0.001)
        expect(boxSize.GetY() - 1.0).toBeLessThan(0.001)
        expect(boxSize.GetZ() - 1.0).toBeLessThan(0.001)
        expect(shape.GetVolume() - 1.0 / 6.0).toBeLessThan(0.001)
        expect(shape.GetMassProperties().mMass - 6.0).toBeLessThan(0.001)

        shape.Release()
        system.Destroy()
    })
})

describe("GodMode", () => {
    test("Basic", () => {
        const system = new PhysicsSystem()
        const box = system.CreateBox(new THREE.Vector3(1, 1, 1), 1, new THREE.Vector3(0, 0, 0), undefined)
        const [ghostObject, ghostConstraint] = system.CreateGodModeBody(box.GetID(), box.GetPosition() as Jolt.Vec3)

        assert(system.GetBody(ghostObject.GetID()) != undefined)
        assert(system.GetBody(box.GetID()) != undefined)
        assert(ghostConstraint != undefined)
        // Check constraint after running for a few seconds
        // TODO: Make sure this is the correct way to do this
        // TODO: Figure out how to make it use substeps to check instead
        for (let i = 0; i < 30; i++) {
            // TODO: Change this once this function actually uses deltaT
            system.Update(i)
        }

        assert(system.GetBody(ghostObject.GetID()) != undefined)
        assert(system.GetBody(box.GetID()) != undefined)
        assert(ghostConstraint != undefined)

        //system.Destroy()
    })
})

describe("Mirabuf Physics Loading", () => {
    test("Body Loading (Dozer)", async () => {
        const assembly = await MirabufCachingService.CacheRemote("/api/mira/Robots/Dozer_v9.mira", MiraType.ROBOT).then(
            x => MirabufCachingService.Get(x!.id, MiraType.ROBOT)
        )
        const parser = new MirabufParser(assembly!)
        const physSystem = new PhysicsSystem()
        const mapping = physSystem.CreateBodiesFromParser(parser, new LayerReserve())

        expect(mapping.size).toBe(7)
    })

    test("Body Loading (Team_2471_(2018)_v7.mira)", async () => {
        const assembly = await MirabufCachingService.CacheRemote(
            "/api/mira/Robots/Team 2471 (2018)_v7.mira",
            MiraType.ROBOT
        ).then(x => {
            return MirabufCachingService.Get(x!.id, MiraType.ROBOT)
        })

        const parser = new MirabufParser(assembly!)
        const physSystem = new PhysicsSystem()
        const mapping = physSystem.CreateBodiesFromParser(parser, new LayerReserve())

        expect(mapping.size).toBe(10)
    })
})
