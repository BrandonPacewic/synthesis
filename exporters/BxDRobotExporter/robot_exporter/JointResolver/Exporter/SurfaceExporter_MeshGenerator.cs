﻿// Should we export textures.  (Useless currently)
// #define USE_TEXTURES

using Inventor;
using System.IO;
using System;
using System.Collections.Generic;

/// <summary>
/// Exports Inventor objects into the BXDA format.  One instance per thread.
/// </summary>
public partial class SurfaceExporter
{
    private class PartialSurface
    {
        public double[] verts = new double[MAX_VERTICES * 3];
        public double[] norms = new double[MAX_VERTICES * 3];
        public int[] indicies = new int[MAX_VERTICES * 3];
#if USE_TEXTURES
        public double[] textureCoords = new double[TMP_VERTICIES * 2];
#endif
        public int vertCount = 0;
        public int facetCount = 0;
    }

    private const int MAX_VERTICES = ushort.MaxValue;

    /// <summary>
    /// Default tolerance used when generating meshes (cm)
    /// </summary>
    private const double DEFAULT_TOLERANCE = 1;

    // Temporary output
    private PartialSurface tempSurface = new PartialSurface();

    // Pre-submesh output
    private PartialSurface outputSurface = new PartialSurface();
    private List<BXDAMesh.BXDASurface> outputMeshSurfaces = new List<BXDAMesh.BXDASurface>();
    private BXDAMesh outputMesh = new BXDAMesh();
    
    /// <summary>
    /// Copies mesh information for the given surface body into the mesh storage structure.
    /// </summary>
    /// <param name="surf">The surface body to export</param>
    /// <param name="bestResolution">Use the best possible resolution</param>
    /// <param name="separateFaces">Separate the surface body into one mesh per face</param>
    private void AddFacets(SurfaceBody surf, bool bestResolution = false, bool separateFaces = false)
    {
        // Find the lowest tolerance specified by a facet
        #region Get Mesh Generation Tolerance
        int tmpToleranceCount = 0;
        double[] tolerances = new double[10];
        surf.GetExistingFacetTolerances(out tmpToleranceCount, out tolerances);

        double tolerance = DEFAULT_TOLERANCE;

        int bestIndex = -1;
        for (int i = 0; i < tmpToleranceCount; i++)
        {
            if (bestIndex < 0 || tolerances[i] < tolerances[bestIndex])
            {
                bestIndex = i;
            }
        }
        
        if (bestResolution || tolerances[bestIndex] > tolerance)
            tolerance = tolerances[bestIndex];
        #endregion

        // Only separate if they are actually different colors
        #region Should Separate Faces
        AssetProperties firstAsset = null;
        Dictionary<string, AssetProperties> assets = new Dictionary<string, AssetProperties>();

        foreach (Face f in surf.Faces)
        {
            try
            {
                if (!assets.ContainsKey(f.Appearance.DisplayName))
                {
                    assets.Add(f.Appearance.DisplayName, new AssetProperties(f.Appearance)); // Used to quickly access asset properties later

                    if (firstAsset == null)
                        firstAsset = assets[f.Appearance.DisplayName];
                }
            }
            catch
            {
                // Failed to create asset for face
            }
        }

        // If more than one different assets exist, separate faces
        separateFaces = separateFaces && assets.Count > 1;
        #endregion

        if (separateFaces)
        {
            foreach (Face face in surf.Faces) // This should be multithreaded
            {
#if USE_TEXTURES
                face.GetExistingFacetsAndTextureMap(tolerance, out tmpSurface.vertCount, out tmpSurface.facetCount, out tmpSurface.verts, out  tmpSurface.norms, out  tmpSurface.indicies, out tmpSurface.textureCoords);
                if (tmpSurface.vertCount == 0)
                {
                    face.CalculateFacetsAndTextureMap(tolerance, out tmpSurface.vertCount, out tmpSurface.facetCount, out  tmpSurface.verts, out tmpSurface.norms, out  tmpSurface.indicies, out tmpSurface.textureCoords);
                }
#else
                face.GetExistingFacets(tolerance, out tempSurface.vertCount, out tempSurface.facetCount, out tempSurface.verts, out tempSurface.norms, out tempSurface.indicies);
                if (tempSurface.vertCount == 0)
                {
                    face.CalculateFacets(tolerance, out tempSurface.vertCount, out tempSurface.facetCount, out tempSurface.verts, out tempSurface.norms, out tempSurface.indicies);
                }
#endif
                AssetProperties asset = assets[face.Appearance.DisplayName];
                AddFacetsInternal(asset);
            }
        }
        else
        {
#if USE_TEXTURES
            surf.GetExistingFacetsAndTextureMap(tolerances[bestIndex], out tmpSurface.vertCount, out tmpSurface.facetCount, out tmpSurface.verts, out  tmpSurface.norms, out  tmpSurface.indicies, out tmpSurface.textureCoords);
            if (tmpSurface.vertCount == 0)
            {
                surf.CalculateFacetsAndTextureMap(tolerances[bestIndex], out tmpSurface.vertCount, out tmpSurface.facetCount, out  tmpSurface.verts, out tmpSurface.norms, out  tmpSurface.indicies, out tmpSurface.textureCoords);
            }
#else
            surf.GetExistingFacets(tolerance, out tempSurface.vertCount, out tempSurface.facetCount, out tempSurface.verts, out tempSurface.norms, out tempSurface.indicies);
            if (tempSurface.vertCount == 0)
            {
                surf.CalculateFacets(tolerance, out tempSurface.vertCount, out tempSurface.facetCount, out tempSurface.verts, out tempSurface.norms, out tempSurface.indicies);
            }
#endif
            AssetProperties asset = firstAsset;

            if (firstAsset == null)
                asset = AssetProperties.Create(surf);

            AddFacetsInternal(asset);
        }
    }

    private void DumpMeshBuffer()
    {
        if (outputSurface.vertCount == 0 || outputSurface.facetCount == 0)
            return;

        // Copy the output surface's vertices and normals into the new sub-object
        BXDAMesh.BXDASubMesh subObject = new BXDAMesh.BXDASubMesh();
        subObject.verts = new double[outputSurface.vertCount * 3];
        subObject.norms = new double[outputSurface.vertCount * 3];
        Array.Copy(outputSurface.verts, 0, subObject.verts, 0, outputSurface.vertCount * 3);
        Array.Copy(outputSurface.norms, 0, subObject.norms, 0, outputSurface.vertCount * 3);
        subObject.surfaces = new List<BXDAMesh.BXDASurface>(outputMeshSurfaces);

        outputMesh.meshes.Add(subObject);

        outputSurface.vertCount = 0;
        outputSurface.facetCount = 0;
        outputMeshSurfaces = new List<BXDAMesh.BXDASurface>();
    }


    /// <summary>
    /// Moves the mesh currently in the temporary mesh buffer into the mesh structure itself, 
    /// with material information from the asset properties.
    /// </summary>
    /// <param name="assetProps">Material information to use</param>
    private void AddFacetsInternal(AssetProperties assetProps)
    {
        if (tempSurface.vertCount > MAX_VERTICES)
        {
            // This is just bad.  It could be fixed by exporting it per-face instead of with a single block.
            System.Windows.Forms.MessageBox.Show("Warning: Mesh segment exceeded " + MAX_VERTICES + " verticies.  Strange things may begin to happen.");
        }
        // If adding this would cause the sub mesh to overflow dump what currently exists.
        if (tempSurface.vertCount + outputSurface.vertCount >= MAX_VERTICES)
        {
            DumpMeshBuffer();
        }

        BXDAMesh.BXDASurface newMeshSurface = new BXDAMesh.BXDASurface();

        #region Apply asset properties to mesh
        newMeshSurface.hasColor = true;
        newMeshSurface.color = 0xFFFFFFFF;
        newMeshSurface.transparency = 0.0f;
        newMeshSurface.translucency = 0.0f;
        newMeshSurface.specular = 0.5f;

        if (assetProps != null)
        {
            if (assetProps.color != 0)
            {
                newMeshSurface.hasColor = true;
                // Create hex color from RGB components
                newMeshSurface.color = assetProps.color;
            }
            else
            {
                //nextSurface.hasColor = false;
                newMeshSurface.color = 0xFFFFFFFF;
            }

            newMeshSurface.transparency = (float)assetProps.transparency;
            newMeshSurface.translucency = (float)assetProps.translucency;
            newMeshSurface.specular = (float)assetProps.specular;
        }
        #endregion

        // Add temp surface information to the output surface
        // Add vertices and normals to the current output surface
        Array.Copy(tempSurface.verts, 0, outputSurface.verts, outputSurface.vertCount * 3, tempSurface.vertCount * 3);
        Array.Copy(tempSurface.norms, 0, outputSurface.norms, outputSurface.vertCount * 3, tempSurface.vertCount * 3);
#if USE_TEXTURES
        Array.Copy(tmpSurface.textureCoords, 0, postSurface.textureCoords, postSurface.vertCount * 2, tmpSurface.vertCount * 2);
#endif
        
        // Raw copy the indicies for now, then fix the offset in a background thread.
        newMeshSurface.indicies = new int[tempSurface.facetCount * 3];
        Array.Copy(tempSurface.indicies, newMeshSurface.indicies, newMeshSurface.indicies.Length);
        outputSurface.facetCount += tempSurface.facetCount;
        for (int i = 0; i < tempSurface.facetCount * 3; i++)
        {
            newMeshSurface.indicies[i] = newMeshSurface.indicies[i] + outputSurface.vertCount - 1;
        }
        outputSurface.vertCount += tempSurface.vertCount;

        outputMeshSurfaces.Add(newMeshSurface);
    }

    /// <summary>
    /// Copies mesh information from the Inventor API to the temporary mesh buffer, then into the mesh structure.
    /// </summary>
    /// <param name="surf">The source mesh</param>
    /// <param name="tolerance">The chord tolerance for the mesh</param>
    private void AddFacets(Face face, double tolerance)
    {
        tempSurface.vertCount = 0;

#if USE_TEXTURES
        surf.GetExistingFacetsAndTextureMap(tolerances[bestIndex], out tmpSurface.vertCount, out tmpSurface.facetCount, out tmpSurface.verts, out  tmpSurface.norms, out  tmpSurface.indicies, out tmpSurface.textureCoords);
        if (tmpSurface.vertCount == 0)
        {
            surf.CalculateFacetsAndTextureMap(tolerances[bestIndex], out tmpSurface.vertCount, out tmpSurface.facetCount, out  tmpSurface.verts, out tmpSurface.norms, out  tmpSurface.indicies, out tmpSurface.textureCoords);
        }
#else
        face.GetExistingFacets(tolerance, out tempSurface.vertCount, out tempSurface.facetCount, out tempSurface.verts, out  tempSurface.norms, out tempSurface.indicies);
        if (tempSurface.vertCount == 0)
        {
            face.CalculateFacets(tolerance, out tempSurface.vertCount, out tempSurface.facetCount, out  tempSurface.verts, out tempSurface.norms, out tempSurface.indicies);
        }
#endif
        
        AssetProperties assetProps = AssetProperties.Create(face);
        AddFacetsInternal(assetProps);
    }
}
