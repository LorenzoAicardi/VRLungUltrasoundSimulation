# VRLungUltrasoundSimulation
This repository contains the relevant code, material and thesis relative to my thesis project developed at Politecnico di Milano: an Immersive Virtual Reality Simulator for Lung Ultrasound.

## The project

The project involves the creation of an online virtual environment in which students can practice together their skills in lung sonography, by doing exercises based on the BLUE protocol.

Below you can see a video of me simulating lung sonography using three different probes on a patient that presents both A-Lines and B-Lines artifacts:

https://github.com/user-attachments/assets/12430595-6f58-4d07-9cb6-3a0fd19f14a2

## Technical features

The project was developed for the Meta Quest 3 using C#, Unity and the HLSL Shader language, using the Oculus XR Plugin. 

After my graduation, I kept working on the project. The main modification involved using Compute Shaders to optimize portions of the rendering pipeline, which drastically improved performance.

## Software architecture

Here you can find the UML Diagram describing the architetcure of the software.

<img width="1221" height="1401" alt="thesiscomponentdiagram drawio(2)(1)" src="https://github.com/user-attachments/assets/ef4b805f-8efe-46c9-b55f-f1a22d38c17d" />

The three main components are the Room component, the Simulation Logic component and the Excercise component:

* The Room Component manages the simulation lifecycle and online functionalities;
* The Simulation Logic Component handles ultrasound image simulation
through probe interaction;
* The Exercise Component administers training scenarios and assessment.

## Results

This work has been positively evaluated by medicine students from Università di Pavia, who especially appreciated the realism of both images and the experience. The thesis has been evaluated a perfect 7/7 by the commission.
