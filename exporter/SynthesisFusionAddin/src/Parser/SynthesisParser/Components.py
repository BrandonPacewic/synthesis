# Contains all of the logic for mapping the Components / Occurrences
import adsk.core, adsk.fusion, uuid, logging, traceback
from proto.proto_out import assembly_pb2, types_pb2, material_pb2, joint_pb2

from .Utilities import *
from .. import ExporterOptions
from typing import *

from . import PhysicalProperties

from .PDMessage import PDMessage
from ...Analyzer.timer import TimeThis

# TODO: Impelement Material overrides


@TimeThis
def _MapAllComponents(
    design: adsk.fusion.Design,
    options: ExporterOptions,
    progressDialog: PDMessage,
    partsData: assembly_pb2.Parts,
    materials: material_pb2.Materials,
) -> None:
    for component in design.allComponents:
        adsk.doEvents()
        if progressDialog.wasCancelled():
            raise RuntimeError("User canceled export")
        progressDialog.addComponent(component.name)

        comp_ref = guid_component(component)

        fill_info(partsData, None)

        partDefinition = partsData.part_definitions[comp_ref]

        fill_info(partDefinition, component, comp_ref)

        PhysicalProperties.GetPhysicalProperties(
            component, partDefinition.physical_data
        )

        if options.exportMode == ExporterOptions.ExportMode.FIELD:
            partDefinition.dynamic = False
        else:
            partDefinition.dynamic = True

        def processBody(body: adsk.fusion.BRepBody | adsk.fusion.MeshBody):
            if progressDialog.wasCancelled():
                raise RuntimeError("User canceled export")
            if body.isLightBulbOn:
                part_body = partDefinition.bodies.add()
                fill_info(part_body, body)
                part_body.part = comp_ref

                if isinstance(body, adsk.fusion.BRepBody):
                    _ParseBRep(body, options, part_body.triangle_mesh)
                else:
                    _ParseMesh(body, options, part_body.triangle_mesh)

                appearance_key = "{}_{}".format(
                    body.appearance.name, body.appearance.id
                )
                # this should be appearance
                if appearance_key in materials.appearances:
                    part_body.appearance_override = appearance_key
                else:
                    part_body.appearance_override = "default"

        for body in component.bRepBodies:
            processBody(body)

        for body in component.meshBodies:
            processBody(body)


@TimeThis
def _ParseComponentRoot(
    component: adsk.fusion.Component,
    progressDialog: PDMessage,
    options: ExporterOptions,
    partsData: assembly_pb2.Parts,
    material_map: dict,
    node: types_pb2.Node,
) -> None:
    mapConstant = guid_component(component)

    part = partsData.part_instances[mapConstant]

    node.value = mapConstant

    fill_info(part, component, mapConstant)

    def_map = partsData.part_definitions

    if mapConstant in def_map:
        part.part_definition_reference = mapConstant

    for occur in component.occurrences:
        if progressDialog.wasCancelled():
            raise RuntimeError("User canceled export")

        if occur.isLightBulbOn:
            child_node = types_pb2.Node()
            __parseChildOccurrence(
                occur, progressDialog, options, partsData, material_map, child_node
            )
            node.children.append(child_node)


def __parseChildOccurrence(
    occurrence: adsk.fusion.Occurrence,
    progressDialog: PDMessage,
    options: ExporterOptions,
    partsData: assembly_pb2.Parts,
    material_map: dict,
    node: types_pb2.Node,
) -> None:
    if occurrence.isLightBulbOn is False:
        return

    progressDialog.addOccurrence(occurrence.name)

    mapConstant = guid_occurrence(occurrence)

    compRef = guid_component(occurrence.component)

    part = partsData.part_instances[mapConstant]

    node.value = mapConstant

    fill_info(part, occurrence, mapConstant)

    collision_attr = occurrence.attributes.itemByName("synthesis", "collision_off")
    if collision_attr != None:
        part.skip_collider = True

    if occurrence.appearance:
        try:
            part.appearance = "{}_{}".format(
                occurrence.appearance.name, occurrence.appearance.id
            )
        except:
            part.appearance = "default"
        # TODO: Add phyical_material parser

    if occurrence.component.material:
        part.physical_material = occurrence.component.material.id

    def_map = partsData.part_definitions

    if compRef in def_map:
        part.part_definition_reference = compRef

    # TODO: Maybe make this a separate step where you dont go backwards and search for the gamepieces
    if options.exportMode == ExporterOptions.ExportMode.FIELD:
        for x in options.gamepieces:
            if x.occurrenceToken == mapConstant:
                partsData.part_definitions[part.part_definition_reference].dynamic = (
                    True
                )
                break

    part.transform.spatial_matrix.extend(occurrence.transform.asArray())

    worldTransform = GetMatrixWorld(occurrence)

    if worldTransform:
        part.global_transform.spatial_matrix.extend(worldTransform.asArray())

    for occur in occurrence.childOccurrences:
        if progressDialog.wasCancelled():
            raise RuntimeError("User canceled export")

        if occur.isLightBulbOn:
            child_node = types_pb2.Node()
            __parseChildOccurrence(
                occur, progressDialog, options, partsData, material_map, child_node
            )
            node.children.append(child_node)


# saw online someone used this to get the correct context but oh boy does it look pricey
# I think if I can make all parts relative to a parent it should return that parents transform maybe
# TESTED AND VERIFIED - but unoptimized
def GetMatrixWorld(occurrence):
    matrix = occurrence.transform
    while occurrence.assemblyContext:
        matrix.transformBy(occurrence.assemblyContext.transform)
        occurrence = occurrence.assemblyContext
    return matrix


def _ParseBRep(
    body: adsk.fusion.BRepBody,
    options: ExporterOptions,
    trimesh: assembly_pb2.TriangleMesh,
) -> any:
    try:
        meshManager = body.meshManager
        calc = meshManager.createMeshCalculator()
        calc.setQuality(options.visualQuality)
        mesh = calc.calculate()

        fill_info(trimesh, body)
        trimesh.has_volume = True

        plainmesh_out = trimesh.mesh

        plainmesh_out.verts.extend(mesh.nodeCoordinatesAsFloat)
        plainmesh_out.normals.extend(mesh.normalVectorsAsFloat)
        plainmesh_out.indices.extend(mesh.nodeIndices)
        plainmesh_out.uv.extend(mesh.textureCoordinatesAsFloat)
    except:
        logging.getLogger("{INTERNAL_ID}.Parser.BrepBody").error(
            "Failed:\n{}".format(traceback.format_exc())
        )


def _ParseMesh(
    meshBody: adsk.fusion.MeshBody,
    options: ExporterOptions,
    trimesh: assembly_pb2.TriangleMesh,
) -> any:
    try:
        mesh = meshBody.displayMesh

        fill_info(trimesh, meshBody)
        trimesh.has_volume = True

        plainmesh_out = trimesh.mesh

        plainmesh_out.verts.extend(mesh.nodeCoordinatesAsFloat)
        plainmesh_out.normals.extend(mesh.normalVectorsAsFloat)
        plainmesh_out.indices.extend(mesh.nodeIndices)
        plainmesh_out.uv.extend(mesh.textureCoordinatesAsFloat)
    except:
        logging.getLogger("{INTERNAL_ID}.Parser.BrepBody").error(
            "Failed:\n{}".format(traceback.format_exc())
        )


def _MapRigidGroups(
    rootComponent: adsk.fusion.Component, joints: joint_pb2.Joints
) -> None:
    groups = rootComponent.allRigidGroups
    for group in groups:
        mira_group = joint_pb2.RigidGroup()
        mira_group.name = group.entityToken
        for occ in group.occurrences:
            if occ == None:
                a = 1
                continue

            if not occ.isLightBulbOn:
                continue

            occRef = guid_occurrence(occ)
            mira_group.occurrences.append(occRef)
        if len(mira_group.occurrences) > 1:
            joints.rigid_groups.append(mira_group)
