//
// ProbePolisher - Light Probe Editor Plugin for Unity
//
// Copyright (C) 2014 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using UnityEngine;
using UnityEditor;
using System.Collections;

// Custom editor for light probes.
[CustomEditor(typeof(LightProbes))]
class ProbePolisher : Editor
{
    #region GUI Interface

    // Inspector GUI function.
    override public void OnInspectorGUI ()
    {
        if (CheckPolishable (target as LightProbes))
        {
            EditorGUILayout.HelpBox ("This is a polishable LightProbes asset.", MessageType.None);
            ShowPolisherGUI (target as LightProbes);
        }
        else
        {
            EditorGUILayout.HelpBox ("This is not a polishable LightProbes asset.", MessageType.None);
            base.OnInspectorGUI ();
        }
    }

    // Inspector GUI function for polishable probes.
    void ShowPolisherGUI (LightProbes probes)
    {
        ResetPolisherIfNeeded (probes);
        
        var coeffs = probes.coefficients;

        // Retrieve the optional information from the second probe.
        var baseIntensity = coeffs [28];
        var skyIntensity = coeffs [29];
        var skyColor1 = new Color (coeffs [30], coeffs [31], coeffs [32]);
        var skyColor2 = new Color (coeffs [33], coeffs [34], coeffs [35]);
        var yaw = coeffs [36];

        EditorGUI.BeginChangeCheck ();

        // Show the controls.
        baseIntensity = EditorGUILayout.Slider ("Base Intensity", baseIntensity, 0.0f, 10.0f);
        skyIntensity = EditorGUILayout.Slider ("Sky Intensity", skyIntensity, 0.0f, 10.0f);
        skyColor1 = EditorGUILayout.ColorField ("Sky Color Top", skyColor1);
        skyColor2 = EditorGUILayout.ColorField ("Sky Color Bottom", skyColor2);
        yaw = EditorGUILayout.Slider ("Rotation (Y-Axis)", yaw, -180.0f, 180.0f);

        if (EditorGUI.EndChangeCheck ())
        {
            // Update the optional information.
            coeffs [28] = baseIntensity;
            coeffs [29] = skyIntensity;
            coeffs [30] = skyColor1.r;
            coeffs [31] = skyColor1.g;
            coeffs [32] = skyColor1.b;
            coeffs [33] = skyColor2.r;
            coeffs [34] = skyColor2.g;
            coeffs [35] = skyColor2.b;
            coeffs [36] = yaw;

            // Retrieve the SH coefficients from the first probe.
            var sh = NewVectorArrayFromCoeffs (coeffs, 0);

            // Apply the intensity value.
            for (var i = 0; i < 9; i++)
                sh [i] *= baseIntensity;

            // Apply y-axis rotation.
            sh = RotateSHCoeffs (sh, yaw);

            // Add the skylight.
            var sky1 = ColorToVector3 (skyColor1) * skyIntensity;
            var sky2 = ColorToVector3 (skyColor2) * skyIntensity;
            sh [0] += (sky1 + sky2) * 0.5f;
            sh [1] += (sky2 - sky1) * 0.5f;

            // Copy the coefficients to the all probes.
            var newCoeffs = NewCoeffsFromVectorArray (sh);
            for (var i = 27 * 2; i < coeffs.Length; i += 27)
                System.Array.Copy (newCoeffs, 0, coeffs, i, 27);

            // Update the asset.
            probes.coefficients = coeffs;
        }

        EditorGUILayout.Space ();

        // Skybox generator button.
        if (GUILayout.Button ("Make/Update Skybox"))
            MakeSkybox (AssetDatabase.GetAssetPath (target), NewVectorArrayFromCoeffs (coeffs, 27 * 2));
    }

    #endregion

    #region Private Functions

    // Converts a color value to a vector with dropping alpha.
    Vector3 ColorToVector3 (Color c)
    {
        return new Vector3 (c.r, c.g, c.b);
    }

    // Converts SH coefficients to a vector array.
    Vector3[] NewVectorArrayFromCoeffs (float[] coeffs, int offset)
    {
        var va = new Vector3[9];
        for (int i1 = 0, i2 = offset; i1 < 9;)
            va [i1++] = new Vector3 (coeffs [i2++], coeffs [i2++], coeffs [i2++]);
        return va;
    }

    // Converts a vector array to SH coefficients.
    float[] NewCoeffsFromVectorArray (Vector3[] va)
    {
        var coeffs = new float[27];
        for (int i1 = 0, i2 = 0; i1 < 27;)
        {
            var v = va [i2++];
            coeffs [i1++] = v.x;
            coeffs [i1++] = v.y;
            coeffs [i1++] = v.z;
        }
        return coeffs;
    }

    // Check if the light probes asset is polishable.
    bool CheckPolishable (LightProbes probes)
    {
        // Check if the probes are placed at strange positions. If true, that's a polishable probe.
        return (probes.count == 10) && (Vector3.Distance (probes.positions [0], Vector3.up * 20000.0f) < 0.1f);
    }

    // Reset the polishable probe (only when it's needed).
    void ResetPolisherIfNeeded (LightProbes probes)
    {
        var coeffs = probes.coefficients;
        if (coeffs [27] > -9.0f)
        {
            coeffs [27] = -10.0f;   // Initialization flag
            coeffs [28] = 1.0f;     // Base Intensity
            coeffs [29] = 0.0f;     // Sky Intensity
            coeffs [30] = 0.9f;     // Sky Color 1
            coeffs [31] = 1.0f;
            coeffs [32] = 1.0f;
            coeffs [33] = 1.0f;     // Sky Color 2
            coeffs [34] = 1.0f;
            coeffs [35] = 0.9f;
            coeffs [36] = 0.0f;     // Y-Axis Rotation
            probes.coefficients = coeffs;
        }
    }

    // Make or update a skybox material with a polishable probe.
    void MakeSkybox (string probePath, Vector3[] coeffs)
    {
        if (!probePath.EndsWith (".asset"))
            return;
        
        // Look for the asset of the material.
        var assetPath = probePath.Substring (0, probePath.Length - 6) + " skybox.mat";
        var material = AssetDatabase.LoadAssetAtPath (assetPath, typeof(Material)) as Material;
        
        // If there is no asset, make a new one.
        if (material == null)
        {
            material = new Material (Shader.Find ("ProbePolisher/SH Skybox"));
            AssetDatabase.CreateAsset (material, assetPath);
        }
        
        // Convert the SH coefficients into the shader parameters.
        float c0 = 0.28209479177f;  //       1  / ( 2 * sqrt(pi))
        float c1 = 0.32573500793f;  // sqrt( 3) / ( 3 * sqrt(pi))
        float c2 = 0.27313710764f;  // sqrt(15) / ( 8 * sqrt(pi))
        float c3 = 0.07884789131f;  // sqrt( 5) / (16 * sqrt(pi))
        float c4 = 0.13656855382f;  // c2 / 2

        var t0 = -c1 * coeffs [3];
        var t1 = -c1 * coeffs [1];
        var t2 = +c1 * coeffs [2];
        var t3 = +c0 * coeffs [0] - c3 * coeffs [6];

        material.SetVector ("_SHAr", new Vector4 (t0.x, t1.x, t2.x, t3.x));
        material.SetVector ("_SHAg", new Vector4 (t0.y, t1.y, t2.y, t3.y));
        material.SetVector ("_SHAb", new Vector4 (t0.z, t1.z, t2.z, t3.z));

        t0 = +c2 * coeffs [4];
        t1 = -c2 * coeffs [5];
        t2 = +c3 * coeffs [6] * 3;
        t3 = -c2 * coeffs [7];

        material.SetVector ("_SHBr", new Vector4 (t0.x, t1.x, t2.x, t3.x));
        material.SetVector ("_SHBg", new Vector4 (t0.y, t1.y, t2.y, t3.y));
        material.SetVector ("_SHBb", new Vector4 (t0.z, t1.z, t2.z, t3.z));

        material.SetVector ("_SHC", coeffs [8] * c4);
    }

    // Rotate SH coefficients around the y-axis.
    Vector3[] RotateSHCoeffs (Vector3[] coeffs, float yawDegree)
    {
        // Constants and variables.
        var yaw = yawDegree * Mathf.PI / 180;
        var sn = Mathf.Sin (yaw);
        var cs = Mathf.Cos (yaw);
        var sn2 = Mathf.Sin (yaw * 2);
        var cs2 = Mathf.Cos (yaw * 2);
        var rt3d2 = 0.86602540378f; // sqrt(3)/2

        // Apply X-90 matrix.
        var temp = new Vector3[9]{
            coeffs [0],
            coeffs [2],
            -coeffs [1],
            coeffs [3],
            coeffs [7],
            -coeffs [5],
            -0.5f * coeffs [6] - rt3d2 * coeffs [8],
            -coeffs [4],
            -rt3d2 * coeffs [6] + 0.5f * coeffs [8]
        };

        // Apply Za matrix.
        var temp2 = new Vector3[9]{
            temp [0],
            cs * temp [1] + sn * temp [3],
            temp [2],
            -sn * temp [1] + cs * temp [3],
            cs2 * temp [4] + sn2 * temp [8],
            cs * temp [5] + sn * temp [7],
            temp [6],
            -sn * temp [5] + cs * temp [7],
            -sn2 * temp [4] + cs2 * temp [8],
        };

        // Apply X90 matrix.
        return new Vector3[9]{
            temp2 [0],
            -temp2 [2],
            temp2 [1],
            temp2 [3],
            -temp2 [7],
            -temp2 [5],
            -0.5f * temp2 [6] - rt3d2 * temp2 [8],
            temp2 [4],
            -rt3d2 * temp2 [6] + 0.5f * temp2 [8]
        };
    }

    #endregion
}

// Cutom tool for baking polishable light probes.
static class ProbeBakingJig
{
    [MenuItem("GameObject/Create Other/Baking Jig")]
    static void CreateBakingJig ()
    {
        var go = GameObject.CreatePrimitive (PrimitiveType.Sphere);
        go.name = "Baking Jig";
        var group = go.AddComponent<LightProbeGroup> ();
        Object.DestroyImmediate (go.GetComponent<SphereCollider> ());
        var positions = new Vector3[]{
            new Vector3 (0, 20000, 0),
            new Vector3 (0, 40000, 0),
            new Vector3 (-10000, -10000, -10000),
            new Vector3 (+10000, -10000, -10000),
            new Vector3 (-10000, +10000, -10000),
            new Vector3 (+10000, +10000, -10000),
            new Vector3 (-10000, -10000, +10000),
            new Vector3 (+10000, -10000, +10000),
            new Vector3 (-10000, +10000, +10000),
            new Vector3 (+10000, +10000, +10000)
        };
        group.probePositions = positions;
        go.isStatic = true;
    }
}
