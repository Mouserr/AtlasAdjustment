# AtlasAdjustment
Unity Editor tool for adjusting NGUI atlases. 

It can be used for minimizing atlases size on the last stages of the development. 
It creates quad atlas with specified size using a list of textures.
It finds the minimum atlas size that can contains all textures, and compress it when requested

## Requires
1.	Unity 4.5 or higher
2.	NGUI 3 or higher
3.	System.Drawing lib from Mono 2.0 referenced to Editor project
