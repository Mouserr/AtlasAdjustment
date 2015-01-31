# AtlasAdjustment
Unity Editor tool for adjust NGUI atlases.
Can be used on last states of development for minimizing atlases size.
From list of textures creates quad atlas with specified size.
It finds the minimum atlas size that can contains all textures, and then compress it if needed.

## Requires
1.	Unity 4.5 or higher
2.	NGUI 3 or higher
3.	System.Drawing lib from Mono 2.0 referenced to Editor project
