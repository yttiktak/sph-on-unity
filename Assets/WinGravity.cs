/***
 * Trying to revert back to working windows version. Reduce back to one compute shader kernel.
 * Seems the problem is a need for a gpufence to be sure compute shader is done before next one fired off.
 * Changed order to material, then cs. Works.. Now what?
 * ***/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class WinGravity : MonoBehaviour
{

    public Material material;
    public ComputeShader powerLawCompute;
    int csIndex;
    int nthr = 8;
    int npts = 64 * 8; 
    ComputeBuffer compute_buffer;

    struct Particle
    {
        public Vector3 position;//3
        public Vector3 velocity;//6
        public Vector3 force;//9
        public Vector3 color; // 12
        public float density;//13
        public float pressure;//14
    }

    void Start()
    {
        npts = 64 * nthr;
        float rad;
        float phi;
        float theta;
        float maxRad = 100f;
        compute_buffer = new ComputeBuffer(npts, sizeof(float) * 14, ComputeBufferType.Default);
        csIndex = powerLawCompute.FindKernel("CSMain");

        Particle[] cloud = new Particle[npts];
        for (uint i = 0; i < npts; ++i)
        {
            cloud[i] = new Particle();
            rad = maxRad * Mathf.Log10(Random.Range(0.1f, 10f));
            theta = Random.Range(0f, 3.1415926535f * 2f);
            phi = Mathf.Acos(Random.Range(0f, 2f) - 1);

            // See https://www.bogotobogo.com/Algorithms/uniform_distribution_sphere.php

            cloud[i].position = new Vector3(rad * Mathf.Cos(phi), rad * Mathf.Sin(theta) * Mathf.Sin(phi), rad * Mathf.Cos(theta) * Mathf.Sin(phi)); //, rad * Mathf.Cos(phi)); 
                                                                                                                                                     // Random.Range (-100f, 100f), Random.Range (-100f, 100f));
            cloud[i].velocity = new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f));
            cloud[i].force = new Vector3(0, 0, 0); //(Random.Range (-1f, 1), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
            cloud[i].color = new Vector3(0, 1, 1);
            cloud[i].density = 100;
            cloud[i].pressure = 0;
        }
        compute_buffer.SetData(cloud);
        int cloudID = Shader.PropertyToID("cloud");
        powerLawCompute.SetBuffer(csIndex, "cloud", compute_buffer);
        Shader.SetGlobalBuffer(cloudID, compute_buffer);
        Graphics.SetRandomWriteTarget(1, compute_buffer, true);
        Shader.SetGlobalInt("_npts", npts);// isnt it global??

    }

    GPUFence fence1;
    void OnPostRender()
    {
        material.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, npts);
        powerLawCompute.Dispatch(csIndex, nthr, 1, 1);
    }

    void OnDestroy()
    {
        compute_buffer.Release();
    }
}

