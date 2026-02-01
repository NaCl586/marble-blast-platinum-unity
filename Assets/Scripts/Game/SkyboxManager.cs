using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Skybox
{
    public Material material;
    public float lightIntensity;
}

public class SkyboxManager : MonoBehaviour
{
    public List<Skybox> skyboxes;

    public static SkyboxManager instance;
    public void Awake() => instance = this;

    public void ApplySkybox(string skyName, out float lightIntensity)
    {
        Skybox skyboxMaterial = null;

        foreach (Skybox sky in skyboxes)
            if (sky.material.name == skyName)
                skyboxMaterial = sky;

        if (skyboxMaterial == null)
        {
            lightIntensity = 0.25f;
            return;
        }

        lightIntensity = skyboxMaterial.lightIntensity;
        RenderSettings.skybox = skyboxMaterial.material;
        DynamicGI.UpdateEnvironment();
    }
}
