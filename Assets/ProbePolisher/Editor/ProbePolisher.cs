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

        // Show the controls.
        baseIntensity = EditorGUILayout.Slider ("Base Intensity", baseIntensity, 0.0f, 10.0f);
        skyIntensity = EditorGUILayout.Slider ("Sky Intensity", skyIntensity, 0.0f, 10.0f);
        skyColor1 = EditorGUILayout.ColorField ("Sky Color Top", skyColor1);
        skyColor2 = EditorGUILayout.ColorField ("Sky Color Bottom", skyColor2);
        yaw = EditorGUILayout.Slider ("Rotation (Y-Axis)", yaw, -180.0f, 180.0f);

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

        // Update the coefficients with the first probe.
        var newCoeffs = RotateSHCoeffs (coeffs, yaw);
        for (var i = 0; i < 27; i++)
            newCoeffs [i] *= baseIntensity;

        // Add the skylight to the coefficients.
        var midColor = Color.Lerp (skyColor1, skyColor2, 0.5f);

        newCoeffs [0] += midColor.r * skyIntensity;
        newCoeffs [1] += midColor.g * skyIntensity;
        newCoeffs [2] += midColor.b * skyIntensity;
        
        newCoeffs [3] += (skyColor2.r - midColor.r) * skyIntensity;
        newCoeffs [4] += (skyColor2.g - midColor.g) * skyIntensity;
        newCoeffs [5] += (skyColor2.b - midColor.b) * skyIntensity;

        // Copy the coefficients to the all probes.
        for (var i = 27 * 2; i < coeffs.Length; i += 27)
            System.Array.Copy (newCoeffs, 0, coeffs, i, 27);

        // Update the asset.
        probes.coefficients = coeffs;

        // Skybox generator button.
        EditorGUILayout.Space ();
        if (GUILayout.Button ("Make/Update Skybox"))
            MakeSkybox (AssetDatabase.GetAssetPath (target), newCoeffs);
    }

    #endregion

    #region Private Functions

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
            coeffs [36] = 0.0f;     // Yaw
            probes.coefficients = coeffs;
        }
    }

    // Make or update a skybox material with a polishable probe.
    void MakeSkybox (string probePath, float[] coeffs)
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

        // Rotate the SH coefficients and set it to the material.
        float c0 = 0.28209479177f;  //       1  / ( 2 * sqrt(pi))
        float c1 = 0.32573500793f;  // sqrt( 3) / ( 3 * sqrt(pi))
        float c2 = 0.27313710764f;  // sqrt(15) / ( 8 * sqrt(pi))
        float c3 = 0.07884789131f;  // sqrt( 5) / (16 * sqrt(pi))
        float c4 = 0.13656855382f;  // c2 / 2

        var vc = new Vector4[3];
        
        for (var i = 0; i < 3; i++)
        {
            vc [i].x = -c1 * coeffs [i + 9];
            vc [i].y = -c1 * coeffs [i + 3];
            vc [i].z = +c1 * coeffs [i + 6];
            vc [i].w = +c0 * coeffs [i] - c3 * coeffs [i + 18];
        }
        
        material.SetVector ("_SHAr", vc [0]);
        material.SetVector ("_SHAg", vc [1]);
        material.SetVector ("_SHAb", vc [2]);
        
        for (var i = 0; i < 3; i++)
        {
            vc [i].x = +c2 * coeffs [i + 12];
            vc [i].y = -c2 * coeffs [i + 15];
            vc [i].z = +c3 * coeffs [i + 18] * 3;
            vc [i].w = -c2 * coeffs [i + 21];
        }
        
        material.SetVector ("_SHBr", vc [0]);
        material.SetVector ("_SHBg", vc [1]);
        material.SetVector ("_SHBb", vc [2]);

        material.SetVector ("_SHC", new Vector4 (coeffs [24], coeffs [25], coeffs [26], 0) * c4);
    }

    // Rotate SH coefficients around the y-axis.
    float[] RotateSHCoeffs (float[] coeffs, float yaw)
    {
        yaw = yaw * Mathf.PI / 180;

        // Constants and variables.
        var rt3d2 = 0.86602540378f; // sqrt(3)/2
        var sn = Mathf.Sin (yaw);
        var cs = Mathf.Cos (yaw);
        var sn2 = Mathf.Sin (yaw * 2);
        var cs2 = Mathf.Cos (yaw * 2);

        // Copy to a Vector3 array.
        var temp = new Vector3[9];
        var ci = 0;
        for (var i = 0; i < 9; i++)
            temp [i] = new Vector3 (coeffs [ci++], coeffs [ci++], coeffs [ci++]);

        // Apply X-90 matrix.
        var temp2 = new Vector3[9];
        temp2 [0] = temp [0];
        temp2 [1] = temp [2];
        temp2 [2] = -temp [1];
        temp2 [3] = temp [3];
        temp2 [4] = temp [7];
        temp2 [5] = -temp [5];
        temp2 [6] = -0.5f * temp [6] - rt3d2 * temp [8];
        temp2 [7] = -temp [4];
        temp2 [8] = -rt3d2 * temp [6] + 0.5f * temp [8];

        // Apply Za matrix.
        temp [0] = temp2 [0];
        temp [1] = cs * temp2 [1] + sn * temp2 [3];
        temp [2] = temp2 [2];
        temp [3] = -sn * temp2 [1] + cs * temp2 [3];
        temp [4] = cs2 * temp2 [4] + sn2 * temp2 [8];
        temp [5] = cs * temp2 [5] + sn * temp2 [7];
        temp [6] = temp2 [6];
        temp [7] = -sn * temp2 [5] + cs * temp2 [7];
        temp [8] = -sn2 * temp2 [4] + cs2 * temp2 [8];

        // Apply X90 matrix.
        temp2 [0] = temp [0];
        temp2 [1] = -temp [2];
        temp2 [2] = temp [1];
        temp2 [3] = temp [3];
        temp2 [4] = -temp [7];
        temp2 [5] = -temp [5];
        temp2 [6] = -0.5f * temp [6] - rt3d2 * temp [8];
        temp2 [7] = temp [4];
        temp2 [8] = -rt3d2 * temp [6] + 0.5f * temp [8];

        // Rearrange the array.
        var newCoeffs = new float[27];
        for (var i = 0; i < 9; i++)
        {
            newCoeffs [i * 3 + 0] = temp2 [i].x;
            newCoeffs [i * 3 + 1] = temp2 [i].y;
            newCoeffs [i * 3 + 2] = temp2 [i].z;
        }

        return newCoeffs;
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
