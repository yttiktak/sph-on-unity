using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

struct Particle
{
	public Vector3 position;//3
	public Vector3 velocity;//6
	public Vector3 force;//9
	public Vector3 color; // 12
	public float density;//13
	public float pressure;//14
	public uint xrank;
	public uint yrank;
	public uint zrank;
}
struct Indexes
{
	public uint i;
}

struct Stats
{
	public int thread_count;
	public float max_density;
	public float min_density;
	// as wanted.. 
}

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
	int csidSort;
	int csidSmoothBall;

    int nthr;
	int ngrps;
    int npts; // 100 mill is too big!
	ComputeBuffer compute_buffer;
	ComputeBuffer index_x_buffer;
	ComputeBuffer reverse_x_index_buffer;
	ComputeBuffer stats_buffer;

	int[] statistics;

    void Start() {
        uint ngx, ngy, ngz; // groups declared in the compute shader
        float rad;
        float phi;
        float theta;
        float maxRad = 100f;
        if ((Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsEditor)) {
            npts = 1024 * 7;
        } else { 
            npts = 1024 * 25;
        }
        if (methods == Methods.sph) {
			maxRad = 10.0f;
		}
		compute_buffer = new ComputeBuffer (npts, sizeof(float) * 17, ComputeBufferType.Default);
		index_x_buffer = new ComputeBuffer (npts, sizeof(uint), ComputeBufferType.Default);
		reverse_x_index_buffer = new ComputeBuffer (npts, sizeof(uint), ComputeBufferType.Default);
		stats_buffer = new ComputeBuffer (1, sizeof(float) * 3, ComputeBufferType.Default);

		csidSPH = cloudCompute.FindKernel ("Sph");
		csidPower = cloudCompute.FindKernel ("Power");
		csidGravity = cloudCompute.FindKernel ("Gravity");
		csidSmoothBall = cloudCompute.FindKernel ("SmoothBall");
		csidSort = cloudCompute.FindKernel ("Sort");

		cloudCompute.GetKernelThreadGroupSizes (csidSPH, out ngx, out ngy,out  ngz); // just trust the same in other kernels
		nthr = npts /(int) ngx;


		Particle[] cloud = new Particle[npts];
		uint[] index_by_x = new uint[npts];
		for (uint i = 0; i < npts; ++i) {
			index_by_x [i] = i;
			cloud [i] = new Particle ();
			if (methods == Methods.power) {
				rad = maxRad;
			} else {
				rad = maxRad * Mathf.Sqrt (UnityEngine.Random.Range (0.010f, 1.0f));
			}
			theta = UnityEngine.Random.Range(0f, 3.1415926535f* 2f);
			phi = Mathf.Acos( UnityEngine.Random.Range(0f, 2f) -1);
            // See https://www.bogotobogo.com/Algorithms/uniform_distribution_sphere.php
            cloud[i].position = new Vector3(rad * Mathf.Cos(phi), rad * Mathf.Sin(theta) * Mathf.Sin(phi), rad * Mathf.Cos(theta) * Mathf.Sin(phi));
			cloud [i].velocity = new Vector3 (0, 0, 0); // 200000.0f *  new Vector3 (Random.Range (-1f, 1f), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
			cloud [i].force = new Vector3 (0,0,0); //(Random.Range (-1f, 1), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
			cloud [i].color = new Vector3(0.0f,0.1f,0.1f);
			cloud [i].density = 100;
			cloud [i].pressure = 0;
		}

	//	Array.Sort (cloud, delegate(Particle p1, Particle p2) {
	//		return p1.position.x.CompareTo (p2.position.x);
	//	});

		statistics = new int[3];
		statistics[0]= 0;
		statistics[1] = 0;
		statistics[2] = 100000;
		stats_buffer.SetData(statistics);

		compute_buffer.SetData (cloud);
		index_x_buffer.SetData (index_by_x);
		reverse_x_index_buffer.SetData (index_by_x);

        int cloudID = Shader.PropertyToID("cloud");
		cloudCompute.SetBuffer(csidSPH, cloudID, compute_buffer);
        Shader.SetGlobalBuffer(cloudID, compute_buffer);
        Graphics.SetRandomWriteTarget(1, compute_buffer,true);

		int indexID = Shader.PropertyToID ("index_by_x");
		cloudCompute.SetBuffer (csidSPH, indexID, index_x_buffer);
		cloudCompute.SetBuffer (csidSort, indexID, index_x_buffer);
		int indexpID = Shader.PropertyToID ("index_by_x_pong");
		cloudCompute.SetBuffer (csidSPH, indexpID, reverse_x_index_buffer);

		int indexStats = Shader.PropertyToID("statistics");
		cloudCompute.SetBuffer(csidSPH, indexStats, stats_buffer);

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
		if (methods == Methods.sph) {
			Shader.SetGlobalFloat ("time_step", 0.00000001f);
		} else if (methods == Methods.power) {
			Shader.SetGlobalFloat ("time_step", 0.001f);
		} else {
			Shader.SetGlobalFloat ("time_step", 0.051f);			
		}
	}
		
	int smoothingCount = 50;
	int thread_count = 10;
	void OnPostRender(){
        material.SetPass(0);
		Graphics.DrawProcedural (MeshTopology.Points, npts, 1);

		if (smoothingCount-- > 0) {
			cloudCompute.Dispatch (csidSmoothBall, nthr, 1, 1);
		} else if (methods == Methods.sph) {
			//thread_count
			cloudCompute.Dispatch (csidSort, nthr, 1, 1);
			statistics[0] = 0;
			stats_buffer.SetData (statistics);
			cloudCompute.SetBuffer(csidSPH,"statistics",stats_buffer); // redundant??
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
		index_x_buffer.Release ();
		reverse_x_index_buffer.Release ();
		stats_buffer.Release ();
	}
}
