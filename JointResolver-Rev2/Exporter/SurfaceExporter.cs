﻿using Inventor;
using System.IO;
using System;
using System.Collections.Generic;

// Not thread safe.
class SurfaceExporter
{
    private const int TMP_VERTICIES = ushort.MaxValue;

    private bool adaptiveIgnoring = false;

    // Temporary output
    private double[] tmpVerts = new double[TMP_VERTICIES * 3];
    private double[] tmpNorms = new double[TMP_VERTICIES * 3];
    private int[] tmpIndicies = new int[TMP_VERTICIES * 3];
    private double[] tmpTextureCoords = new double[TMP_VERTICIES * 2];
    private int tmpVertCount = 0;
    private int tmpFacetCount = 0;


    // Temporary output
    private double[] postVerts = new double[TMP_VERTICIES * 3];
    private double[] postNorms = new double[TMP_VERTICIES * 3];
    private int[] postIndicies = new int[TMP_VERTICIES * 3];
    private uint[] postColors = new uint[TMP_VERTICIES];
    private double[] postTextureCoords = new double[TMP_VERTICIES * 2];
    private int postVertCount = 0;
    private int postFacetCount = 0;

    // Final output
    private struct Mesh
    {
        public double[] verts;
        public double[] norms;
        public double[] textureCoords;
        public uint[] colors;
        public int[] indicies;
        public int vertCount;
        public int facetCount;
    }

    private List<Mesh> meshes = new List<Mesh>();
    private PhysicalProperties physics = new PhysicalProperties();

    // Tolerances
    private double[] tolerances = new double[10];
    private int tmpToleranceCount = 0;

    /// <summary>
    /// Copies mesh information for the given surface body into the mesh storage structure.
    /// </summary>
    /// <param name="surf">The surface body to export</param>
    /// <param name="bestResolution">Use the best possible resolution</param>
    /// <param name="separateFaces">Separate the surface body into one mesh per face</param>
    public void AddFacets(SurfaceBody surf, bool bestResolution = false, bool separateFaces = false)
    {
        surf.GetExistingFacetTolerances(out tmpToleranceCount, out tolerances);
        int bestIndex = -1;
        for (int i = 0; i < tmpToleranceCount; i++)
        {
            if (bestIndex < 0 || ((tolerances[i] < tolerances[bestIndex]) == bestResolution))
            {
                bestIndex = i;
            }
        }
        Console.WriteLine("(" + postVertCount + "v/" + postFacetCount + "f)\tExporting " + surf.Parent.Name + "." + surf.Name + " with tolerance " + tolerances[bestIndex]);

        if (separateFaces)
        {
            foreach (Face f in surf.Faces)
            {
                AddFacets(f, tolerances[bestIndex]);
            }
        }
        else
        {
            tmpVertCount = 0;
            surf.GetExistingFacetsAndTextureMap(tolerances[bestIndex], out tmpVertCount, out tmpFacetCount, out tmpVerts, out  tmpNorms, out  tmpIndicies, out tmpTextureCoords);
            if (tmpVertCount == 0)
            {
                Console.WriteLine("Calculate values");
                surf.CalculateFacetsAndTextureMap(tolerances[bestIndex], out tmpVertCount, out tmpFacetCount, out  tmpVerts, out tmpNorms, out  tmpIndicies, out tmpTextureCoords);
            }
            AssetProperties assetProps;
            try
            {
                assetProps = new AssetProperties(surf.Appearance);
            }
            catch
            {
                assetProps = new AssetProperties(surf.Parent.Appearance);
            }
            AddFacetsInternal(assetProps);
        }
    }

    private void DumpMeshBuffer()
    {
        if (postVertCount == 0 || postFacetCount == 0)
            return;
        Mesh subObject = new Mesh();
        subObject.verts = new double[postVertCount * 3];
        subObject.norms = new double[postVertCount * 3];
        subObject.textureCoords = new double[postVertCount * 2];
        subObject.colors = new uint[postVertCount];
        subObject.indicies = new int[postFacetCount * 3];
        subObject.facetCount = postFacetCount;
        subObject.vertCount = postVertCount;
        Array.Copy(postVerts, 0, subObject.verts, 0, postVertCount * 3);
        Array.Copy(postNorms, 0, subObject.norms, 0, postVertCount * 3);
        Array.Copy(postTextureCoords, 0, subObject.textureCoords, 0, postVertCount * 2);
        Array.Copy(postIndicies, 0, subObject.indicies, 0, postFacetCount * 3);
        Array.Copy(postColors, 0, subObject.colors, 0, postVertCount);
        Console.WriteLine("Mesh segment " + meshes.Count + " has " + postVertCount + " verts and " + postFacetCount + " facets");
        postVertCount = 0;
        postFacetCount = 0;
        meshes.Add(subObject);
    }

    /// <summary>
    /// Moves the mesh currently in the temporary mesh buffer into the mesh structure itself, 
    /// with material information from the asset properties.
    /// </summary>
    /// <param name="assetProps">Material information to use</param>
    private void AddFacetsInternal(AssetProperties assetProps)
    {
        if (tmpVertCount > TMP_VERTICIES)
        {
            // This won't actually happen since TMP_VERTEX_COUNT is max short.
            System.Windows.Forms.MessageBox.Show("Warning: Mesh segment exceededed " + TMP_VERTICIES + " verticies.  Strange things may begin to happen.");
        }
        if (tmpVertCount + postVertCount >= TMP_VERTICIES)
        {
            DumpMeshBuffer();
        }

        Array.Copy(tmpVerts, 0, postVerts, postVertCount * 3, tmpVertCount * 3);
        Array.Copy(tmpNorms, 0, postNorms, postVertCount * 3, tmpVertCount * 3);
        Array.Copy(tmpTextureCoords, 0, postTextureCoords, postVertCount * 2, tmpVertCount * 2);
        uint colorVal = 0xFFFFFFFF;
        if (assetProps.color != null)
        {
            colorVal = ((uint) assetProps.color.Red << 0) | ((uint) assetProps.color.Green << 8) | ((uint) assetProps.color.Blue << 16) | ((((uint) (assetProps.color.Opacity * 255)) & 0xFF) << 24);
        }
        for (int i = postVertCount; i < postVertCount + tmpVertCount; i++)
        {
            postColors[i] = colorVal;
        }

        // Now we must manually copy the indicies
        int indxOffset = postFacetCount * 3;
        for (int i = 0; i < tmpFacetCount * 3; i++)
        {
            postIndicies[i + indxOffset] = tmpIndicies[i] + postVertCount;
        }
        postFacetCount += tmpFacetCount;
        postVertCount += tmpVertCount;
    }

    /// <summary>
    /// Copies mesh information from the Inventor API to the temporary mesh buffer, then into the mesh structure.
    /// </summary>
    /// <param name="surf">The source mesh</param>
    /// <param name="tolerance">The chord tolerance for the mesh</param>
    private void AddFacets(Face surf, double tolerance)
    {
        tmpVertCount = 0;
        surf.GetExistingFacetsAndTextureMap(tolerance, out tmpVertCount, out tmpFacetCount, out tmpVerts, out  tmpNorms, out  tmpIndicies, out tmpTextureCoords);
        if (tmpVertCount == 0)
        {
            Console.WriteLine("Calculate values");
            surf.CalculateFacetsAndTextureMap(tolerance, out tmpVertCount, out tmpFacetCount, out  tmpVerts, out tmpNorms, out  tmpIndicies, out tmpTextureCoords);
        }
        AssetProperties assetProps;
        try
        {
            assetProps = new AssetProperties(surf.Appearance);
        }
        catch
        {
            try
            {
                assetProps = new AssetProperties(surf.Parent.Appearance);
            }
            catch
            {
                assetProps = new AssetProperties(surf.Parent.Parent.Appearance);
            }
        }
        AddFacetsInternal(assetProps);
    }

    /// <summary>
    /// Clears the mesh structure and physical properties, 
    /// preparing this exporter for another set of objects.
    /// </summary>
    public void Reset()
    {
        meshes.Clear();
        physics = new PhysicalProperties();
    }

    /// <summary>
    /// Adds the mesh for the given component, and all its subcomponents to the mesh storage structure.
    /// </summary>
    /// <param name="occ">The component to export</param>
    /// <param name="bestResolution">Use the best possible resolution</param>
    /// <param name="separateFaces">Export each face as a separate mesh</param>
    /// <param name="ignorePhysics">Don't add the physical properties of this component to the exporter</param>
    public void ExportAll(ComponentOccurrence occ, bool bestResolution = false, bool separateFaces = false, bool ignorePhysics = false)
    {
        if (!ignorePhysics)
        {
            // Compute physics
            physics.centerOfMass.Multiply(physics.mass);
            float myMass = (float) occ.MassProperties.Mass;
            physics.mass += myMass;
            physics.centerOfMass.Add(Utilities.ToBXDVector(occ.MassProperties.CenterOfMass).Multiply(myMass));
            physics.centerOfMass.Multiply(1.0f / physics.mass);
        }

        if (!occ.Visible)
            return;

        foreach (SurfaceBody surf in occ.SurfaceBodies)
        {
            AddFacets(surf, bestResolution, separateFaces);
        }

        double totalVolume = 0;
        foreach (ComponentOccurrence occ2 in occ.SubOccurrences)
        {
            totalVolume += occ2.MassProperties.Volume;
        }
        totalVolume /= occ.SubOccurrences.Count * 5;

        foreach (ComponentOccurrence item in occ.SubOccurrences)
        {
            if (adaptiveIgnoring && item.MassProperties.Volume < totalVolume)
            {
                Console.WriteLine("Drop: " + item.Name);
            }
            else
            {
                ExportAll(item, bestResolution, separateFaces, true);
            }
        }
    }

    /// <summary>
    /// Adds the mesh for the given components, and all their subcomponents to the mesh storage structure.
    /// </summary>
    /// <param name="occs">The components to export</param>
    /// <param name="bestResolution">Use the best possible resolution</param>
    /// <param name="separateFaces">Export each face as a separate mesh</param>
    public void ExportAll(ComponentOccurrences occs, bool bestResolution = false, bool separateFaces = false)
    {
        foreach (ComponentOccurrence occ in occs)
        {
            ExportAll(occ, bestResolution, separateFaces);
        }
    }
    
    /// <summary>
    /// Adds the mesh for the given components, and all their subcomponents to the mesh storage structure.
    /// </summary>
    /// <param name="occs">The components to export</param>
    /// <param name="bestResolution">Use the best possible resolution</param>
    /// <param name="separateFaces">Export each face as a separate mesh</param>
    public void ExportAll(List<ComponentOccurrence> occs, bool bestResolution = false, bool separateFaces = false)
    {
        foreach (ComponentOccurrence occ in occs)
        {
            ExportAll(occ, bestResolution, separateFaces);
        }
    }
    
    /// <summary>
    /// Adds the mesh for all the components and their subcomponenets in the custom rigid group.  <see cref="ExportAll(ComponentOccurrence,bool,bool,bool)"/>
    /// </summary>
    /// <remarks>
    /// This uses the best resolution and separate faces options stored inside the provided custom rigid group.
    /// </remarks>
    /// <param name="group">The group to export from</param>
    public void ExportAll(CustomRigidGroup group)
    {
        double totalVolume = 0;
        foreach (ComponentOccurrence occ in group.occurrences)
        {
            totalVolume += occ.MassProperties.Volume;
        }
        totalVolume /= group.occurrences.Count * 5;

        foreach (ComponentOccurrence occ in group.occurrences)
        {
            if (adaptiveIgnoring && occ.MassProperties.Volume < totalVolume)
            {
                Console.WriteLine("Drop: " + occ.Name); // TODO Ignores physics
            }
            else
            {
                ExportAll(occ, group.highRes, group.colorFaces);
            }
        }
    }

    //[4 byte integer]  Mesh count
    //[Extensible]      Meshes

    //Each Mesh:
    //
    //[4 byte integer]	Vertex Count
    //For each vertex…
    //[8 byte double]	Vertex position x
    //[8 byte double]	Vertex position y
    //[8 byte double]	Vertex position z
    //[8 byte double]	Vertex normal x
    //[8 byte double]	Vertex normal y
    //[8 byte double]	Vertex normal z
    //[1 byte]		Vertex color red
    //[1 byte]		Vertex color green
    //[1 byte]		Vertex color blue
    //[1 byte]		Vertex color alpha
    //[8 byte double]	Vertex texture u
    //[8 byte double]	Vertex texture v
    //
    //[4 byte integer]	Facet count
    //For each facet…
    //[4 byte integer]	Vertex 1
    //[4 byte integer]	Vertex 2
    //[4 byte integer]	Vertex 3

    /// <summary>
    /// Writes the current mesh storage structure as a segmented BXDA to the given file path.
    /// </summary>
    /// <param name="path">Output path</param>
    public void WriteBXDA(String path)
    {
        DumpMeshBuffer();
        BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.OpenOrCreate));
        writer.Write(meshes.Count);
        foreach (Mesh mesh in meshes)
        {
            writer.Write(mesh.vertCount);
            for (int i = 0; i < mesh.vertCount; i++)
            {
                int vecI = i * 3;
                int texI = i * 2;
                int colI = i;
                writer.Write(mesh.verts[vecI]);
                writer.Write(mesh.verts[vecI + 1]);
                writer.Write(mesh.verts[vecI + 2]);
                writer.Write(mesh.norms[vecI]);
                writer.Write(mesh.norms[vecI + 1]);
                writer.Write(mesh.norms[vecI + 2]);
                writer.Write(mesh.colors[colI]);
                writer.Write(mesh.textureCoords[texI]);
                writer.Write(mesh.textureCoords[texI + 1]);
            }
            writer.Write(mesh.facetCount);
            for (int i = 0; i < mesh.facetCount; i++)
            {
                int fI = i * 3;
                writer.Write(mesh.indicies[fI] - 1);
                writer.Write(mesh.indicies[fI + 1] - 1);
                writer.Write(mesh.indicies[fI + 2] - 1);
            }
        }
        physics.WriteData(writer);
        writer.Close();
    }
}
