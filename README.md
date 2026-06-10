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

<img width="1072" height="1321" alt="thesiscomponentdiagram drawio(2)" src="https://github.com/user-attachments/assets/f613211d-d27d-404b-9c2c-ca1207264669" />

The three main components are the Room component, the Simulation Logic component and the Excercise component:

* The Room Component manages the simulation lifecycle and online func-
tionalities;
* The Simulation Logic Component handles ultrasound image simulation
through probe interaction;
* The Exercise Component administers training scenarios and assessment.

## Results

This work has been positively evaluated by medicine students from Università di Pavia, who especially appreciated the realism of the images and the realism of the experience. The thesis has been evaluated with a perfect 7/7 by the commission.
