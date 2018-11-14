using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum Methods {
	power,sph,gravity
};

public class Cloud : MonoBehaviour {

	public Methods methods;
	public Material material;
    public ComputeShader cloudCompute;
	public float particleMass = 1.0f;
	public float particleRestDensity = 1.0f;
	public float particleStiffness = 2000f;
	public float particleViscosity = 3000f;
	public float smoothingLength = 0.5f;
	/**
	 * 		Shader.SetGlobalFloat("particleMass",1f);
		Shader.SetGlobalFloat("particleRadius",0.5f);
		Shader.SetGlobalFloat("particleRestDensity",1.0f);
		Shader.SetGlobalFloat("particleStiffness",2000.0f);
		Shader.SetGlobalFloat("particleViscosity",3000.0f);
		Shader.SetGlobalFloat("smoothingLength",slen);
		Shader.SetGlobalFloat("smoothingLength3",Mathf.Pow(slen,3));
		Shader.SetGlobalFloat("smoothingLength6",Mathf.Pow(slen,6));
		Shader.SetGlobalFloat("time_step",0.005f);
	 * **/

	int csidDensityPressure;
	int csidPressureForce;
	int csidGravity;
	int csidPower;
	int csidIntegrate;
    int nthr = 10;
	int npts = 1024 * 64; // 100 mill is too big!
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

	void Start () {
        npts = 1024 * nthr;
        float rad;
        float phi;
        float theta;
		float maxRad = 100f;
		if (methods == Methods.sph) {
			maxRad = 50.0f;
		}
		compute_buffer = new ComputeBuffer (npts, sizeof(float) * 14, ComputeBufferType.Default);
		csidDensityPressure = cloudCompute.FindKernel ("DensityPressure");
		csidPressureForce = cloudCompute.FindKernel ("PressureForce");
		csidGravity = cloudCompute.FindKernel ("Gravity");
		csidPower = cloudCompute.FindKernel ("Power");
		csidIntegrate = cloudCompute.FindKernel ("Integrate");

		Particle[] cloud = new Particle[npts];
		for (uint i = 0; i < npts; ++i) {
			cloud [i] = new Particle ();
			rad = maxRad *  Random.Range(0f, 0.99f)*Random.Range(0f,0.99f);
            theta = Random.Range(0f, 3.1415926535f* 2f);
            phi = Mathf.Acos( Random.Range(0f, 2f) -1);
            // See https://www.bogotobogo.com/Algorithms/uniform_distribution_sphere.php
            cloud[i].position = new Vector3(rad * Mathf.Cos(phi), rad * Mathf.Sin(theta) * Mathf.Sin(phi), rad * Mathf.Cos(theta) * Mathf.Sin(phi));
			cloud [i].velocity = new Vector3 (0, 0, 0); //Random.Range (-0.01f, 0.01f), Random.Range (-0.01f, 0.01f), Random.Range (-0.01f, 0.01f));
			cloud [i].force = new Vector3 (0,0,0); //(Random.Range (-1f, 1), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
			cloud [i].color = new Vector3(0,1,1);
			cloud [i].density = 100;
			cloud [i].pressure = 0;
		}
			
		Shader.SetGlobalFloat("particleMass",particleMass);
		Shader.SetGlobalFloat("particleRadius",0.5f);
		Shader.SetGlobalFloat("particleRestDensity",particleRestDensity);
		Shader.SetGlobalFloat("particleStiffness",particleStiffness);
		Shader.SetGlobalFloat("particleViscosity",particleViscosity);
		Shader.SetGlobalFloat("smoothingLength",smoothingLength);
		Shader.SetGlobalFloat("smoothingLength3",Mathf.Pow(smoothingLength,3));
		Shader.SetGlobalFloat("smoothingLength6",Mathf.Pow(smoothingLength,6));
		Shader.SetGlobalFloat("time_step",0.0001f);



		compute_buffer.SetData (cloud);
        int cloudID = Shader.PropertyToID("cloud");
		cloudCompute.SetBuffer(csidDensityPressure, cloudID, compute_buffer);
		cloudCompute.SetBuffer(csidPressureForce, cloudID, compute_buffer);
		cloudCompute.SetBuffer(csidGravity, cloudID, compute_buffer);
		cloudCompute.SetBuffer(csidPower, cloudID, compute_buffer);
		cloudCompute.SetBuffer(csidIntegrate, cloudID, compute_buffer);

        Shader.SetGlobalBuffer(cloudID, compute_buffer);
        Graphics.SetRandomWriteTarget(1, compute_buffer,true);
        Shader.SetGlobalInt("_npts", npts);// twice?? but isnt it global??

    }
	
	void OnValidate() {
		Shader.SetGlobalFloat("particleMass",particleMass);
		Shader.SetGlobalFloat("particleRadius",0.5f);
		Shader.SetGlobalFloat("particleRestDensity",particleRestDensity);
		Shader.SetGlobalFloat("particleStiffness",particleStiffness);
		Shader.SetGlobalFloat("particleViscosity",particleViscosity);
		Shader.SetGlobalFloat("smoothingLength",smoothingLength);
		Shader.SetGlobalFloat("smoothingLength3",Mathf.Pow(smoothingLength,3));
		Shader.SetGlobalFloat("smoothingLength6",Mathf.Pow(smoothingLength,6));
	}

	int ping = 0;
	void OnPostRender(){
        material.SetPass(0);
		Graphics.DrawProcedural (MeshTopology.Points, npts, 1);
		if (methods == Methods.sph) {
			//if (ping == 0) {
				cloudCompute.Dispatch (csidDensityPressure, nthr, 1, 1);
			//	ping = 1;
			//} else {
				//	GPUFence fence1 =  Graphics.CreateGPUFence ();
				//	Graphics.WaitOnGPUFence (fence1);
				cloudCompute.Dispatch (csidPressureForce, nthr, 1, 1);
			//	ping = 0;
			//}
			cloudCompute.Dispatch (csidIntegrate, nthr, 1, 1);
		//	GPUFence fence2 = Graphics.CreateGPUFence ();
		//	Graphics.WaitOnGPUFence (fence2);
		} else if (methods == Methods.power) {
			cloudCompute.Dispatch (csidPower, nthr, 1, 1);
			cloudCompute.Dispatch (csidIntegrate, nthr, 1, 1);
		} else if (methods == Methods.gravity) {
			cloudCompute.Dispatch (csidGravity, nthr, 1, 1);
		}

    }

	void OnDestroy(){
		compute_buffer.Release ();
	}
}
