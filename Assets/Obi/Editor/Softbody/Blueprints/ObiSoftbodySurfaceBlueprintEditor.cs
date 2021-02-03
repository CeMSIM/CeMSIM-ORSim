using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System;

namespace Obi
{
    [CustomEditor(typeof(ObiSoftbodySurfaceBlueprint), true)]
    public class ObiSoftbodySurfaceBlueprintEditor : ObiMeshBasedActorBlueprintEditor
    {

        public override Mesh sourceMesh
        {
            get { return softbodyBlueprint != null ? softbodyBlueprint.generatedMesh : null; }
        }

        public ObiSoftbodySurfaceBlueprint softbodyBlueprint
        {
            get { return blueprint as ObiSoftbodySurfaceBlueprint; }
        }

        protected override bool ValidateBlueprint()
        {
            if (softbodyBlueprint != null && softbodyBlueprint.inputMesh != null)
            {
                if (!softbodyBlueprint.inputMesh.isReadable)
                {
                    NonReadableMeshWarning(softbodyBlueprint.inputMesh);
                    return false;
                }
                return true;
            }
            return false;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            properties.Add(new ObiBlueprintMass(this));
            properties.Add(new ObiBlueprintRadius(this));
            properties.Add(new ObiBlueprintLayer(this));
            properties.Add(new ObiBlueprintColor(this));

            renderModes.Add(new ObiBlueprintRenderModeMesh(this));
            renderModes.Add(new ObiBlueprintRenderModeShapeMatchingConstraints(this));

            tools.Clear();
            tools.Add(new ObiParticleSelectionEditorTool(this));
            tools.Add(new ObiPaintBrushEditorTool(this));
            tools.Add(new ObiPropertyTextureEditorTool(this));
        }

        public override int VertexToParticle(int vertexIndex)
        {
            return softbodyBlueprint.vertexToParticle[vertexIndex];
        }
    }


}
