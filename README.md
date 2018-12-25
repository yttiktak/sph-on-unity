# sph-on-unity
Getting Smoothed Particle Hydrodynamics running on computeShaders in Unity3d

Runs on Unity2018 on Ubuntu (Bionic) and on Unity2018 Windows

Trying some Ideas for sorting. Like, keep a table of neighbors on each particle.

but, but.. that adds N*Nneighbors^2 integer compare ops to reduce N^2 distance measurements to N^Nneighbor distance measurements.

At N = 10k and Nn = 100, add 10^4*(10^4 integer compare), remove 10^4*((10^4-10^2) distance).

Nibbling at it. Buffer of size N*100 didnt crash. N apx 10000. Assigning neighbores each and every loop from distance test of all N didnt kill frame rate too much.
Using assigned neighbors for the force compute loop didnt crash. Even when ignoring neighbors more than 99. 
Merry Christmas!




