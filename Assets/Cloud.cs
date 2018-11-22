using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum Methods {
	power,sph,gravity,smooth
};

public class Cloud : MonoBehaviour {

	public Methods methods;
	public Material material;
    public ComputeShader cloudCompute;
	public float particleRadius = 0.5f;
	public float particleMass = 1.0f;
	public float particleRestDensity = 10f;
	public float particleStiffness = 2000f;
	public float particleViscosity = 3000f;
	public float smoothingLength = 40f;

	int csidGravity;
	int csidPower;
	int csidSPH;
	int csidSmoothBall;

    int nthr;
	int ngrps;
	int npts = 512 * 64; // 100 mill is too big!
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
		uint ngx,ngy,ngz; // groups declared in the compute shader
        float rad;
        float phi;
        float theta;
		float maxRad = 100f;
		if (methods == Methods.sph) {
			maxRad = 50.0f;
		}
		compute_buffer = new ComputeBuffer (npts, sizeof(float) * 14, ComputeBufferType.Default);

	//	csidDensityPressure = cloudCompute.FindKernel ("DensityPressure");
	//	csidPressureForce = cloudCompute.FindKernel ("PressureForce");
	//	csidGravity = cloudCompute.FindKernel ("Gravity");
	//	csidPower = cloudCompute.FindKernel ("Power");
	//	csidIntegrate = cloudCompute.FindKernel ("Integrate");

		csidSPH = cloudCompute.FindKernel ("Sph");
		csidPower = cloudCompute.FindKernel ("Power");
		csidGravity = cloudCompute.FindKernel ("Gravity");
		csidSmoothBall = cloudCompute.FindKernel ("SmoothBall");

		cloudCompute.GetKernelThreadGroupSizes (csidSPH, out ngx, out ngy,out  ngz); // just trust the same in other kernels
		nthr = npts /(int) ngx;
		Debug.Log (nthr);
		Particle[] cloud = new Particle[npts];
		for (uint i = 0; i < npts; ++i) {
			cloud [i] = new Particle ();
			if (methods == Methods.power) {
				rad = maxRad;
			} else {
				rad = maxRad * Mathf.Sqrt (Random.Range (0f, 1.0f));
			}
            theta = Random.Range(0f, 3.1415926535f* 2f);
            phi = Mathf.Acos( Random.Range(0f, 2f) -1);
            // See https://www.bogotobogo.com/Algorithms/uniform_distribution_sphere.php
            cloud[i].position = new Vector3(rad * Mathf.Cos(phi), rad * Mathf.Sin(theta) * Mathf.Sin(phi), rad * Mathf.Cos(theta) * Mathf.Sin(phi));
			cloud [i].velocity = new Vector3 (0, 0, 0); // 200000.0f *  new Vector3 (Random.Range (-1f, 1f), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
			cloud [i].force = new Vector3 (0,0,0); //(Random.Range (-1f, 1), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
			cloud [i].color = new Vector3(0,0.5f,1);
			cloud [i].density = 100;
			cloud [i].pressure = 0;
		}
			
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
		if (methods == Methods.sph) {
			Shader.SetGlobalFloat ("time_step", 0.0000001f);
		} else {
			Shader.SetGlobalFloat ("time_step", 0.051f);			
		}



		compute_buffer.SetData (cloud);
        int cloudID = Shader.PropertyToID("cloud");
	//	cloudCompute.SetBuffer(csidDensityPressure, cloudID, compute_buffer);
	//	cloudCompute.SetBuffer(csidPressureForce, cloudID, compute_buffer);
	//	cloudCompute.SetBuffer(csidGravity, cloudID, compute_buffer);
	//	cloudCompute.SetBuffer(csidPower, cloudID, compute_buffer);
		cloudCompute.SetBuffer(csidSPH, cloudID, compute_buffer);

        Shader.SetGlobalBuffer(cloudID, compute_buffer);
        Graphics.SetRandomWriteTarget(1, compute_buffer,true);
        Shader.SetGlobalInt("_npts", npts);

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
	}
		
	int smoothingCount = 100;
	void OnPostRender(){
        material.SetPass(0);
		Graphics.DrawProcedural (MeshTopology.Points, npts, 1);

		if (smoothingCount-- > 0) {
			cloudCompute.Dispatch (csidSmoothBall, nthr, 1, 1);
		} else if (methods == Methods.sph) {
			cloudCompute.Dispatch (csidSPH, nthr, 1, 1);
		} else if (methods == Methods.power) {
			cloudCompute.Dispatch (csidPower, nthr, 1, 1);
		} else if (methods == Methods.gravity) {
			cloudCompute.Dispatch (csidGravity, nthr, 1, 1);
		} else if (methods == Methods.smooth) {
			cloudCompute.Dispatch (csidSmoothBall, nthr, 1, 1);
		}

    }

	void OnDestroy(){
		compute_buffer.Release ();
	}
}
