using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System;

namespace Obi
{
    [CustomEditor(typeof(ObiSoftbodyVolumeBlueprint), true)]
    public class ObiSoftbodyVolumeBlueprintEditor : ObiActorBlueprintEditor
    {

        public ObiSoftbodySurfaceBlueprint softbodyBlueprint
        {
            get { return blueprint as ObiSoftbodySurfaceBlueprint; }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            properties.Add(new ObiBlueprintMass(this));
            properties.Add(new ObiBlueprintRadius(this));
            properties.Add(new ObiBlueprintLayer(this));
            properties.Add(new ObiBlueprintColor(this));

            renderModes.Add(new ObiBlueprintRenderModeShapeMatchingConstraints(this));

            tools.Clear();
            tools.Add(new ObiParticleSelectionEditorTool(this));
        }

    }


}
