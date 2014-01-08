using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(LightProbes))]
class ProbePolisher : Editor
{
    bool CheckPolishable (LightProbes probes)
    {
        return (probes.count == 10) && (Vector3.Distance (probes.positions [0], Vector3.up * 20000.0f) < 0.1f);
    }

    override public void OnInspectorGUI ()
    {
        if (CheckPolishable (target as LightProbes))
        {
            EditorGUILayout.HelpBox("This is a polishable LightProbes asset.", MessageType.None);
            ShowPolisherGUI (target as LightProbes);
        }
        else
        {
            EditorGUILayout.HelpBox("This is not a polishable LightProbes asset.", MessageType.None);
            base.OnInspectorGUI();
        }
    }

    void ResetPolisherIfNeeded(LightProbes probes)
    {
        var coeffs = probes.coefficients;
        if (coeffs [27] > -9.0f)
        {
            coeffs [27] = -10.0f;
            coeffs [28] = 1.0f;     // Base Intensity
            coeffs [29] = 0.0f;     // Sky Intensity
            coeffs [30] = 0.9f;     // Sky Color 1
            coeffs [31] = 1.0f;
            coeffs [32] = 1.0f;
            coeffs [33] = 1.0f;     // Sky Color 2
            coeffs [34] = 1.0f;
            coeffs [35] = 0.9f;
            probes.coefficients = coeffs;
        }
    }

    void ShowPolisherGUI (LightProbes probes)
    {
        ResetPolisherIfNeeded (probes);

        var coeffs = probes.coefficients;
        
        var baseIntensity = coeffs [28];
        var skyIntensity = coeffs [29];
        var skyColor1 = new Color (coeffs [30], coeffs [31], coeffs [32]);
        var skyColor2 = new Color (coeffs [33], coeffs [34], coeffs [35]);
        
        baseIntensity = EditorGUILayout.Slider ("Base Intensity", baseIntensity, 0.0f, 10.0f);
        skyIntensity = EditorGUILayout.Slider ("Sky Intensity", skyIntensity, 0.0f, 10.0f);
        skyColor1 = EditorGUILayout.ColorField ("Sky Color Top", skyColor1);
        skyColor2 = EditorGUILayout.ColorField ("Sky Color Bottom", skyColor2);
        
        coeffs [28] = baseIntensity;
        coeffs [29] = skyIntensity;
        coeffs [30] = skyColor1.r;
        coeffs [31] = skyColor1.g;
        coeffs [32] = skyColor1.b;
        coeffs [33] = skyColor2.r;
        coeffs [34] = skyColor2.g;
        coeffs [35] = skyColor2.b;
        
        var midColor = Color.Lerp (skyColor1, skyColor2, 0.5f);
        
        var newCoeffs = new float[27];
        for (var i = 0; i < 27; i++)
            newCoeffs [i] = coeffs [i] * baseIntensity;
        
        newCoeffs [0] += midColor.r * skyIntensity;
        newCoeffs [1] += midColor.g * skyIntensity;
        newCoeffs [2] += midColor.b * skyIntensity;
        
        newCoeffs [3] += (skyColor2.r - midColor.r) * skyIntensity;
        newCoeffs [4] += (skyColor2.g - midColor.g) * skyIntensity;
        newCoeffs [5] += (skyColor2.b - midColor.b) * skyIntensity;
        
        for (var i = 27 * 2; i < coeffs.Length; i += 27)
            System.Array.Copy (newCoeffs, 0, coeffs, i, 27);
        
        probes.coefficients = coeffs;
    }
}

class ProbeBakingJig
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
