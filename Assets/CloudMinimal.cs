using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

struct ParticlePosition
{
	public Vector3 position;
}

struct ParticlePhysics
{
	public Vector3 velocity;//3
	public Vector3 force;//6
	public float density;//7
	public float pressure;//8
}

struct ParticleData
{
	public Vector3 color;
}
		

public class CloudMinimal : MonoBehaviour {

	public Material material;
    public ComputeShader minimalCloudCompute;
	public ComputeShader legacyCloudCompute;
	public float particleRadius = 0.5f;
	public float particleMass = 1.0f;
	public float particleRestDensity = 10f;
	public float particleStiffness = 2000f;
	public float particleViscosity = 3000f;
	public float smoothingLength = 40f;

	public Vector3 point1k;


	int csidSPH;
	int csidSmoothBall;

    int nthr;
	int ngrps;
    int npts; 
	public static ComputeBuffer positionBuffer; //3 floats
	ComputeBuffer physicsBuffer;  //8 floats
	ComputeBuffer dataBuffer;	  //3 floats
	ComputeBuffer stats_buffer;

	int[] statistics;

    void Start() {
        uint ngx, ngy, ngz; // groups declared in the compute shader
        float rad;
        float phi;
        float theta;
        float maxRad = 100f;
		npts = 1024 * 25;
		maxRad = 10.0f;
	
		positionBuffer = new ComputeBuffer (npts, sizeof(float) * 3, ComputeBufferType.Default);
		physicsBuffer = new ComputeBuffer(npts,sizeof(float)*8, ComputeBufferType.Default);
		dataBuffer = new ComputeBuffer(npts,sizeof(float)*3, ComputeBufferType.Default);
		stats_buffer = new ComputeBuffer (1, sizeof(float) * 3, ComputeBufferType.Default);

		csidSPH = minimalCloudCompute.FindKernel ("Sph");
		csidSmoothBall = legacyCloudCompute.FindKernel ("SmoothBall");

		minimalCloudCompute.GetKernelThreadGroupSizes (csidSPH, out ngx, out ngy,out  ngz); // just trust the same in other kernels
		nthr = npts /(int) ngx;


		ParticlePosition[] cloudPosition = new ParticlePosition[npts];
		ParticlePhysics[] cloudPhysics = new ParticlePhysics[npts];
		ParticleData[] cloudData = new ParticleData[npts];

		for (uint i = 0; i < npts; ++i) {
			cloudPosition [i] = new ParticlePosition ();
			rad = maxRad * Mathf.Sqrt (UnityEngine.Random.Range (0.010f, 1.0f));
			theta = UnityEngine.Random.Range(0f, 3.1415926535f* 2f);
			phi = Mathf.Acos( UnityEngine.Random.Range(0f, 2f) -1);  // See https://www.bogotobogo.com/Algorithms/uniform_distribution_sphere.php
			cloudPosition[i].position = new Vector3(rad * Mathf.Cos(phi), rad * Mathf.Sin(theta) * Mathf.Sin(phi), rad * Mathf.Cos(theta) * Mathf.Sin(phi));
			cloudPhysics [i].velocity = new Vector3 (0, 0, 0); // 200000.0f *  new Vector3 (Random.Range (-1f, 1f), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
			cloudPhysics [i].force = new Vector3 (0,0,0); //(Random.Range (-1f, 1), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
			cloudData [i].color = new Vector3(0.0f,0.1f,0.1f);
			cloudPhysics [i].density = 100;
			cloudPhysics [i].pressure = 0;
		}
		cloudPhysics [0].density = 90; // to see if it floats 
		cloudPhysics [9].density = 110; // and if it sinks!

		statistics = new int[3];
		statistics[0]= 0;
		statistics[1] = 0;
		statistics[2] = 100000;
		stats_buffer.SetData(statistics);

		positionBuffer.SetData (cloudPosition);
		physicsBuffer.SetData (cloudPhysics);
		dataBuffer.SetData (cloudData);

		int cloudID = Shader.PropertyToID("cloudPosition");
		minimalCloudCompute.SetBuffer(csidSPH, cloudID, positionBuffer);
		Shader.SetGlobalBuffer(cloudID, positionBuffer);
		Graphics.SetRandomWriteTarget(1, positionBuffer,true);

		int physID = Shader.PropertyToID("cloudPhysics");
		minimalCloudCompute.SetBuffer(csidSPH, physID, physicsBuffer);
		Shader.SetGlobalBuffer(cloudID, physicsBuffer);

		int dataID = Shader.PropertyToID("cloudData");
		minimalCloudCompute.SetBuffer(csidSPH, dataID, dataBuffer);
		Shader.SetGlobalBuffer(cloudID, dataBuffer);

		int indexStats = Shader.PropertyToID("statistics");
		minimalCloudCompute.SetBuffer(csidSPH, indexStats, stats_buffer);

		OnValidate (); // set global shader values

    }
	
	void OnValidate() {
		Shader.SetGlobalFloat("particleMass",particleMass);
		Shader.SetGlobalFloat("particleRadius",particleRadius);
		Shader.SetGlobalFloat("particleRestDensity",particleRestDensity);
		Shader.SetGlobalFloat("particleStiffness",particleStiffness);
		Shader.SetGlobalFloat("particleViscosity",particleViscosity);
		Shader.SetGlobalFloat("smoothingLength",smoothingLength);
		Shader.SetGlobalFloat("smoothingLength2",Mathf.Pow(smoothingLength,2));
		Shader.SetGlobalFloat("smoothingLength3",Mathf.Pow(smoothingLength,3));
		Shader.SetGlobalFloat("smoothingLength6",Mathf.Pow(smoothingLength,6));
		Shader.SetGlobalFloat ("smoothingLength9", Mathf.Pow (smoothingLength, 9));

		Shader.SetGlobalFloat ("time_step", 0.00000001f);

	}
		
	int smoothingCount = 50;
	int thread_count = 10;
	void OnPostRender(){
        material.SetPass(0);
		Graphics.DrawProcedural (MeshTopology.Points, npts, 1); // does not speed up it not drawn!

		statistics[0] = 0;
		stats_buffer.SetData (statistics);
		minimalCloudCompute.Dispatch (csidSPH, nthr, 1, 1);

    }

	void OnDestroy(){
		positionBuffer.Release ();
		physicsBuffer.Release ();
		dataBuffer.Release();
		stats_buffer.Release ();
	}
}
